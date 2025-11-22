namespace Chip8Emulator.Core;

public record Interfaces
{
    public Interfaces(IDisplay renderer, ISoundPlayer soundPlayer)
    {
        Display = renderer;
        Audio = soundPlayer;
    }

    public ISoundPlayer Audio { get; }
    public IDisplay Display { get; }

    public Input Input { get; } = new();
}