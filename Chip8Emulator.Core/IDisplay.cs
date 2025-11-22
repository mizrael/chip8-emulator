namespace Chip8Emulator.Core;

public interface IDisplay
{
    void Refresh(VideoBuffer screen);
}