using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Packets
{
    public class PlayerUpdate : Payload
    {
        private const int SIZE = (8 + 8) + 4 + 4;

        public Vec2 Position => new Vec2(
            EndianUnsafe.FromBytes<double>(Bytes, 0),  // 8B
            EndianUnsafe.FromBytes<double>(Bytes, 8)); // 8B
        public float Rotation => EndianUnsafe.FromBytes<float>(Bytes, 16); // 4B
        public uint FixedFrame => EndianUnsafe.FromBytes<uint>(Bytes, 20); // 4B

        public PlayerUpdate() { }
        public PlayerUpdate(Vec2 position, float rotation, uint fixedFrame, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(position.x),
                EndianUnsafe.GetBytes(position.y),
                EndianUnsafe.GetBytes(rotation),
                EndianUnsafe.GetBytes(fixedFrame)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
