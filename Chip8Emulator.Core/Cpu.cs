using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chip8Emulator.Core
{
    public class Cpu
    {
        private const int MEMORY_START = 0x200;         
        private readonly byte[] _memory = new byte[0x1000];        
        private readonly byte[] _v = new byte[16];
        private readonly ushort[] _stack = new ushort[16];

        public const int SCREEN_WIDTH = 64;
        public const int SCREEN_HEIGHT = 32;
        private readonly bool[,] _screen = new bool[SCREEN_WIDTH, SCREEN_HEIGHT];

        private ushort _pc = MEMORY_START;
        private ushort _i = 0;
        private byte _sp = 0; 
        private byte _delay = 0;       

        private readonly Dictionary<byte, bool> _keyboard;

        private readonly Dictionary<byte, Action<OpCode>> _instructions = new();
        private readonly Dictionary<byte, Action<OpCode>> _miscInstructions = new();

        public Cpu()
        {
            _keyboard = new();
            for (byte i = 0; i < 16; i++)
                _keyboard.Add(i, false);

            _instructions[0x0] = this.ZeroOps;
            _instructions[0x1] = this.Jump;
            _instructions[0x2] = this.Call;
            _instructions[0x3] = this.SkipVxEqNN;
            _instructions[0x4] = this.SkipVxNeqNN;
            _instructions[0x6] = this.SetVReg;
            _instructions[0x7] = this.AddVReg;
            _instructions[0x8] = this.XYOps;
            _instructions[0xA] = this.SetI;
            _instructions[0xD] = this.Draw;
            _instructions[0xE] = this.SkipOnKey;
            _instructions[0xF] = this.Misc;

            _miscInstructions[0x1E] = this.AddVRegToI;
            _miscInstructions[0x65] = this.FillVFromMI;
            _miscInstructions[0x15] = this.SetDelay;
            _miscInstructions[0x7] = this.GetDelay;
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
            var opCode = new OpCode(data);

            if (!_instructions.TryGetValue(opCode.Set, out var instruction))
                throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");
                
            instruction(opCode);
        }

        public void Render(IRenderer renderer)
        {
            renderer.Update(_screen);
        }

        #region instructions

        // 0x1NNN
        private void Jump(OpCode opCode)
        {
            _pc = opCode.NNN; 
        }

        // 0x6XNN
        private void SetVReg(OpCode opCode)
        {
            _v[opCode.X] = opCode.NN;
        }

        // 0x7XNN
        private void AddVReg(OpCode opCode)
        {
            int result = _v[opCode.X] + opCode.NN;
            bool carry = result > 255;
            if (carry)
                result = result - 256;
            
            _v[opCode.X] = (byte)(result & 0x00FF);
        }

        // 0xANNN
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
            var startX = _v[opCode.X];
            var startY = _v[opCode.Y];
            var rows = opCode.N;
            byte carry = 0;

            for(byte row = 0; row < rows; row++)
            {
                byte rowData = _memory[_i + row];
                int py = (startY + row) % SCREEN_HEIGHT;

                for (byte col = 0; col != 8; col++)
                {
                    int px = (startX + col) % SCREEN_WIDTH;
                    byte oldPixel = (byte)(_screen[px, py] ? 1 : 0);
                    byte spritePixel = (byte)((rowData >> (7 - col)) & 1);

                    byte newPixel = (byte)(oldPixel ^ spritePixel);
                    _screen[px, py] = (newPixel != 0);

                    if (oldPixel == 1 && spritePixel == 1)
                        carry = 1;
                }                
            }

            _v[0xF] = carry;
        }

        private void Misc(OpCode opCode){
            if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
                throw new NotImplementedException($"instruction '0xF{opCode.NN:X}' not implemented");
                
            instruction(opCode);
        }

        // 0x3XNN
        private void SkipVxEqNN(OpCode opCode){
            if(_v[opCode.X] == opCode.NN)
                _pc+=2;
        }

        // 0x4XNN
        private void SkipVxNeqNN(OpCode opCode){
            if(_v[opCode.X] != opCode.NN)
                _pc+=2;
        }

        // 0x2NNN
        private void Call(OpCode opCode){
            Push(_pc);
            _pc = opCode.NNN;
        }

        void Push(ushort value) =>
	        _stack[_sp++] = value;    

        ushort Pop() => 
	        _stack[--_sp];

        private void ZeroOps(OpCode opCode){
            switch(opCode.NN){
                case 0xE0:
                    Array.Clear(_screen, 0, _screen.Length); 
                    break;
                case 0xEE:
                    _pc = Pop();
                    break;
                default:
                    throw new NotImplementedException($"instruction '0x0{opCode.NN:X}' not implemented");
            }
        }

        private void XYOps(OpCode opCode){
            switch(opCode.N){
                case 0x0:
                    _v[opCode.X] = _v[opCode.Y];
                    break;
                case 0x1:
                    _v[opCode.X] |= _v[opCode.Y]; 
                    break;
                case 0x2:
                    _v[opCode.X] &= _v[opCode.Y];
                    break;
                case 0x3:
                    _v[opCode.X] ^= _v[opCode.Y];
                    break;
                case 0x4:
                    var res = _v[opCode.X] + _v[opCode.Y];
                    var carry = res > 255;
                    _v[opCode.X] = (byte)res;
                    _v[0xF] = (byte)(carry ? 1 : 0);
                    break;
                default:
                    throw new NotImplementedException($"instruction '0x8XY{opCode.N:X}' not implemented");
            }
        }

        private void SkipOnKey(OpCode opCode){
            switch(opCode.NN){
                case 0x9E:
                    if(_keyboard[_v[opCode.X]])
                        _pc +=2;
                    break;
                case 0xA1:
                    if(!_keyboard[_v[opCode.X]])
                        _pc +=2;
                    break;
                default:
                    throw new NotImplementedException($"instruction '0xEX{opCode.NN:X}' not implemented");
            }
        }

        #endregion instructions

        #region misc instructions


        //0xFX1E
        private void AddVRegToI(OpCode opCode){
            _i += _v[opCode.X];
        }

        //0xFX65
        private void FillVFromMI(OpCode opCode){
            for(byte i=0;i<=opCode.X;++i)
                _v[i] = _memory[_i+i];
        }

        private void SetDelay(OpCode opCode){
            _delay = _v[opCode.X];
        }

        private void GetDelay(OpCode opCode){
            _v[opCode.X] = _delay;
        }

        #endregion misc instructions
    }
}
