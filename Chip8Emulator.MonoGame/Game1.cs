using Chip8Emulator.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;
using MonoKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Chip8Emulator.MonoGame;

public class Game1 : Game
{
    private SpriteBatch _spriteBatch;
    private Cpu _cpu;
    private TextureRenderer _renderer;
    private const int InstructionsPerSecond = 400; 
    private const double TargetFrameInterval = 1.0 / 60.0;

    private readonly Dictionary<MonoKeys, Core.Keys> _keyMappings = new Dictionary<MonoKeys, Keys>() {
        { MonoKeys.D1, Keys.Number1 },
        { MonoKeys.D2, Keys.Number2 },
        { MonoKeys.D3, Keys.Number3 },
        { MonoKeys.D4, Keys.C },
        { MonoKeys.Q, Keys.Number4 },
        { MonoKeys.W, Keys.Number5 },
        { MonoKeys.E, Keys.Number6 },
        { MonoKeys.R, Keys.D },
        { MonoKeys.A, Keys.Number7 },
        { MonoKeys.S, Keys.Number8 },
        { MonoKeys.D, Keys.Number9 },
        { MonoKeys.F, Keys.E },
        { MonoKeys.Z, Keys.A },
        { MonoKeys.X, Keys.Number0 },
        { MonoKeys.C, Keys.B },
        { MonoKeys.V, Keys.F }
    };

    public Game1()
    {
        var _ = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1f / 60f);
    }

    protected override void Initialize()
    {
        base.Initialize();
        this.Window.KeyDown += Window_KeyDown;
        this.Window.KeyUp += Window_KeyUp;
    }

    private void Window_KeyUp(object sender, InputKeyEventArgs e)
    {
        if (_keyMappings.ContainsKey(e.Key))
            _cpu.SetKeyUp(_keyMappings[e.Key]);
    }

    private void Window_KeyDown(object sender, InputKeyEventArgs e)
    {
        if (_keyMappings.ContainsKey(e.Key))
            _cpu.SetKeyDown(_keyMappings[e.Key]);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new TextureRenderer(this.GraphicsDevice);
        _cpu = new Cpu(_renderer, new DefaultSoundPlayer());

        var romPath = "Content/roms/TETRIS";
        using var romData = System.IO.File.OpenRead(romPath);
        _cpu.LoadRom(romData);
    }

    protected override void Update(GameTime gameTime)
    {
        _cpu.Update(
            gameTime.ElapsedGameTime.TotalSeconds,
            InstructionsPerSecond,
            TargetFrameInterval);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: new SamplerState()
        {
            Filter = TextureFilter.Point
        });

        _spriteBatch.Draw(_renderer.Texture, this.GraphicsDevice.Viewport.Bounds, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}