#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Enums;
using Larnix.Model.Worldgen.Biomes;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;
using Larnix.Core.Serialization;

namespace Larnix.Server.Packets;

[CmdId(0x07)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FrameInfo
{
    public long ServerTick { get; }
    public Col32 SkyColor { get; }
    public BiomeID BiomeId { get; }
    public WeatherID WeatherId { get; }
    public float Tps { get; }

    public FrameInfo(long serverTick, Col32 skyColor, BiomeID biomeId, WeatherID weatherId, float tps)
    {
        ServerTick = serverTick;
        SkyColor = skyColor;
        BiomeId = Sanitizer_Legacy.SanitizeEnum(biomeId);
        WeatherId = Sanitizer_Legacy.SanitizeEnum(weatherId);
        Tps = Sanitizer_Legacy.SanitizeFloat(tps);
    }

    public FrameInfo Sanitize()
    {
        return new FrameInfo(ServerTick, SkyColor, BiomeId, WeatherId, Tps);
    }
}
