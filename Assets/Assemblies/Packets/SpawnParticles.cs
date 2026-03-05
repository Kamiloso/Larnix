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
        private const int SIZE = Vec2.SIZE + sizeof(ParticleID) + sizeof(ulong);

        public Vec2 Position => Structures.FromBytes<Vec2>(Bytes, 0); // Vec2.SIZE
        public ParticleID ParticleID => Primitives.FromBytes<ParticleID>(Bytes, Vec2.SIZE); // ParticleID size
        public ulong EntityUid => Primitives.FromBytes<ulong>(Bytes, Vec2.SIZE + sizeof(ParticleID)); // ulong size
        public bool IsEntityParticle => EntityUid != 0;

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
