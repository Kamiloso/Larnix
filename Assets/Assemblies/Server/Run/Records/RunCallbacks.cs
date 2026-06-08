#nullable enable
using System;

namespace Larnix.Server.Run.Records;

public record RunCallbacks(
    Action<RunAnswer>? SetAnswer,
    Action? StopSignal
    )
{
    public void Answer(RunAnswer answer) => SetAnswer?.Invoke(answer);
    public void Stop() => StopSignal?.Invoke();
}
