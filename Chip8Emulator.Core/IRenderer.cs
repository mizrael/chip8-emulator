namespace Chip8Emulator.Core
{
    public interface IRenderer
    {
        void Update(bool[,] screen);
        void Render();
    }
}