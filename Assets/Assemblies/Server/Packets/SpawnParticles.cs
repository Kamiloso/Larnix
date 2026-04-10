using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Model.Enums;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;

namespace Larnix.Server.Packets;

public sealed class SpawnParticles : Payload_Legacy
{
	private static int SIZE => Binary<Vec2>.Size + sizeof(ParticleID) + sizeof(ulong);

    public Vec2 Position => Binary<Vec2>.Deserialize(Bytes, 0); // Binary<Vec2>.Size
    public ParticleID ParticleID => Binary<ParticleID>.Deserialize(Bytes, 16); // ParticleID size
    public ulong EntityUid => Binary<ulong>.Deserialize(Bytes, 18); // ulong size
    public bool IsEntityParticle => EntityUid != 0;

    public SpawnParticles(Vec2 position, ParticleID particleID, ulong entityUid = 0, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2>.Serialize(position),
            Binary<ParticleID>.Serialize(particleID),
            Binary<ulong>.Serialize(entityUid)
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE;
    }
}
