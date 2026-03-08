using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Misc;
using Larnix.Socket.Packets;
using Larnix.GameCore.Enums;

namespace Larnix.Packets
{
    public sealed class FrameInfo : Payload
    {
        private const int SIZE = sizeof(long) + Col32.SIZE + sizeof(BiomeID) + sizeof(WeatherID) + sizeof(float);

        public long ServerTick => Primitives.FromBytes<long>(Bytes, 0);
        public Col32 SkyColor => Structures.FromBytes<Col32>(Bytes, 8);
        public BiomeID BiomeID => Primitives.FromBytes<BiomeID>(Bytes, 12);
        public WeatherID Weather => Primitives.FromBytes<WeatherID>(Bytes, 14);
        public float Tps => Primitives.FromBytes<float>(Bytes, 16);

        public FrameInfo(long serverTick, Col32 skyColor, BiomeID biomeID, WeatherID weather, float tps, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(serverTick),
                Structures.GetBytes(skyColor),
                Primitives.GetBytes(biomeID),
                Primitives.GetBytes(weather),
                Primitives.GetBytes(tps)
            ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
