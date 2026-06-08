#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Server.Run.Records;

namespace Larnix.Server.Commands.All;

internal class Stop : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name}";
    public override string ShortDescription => "Turns off the server.";

    private RunCallbacks RunCallbacks => GlobRef.Get<RunCallbacks>();

    public override void Inject(string command)
    {
        if (!TrySplit(command, 1, out _))
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        RunCallbacks.Stop();

        return (CmdResult.Info,
            "Server is shutting down...");
    }
}
