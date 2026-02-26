using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using Larnix.Server.Data;

namespace Larnix.Server.Commands.All
{
    internal class Sethost : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name} <nickname>";
        public override string ShortDescription => "Changes a host user and stops the server.";

        private Server Server => GlobRef.Get<Server>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();

        private string _nickname;

        public override void Inject(string command)
        {
            if (TrySplit(command, 2, out string[] parts))
            {
                if (!Parsing.TryParseNickname(parts[1], out var nickname))
                {
                    throw FormatException(Validation.WrongNicknameInfo);
                }

                _nickname = nickname;
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            DataSaver.HostNickname = (String32)_nickname;
            Server.CloseServer();

            return (CmdResult.Info,
                "Server is shutting down...");
        }
    }
}
