using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class PlayerUpdate : Payload
    {
        private const int SIZE = Vec2.SIZE + sizeof(float) + sizeof(uint);

        public Vec2 Position => Structures.FromBytes<Vec2>(Bytes, 0); // Vec2.SIZE
        public float Rotation => Primitives.FromBytes<float>(Bytes, Vec2.SIZE); // sizeof(float)
        public uint FixedFrame => Primitives.FromBytes<uint>(Bytes, Vec2.SIZE + sizeof(float)); // sizeof(uint)

        public PlayerUpdate() { }
        public PlayerUpdate(Vec2 position, float rotation, uint fixedFrame, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(position),
                Primitives.GetBytes(rotation),
                Primitives.GetBytes(fixedFrame)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
