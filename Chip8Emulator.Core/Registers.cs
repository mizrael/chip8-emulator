using System;

namespace Chip8Emulator.Core;

public record Registers
{
    private const int AddressMask = 0x0FFF;

    public byte[] V { get; } = new byte[16];

    /// <summary>
    /// The Index/Address register. CHIP-8 has only 4kb of memory, which means 
    /// that the address space is 0x000 to 0xFFF. Therefore, the I register should be 12 bits wide, 
    /// but we'll use a 16-bit ushort for simplicity.
    /// </summary>
    public ushort I { get; set; }

    public ushort PC { get; set; } = Constants.ROM_START_LOCATION;

    public byte SP { get; private set; }

    // The call stack. Can hold up to 12 locations.
    // As the Index register, each location can be up to 12 bits wide, so we'll use ushort for simplicity
    public ushort[] Stack { get; } = new ushort[12];

    public void Push(ushort addr)
    {
        if (SP >= Stack.Length)
            throw new InvalidOperationException("Stack overflow.");
        Stack[SP++] = (ushort)(addr & AddressMask); 
    }

    public ushort Pop()
    {
        if (SP == 0)
            throw new InvalidOperationException("Stack underflow.");
        var ret = Stack[--SP];
        return (ushort)(ret & AddressMask);
    }

    internal void Reset()
    {
        Array.Clear(this.V);
        Array.Clear(this.Stack);

        this.PC = Constants.ROM_START_LOCATION;
        this.I = 0;
        this.SP = 0;
    }
}