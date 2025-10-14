namespace Chip8Emulator.Core;

public interface IRenderer
{
    void Draw(bool[,] data);
}