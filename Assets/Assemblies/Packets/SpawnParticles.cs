using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Utils;
using Larnix.Socket.Packets;
using Larnix.Core;

namespace Larnix.Packets
{
    public sealed class SpawnParticles : Payload
    {
        private const int SIZE = 16 + 2 + 8;

        public Vec2 Position => Structures.FromBytes<Vec2>(Bytes, 0); // 16B
        public ParticleID ParticleID => Primitives.FromBytes<ParticleID>(Bytes, 16); // 2B
        public ulong EntityUid => Primitives.FromBytes<ulong>(Bytes, 18); // 8B
        public bool IsEntityParticle => EntityUid != 0;

        public SpawnParticles() { }
        public SpawnParticles(Vec2 position, ParticleID particleID, ulong entityUid = 0, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Structures.GetBytes(position),
                Primitives.GetBytes(particleID),
                Primitives.GetBytes(entityUid)
                ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
