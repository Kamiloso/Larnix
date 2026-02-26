using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Deluser : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name} <nickname>";
        public override string ShortDescription => "Deletes a user.";

        private IUserManager UserManager => GlobRef.Get<IUserManager>();

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
            if (UserManager.IsOnline(_nickname))
            {
                return (CmdResult.Error,
                    $"Cannot delete online user {_nickname}.");
            }

            if (UserManager.TryDeleteUserLink(_nickname))
            {
                return (CmdResult.Success,
                    $"Deleted user {_nickname} successfully.");
            }

            return (CmdResult.Error,
                $"Cannot delete user {_nickname}.");
        }
    }
}
