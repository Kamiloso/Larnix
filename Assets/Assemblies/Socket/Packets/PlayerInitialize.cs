using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Packets
{
    public class PlayerInitialize : Payload
    {
        private const int SIZE = (8 + 8) + 8 + 4;

        public Vec2 Position => new Vec2(
            EndianUnsafe.FromBytes<double>(Bytes, 0),  // 8B
            EndianUnsafe.FromBytes<double>(Bytes, 8)); // 8B
        public ulong MyUid => EndianUnsafe.FromBytes<ulong>(Bytes, 16); // 8B
        public uint LastFixedFrame => EndianUnsafe.FromBytes<uint>(Bytes, 24); // 4B

        public PlayerInitialize() { }
        public PlayerInitialize(Vec2 position, ulong myUid, uint lastFixedFrame, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(position.x),
                EndianUnsafe.GetBytes(position.y),
                EndianUnsafe.GetBytes(myUid),
                EndianUnsafe.GetBytes(lastFixedFrame)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
