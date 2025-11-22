using Chip8Emulator.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Chip8Emulator.MonoGame;

public class TextureRenderer : IDisplay
{
    public readonly Texture2D Texture;
    private readonly Color[] _pixels;

    public TextureRenderer(GraphicsDevice graphicsDevice)
    {
        _pixels = new Color[Constants.SCREEN_WIDTH * Constants.SCREEN_HEIGHT];
        Texture = new Texture2D(graphicsDevice, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT, false, SurfaceFormat.Color);
    }

    public void Refresh(VideoBuffer screen)
    {
        for (int col = 0; col != Constants.SCREEN_WIDTH; col++)
            for (int row = 0; row != Constants.SCREEN_HEIGHT; row++)
            {
                int index = row * Constants.SCREEN_WIDTH + col;
                _pixels[index] = screen[col, row] ? Color.White : Color.Black;
            }

        Texture.SetData(_pixels);
    }
}