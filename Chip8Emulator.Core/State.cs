namespace Chip8Emulator.Core;

public record State
{
    public Registers Registers { get; } = new();
    public Memory Memory { get; } = new();
    public Screen Screen { get; } = new();


    public OpCode GetCurrentOp()
    {
        ushort data = (ushort)(this.Memory[this.Registers.PC++] << 8 | this.Memory[this.Registers.PC++]);
        var opCode = new OpCode(data);
        return opCode;
    }
}