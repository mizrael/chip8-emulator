using Chip8Emulator.Core.Utils;
using System;

namespace Chip8Emulator.Core;

public record Screen
{
    private readonly bool[,] _data;

    public Screen()
    {
        _data = new bool[Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT];
    }

    public int Length => _data.Length;

    public bool this[int i, int j]
    {
        get => _data[i, j];
        set => _data[i, j] = value;
    }

    public void Reset()
    {
        Array.Clear(_data);
    }
}
