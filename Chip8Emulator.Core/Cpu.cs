using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Chip8Emulator.Core;

public class Cpu
{
    #region members

    private readonly byte[] _memory = new byte[0x1000];
    private readonly byte[] _v = new byte[16];
    private readonly ushort[] _stack = new ushort[16];
    private readonly bool[,] _screen = new bool[Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT];

    private ushort _pc = Constants.ROM_START_LOCATION;
    private ushort _i = 0;
    private byte _sp = 0;
    private byte _delay = 0;
    // Accumulates elapsed time for 60Hz timer updates
    private double _timerAccumulator = 0.0;
    // Accumulates elapsed time for instruction scheduling
    private double _instructionAccumulator = 0.0;

    private readonly HashSet<byte> _pressedKeys = new();

    private readonly Dictionary<byte, Action<OpCode>> _instructions = new();
    private readonly Dictionary<byte, Action<OpCode>> _miscInstructions = new();

    private readonly IRenderer _renderer;
    private readonly ISoundPlayer _soundPlayer;

    #endregion members

    public Cpu(IRenderer renderer, ISoundPlayer soundPlayer)
    {
        _instructions[0x0] = this.ZeroOps;
        _instructions[0x1] = this.Jump;
        _instructions[0x2] = this.Call;
        _instructions[0x3] = this.SkipVxEqNN;
        _instructions[0x4] = this.SkipVxNeqNN;
        _instructions[0x6] = this.SetVReg;
        _instructions[0x7] = this.AddVReg;
        _instructions[0x8] = this.XYOps;
        _instructions[0x9] = this.SkipVxNeqVy;
        _instructions[0xA] = this.SetI;
        _instructions[0xC] = this.Rand;
        _instructions[0xD] = this.Draw;
        _instructions[0xE] = this.SkipOnKey;
        _instructions[0xF] = this.Misc;

        _miscInstructions[0x1E] = this.AddVRegToI;
        _miscInstructions[0x65] = this.FillVFromMI;
        _miscInstructions[0x15] = this.SetDelay;
        _miscInstructions[0x07] = this.GetDelay;
        _miscInstructions[0x0A] = this.WaitKey;
        _miscInstructions[0x33] = this.SetBCD;
        _miscInstructions[0x29] = this.SetIToCharSprite;
        _miscInstructions[0x18] = this.PlaySound;

        _renderer = renderer;
        _soundPlayer = soundPlayer;
    }

    public void LoadRom(ReadOnlySpan<byte> romData)
    {
        Reset();

        romData.CopyTo(_memory.AsSpan(Constants.ROM_START_LOCATION));
    }

    public void LoadRom(System.IO.Stream romData)
    {
        Reset();

        int romSize = (int)romData.Length;

        var dest = _memory.AsSpan(Constants.ROM_START_LOCATION);
        if (romData.Read(dest) < 1)
            throw new ArgumentException("input stream is invalid");
    }

    public void Reset()
    {
        Array.Clear(_v);
        Array.Clear(_stack);
        Array.Clear(_screen);

        Array.Clear(_memory);
        for (var i = 0; i != Font.Characters.Length; ++i)
            _memory[i] = Font.Characters[i];

        _pc = Constants.ROM_START_LOCATION;
        _i = 0;
        _sp = 0;
    }

    // Execute a single instruction (internal scheduling uses this)
    private void Tick()
    {
        ushort data = (ushort)(_memory[_pc++] << 8 | _memory[_pc++]);
        var opCode = new OpCode(data);

        if (!_instructions.TryGetValue(opCode.Set, out var instruction))
            throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");

        instruction(opCode);
    }

    private void UpdateTimers(double elapsedSeconds, double targetFrameInterval)
    {
        _timerAccumulator += elapsedSeconds;
        
        while (_timerAccumulator >= targetFrameInterval)
        {
            if (_delay > 0)
                _delay--;
            _timerAccumulator -= targetFrameInterval;
        }
    }

    public void Update(
        double elapsedSeconds, 
        int targetInstructionsPerSecond,
        double targetFrameInterval)
    {
        if (targetInstructionsPerSecond < 1)
            targetInstructionsPerSecond = 1;

        _instructionAccumulator += elapsedSeconds;
        double instructionInterval = 1.0 / targetInstructionsPerSecond;
        while (_instructionAccumulator >= instructionInterval)
        {
            Tick();
            _instructionAccumulator -= instructionInterval;
        }

        UpdateTimers(elapsedSeconds, targetFrameInterval);
    }

    public void SetKeyDown(Keys key)
        => _pressedKeys.Add((byte)key);

    public void SetKeyUp(Keys key)
        => _pressedKeys.Remove((byte)key);

    #region instructions

