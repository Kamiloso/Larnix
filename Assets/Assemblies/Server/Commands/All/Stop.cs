using Larnix.Core;
using Larnix.Model.Interfaces;

namespace Larnix.Server.Commands.All;

internal class Stop : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name}";
    public override string ShortDescription => "Turns off the server.";

    private IServer Server => GlobRef.Get<IServer>();

    public override void Inject(string command)
    {
        if (!TrySplit(command, 1, out _))
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        Server.Close();

        return (CmdResult.Info,
            "Server is shutting down...");
    }
}
