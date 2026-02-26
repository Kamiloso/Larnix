using System;
using System.Collections.Generic;
using System.IO;
using Larnix.Core;
using Larnix.Core.Files;
using Larnix.Socket.Backend;
using Larnix.Socket.Frontend;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Authcoderegen : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Regenerates the authcode and stops the server.";

        private Server Server => GlobRef.Get<Server>();

        public override void Inject(string command)
        {
            if (!TrySplit(command, 1, out _))
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            FileManager.Delete(Server.SocketPath,
                QuickServer.PRIVATE_KEY_FILENAME, QuickServer.SERVER_SECRET_FILENAME);

            Server.CloseServer();

            return (CmdResult.Info,
                "Server is shutting down...");
        }
    }
}
