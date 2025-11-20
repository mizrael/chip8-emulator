using System;

namespace Chip8Emulator.Core;

public class Clock
{
    private double _timerAccumulator = 0.0;
    private double _instructionAccumulator = 0.0;

    private const double delayRefreshRate = 1.0 / 60.0;

    public byte Delay { get; set; }

    public void Update(
        Action onTick,
        double elapsedSeconds,
        int targetInstructionsPerSecond)
    {
        ProcessInstructions(onTick, elapsedSeconds, targetInstructionsPerSecond);

        UpdateDelay(elapsedSeconds);
    }

    private void ProcessInstructions(Action onTick, double elapsedSeconds, int targetInstructionsPerSecond)
    {
        if (targetInstructionsPerSecond < 1)
            targetInstructionsPerSecond = 1;
        var instructionInterval = 1.0 / targetInstructionsPerSecond;

        _instructionAccumulator += elapsedSeconds;

        while (_instructionAccumulator >= instructionInterval)
        {
            _instructionAccumulator -= instructionInterval;

            onTick();
        }
    }

    private void UpdateDelay(double elapsedSeconds)
    {
        _timerAccumulator += elapsedSeconds;

        while (_timerAccumulator >= delayRefreshRate)
        {
            _timerAccumulator -= delayRefreshRate;

            if (this.Delay > 0)
                this.Delay--;
        }
    }
}
