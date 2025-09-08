using System;
using System.Collections;
using QuickNet;
using QuickNet.Channel;
using UnityEngine;

namespace Larnix.Packets
{
    public class Teleport : Payload
    {
        private const int SIZE = (8 + 8);

        public Vec2 TargetPosition => new Vec2(
            EndianUnsafe.FromBytes<double>(Bytes, 0),  // 8B
            EndianUnsafe.FromBytes<double>(Bytes, 8)); // 8B

        public Teleport() { }
        public Teleport(Vec2 targetPosition, byte code = 0) 
        {
            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(targetPosition.x),
                EndianUnsafe.GetBytes(targetPosition.y)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes?.Length == SIZE;
        }
    }
}
