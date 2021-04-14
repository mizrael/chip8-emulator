namespace Chip8Emulator.Console
{
    public interface IRenderer
    {
        void Render(bool[,] screen);
    }
}