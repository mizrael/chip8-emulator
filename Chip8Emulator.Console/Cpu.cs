using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chip8Emulator.Console
{
    public class Cpu
    {
        private const int MEMORY_START = 0x200;         
        private readonly byte[] _memory = new byte[0x1000];        
        private readonly byte[] _v = new byte[16];
        private readonly byte[] _stack = new byte[48];

        private const int SCREEN_WIDTH = 64;
        private const int SCREEN_HEIGHT = 32;
        private readonly bool[,] _screen = new bool[SCREEN_WIDTH, SCREEN_HEIGHT];

        private ushort _pc = MEMORY_START;
        private ushort _i = 0;
        private byte _sp = 0;       

        private readonly Dictionary<byte, bool> _keyboard;

        private readonly Dictionary<byte, Action<OpCode>> _instructions = new();

        public Cpu()
        {
            _keyboard = new();
            for (byte i = 0; i < 16; i++)
                _keyboard.Add(i, false);

            _instructions[0x1] = this.Jump;
            _instructions[0x6] = this.SetVReg;
            _instructions[0xA] = this.SetI;
            _instructions[0xD] = this.Draw;
        }

        public async Task LoadAsync(System.IO.Stream romData)
        {
            Reset();

            using var ms = new System.IO.MemoryStream(_memory, MEMORY_START, (int)romData.Length, true);
            await romData.CopyToAsync(ms);
        }

        public void Reset()
        {
            Array.Clear(_memory, 0, _memory.Length);
            Array.Clear(_v, 0, _v.Length);
            Array.Clear(_stack, 0, _stack.Length);
            Array.Clear(_screen, 0, _screen.Length);
            _pc = MEMORY_START;
            _i = 0;
            _sp = 0;
        }

        public void Tick()
        {
            ushort data = (ushort)(_memory[_pc++] << 8 | _memory[_pc++]);
            var opcode = new OpCode(data);

            if (!_instructions.TryGetValue(opcode.Set, out var instruction))
                throw new MethodAccessException($"instruction '{opcode.Set:X}' not implemented");
            instruction(opcode);
        }

        #region instructions

        private void Jump(OpCode opCode)
        {
            _pc = opCode.NNN; 
        }

        private void SetVReg(OpCode opCode)
        {
            _v[opCode.X] = opCode.NN;
        }

        private void SetI(OpCode opCode)
        {
            _i = opCode.NNN;
        }

        /// <summary>
        /// Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels 
        /// and a height of N+1 pixels. Each row of 8 pixels is read as 
        /// bit-coded starting from memory location I; 
        /// I value doesn’t change after the execution of this instruction. 
        /// https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
        /// Sprite pixels are XOR'd with corresponding screen pixels. 
        /// In other words, sprite pixels that are set flip the color of the 
        /// corresponding screen pixel, while unset sprite pixels do nothing. 
        /// The carry flag (VF) is set to 1 if any screen pixels are flipped 
        /// from set to unset when a sprite is drawn and set to 0 otherwise. 
        /// </summary>
        /// <param name="opCode"></param>
        private void Draw(OpCode opCode)
        {
            var startX = opCode.X;
            var startY = opCode.Y;
            var rows = opCode.N + 1;
            byte carry = 0;

            for(byte row = 0; row != rows; row++)
            {
                byte rowData = _memory[_i + row];
                if (0 == rowData)
                    continue;

                int py = (startY + row) % SCREEN_HEIGHT;

                for (byte x = 0; x != 8; x++)
                {
                    byte bit = (byte)(rowData & 0xF000);
                    rowData <<= 1; 
                    
                    if (bit == 0)
                        continue;

                    int px = (startX + x) % SCREEN_WIDTH;                   

                    var oldPixel = _screen[px, py];
                    _screen[px, py] = !oldPixel;

                    if (oldPixel) carry = 1;
                }                
            }

            _v[0xF] = carry;
        }

        #endregion instructions
    }

}