    // 0xCXNN
    private void Rand(OpCode opCode)
    {
        _v[opCode.X] = (byte)(Random.Shared.Next(0, 255) & opCode.NN);
    }

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
        bool updateRenderer = false;

        for (byte row = 0; row < rows; row++)
        {
            var rowData = _memory[_i + row];
            var py = (startY + row) % Constants.SCREEN_HEIGHT;

            for (byte col = 0; col != 8; col++)
            {
                var px = (startX + col) % Constants.SCREEN_WIDTH;
                var oldPixel = (byte)(_screen[px, py] ? 1 : 0);
                var spritePixel = (byte)((rowData >> (7 - col)) & 1);

                if (oldPixel != spritePixel)
                    updateRenderer = true;
                
                var newPixel = (byte)(oldPixel ^ spritePixel);
                _screen[px, py] = (newPixel != 0);

                if (oldPixel == 1 && spritePixel == 1)
                    carry = 1;
            }
        }

        _v[0xF] = carry;

        if (updateRenderer)
            _renderer.Draw(_screen);
    }

    private void Misc(OpCode opCode)
    {
        if (!_miscInstructions.TryGetValue(opCode.NN, out var instruction))
            throw new NotImplementedException($"misc instruction '0xF{opCode.NN:X}' not implemented");

        instruction(opCode);
    }

    // 0x3XNN
    private void SkipVxEqNN(OpCode opCode)
    {
        if (_v[opCode.X] == opCode.NN)
            _pc += 2;
    }

    // 0x4XNN
    private void SkipVxNeqNN(OpCode opCode)
    {
        if (_v[opCode.X] != opCode.NN)
            _pc += 2;
    }

    // 0x9XY0
    private void SkipVxNeqVy(OpCode opCode)
    {
        if (_v[opCode.X] != _v[opCode.Y])
            _pc += 2;
    }

    // 0x2NNN
    private void Call(OpCode opCode)
    {
        Push(_pc);
        _pc = opCode.NNN;
    }

    void Push(ushort value)
    => _stack[_sp++] = value;

    ushort Pop()
    => _stack[--_sp];

    private void ZeroOps(OpCode opCode)
    {
        switch (opCode.NN)
        {
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

    private void XYOps(OpCode opCode)
    {
        switch (opCode.N)
        {
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
            case 0x5:
                _v[0xF] = (byte)(_v[opCode.X] > _v[opCode.Y] ? 1 : 0);
                _v[opCode.X] -= _v[opCode.Y];
                break;
            case 0x6:
                _v[0xF] = (byte)((_v[opCode.X] & 0x1) == 1 ? 1 : 0);
                _v[opCode.X] >>= 1;
                break;
            case 0x7:
                _v[0xF] = (byte)(_v[opCode.Y] > _v[opCode.X] ? 1 : 0);
                _v[opCode.X] = (byte)(_v[opCode.Y] - _v[opCode.X]);
                break;
            default:
                throw new NotImplementedException($"instruction '0x8XY{opCode.N:X}' not implemented");
        }
    }

    // 0xE
    private void SkipOnKey(OpCode opCode)
    {
        switch (opCode.NN)
        {
            case 0x9E:
                _pc += (ushort)(_pressedKeys.Contains(_v[opCode.X]) ? 2 : 0);
                break;
            case 0xA1:
                _pc += (ushort)(!_pressedKeys.Contains(_v[opCode.X]) ? 2 : 0);
                break;
        }
    }

    #endregion instructions

    #region misc instructions

    //0xFX1E
    private void AddVRegToI(OpCode opCode)
    {
        _i += _v[opCode.X];
    }

    //0xFX65
    private void FillVFromMI(OpCode opCode)
    {
        for (byte i = 0; i <= opCode.X; ++i)
            _v[i] = _memory[_i + i];
    }

    private void SetDelay(OpCode opCode)
    {
        _delay = _v[opCode.X];
    }

    //0xFX07
    private void GetDelay(OpCode opCode)
    {
        _v[opCode.X] = _delay;
    }

    //0xFX0A
    private void WaitKey(OpCode opCode)
    {
        if (_pressedKeys.Count == 0)
        {
            _pc -= 2;
            return;
        }

        _v[opCode.X] = _pressedKeys.First();
    }

    //0xFX33
    private void SetBCD(OpCode opCode)
    {
        var vx = _v[opCode.X];
        _memory[_i] = (byte)(vx / 100);
        _memory[_i + 1] = (byte)((vx / 10) % 10);
        _memory[_i + 2] = (byte)(vx % 10);
    }

    //0xFX29
    private void SetIToCharSprite(OpCode opCode)
    {
        _i = (ushort)(_v[opCode.X] * 5);
    }

    // 0xFX18
    private void PlaySound(OpCode opCode)
    {
        var duration = _v[opCode.X];
        _soundPlayer.Beep(duration);
    }

    #endregion misc instructions
}
