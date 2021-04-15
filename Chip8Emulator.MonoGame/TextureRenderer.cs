using Chip8Emulator.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Chip8Emulator.MonoGame
{
    public class TextureRenderer : IRenderer
    {
        private readonly Texture2D _texture;
        private readonly Color[] _pixels;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        
        public TextureRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _pixels = new Color[Cpu.SCREEN_WIDTH * Cpu.SCREEN_HEIGHT];
            _texture = new Texture2D(graphicsDevice, Cpu.SCREEN_WIDTH, Cpu.SCREEN_HEIGHT, false, SurfaceFormat.Color);
        }

        public void Update(bool[,] screen)
        {
            for (int col = 0; col != Cpu.SCREEN_WIDTH; col++)
                for (int row = 0; row != Cpu.SCREEN_HEIGHT; row++)
                {
                    int index = row * Cpu.SCREEN_WIDTH + col;
                    _pixels[index] = screen[col, row] ? Color.White : Color.Black;
                }

            _texture.SetData(_pixels);
        }

        public void Render()
        {
            _spriteBatch.Draw(_texture, _graphicsDevice.Viewport.Bounds, Color.White);
            // _spriteBatch.Draw(_texture, Vector2.Zero, null, Color.White, 0f, 
            //     Vector2.Zero, this.Scale, SpriteEffects.None, 0);
        }

        public Vector2 Scale { get; set; } = new Vector2(4, 4);
    }
}