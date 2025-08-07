using Larnix.Server.Worldgen;
using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Biomes
{
    public class Arctic : Biome
    {
        public override BlockData TranslateProtoBlock(ProtoBlock protoBlock)
        {
            switch (protoBlock)
            {
                case ProtoBlock.Air:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { }
                    );

                case ProtoBlock.Stone:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Ice },
                    new SingleBlockData { ID = BlockID.Ice }
                    );

                case ProtoBlock.Soil:
                case ProtoBlock.SoilSurface:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Snow },
                    new SingleBlockData { }
                    );

                case ProtoBlock.Cave:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { ID = BlockID.Ice }
                    );

                case ProtoBlock.Liquid:
                    return new BlockData(
                    new SingleBlockData { ID = BlockID.Water },
                    new SingleBlockData { }
                    );

                default:
                    return new BlockData(
                    new SingleBlockData { },
                    new SingleBlockData { }
                    );
            }
        }
    }
}
