using System;
using System.Collections.Generic;

namespace Chip8Emulator.Core;

public class Cpu
{
    #region members

    private readonly Dictionary<byte, Action<State, OpCode>> _instructions = new();
    private readonly Dictionary<byte, Action<State, OpCode>> _miscInstructions = new();

    private readonly IRenderer _renderer;
    private readonly ISoundPlayer _soundPlayer;
    private readonly Input _input;
    private readonly Clock _clock;

    #endregion members

    public Cpu(IRenderer renderer, ISoundPlayer soundPlayer, Input input, Clock clock)
    {
        _instructions[0x0] = this.ZeroOps;
        _instructions[0x1] = this.Jump;
        _instructions[0x2] = this.Call;
        _instructions[0x3] = this.SkipVxEqNN;
        _instructions[0x4] = this.SkipVxNeqNN;
        _instructions[0x6] = this.SetVReg;
        _instructions[0x7] = this.AddVReg;
        _instructions[0x8] = this.XYOps;
        _instructions[0x9] = this.SkipVxNeqVy;
        _instructions[0xA] = this.SetI;
        _instructions[0xC] = this.Rand;
        _instructions[0xD] = this.Draw;
        _instructions[0xE] = this.SkipOnKey;
        _instructions[0xF] = this.Misc;

        _miscInstructions[0x1E] = this.AddVRegToI;
        _miscInstructions[0x65] = this.FillVFromMI;
        _miscInstructions[0x15] = this.SetDelay;
        _miscInstructions[0x07] = this.GetDelay;
        _miscInstructions[0x0A] = this.WaitKey;
        _miscInstructions[0x33] = this.SetBCD;
        _miscInstructions[0x29] = this.SetIToCharSprite;
        _miscInstructions[0x18] = this.PlaySound;

        _renderer = renderer;
        _soundPlayer = soundPlayer;
        _input = input;
        _clock = clock;
    }

    private void ExecuteCurrentInstruction(State state)
    {
        var opCode = state.GetCurrentOp();

        if (!_instructions.TryGetValue(opCode.Set, out var instruction))
            throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");

        instruction(state, opCode);
    }

    public void Update(
        State state,
        double elapsedSeconds,
        int targetInstructionsPerSecond)
    {
        var onTick = () => this.ExecuteCurrentInstruction(state);

        _clock.Update(onTick, elapsedSeconds, targetInstructionsPerSecond);
    }

    #region instructions

    // 0xCXNN
    private void Rand(State state, OpCode opCode)
    {
        state.Registers.V[opCode.X] = (byte)(Random.Shared.Next(0, 255) & opCode.NN);
    }

    // 0x1NNN
    private void Jump(State state, OpCode opCode)
    {
        state.Registers.PC = opCode.NNN;
    }

    // 0x6XNN
    private void SetVReg(State state, OpCode opCode)
    {
        state.Registers.V[opCode.X] = opCode.NN;
    }

    // 0x7XNN
    private void AddVReg(State state, OpCode opCode)
    {
        int result = state.Registers.V[opCode.X] + opCode.NN;
        bool carry = result > 255;
        if (carry)
            result = result - 256;

        state.Registers.V[opCode.X] = (byte)(result & 0x00FF);
    }

    // 0xANNN
    private void SetI(State state, OpCode opCode)
    {
        state.Registers.I = opCode.NNN;
    }

    /// <summary>
    /// Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels 
    /// and a height of N+1 pixels. Each row of 8 pixels is read as 
    /// bit-coded starting from memory location I; 
    /// I value doesn’t change after the execution of this instruction. 
    /// https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
    /// Sprite pixels are XOR'd with corresponding screen pixels. 
    /// In other words, sprite pixels that are set flip the color of the 
    /// corresponding screen pixel, while unset sprite pixels do nothing. 
    /// The carry flag (VF) is set to 1 if any screen pixels are flipped 
    /// from set to unset when a sprite is drawn and set to 0 otherwise. 
    /// </summary>
    /// <param name="opCode"></param>
    private void Draw(State state, OpCode opCode)
    {
        var startX = state.Registers.V[opCode.X];
        var startY = state.Registers.V[opCode.Y];
        var rows = opCode.N;
        byte carry = 0;
        bool updateRenderer = false;

        for (byte row = 0; row < rows; row++)
        {
            var rowData = state.Memory[state.Registers.I + row];
            var py = (startY + row) % Constants.SCREEN_HEIGHT;

            for (byte col = 0; col != 8; col++)
            {
                var px = (startX + col) % Constants.SCREEN_WIDTH;
                var oldPixel = (byte)(state.Screen[px, py] ? 1 : 0);
                var spritePixel = (byte)((rowData >> (7 - col)) & 1);

                if (oldPixel != spritePixel)
                    updateRenderer = true;

                var newPixel = (byte)(oldPixel ^ spritePixel);
                state.Screen[px, py] = (newPixel != 0);

                if (oldPixel == 1 && spritePixel == 1)
                    carry = 1;
            }
        }

        state.Registers.V[0xF] = carry;

        if (updateRenderer)
            _renderer.Draw(state.Screen);
    }

