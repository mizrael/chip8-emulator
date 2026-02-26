namespace Chip8Emulator.Core;

public record Interfaces
{
    public Interfaces(
        IDisplay renderer,
        ISoundPlayer soundPlayer,
        IInput input)
    {
        Display = renderer ?? throw new System.ArgumentNullException(nameof(renderer));
        Audio = soundPlayer ?? throw new System.ArgumentNullException(nameof(soundPlayer));
        Input = input ?? throw new System.ArgumentNullException(nameof(input));
    }

    public ISoundPlayer Audio { get; }
    public IDisplay Display { get; }
    public IInput Input { get; } 
}