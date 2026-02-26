namespace Chip8Emulator.Core
{
    public interface IInput
    {
        Keys? GetMostRecentKeyPressed();
        bool IsAnyKeyPressed();
        bool IsKeyPressed(Keys key);
        void SetKeyDown(Keys key);
        void SetKeyUp(Keys key);
    }
}