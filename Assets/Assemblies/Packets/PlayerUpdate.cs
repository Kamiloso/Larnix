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
        private const int SIZE = 16 + 4 + 4;

        public Vec2 Position => Structures.FromBytes<Vec2>(Bytes, 0); // 16B
        public float Rotation => Primitives.FromBytes<float>(Bytes, 16); // 4B
        public uint FixedFrame => Primitives.FromBytes<uint>(Bytes, 20); // 4B

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