    private void Misc(State state, OpCode opCode)
    {
        if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
            throw new NotImplementedException($"misc instruction '0xF{opCode.NN:X}' not implemented");

        instruction(state, opCode);
    }

    // 0x3XNN
    private void SkipVxEqNN(State state, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] == opCode.NN)
            state.Registers.PC += 2;
    }

    // 0x4XNN
    private void SkipVxNeqNN(State state, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] != opCode.NN)
            state.Registers.PC += 2;
    }

    // 0x9XY0
    private void SkipVxNeqVy(State state, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] != state.Registers.V[opCode.Y])
            state.Registers.PC += 2;
    }

    // 0x2NNN
    private void Call(State state, OpCode opCode)
    {
        Push(state.Registers, state.Registers.PC);
        state.Registers.PC = opCode.NNN;
    }

    private void Push(Registers registers, ushort value)
    => registers.Stack[registers.SP++] = value;

    private ushort Pop(Registers registers)
    => registers.Stack[--registers.SP];

    private void ZeroOps(State state, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0xE0:
                state.Screen.Reset();
                break;
            case 0xEE:
                state.Registers.PC = Pop(state.Registers);
                break;
            default:
                throw new NotImplementedException($"instruction '0x0{opCode.NN:X}' not implemented");
        }
    }

    private void XYOps(State state, OpCode opCode)
    {
        switch (opCode.N)
        {
            case 0x0:
                state.Registers.V[opCode.X] = state.Registers.V[opCode.Y];
                break;
            case 0x1:
                state.Registers.V[opCode.X] |= state.Registers.V[opCode.Y];
                break;
            case 0x2:
                state.Registers.V[opCode.X] &= state.Registers.V[opCode.Y];
                break;
            case 0x3:
                state.Registers.V[opCode.X] ^= state.Registers.V[opCode.Y];
                break;
            case 0x4:
                var res = state.Registers.V[opCode.X] + state.Registers.V[opCode.Y];
                var carry = res > 255;
                state.Registers.V[opCode.X] = (byte)res;
                state.Registers.V[0xF] = (byte)(carry ? 1 : 0);
                break;
            case 0x5:
                state.Registers.V[0xF] = (byte)(state.Registers.V[opCode.X] > state.Registers.V[opCode.Y] ? 1 : 0);
                state.Registers.V[opCode.X] -= state.Registers.V[opCode.Y];
                break;
            case 0x6:
                state.Registers.V[0xF] = (byte)((state.Registers.V[opCode.X] & 0x1) == 1 ? 1 : 0);
                state.Registers.V[opCode.X] >>= 1;
                break;
            case 0x7:
                state.Registers.V[0xF] = (byte)(state.Registers.V[opCode.Y] > state.Registers.V[opCode.X] ? 1 : 0);
                state.Registers.V[opCode.X] = (byte)(state.Registers.V[opCode.Y] - state.Registers.V[opCode.X]);
                break;
            default:
                throw new NotImplementedException($"instruction '0x8XY{opCode.N:X}' not implemented");
        }
    }

    // 0xE
    private void SkipOnKey(State state, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0x9E:
                state.Registers.PC += (ushort)(_input.IsKeyPressed((Keys)state.Registers.V[opCode.X]) ? 2 : 0);
                break;
            case 0xA1:
                state.Registers.PC += (ushort)(!_input.IsKeyPressed((Keys)state.Registers.V[opCode.X]) ? 2 : 0);
                break;
        }
    }

    #endregion instructions

    #region misc instructions

    //0xFX1E
    private void AddVRegToI(State state, OpCode opCode)
    {
        state.Registers.I += state.Registers.V[opCode.X];
    }

    //0xFX65
    private void FillVFromMI(State state, OpCode opCode)
    {
        for (byte i = 0; i <= opCode.X; ++i)
            state.Registers.V[i] = state.Memory[state.Registers.I + i];
    }

    private void SetDelay(State state, OpCode opCode)
    {
        _clock.Delay = state.Registers.V[opCode.X];
    }

    //0xFX07
    private void GetDelay(State state, OpCode opCode)
    {
        state.Registers.V[opCode.X] = _clock.Delay;
    }

    //0xFX0A
    private void WaitKey(State state, OpCode opCode)
    {
        if (!_input.IsAnyKeyPressed())
        {
            state.Registers.PC -= 2;
            return;
        }

        state.Registers.V[opCode.X] = (byte)_input.GetMostRecentKeyPressed()!;
    }

    //0xFX33
    private void SetBCD(State state, OpCode opCode)
    {
        var vx = state.Registers.V[opCode.X];
        state.Memory[state.Registers.I] = (byte)(vx / 100);
        state.Memory[state.Registers.I + 1] = (byte)((vx / 10) % 10);
        state.Memory[state.Registers.I + 2] = (byte)(vx % 10);
    }

    //0xFX29
    private void SetIToCharSprite(State state, OpCode opCode)
    {
        state.Registers.I = (ushort)(state.Registers.V[opCode.X] * 5);
    }

    // 0xFX18
    private void PlaySound(State state, OpCode opCode)
    {
        var duration = state.Registers.V[opCode.X];
        _soundPlayer.Beep(duration);
    }

    #endregion misc instructions
}
