using System.Collections;
using Larnix.GameCore.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Misc;

namespace Larnix.Socket.Packets.Control
{
    public sealed class DebugMessage : Payload
    {
        private const int SIZE = 512;
        public String512 Message => Primitives.FromBytes<String512>(Bytes, 0);

        public DebugMessage(in String512 message, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(message)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE &&
                Validation.IsGoodText<String512>(Message);
        }
    }
}
