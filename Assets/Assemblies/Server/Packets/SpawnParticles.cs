#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Enums;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x05)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct SpawnParticles : ISanitizable<SpawnParticles>
{
    public Vec2 Position { get; }
    public ParticleID ParticleID { get; }
    public ulong EntityUid { get; }

    public bool IsEntityParticle() => EntityUid != 0;

    public SpawnParticles(Vec2 position, ParticleID particleID, ulong entityUid = 0)
    {
        Position = position.Sanitize();
        ParticleID = Sanitizer_Legacy.SanitizeEnum(particleID);
        EntityUid = entityUid;
    }

    public SpawnParticles Sanitize()
    {
        return new SpawnParticles(Position, ParticleID, EntityUid);
    }
}
