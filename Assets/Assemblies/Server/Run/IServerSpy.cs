#nullable enable
using Larnix.Server.Run.Records;

namespace Larnix.Server.Run;

public interface IServerSpy
{
    bool IsAlive { get; }
    bool IsCrashed { get; }
    RunAnswer? RunAnswer { get; }
}
