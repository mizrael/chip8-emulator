using System;

namespace Chip8Emulator.Core;

public class Cpu(Interfaces interfaces)
{
    private readonly Interfaces _interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));

    private void ExecuteCurrentInstruction(State state)
    {
        var opCode = state.GetCurrentOp();

        if (!Instructions.TryGet(opCode, out var instruction))
            throw new NotImplementedException($"instruction '{opCode.Set:X}' not implemented");

        instruction(state, _interfaces, opCode);
    }

    public void Update(
        State state,
        double elapsedSeconds,
        int targetInstructionsPerSecond)
    {
        var onTick = () => this.ExecuteCurrentInstruction(state);

        state.Clock.Update(onTick, elapsedSeconds, targetInstructionsPerSecond);
    }
}
