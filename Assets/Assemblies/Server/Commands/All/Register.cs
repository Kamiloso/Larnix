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
    internal class Register : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name} <nickname> <password>";
        public override string ShortDescription => "Registers a new user.";

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
            UserManager.RegisterUserAsync(_nickname, _password, success =>
            {
                if (success)
                {
                    Core.Debug.LogSuccess($"Registered user {_nickname} successfully.");
                }
                else
                {
                    Core.Debug.LogError($"Failed to register user {_nickname}.");
                }
            });

            return (CmdResult.Info,
                $"Registering user {_nickname}...");
        }
    }
}
