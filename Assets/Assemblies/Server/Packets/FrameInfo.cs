#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using Larnix.Model.Enums;
using Larnix.Model.Worldgen.Biomes;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(6)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FrameInfo : ISanitizable<FrameInfo>
{
    public long ServerTick { get; }
    public Col32 SkyColor { get; }
    public BiomeID BiomeId { get; }
    public WeatherID WeatherId { get; }
    public float Tps { get; }

    public FrameInfo(long serverTick, Col32 skyColor, BiomeID biomeId, WeatherID weatherId, float tps)
    {
        ServerTick = serverTick;
        SkyColor = Sanitizer.Filter(skyColor);
        BiomeId = Sanitizer.Filter(biomeId);
        WeatherId = Sanitizer.Filter(weatherId);
        Tps = Sanitizer.ToFinite(tps);
    }

    public FrameInfo Sanitize()
    {
        return new FrameInfo(ServerTick, SkyColor, BiomeId, WeatherId, Tps);
    }
}
