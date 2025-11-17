using System;

namespace Chip8Emulator.Core;

public record Buffers
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
