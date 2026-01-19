using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Packets.Game
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
