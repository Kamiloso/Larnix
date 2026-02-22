using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core.Utils;
using Larnix.Server.Entities;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Kill : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <nickname>";
        public override string ShortDescription => "Kills a player.";

        private EntityManager EntityManager => Ref<EntityManager>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();

        private string _nickname;

        public override void Inject(string command)
        {
            if (TrySplit(command, 2, out string[] parts))
            {
                _nickname = parts[1];

                if (!Validation.IsGoodNickname(_nickname))
                {
                    throw FormatException(Validation.WrongNicknameInfo);
                }
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            if (PlayerManager.IsAlive(_nickname))
            {
                ulong uid = PlayerManager.UidByNickname(_nickname);
                EntityManager.KillEntity(uid);

                return (CmdResult.Success,
                    $"Player {_nickname} has been killed.");
            }
            else
            {
                return (CmdResult.Error,
                    $"Player {_nickname} is not alive.");
            }
        }
    }
}
