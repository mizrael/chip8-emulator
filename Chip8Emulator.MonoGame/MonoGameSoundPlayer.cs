using Chip8Emulator.Core;
using Chip8Emulator.Core.Utils;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Buffers;

namespace Chip8Emulator.MonoGame;

public class MonoGameSoundPlayer : ISoundPlayer
{
    private const int SampleRate = 44100;
    private const int Frequency = 500; // Hz

    private readonly LRUCache<int, SoundEffect> _effectsByDuration = new(10);

    public void Beep(int milliseconds)
    {
        if (milliseconds <= 0) return;

        var effect = _effectsByDuration.GetOrAdd(milliseconds, CreateSoundEffect);

        effect.Play();
    }

    private SoundEffect CreateSoundEffect(int milliseconds)
    {
        int sampleCount = (int)((SampleRate * milliseconds) / 1000.0);

        var samples = ArrayPool<short>.Shared.Rent(sampleCount);

        try
        {
            double amplitude = 0.25 * short.MaxValue;
            double angleIncrement = 2.0 * Math.PI * Frequency / SampleRate;

            for (int i = 0; i < sampleCount; i++)
                samples[i] = (short)(amplitude * Math.Sin(i * angleIncrement));

            var byteBuffer = new byte[sampleCount * sizeof(short)];
            Buffer.BlockCopy(samples, 0, byteBuffer, 0, byteBuffer.Length);

            var soundEffect = new SoundEffect(byteBuffer, SampleRate, AudioChannels.Mono);

            return soundEffect;
        }
        finally
        {
            ArrayPool<short>.Shared.Return(samples);
        }
    }
}