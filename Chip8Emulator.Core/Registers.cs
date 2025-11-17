using System;

namespace Chip8Emulator.Core;

public record Registers
{
    public byte[] V { get; } = new byte[16];
    public ushort I { get; set; }
    public ushort PC { get; set; } = Constants.ROM_START_LOCATION;
    public byte SP { get; set; }
    public ushort[] Stack { get; } = new ushort[16];

    public OpCode GetCurrentOp(Buffers memory)
    {
        ushort data = (ushort)(memory.Memory[this.PC++] << 8 | memory.Memory[this.PC++]);
        var opCode = new OpCode(data);
        return opCode;
    }

    public void Reset()
    {
        Array.Clear(this.V);
        Array.Clear(this.Stack);

        this.PC = Constants.ROM_START_LOCATION;
        this.I = 0;
        this.SP = 0;
    }
}
