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
    internal class Passwd : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name} <nickname> <password>";
        public override string ShortDescription => "Changes a user's password.";

        private Server Server => GlobRef.Get<Server>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();
        private IUserManager UserManager => GlobRef.Get<IUserManager>();

        private string _nickname;
        private string _password;

        public override void Inject(string command)
        {
            if (TrySplit(command, 3, out string[] parts))
            {
                if (!Parsing.TryParseNickname(parts[1], out var nickname))
                {
                    throw FormatException(Validation.WrongNicknameInfo);
                }

                if (!Parsing.TryParsePassword(parts[2], out var password))
                {
                    throw FormatException(Validation.WrongPasswordInfo);
                }

                _nickname = nickname;
                _password = password;
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            if (Server.Type == ServerType.Local || Server.Type == ServerType.Host)
            {
                if (DataSaver.HostNickname == _nickname)
                {
                    return (CmdResult.Error,
                        "Cannot change host user's password.");
                }
            }

            UserManager.ChangePasswordAsync(_nickname, _password, success =>
            {
                if (success)
                {
                    Core.Debug.LogSuccess($"Changed {_nickname}'s password successfully.");
                }
                else
                {
                    Core.Debug.LogError($"Failed to change {_nickname}'s password.");
                }
            });

            return (CmdResult.Info,
                $"Changing {_nickname}'s password...");
        }
    }
}
