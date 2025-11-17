using System;
using System.Collections.Generic;
using System.Linq;

namespace Chip8Emulator.Core;

public class Cpu
{
    #region members

    private double _timerAccumulator = 0.0;
    private double _instructionAccumulator = 0.0;
    private byte _delay = 0;

    private readonly HashSet<byte> _pressedKeys = new();

    private readonly Dictionary<byte, Action<Registers, Mem, OpCode>> _instructions = new();
    private readonly Dictionary<byte, Action<Registers, Mem, OpCode>> _miscInstructions = new();

    private readonly IRenderer _renderer;
    private readonly ISoundPlayer _soundPlayer;

    #endregion members

    public Cpu(IRenderer renderer, ISoundPlayer soundPlayer)
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
    }

    private void ExecuteCurrentInstruction(Registers registers, Mem memory)
    {
        var opCode = registers.GetCurrentOp(memory);

        if (!_instructions.TryGetValue(opCode.Set, out var instruction))
            throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");

        instruction(registers, memory, opCode);
    }

    private void UpdateTimers(double elapsedSeconds, double targetFrameInterval)
    {
        _timerAccumulator += elapsedSeconds;

        while (_timerAccumulator >= targetFrameInterval)
        {
            if (_delay > 0)
                _delay--;
            _timerAccumulator -= targetFrameInterval;
        }
    }

    public void Update(
        Registers registers,
        Mem memory,
        double elapsedSeconds,
        int targetInstructionsPerSecond,
        double targetFrameInterval)
    {
        if (targetInstructionsPerSecond < 1)
            targetInstructionsPerSecond = 1;

        _instructionAccumulator += elapsedSeconds;
        double instructionInterval = 1.0 / targetInstructionsPerSecond;

        while (_instructionAccumulator >= instructionInterval)
        {
            ExecuteCurrentInstruction(registers, memory);
            _instructionAccumulator -= instructionInterval;
        }

        UpdateTimers(elapsedSeconds, targetFrameInterval);
    }

    public void SetKeyDown(Keys key)
        => _pressedKeys.Add((byte)key);

    public void SetKeyUp(Keys key)
        => _pressedKeys.Remove((byte)key);

   
    #region instructions

    // 0xCXNN
    private void Rand(Registers state, Mem memory, OpCode opCode)
    {
        state.V[opCode.X] = (byte)(Random.Shared.Next(0, 255) & opCode.NN);
    }

    // 0x1NNN
    private void Jump(Registers state, Mem memory, OpCode opCode)
    {
        state.PC = opCode.NNN;
    }

    // 0x6XNN
    private void SetVReg(Registers state, Mem memory, OpCode opCode)
    {
        state.V[opCode.X] = opCode.NN;
    }

    // 0x7XNN
    private void AddVReg(Registers state, Mem memory, OpCode opCode)
    {
        int result = state.V[opCode.X] + opCode.NN;
        bool carry = result > 255;
        if (carry)
            result = result - 256;

        state.V[opCode.X] = (byte)(result & 0x00FF);
    }

    // 0xANNN
    private void SetI(Registers state, Mem memory, OpCode opCode)
    {
        state.I = opCode.NNN;
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
    private void Draw(Registers state, Mem memory, OpCode opCode)
    {
        var startX = state.V[opCode.X];
        var startY = state.V[opCode.Y];
        var rows = opCode.N;
        byte carry = 0;
        bool updateRenderer = false;

        for (byte row = 0; row < rows; row++)
        {
            var rowData = memory.Memory[state.I + row];
            var py = (startY + row) % Constants.SCREEN_HEIGHT;

            for (byte col = 0; col != 8; col++)
            {
                var px = (startX + col) % Constants.SCREEN_WIDTH;
                var oldPixel = (byte)(memory.Screen[px, py] ? 1 : 0);
                var spritePixel = (byte)((rowData >> (7 - col)) & 1);

                if (oldPixel != spritePixel)
                    updateRenderer = true;

                var newPixel = (byte)(oldPixel ^ spritePixel);
                memory.Screen[px, py] = (newPixel != 0);

                if (oldPixel == 1 && spritePixel == 1)
                    carry = 1;
            }
        }

        state.V[0xF] = carry;

        if (updateRenderer)
            _renderer.Draw(memory.Screen);
    }

    private void Misc(Registers state, Mem memory, OpCode opCode)
    {
        if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
            throw new NotImplementedException($"misc instruction '0xF{opCode.NN:X}' not implemented");

        instruction(state, memory, opCode);
    }

    // 0x3XNN
    private void SkipVxEqNN(Registers state, Mem memory, OpCode opCode)
    {
        if (state.V[opCode.X] == opCode.NN)
            state.PC += 2;
    }

    // 0x4XNN
    private void SkipVxNeqNN(Registers state, Mem memory, OpCode opCode)
    {
        if (state.V[opCode.X] != opCode.NN)
            state.PC += 2;
    }

    // 0x9XY0
    private void SkipVxNeqVy(Registers state, Mem memory, OpCode opCode)
    {
        if (state.V[opCode.X] != state.V[opCode.Y])
            state.PC += 2;
    }

    // 0x2NNN
    private void Call(Registers state, Mem memory, OpCode opCode)
    {
        Push(state, state.PC);
        state.PC = opCode.NNN;
    }

    private void Push(Registers state, ushort value)
    => state.Stack[state.SP++] = value;

    private ushort Pop(Registers state)
    => state.Stack[--state.SP];

    private void ZeroOps(Registers state, Mem memory, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0xE0:
                Array.Clear(memory.Screen, 0, memory.Screen.Length);
                break;
            case 0xEE:
                state.PC = Pop(state);
                break;
            default:
                throw new NotImplementedException($"instruction '0x0{opCode.NN:X}' not implemented");
        }
    }

    private void XYOps(Registers state, Mem memory, OpCode opCode)
    {
        switch (opCode.N)
        {
            case 0x0:
                state.V[opCode.X] = state.V[opCode.Y];
                break;
            case 0x1:
                state.V[opCode.X] |= state.V[opCode.Y];
                break;
            case 0x2:
                state.V[opCode.X] &= state.V[opCode.Y];
                break;
            case 0x3:
                state.V[opCode.X] ^= state.V[opCode.Y];
                break;
            case 0x4:
                var res = state.V[opCode.X] + state.V[opCode.Y];
                var carry = res > 255;
                state.V[opCode.X] = (byte)res;
                state.V[0xF] = (byte)(carry ? 1 : 0);
                break;
            case 0x5:
                state.V[0xF] = (byte)(state.V[opCode.X] > state.V[opCode.Y] ? 1 : 0);
                state.V[opCode.X] -= state.V[opCode.Y];
                break;
            case 0x6:
                state.V[0xF] = (byte)((state.V[opCode.X] & 0x1) == 1 ? 1 : 0);
                state.V[opCode.X] >>= 1;
                break;
            case 0x7:
                state.V[0xF] = (byte)(state.V[opCode.Y] > state.V[opCode.X] ? 1 : 0);
                state.V[opCode.X] = (byte)(state.V[opCode.Y] - state.V[opCode.X]);
                break;
            default:
                throw new NotImplementedException($"instruction '0x8XY{opCode.N:X}' not implemented");
        }
    }

    // 0xE
    private void SkipOnKey(Registers state, Mem memory, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0x9E:
                state.PC += (ushort)(_pressedKeys.Contains(state.V[opCode.X]) ? 2 : 0);
                break;
            case 0xA1:
                state.PC += (ushort)(!_pressedKeys.Contains(state.V[opCode.X]) ? 2 : 0);
                break;
        }
    }

    #endregion instructions

    #region misc instructions

    //0xFX1E
    private void AddVRegToI(Registers state, Mem memory, OpCode opCode)
    {
        state.I += state.V[opCode.X];
    }

    //0xFX65
    private void FillVFromMI(Registers state, Mem memory, OpCode opCode)
    {
        for (byte i = 0; i <= opCode.X; ++i)
            state.V[i] = memory.Memory[state.I + i];
    }

    private void SetDelay(Registers state, Mem memory, OpCode opCode)
    {
        _delay = state.V[opCode.X];
    }

    //0xFX07
    private void GetDelay(Registers state, Mem memory, OpCode opCode)
    {
        state.V[opCode.X] = _delay;
    }

    //0xFX0A
    private void WaitKey(Registers state, Mem memory, OpCode opCode)
    {
        if (_pressedKeys.Count == 0)
        {
            state.PC -= 2;
            return;
        }

        state.V[opCode.X] = _pressedKeys.First();
    }

    //0xFX33
    private void SetBCD(Registers state, Mem memory, OpCode opCode)
    {
        var vx = state.V[opCode.X];
        memory.Memory[state.I] = (byte)(vx / 100);
        memory.Memory[state.I + 1] = (byte)((vx / 10) % 10);
        memory.Memory[state.I + 2] = (byte)(vx % 10);
    }

    //0xFX29
    private void SetIToCharSprite(Registers state, Mem memory, OpCode opCode)
    {
        state.I = (ushort)(state.V[opCode.X] * 5);
    }

    // 0xFX18
    private void PlaySound(Registers state, Mem memory, OpCode opCode)
    {
        var duration = state.V[opCode.X];
        _soundPlayer.Beep(duration);
    }

    #endregion misc instructions
}