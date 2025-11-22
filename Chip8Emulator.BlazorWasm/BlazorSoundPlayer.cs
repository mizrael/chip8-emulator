using Chip8Emulator.Core;
using Microsoft.JSInterop;

namespace Chip8Emulator.BlazorWasm;

public class BlazorSoundPlayer : ISoundPlayer
{
    private readonly IJSRuntime _js;
    public BlazorSoundPlayer(IJSRuntime js) => _js = js;

    public void Beep(int milliseconds)
    {
        if (milliseconds <= 0) return;

        _ = _js.InvokeVoidAsync("chip8Sound.beep", 500, milliseconds);
    }
}