using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Utils;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class PlayerInitialize : Payload
    {
        private const int SIZE = Vec2.SIZE + sizeof(ulong) + sizeof(uint);

        public Vec2 Position => Structures.FromBytes<Vec2>(Bytes, 0); // Vec2.SIZE
        public ulong MyUid => Primitives.FromBytes<ulong>(Bytes, Vec2.SIZE); // sizeof(ulong)
        public uint LastFixedFrame => Primitives.FromBytes<uint>(Bytes, Vec2.SIZE + sizeof(ulong)); // sizeof(uint)

        public PlayerInitialize() { }
        public PlayerInitialize(Vec2 position, ulong myUid, uint lastFixedFrame, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(position),
                Primitives.GetBytes(myUid),
                Primitives.GetBytes(lastFixedFrame)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
