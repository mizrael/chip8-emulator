using System;
using System.Threading.Tasks;

namespace Chip8Emulator.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cpu = new Cpu();

            var romPath = "roms/Space Invaders [David Winter].ch8";
            using (var romData = System.IO.File.OpenRead(romPath))
            {
                await cpu.LoadAsync(romData);
            }

            var hertz = 60;
            var clock = TimeSpan.FromSeconds(1f / hertz);

            while (true)
            {
                cpu.Tick();
                await Task.Delay(clock);
            }                

            System.Console.ReadKey();
        }
    }
}
