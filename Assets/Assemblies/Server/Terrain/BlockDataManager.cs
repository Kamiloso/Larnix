using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using System.Linq;
using Larnix.Server.Data;
using Larnix.Worldgen;
using Larnix.Server.References;

namespace Larnix.Server.Terrain
{
    internal class BlockDataManager : ServerSingleton
    {
        private readonly Dictionary<Vector2Int, BlockData2[,]> ChunkCache = new();
        private readonly HashSet<Vector2Int> ReferencedChunks = new();

        private bool DebugUnlinkDatabase = false;

        public BlockDataManager(Server server) : base(server) {}

        /// <summary>
        /// Modify this reference during FixedUpdate time and it will automatically update in this script.
        /// Don't forget to DisableChunkReference(...) when unloading chunk!
        /// </summary>
        public BlockData2[,] GetChunkReference(Vector2Int chunk)
        {
            if (ReferencedChunks.Contains(chunk))
                throw new System.InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

            ReferencedChunks.Add(chunk);

            if (ChunkCache.ContainsKey(chunk)) // Get from cache
            {
                return ChunkCache[chunk];
            }
            else if(!DebugUnlinkDatabase && Ref<Database>().TryGetChunk(chunk.x, chunk.y, out byte[] bytes)) // Get from database
            {
                //BlockData2[,] blocks = BytesToChunk(bytes);
                BlockData2[,] blocks = ChunkMethods.DeserializeChunk(bytes);
                ChunkCache[chunk] = blocks;
                return blocks;
            }
            else // Generate a new chunk
            {
                BlockData2[,] blocks = Ref<Generator>().GenerateChunk(chunk);
                ChunkCache[chunk] = blocks;
                return blocks;
            }
        }

        public void DisableChunkReference(Vector2Int chunk)
        {
            if (!ReferencedChunks.Contains(chunk))
                throw new System.InvalidOperationException($"Reference to chunk {chunk} is not marked as active!");

            ReferencedChunks.Remove(chunk);
        }

        public void FlushIntoDatabase()
        {
            if (DebugUnlinkDatabase)
                return;

            foreach(var vkp in ChunkCache.ToList())
            {
                Vector2Int chunk = vkp.Key;
                BlockData2[,] data = vkp.Value;

                // Flush data

                byte[] bytes = ChunkMethods.SerializeChunk(data);
                Ref<Database>().SetChunk(chunk.x, chunk.y, bytes);

                // Remove disabled chunks from cache

                if (!ReferencedChunks.Contains(chunk))
                    ChunkCache.Remove(chunk);
            }
        }
    }
}
