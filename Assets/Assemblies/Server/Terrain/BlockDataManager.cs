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
        private readonly Dictionary<Vector2Int, BlockData2[,]> chunkCache = new();
        private readonly HashSet<Vector2Int> referencedChunks = new();
        private bool DEBUG_UNLINK_DATABASE = false;

        public BlockDataManager(Server server) : base(server) {}

        /// <summary>
        /// Modify this reference during FixedUpdate time and it will automatically update in this script.
        /// Don't forget to DisableChunkReference(...) when unloading chunk!
        /// </summary>
        public BlockData2[,] GetChunkReference(Vector2Int chunk)
        {
            if (referencedChunks.Contains(chunk))
                throw new System.InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

            referencedChunks.Add(chunk);

            if (chunkCache.ContainsKey(chunk)) // Get from cache
            {
                return chunkCache[chunk];
            }
            else if(!DEBUG_UNLINK_DATABASE && Ref<Database>().TryGetChunk(chunk.x, chunk.y, out byte[] bytes)) // Get from database
            {
                //BlockData2[,] blocks = BytesToChunk(bytes);
                BlockData2[,] blocks = ChunkMethods.DeserializeChunk(bytes);
                chunkCache[chunk] = blocks;
                return blocks;
            }
            else // Generate a new chunk
            {
                BlockData2[,] blocks = Ref<Generator>().GenerateChunk(chunk);
                chunkCache[chunk] = blocks;
                return blocks;
            }
        }

        public void DisableChunkReference(Vector2Int chunk)
        {
            if (!referencedChunks.Contains(chunk))
                throw new System.InvalidOperationException($"Reference to chunk {chunk} is not marked as active!");

            referencedChunks.Remove(chunk);
        }

        public void FlushIntoDatabase()
        {
            if (DEBUG_UNLINK_DATABASE)
                return;

            foreach(var vkp in chunkCache.ToList())
            {
                Vector2Int chunk = vkp.Key;
                BlockData2[,] data = vkp.Value;

                // Flush data

                byte[] bytes = ChunkMethods.SerializeChunk(data);
                Ref<Database>().SetChunk(chunk.x, chunk.y, bytes);

                // Remove disabled chunks from cache

                if (!referencedChunks.Contains(chunk))
                    chunkCache.Remove(chunk);
            }
        }
    }
}
