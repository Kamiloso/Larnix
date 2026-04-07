using Larnix.Core.Vectors;
using Larnix.Socket.Packets;
using Larnix.Model.Enums;
using Larnix.Model.Worldgen.Biomes;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class FrameInfo : Payload
{
    private static int SIZE => sizeof(long) + Binary<Col32>.Size + sizeof(BiomeID) + sizeof(WeatherID) + sizeof(float);

    public long ServerTick => Binary<long>.Deserialize(Bytes, 0);
    public Col32 SkyColor => Binary<Col32>.Deserialize(Bytes, 8);
    public BiomeID BiomeID => Binary<BiomeID>.Deserialize(Bytes, 12);
    public WeatherID Weather => Binary<WeatherID>.Deserialize(Bytes, 14);
    public float Tps => Binary<float>.Deserialize(Bytes, 16);

    public FrameInfo(long serverTick, Col32 skyColor, BiomeID biomeID, WeatherID weather, float tps, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<long>.Serialize(serverTick),
            Binary<Col32>.Serialize(skyColor),
            Binary<BiomeID>.Serialize(biomeID),
            Binary<WeatherID>.Serialize(weather),
            Binary<float>.Serialize(tps)
        ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE;
    }
}
