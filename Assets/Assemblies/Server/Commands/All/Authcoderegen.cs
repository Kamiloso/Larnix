#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Model.Database;
using Larnix.Server.Data;
using Larnix.Socket;

namespace Larnix.Server.Commands.All;

internal class Authcoderegen : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name}";
    public override string ShortDescription => "Regenerates the authcode and stops the server.";

    private IServer Server => GlobRef.Get<IServer>();
    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IValueRepository ValueRepository => GlobRef.Get<IValueRepository>();

    public override void Inject(string command)
    {
        if (!TrySplit(command, 1, out _))
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        Db.Handle.AsTransaction(() =>
        {
            ValueRepository.StoreSecret(SocketInfo.PathPrivateKey, "");
            ValueRepository.StoreSecret(SocketInfo.PathServerSecret, "");
        });

        Server.Close();

        return (CmdResult.Info,
            "Server is shutting down...");
    }
}
