namespace Chip8Emulator.Core
{
    public class DefaultSoundPlayer : ISoundPlayer
    {
        public void Beep(int milliseconds)
        {
            //TODO: a better implementation
            System.Console.Beep(500, milliseconds);
        }
    }
}
