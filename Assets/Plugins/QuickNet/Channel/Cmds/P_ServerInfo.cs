using System.Collections;
using System.Collections.Generic;

namespace QuickNet.Channel.Cmds
{
    public class P_ServerInfo : Payload
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
