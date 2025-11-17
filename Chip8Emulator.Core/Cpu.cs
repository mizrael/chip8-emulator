using System;
using System.Collections.Generic;

namespace Chip8Emulator.Core;

public class Cpu
{
    #region members

    private readonly Dictionary<byte, Action<Registers, Buffers, OpCode>> _instructions = new();
    private readonly Dictionary<byte, Action<Registers, Buffers, OpCode>> _miscInstructions = new();

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

    private void ExecuteCurrentInstruction(Registers registers, Buffers memory)
    {
        var opCode = registers.GetCurrentOp(memory);

        if (!_instructions.TryGetValue(opCode.Set, out var instruction))
            throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");

        instruction(registers, memory, opCode);
    }

    public void Update(
        Registers registers,
        Buffers memory,
        double elapsedSeconds,
        int targetInstructionsPerSecond)
    {
        var onTick = () => this.ExecuteCurrentInstruction(registers, memory);

        _clock.Update(onTick, elapsedSeconds, targetInstructionsPerSecond);
    }

    #region instructions

    // 0xCXNN
    private void Rand(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.V[opCode.X] = (byte)(Random.Shared.Next(0, 255) & opCode.NN);
    }

    // 0x1NNN
    private void Jump(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.PC = opCode.NNN;
    }

    // 0x6XNN
    private void SetVReg(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.V[opCode.X] = opCode.NN;
    }

    // 0x7XNN
    private void AddVReg(Registers registers, Buffers memory, OpCode opCode)
    {
        int result = registers.V[opCode.X] + opCode.NN;
        bool carry = result > 255;
        if (carry)
            result = result - 256;

        registers.V[opCode.X] = (byte)(result & 0x00FF);
    }

    // 0xANNN
    private void SetI(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.I = opCode.NNN;
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
    private void Draw(Registers registers, Buffers memory, OpCode opCode)
    {
        var startX = registers.V[opCode.X];
        var startY = registers.V[opCode.Y];
        var rows = opCode.N;
        byte carry = 0;
        bool updateRenderer = false;

        for (byte row = 0; row < rows; row++)
        {
            var rowData = memory.Memory[registers.I + row];
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

        registers.V[0xF] = carry;

        if (updateRenderer)
            _renderer.Draw(memory.Screen);
    }

    private void Misc(Registers registers, Buffers memory, OpCode opCode)
    {
        if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
            throw new NotImplementedException($"misc instruction '0xF{opCode.NN:X}' not implemented");

        instruction(registers, memory, opCode);
    }

    // 0x3XNN
    private void SkipVxEqNN(Registers registers, Buffers memory, OpCode opCode)
    {
        if (registers.V[opCode.X] == opCode.NN)
            registers.PC += 2;
    }

    // 0x4XNN
    private void SkipVxNeqNN(Registers registers, Buffers memory, OpCode opCode)
    {
        if (registers.V[opCode.X] != opCode.NN)
            registers.PC += 2;
    }

    // 0x9XY0
    private void SkipVxNeqVy(Registers registers, Buffers memory, OpCode opCode)
    {
        if (registers.V[opCode.X] != registers.V[opCode.Y])
            registers.PC += 2;
    }

    // 0x2NNN
    private void Call(Registers registers, Buffers memory, OpCode opCode)
    {
        Push(registers, registers.PC);
        registers.PC = opCode.NNN;
    }

    private void Push(Registers registers, ushort value)
    => registers.Stack[registers.SP++] = value;

    private ushort Pop(Registers registers)
    => registers.Stack[--registers.SP];

    private void ZeroOps(Registers registers, Buffers memory, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0xE0:
                Array.Clear(memory.Screen, 0, memory.Screen.Length);
                break;
            case 0xEE:
                registers.PC = Pop(registers);
                break;
            default:
                throw new NotImplementedException($"instruction '0x0{opCode.NN:X}' not implemented");
        }
    }

    private void XYOps(Registers registers, Buffers memory, OpCode opCode)
    {
        switch (opCode.N)
        {
            case 0x0:
                registers.V[opCode.X] = registers.V[opCode.Y];
                break;
            case 0x1:
                registers.V[opCode.X] |= registers.V[opCode.Y];
                break;
            case 0x2:
                registers.V[opCode.X] &= registers.V[opCode.Y];
                break;
            case 0x3:
                registers.V[opCode.X] ^= registers.V[opCode.Y];
                break;
            case 0x4:
                var res = registers.V[opCode.X] + registers.V[opCode.Y];
                var carry = res > 255;
                registers.V[opCode.X] = (byte)res;
                registers.V[0xF] = (byte)(carry ? 1 : 0);
                break;
            case 0x5:
                registers.V[0xF] = (byte)(registers.V[opCode.X] > registers.V[opCode.Y] ? 1 : 0);
                registers.V[opCode.X] -= registers.V[opCode.Y];
                break;
            case 0x6:
                registers.V[0xF] = (byte)((registers.V[opCode.X] & 0x1) == 1 ? 1 : 0);
                registers.V[opCode.X] >>= 1;
                break;
            case 0x7:
                registers.V[0xF] = (byte)(registers.V[opCode.Y] > registers.V[opCode.X] ? 1 : 0);
                registers.V[opCode.X] = (byte)(registers.V[opCode.Y] - registers.V[opCode.X]);
                break;
            default:
                throw new NotImplementedException($"instruction '0x8XY{opCode.N:X}' not implemented");
        }
    }

    // 0xE
    private void SkipOnKey(Registers registers, Buffers memory, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0x9E:
                registers.PC += (ushort)(_input.IsKeyPressed((Keys)registers.V[opCode.X]) ? 2 : 0);
                break;
            case 0xA1:
                registers.PC += (ushort)(!_input.IsKeyPressed((Keys)registers.V[opCode.X]) ? 2 : 0);
                break;
        }
    }

    #endregion instructions

    #region misc instructions

    //0xFX1E
    private void AddVRegToI(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.I += registers.V[opCode.X];
    }

    //0xFX65
    private void FillVFromMI(Registers registers, Buffers memory, OpCode opCode)
    {
        for (byte i = 0; i <= opCode.X; ++i)
            registers.V[i] = memory.Memory[registers.I + i];
    }

    private void SetDelay(Registers registers, Buffers memory, OpCode opCode)
    {
        _clock.Delay = registers.V[opCode.X];
    }

    //0xFX07
    private void GetDelay(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.V[opCode.X] = _clock.Delay;
    }

    //0xFX0A
    private void WaitKey(Registers registers, Buffers memory, OpCode opCode)
    {
        if (!_input.IsAnyKeyPressed())
        {
            registers.PC -= 2;
            return;
        }

        registers.V[opCode.X] = (byte)_input.GetMostRecentKeyPressed()!;
    }

    //0xFX33
    private void SetBCD(Registers registers, Buffers memory, OpCode opCode)
    {
        var vx = registers.V[opCode.X];
        memory.Memory[registers.I] = (byte)(vx / 100);
        memory.Memory[registers.I + 1] = (byte)((vx / 10) % 10);
        memory.Memory[registers.I + 2] = (byte)(vx % 10);
    }

    //0xFX29
    private void SetIToCharSprite(Registers registers, Buffers memory, OpCode opCode)
    {
        registers.I = (ushort)(registers.V[opCode.X] * 5);
    }

    // 0xFX18
    private void PlaySound(Registers registers, Buffers memory, OpCode opCode)
    {
        var duration = registers.V[opCode.X];
        _soundPlayer.Beep(duration);
    }

    #endregion misc instructions
}
