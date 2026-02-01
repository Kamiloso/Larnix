using System.Collections;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets.Control
{
    internal sealed class P_ServerInfo : Payload
    {
        private const int SIZE = 32;
        public String32 Nickname => EndianUnsafe.FromBytes<String32>(Bytes, 0);

        public P_ServerInfo() { }
        public P_ServerInfo(string message, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes<String32>(message)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE &&
                Validation.IsGoodNickname(Nickname);
        }
    }
}
