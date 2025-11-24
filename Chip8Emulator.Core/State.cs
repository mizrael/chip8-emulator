using System.IO;

namespace Chip8Emulator.Core;

public record State
{
    public Registers Registers { get; } = new();
    public Memory Memory { get; } = new();
    public VideoBuffer VideoBuffer { get; } = new();

    public Clock Clock { get; } = new();

    public OpCode GetCurrentOp()
    {
        ushort data = (ushort)(this.Memory[this.Registers.PC++] << 8 | this.Memory[this.Registers.PC++]);
        var opCode = new OpCode(data);
        return opCode;
    }

    public void LoadRom(FileStream romData)
    {
        this.Clock.Reset();
        this.VideoBuffer.Reset();
        this.Registers.Reset();
        this.Memory.LoadRom(romData);

    }
}
