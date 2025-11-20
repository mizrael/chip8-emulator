using Chip8Emulator.Core.Utils;
using System;

namespace Chip8Emulator.Core;

public class Input
{
    private readonly LRUCache<Keys, Keys> _pressedKeys = new((uint)Enum.GetValues<Keys>().Length);

    public void SetKeyDown(Keys key)
    {
        _pressedKeys.AddOrUpdate(key, key);
    }

    public void SetKeyUp(Keys key)
        => _pressedKeys.Remove(key);

    public bool IsKeyPressed(Keys key)
        => _pressedKeys.ContainsKey(key);

    public bool IsAnyKeyPressed()
        => _pressedKeys.Count > 0;

    public Keys? GetMostRecentKeyPressed()
    {
        if (_pressedKeys.Count == 0)
            return null;

        var (key, _) = _pressedKeys.GetLast();
        return key;
    }
}
