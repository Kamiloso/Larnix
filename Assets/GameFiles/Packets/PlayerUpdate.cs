using System;
using System.Collections;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class PlayerUpdate : Payload
    {
        private const int SIZE = (4 + 4) + 4 + 4;

        public Vector2 Position => new Vector2(
            EndianUnsafe.FromBytes<float>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<float>(Bytes, 4)); // 4B
        public float Rotation => EndianUnsafe.FromBytes<float>(Bytes, 8); // 4B
        public uint FixedFrame => EndianUnsafe.FromBytes<uint>(Bytes, 12); // 4B

        public PlayerUpdate() { }
        public PlayerUpdate(Vector2 position, float rotation, uint fixedFrame, byte code = 0)
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
