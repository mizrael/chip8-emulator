namespace Chip8Emulator.Core
{
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

        public ushort Data { get; }
        public byte Set { get; }
        public ushort NNN { get; }
        public byte NN { get; }
        public byte N { get; }
        public byte X { get; }
        public byte Y { get; }
    }
}
