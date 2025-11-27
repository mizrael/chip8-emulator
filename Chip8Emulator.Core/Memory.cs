using System;

namespace Chip8Emulator.Core;

public record Memory
{
    private readonly byte[] _data = new byte[0x1000];
   
    public int Length => _data.Length;

    public byte this[int index]
    {
        get => _data[index];
        set => _data[index] = value;
    }

    internal void Reset()
    {
        Array.Clear(_data);
        for (var i = 0; i != Constants.Fonts.Length; ++i)
            _data[i] = Constants.Fonts[i];
    }

    internal void LoadRom(System.IO.Stream romData)
    {
        Reset();

        int romSize = (int)romData.Length;

        var dest = _data.AsSpan(Constants.ROM_START_LOCATION);
        if (romData.Read(dest) < 1)
            throw new ArgumentException("input stream is invalid");
    }
}