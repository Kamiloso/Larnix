using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class Teleport : Payload
    {
        private const int SIZE = Vec2.SIZE;

        public Vec2 TargetPosition => Structures.FromBytes<Vec2>(Bytes, 0); // Vec2.SIZE
        
        public Teleport() { }
        public Teleport(Vec2 targetPosition, byte code = 0) 
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(targetPosition)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
