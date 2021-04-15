using Chip8Emulator.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace Chip8Emulator.MonoGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Cpu _cpu;
        
        private TextureRenderer _renderer;
        
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        { 
            base.Initialize();
            this.Window.KeyDown += Window_KeyDown;            
        }

        private void Window_KeyDown(object sender, InputKeyEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            _cpu = new Cpu();
            _renderer = new TextureRenderer(this.GraphicsDevice, _spriteBatch);

            var romPath = "Content/roms/Space Invaders [David Winter].ch8";
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
            
            _cpu.Render(_renderer);

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