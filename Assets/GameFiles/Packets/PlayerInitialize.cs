using System;
using System.Collections;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class PlayerInitialize : Payload
    {
        private const int SIZE = (4 + 4) + 8 + 4;

        public Vector2 Position => new Vector2(
            EndianUnsafe.FromBytes<float>(Bytes, 0),  // 4B
            EndianUnsafe.FromBytes<float>(Bytes, 4)); // 4B
        public ulong MyUid => EndianUnsafe.FromBytes<ulong>(Bytes, 8); // 8B
        public uint LastFixedFrame => EndianUnsafe.FromBytes<uint>(Bytes, 16); // 4B

        public PlayerInitialize() { }
        public PlayerInitialize(Vector2 position, ulong myUid, uint lastFixedFrame, byte code = 0)
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
