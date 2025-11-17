using System;

namespace Chip8Emulator.Core;

public record Mem
{
    public byte[] Memory { get; } = new byte[0x1000];
    public bool[,] Screen { get; } = new bool[Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT];

    public void LoadRom(ReadOnlySpan<byte> romData)
    {
        Reset();

        romData.CopyTo(this.Memory.AsSpan(Constants.ROM_START_LOCATION));
    }

    public void LoadRom(System.IO.Stream romData)
    {
        Reset();

        int romSize = (int)romData.Length;

        var dest = this.Memory.AsSpan(Constants.ROM_START_LOCATION);
        if (romData.Read(dest) < 1)
            throw new ArgumentException("input stream is invalid");
    }

    public void Reset()
    {
        Array.Clear(this.Screen);

        Array.Clear(this.Memory);
        for (var i = 0; i != Font.Characters.Length; ++i)
            this.Memory[i] = Font.Characters[i];
    }
}

public record Registers
{
    public byte[] V { get; } = new byte[16];
    public ushort I { get; set; }
    public ushort PC { get; set; } = Constants.ROM_START_LOCATION;
    public byte SP { get; set; }
    public ushort[] Stack { get; } = new ushort[16];

    public OpCode GetCurrentOp(Mem memory)
    {
        ushort data = (ushort)(memory.Memory[this.PC++] << 8 | memory.Memory[this.PC++]);
        var opCode = new OpCode(data);
        return opCode;
    }


    public void Reset()
    {
        Array.Clear(this.V);
        Array.Clear(this.Stack);

        this.PC = Constants.ROM_START_LOCATION;
        this.I = 0;
        this.SP = 0;
    }
}
