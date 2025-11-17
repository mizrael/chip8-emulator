using Chip8Emulator.Core;
using System;

namespace Chip8Emulator.BlazorWasm;

public class LambdaRenderer : IRenderer
{
    private readonly Action<bool[,]> _renderer;

    public LambdaRenderer(Action<bool[,]> renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public void Draw(bool[,] data)
        => _renderer(data);
}
