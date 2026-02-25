using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core.Utils;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Kick : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <nickname>";
        public override string ShortDescription => "Kicks a player.";

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();

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
            if (PlayerActions.IsConnected(_nickname))
            {
                QuickServer.FinishConnectionRequest(_nickname);

                return (CmdResult.Info,
                    $"Player {_nickname} is being kicked...");
            }
            else
            {
                return (CmdResult.Error,
                    $"Player {_nickname} is not connected.");
            }
        }
    }
}
