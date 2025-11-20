using Chip8Emulator.Core;
using System;

namespace Chip8Emulator.BlazorWasm;

public class LambdaRenderer : IRenderer
{
    private readonly Action<Screen> _renderer;

    public LambdaRenderer(Action<Screen> renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public void Draw(Screen screen)
        => _renderer(screen);
}
