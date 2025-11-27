using System.Threading;

namespace Chip8Emulator.Core;

public readonly struct OpCode
{
    public OpCode(ushort data)
    {
        this.Data = data;
        this.Set = (byte)(data >> 12);
        this.NNN = (ushort)(data & 0x0FFF);
        this.NN = (byte)(data & 0x00FF);
        this.N = (byte)(data & 0x000F);
        this.X = (byte)((data & 0x0F00) >> 8);
        this.Y = (byte)((data & 0x00F0) >> 4);
    }

    /// <summary>
    /// the 2 bytes being parsed
    /// </summary>
    public ushort Data { get; }

    /// <summary>
    /// the opcode category, stored in the first 4 bits - data >> 12
    /// </summary>
    public byte Set { get; }

    /// <summary>
    /// the last 12 bits : data & 0x0FFF
    /// </summary>
    public ushort NNN { get; }

    /// <summary>
    /// the last 8 bits : data & 0x00FF
    /// </summary>
    public byte NN { get; }

    /// <summary>
    /// the last 4 bits : data & 0x000F
    /// </summary>
    public byte N { get; }

    /// <summary>
    /// data & 0x0F00
    /// </summary>
    public byte X { get; }

    /// <summary>
    /// data & 0x00F0
    /// </summary>
    public byte Y { get; }
}