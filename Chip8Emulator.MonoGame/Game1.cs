﻿using Chip8Emulator.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;
using MonoKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Chip8Emulator.MonoGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Cpu _cpu;
        
        private TextureRenderer _renderer;

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
            { MonoKeys.D, Keys.Number8 },
            { MonoKeys.F, Keys.E },
            { MonoKeys.Z, Keys.A },
            { MonoKeys.X, Keys.Number0 },
            { MonoKeys.C, Keys.B },
            { MonoKeys.V, Keys.F }
        };

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = false;
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
            _renderer = new TextureRenderer(this.GraphicsDevice, _spriteBatch);
            _cpu = new Cpu(_renderer, new DefaultSoundPlayer());

            var romPath = "Content/roms/PONG";
            using (var romData = System.IO.File.OpenRead(romPath))
            {
                _cpu.LoadAsync(romData).Wait();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            //    Exit();

            _cpu.Tick();
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);                       
            
            _renderer.Render();

            base.Draw(gameTime);
        }
    }
}