using Chip8Emulator.Core;
using System;

namespace Chip8Emulator.BlazorWasm;

public class LambdaRenderer : IDisplay
{
    private readonly Action<VideoBuffer> _renderer;

    public LambdaRenderer(Action<VideoBuffer> renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public void Refresh(VideoBuffer screen)
        => _renderer(screen);
}
