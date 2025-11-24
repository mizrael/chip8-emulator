using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Chip8Emulator.Core;

public static class Instructions
{
    private readonly static Dictionary<byte, Action<State, Interfaces, OpCode>> _instructions = new();
    private readonly static Dictionary<byte, Action<State, Interfaces, OpCode>> _miscInstructions = new();

    static Instructions()
    {
        _instructions[0x0] = ZeroOps;
        _instructions[0x1] = Jump;
        _instructions[0x2] = Call;
        _instructions[0x3] = SkipVxEqNN;
        _instructions[0x4] = SkipVxNeqNN;
        _instructions[0x6] = SetVReg;
        _instructions[0x7] = AddVReg;
        _instructions[0x8] = XYOps;
        _instructions[0x9] = SkipVxNeqVy;
        _instructions[0xA] = SetI;
        _instructions[0xC] = Rand;
        _instructions[0xD] = Draw;
        _instructions[0xE] = SkipOnKey;
        _instructions[0xF] = Misc;

        _miscInstructions[0x1E] = AddVRegToI;
        _miscInstructions[0x65] = FillVFromMI;
        _miscInstructions[0x15] = SetDelay;
        _miscInstructions[0x07] = GetDelay;
        _miscInstructions[0x0A] = WaitKey;
        _miscInstructions[0x33] = SetBCD;
        _miscInstructions[0x29] = SetIToCharSprite;
        _miscInstructions[0x18] = PlaySound;
    }

    public static bool TryGet(OpCode opCode, [NotNullWhen(true)] out Action<State, Interfaces, OpCode>? instruction)
    => _instructions.TryGetValue(opCode.Set, out instruction);

    #region instructions

    // 0xCXNN
    public static void Rand(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.V[opCode.X] = (byte)(Random.Shared.Next(0, 255) & opCode.NN);
    }

    // 0x1NNN
    public static void Jump(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.PC = opCode.NNN;
    }

    // 0x6XNN
    public static void SetVReg(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.V[opCode.X] = opCode.NN;
    }

    // 0x7XNN
    public static void AddVReg(State state, Interfaces interfaces, OpCode opCode)
    {
        int result = state.Registers.V[opCode.X] + opCode.NN;
        bool carry = result > 255;
        if (carry)
            result = result - 256;

        state.Registers.V[opCode.X] = (byte)(result & 0x00FF);
    }

    // 0xANNN
    public static void SetI(State state, Interfaces interfaces, OpCode opCode)
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
    public static void Draw(State state, Interfaces interfaces, OpCode opCode)
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
                var oldPixel = (byte)(state.VideoBuffer[px, py] ? 1 : 0);
                var spritePixel = (byte)((rowData >> (7 - col)) & 1);

                if (oldPixel != spritePixel)
                    updateRenderer = true;

                var newPixel = (byte)(oldPixel ^ spritePixel);
                state.VideoBuffer[px, py] = (newPixel != 0);

                if (oldPixel == 1 && spritePixel == 1)
                    carry = 1;
            }
        }

        state.Registers.V[0xF] = carry;

        if (updateRenderer)
            interfaces.Display.Refresh(state.VideoBuffer);
    }

    public static void Misc(State state, Interfaces interfaces, OpCode opCode)
    {
        if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
            throw new NotImplementedException($"misc instruction '0xF{opCode.NN:X}' not implemented");

        instruction(state, interfaces, opCode);
    }

    // 0x3XNN
    public static void SkipVxEqNN(State state, Interfaces interfaces, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] == opCode.NN)
            state.Registers.PC += 2;
    }

    // 0x4XNN
    public static void SkipVxNeqNN(State state, Interfaces interfaces, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] != opCode.NN)
            state.Registers.PC += 2;
    }

    // 0x9XY0
    public static void SkipVxNeqVy(State state, Interfaces interfaces, OpCode opCode)
    {
        if (state.Registers.V[opCode.X] != state.Registers.V[opCode.Y])
            state.Registers.PC += 2;
    }

    // 0x2NNN
    public static void Call(State state, Interfaces interfaces, OpCode opCode)
    {
        Push(state.Registers, state.Registers.PC);
        state.Registers.PC = opCode.NNN;
    }

    public static void Push(Registers registers, ushort value)
   => registers.Stack[registers.SP++] = value;

    private static ushort Pop(Registers registers)
    => registers.Stack[--registers.SP];

    public static void ZeroOps(State state, Interfaces interfaces, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0xE0:
                state.VideoBuffer.Reset();
                break;
            case 0xEE:
                state.Registers.PC = Pop(state.Registers);
                break;
            default:
                throw new NotImplementedException($"instruction '0x0{opCode.NN:X}' not implemented");
        }
    }

    public static void XYOps(State state, Interfaces interfaces, OpCode opCode)
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
    public static void SkipOnKey(State state, Interfaces interfaces, OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0x9E:
                state.Registers.PC += (ushort)(interfaces.Input.IsKeyPressed((Keys)state.Registers.V[opCode.X]) ? 2 : 0);
                break;
            case 0xA1:
                state.Registers.PC += (ushort)(!interfaces.Input.IsKeyPressed((Keys)state.Registers.V[opCode.X]) ? 2 : 0);
                break;
        }
    }

    #endregion instructions

    #region misc instructions

    //0xFX1E
    public static void AddVRegToI(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.I += state.Registers.V[opCode.X];
    }

    //0xFX65
    public static void FillVFromMI(State state, Interfaces interfaces, OpCode opCode)
    {
        for (byte i = 0; i <= opCode.X; ++i)
            state.Registers.V[i] = state.Memory[state.Registers.I + i];
    }

    public static void SetDelay(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Clock.Delay = state.Registers.V[opCode.X];
    }

    //0xFX07
    public static void GetDelay(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.V[opCode.X] = state.Clock.Delay;
    }

    //0xFX0A
    public static void WaitKey(State state, Interfaces interfaces, OpCode opCode)
    {
        if (!interfaces.Input.IsAnyKeyPressed())
        {
            state.Registers.PC -= 2;
            return;
        }

        state.Registers.V[opCode.X] = (byte)interfaces.Input.GetMostRecentKeyPressed()!;
    }

    //0xFX33
    public static void SetBCD(State state, Interfaces interfaces, OpCode opCode)
    {
        var vx = state.Registers.V[opCode.X];
        state.Memory[state.Registers.I] = (byte)(vx / 100);
        state.Memory[state.Registers.I + 1] = (byte)((vx / 10) % 10);
        state.Memory[state.Registers.I + 2] = (byte)(vx % 10);
    }

    //0xFX29
    public static void SetIToCharSprite(State state, Interfaces interfaces, OpCode opCode)
    {
        state.Registers.I = (ushort)(state.Registers.V[opCode.X] * 5);
    }

    // 0xFX18
    public static void PlaySound(State state, Interfaces interfaces, OpCode opCode)
    {
        var duration = state.Registers.V[opCode.X];
        interfaces.Audio.Beep(duration);
    }

    #endregion misc instructions
}