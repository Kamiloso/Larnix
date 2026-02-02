using System.Collections;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets.Control
{
    public sealed class DebugMessage : Payload
    {
        private const int SIZE = 512;
        public String512 Message => Primitives.FromBytes<String512>(Bytes, 0);

        public DebugMessage() { }
        public DebugMessage(string message, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes<String512>(message)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE &&
                Validation.IsGoodText<String512>(Message);
        }
    }
}
