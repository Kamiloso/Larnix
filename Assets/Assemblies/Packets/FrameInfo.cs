using System;
using System.Collections;
using Larnix.Core.Vectors;
using Larnix.Core.Binary;
using Larnix.Core.Utils;
using Larnix.Socket.Packets;
using Larnix.Worldgen;
using Larnix.Core;

namespace Larnix.Packets
{
    public sealed class FrameInfo : Payload
    {
        private const int SIZE = sizeof(long) + Col32.SIZE + sizeof(BiomeID) + sizeof(Weather);

        public long ServerTick => Primitives.FromBytes<long>(Bytes, 0);
        public Col32 SkyColor => Structures.FromBytes<Col32>(Bytes, 8);
        public BiomeID BiomeID => Primitives.FromBytes<BiomeID>(Bytes, 12);
        public Weather Weather => Primitives.FromBytes<Weather>(Bytes, 14);

        public FrameInfo() { }
        public FrameInfo(long serverTick, Col32 skyColor, BiomeID biomeID, Weather weather, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(serverTick),
                Structures.GetBytes(skyColor),
                Primitives.GetBytes(biomeID),
                Primitives.GetBytes(weather)
            ), code);
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE;
        }
    }
}
