using Larnix.Blocks;
using Larnix.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using ResultType = Larnix.Core.ICmdExecutor.CmdResult;
using System;

namespace Larnix.Server.Terrain
{
    internal class WorldAPI : Singleton, IWorldAPI
    {
        private Chunks Chunks => Ref<Chunks>();
        private AtomicChunks AtomicChunks => Ref<AtomicChunks>();
        private Commands Commands => Ref<Commands>();

        public WorldAPI(Server server) : base(server) { }

        public long ServerTick()
        {
            return Ref<Server>().ServerTick;
        }

        public bool IsChunkLoaded(Vec2Int chunk)
        {
            return Chunks.IsChunkLoaded(chunk);
        }

        public bool IsChunkAtomicLoaded(Vec2Int chunk)
        {
            return AtomicChunks.IsAtomicLoaded(chunk);
        }

        public Block GetBlock(Vec2Int POS, bool isFront)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                return chunkObject.GetBlock(pos, isFront);
            }
            return null;
        }

        public Block ReplaceBlock(Vec2Int POS, bool isFront, BlockData1 blockTemplate,
            IWorldAPI.BreakMode breakMode)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                BlockData1 blockDeepCopy = blockTemplate.DeepCopy();
                return chunkObject.UpdateBlock(pos, isFront, blockDeepCopy, breakMode);
            }
            return null;
        }

        public Block MutateBlockVariant(Vec2Int POS, bool isFront, byte variant)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (Chunks.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                GetBlock(POS, isFront).BlockData.__MutateVariant__(variant);
                return chunkObject.UpdateBlockMutated(pos, isFront);
            }
            return null;
        }

        public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item)
        {
            BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
            BlockData1 backBlock = GetBlock(POS, false)?.BlockData;

            if (frontBlock != null && backBlock != null)
            {
                BlockData2 blockPair = new BlockData2(frontBlock, backBlock);
                return BlockInteractions.CanBePlaced(blockPair, item, front);
            }
            return false;
        }

        public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool)
        {
            BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
            BlockData1 backBlock = GetBlock(POS, false)?.BlockData;
            
            if (frontBlock != null && backBlock != null)
            {
                BlockData2 blockPair = new BlockData2(frontBlock, backBlock);
                return BlockInteractions.CanBeBroken(blockPair, item, tool, front);
            }
            return false;
        }

        public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item)
        {
            ReplaceBlock(POS, front, item, IWorldAPI.BreakMode.Effects);
        }

        public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool)
        {
            BlockData1 oldBlock = GetBlock(POS, front)?.BlockData;
            if (oldBlock == null) return;

            // TODO: Drop items code here

            ReplaceBlock(POS, front, new BlockData1(), IWorldAPI.BreakMode.Effects);
        }

        public (ResultType, string) ExecuteCommand(string command, string sender = null)
        {
            return Commands.ExecuteCommand(command, sender);
        }
    }
}
