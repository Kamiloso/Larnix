using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;
using System.Linq;

namespace Larnix.Server.Terrain
{
    public class BlockServer
    {
        public readonly Vector2Int Position;
        public readonly bool IsFront;
        public readonly SingleBlockData BlockData;

        public bool MarkedToUpdate { get; set; } = false;

        public BlockServer(Vector2Int position, SingleBlockData blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData;
            IsFront = isFront;
        }

        public void DoFrameUpdate()
        {
            FrameUpdate();
            MarkedToUpdate = false;
        }

        public void DoOnBreak()
        {
            OnBreak();
        }

        protected virtual void FrameUpdate() { }
        protected virtual void OnBreak() { }

        // ===== Static Methods =====

        public static BlockServer ConstructBlockObject(Vector2Int POS, SingleBlockData block, bool isFront)
        {
            string blockName = block.ID.ToString();
            string className = "Larnix.Modules.Blocks." + blockName;

            Type type = Type.GetType(className);
            if (
                type == null ||
                !typeof(BlockServer).IsAssignableFrom(type) ||
                type.GetConstructor(new Type[] { typeof(Vector2Int), typeof(SingleBlockData), typeof(bool) }) == null
                )
            {
                type = typeof(BlockServer);
                UnityEngine.Debug.LogWarning($"Class {className} cannot be loaded! Loading base class instead...");
            }

            object instance = Activator.CreateInstance(type, POS, block, isFront);
            return instance as BlockServer;
        }

        public static BlockServer GetBlock(Vector2Int POS, bool isFront) // can return null if chunk is not loaded!
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);

            if (!References.ChunkLoading.TryGetChunk(chunk, out var chunkObject))
                return null;

            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);
            return chunkObject.GetBlock(pos, isFront);
        }

        public static BlockServer SetBlock(Vector2Int POS, bool isFront, SingleBlockData blockData) // can return null if chunk is not loaded!
        {
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);

            if (!References.ChunkLoading.TryGetChunk(chunk, out var chunkObject))
                return null;

            Vector2Int pos = ChunkMethods.LocalBlockCoords(POS);
            return chunkObject.SetBlock(pos, isFront, blockData);
        }
    }
}
