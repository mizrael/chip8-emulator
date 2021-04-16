using System;

namespace Chip8Emulator.Core
{
    public class LambdaRenderer : IRenderer
    {
        private readonly Action<bool[,]> _update;
        private readonly Action _render;

        public LambdaRenderer(Action<bool[,]> update, Action render)
        {
            _update = update;
            _render = render;
        }

        public void Render()
            => _render?.Invoke();

        public void Update(bool[,] screen)
            =>_update?.Invoke(screen);        
    }
}
