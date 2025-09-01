using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using Unity.VisualScripting;
using Larnix.Server.Worldgen;
using System;

namespace Larnix.Server.Terrain
{
    public class BlockDataManager : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, BlockData[,]> ChunkCache = new();
        private readonly HashSet<Vector2Int> ReferencedChunks = new();

        private bool DebugUnlinkDatabase
        {
            get => Client.References.Debug != null && Client.References.Debug.UnlinkTerrainData;
        }

        private void Awake()
        {
            References.BlockDataManager = this;
        }

        /// <summary>
        /// Modify this reference during FixedUpdate time and it will automatically update in this script.
        /// Don't forget to DisableChunkReference(...) when unloading chunk!
        /// </summary>
        public BlockData[,] GetChunkReference(Vector2Int chunk)
        {
            if (ReferencedChunks.Contains(chunk))
                throw new System.InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

            ReferencedChunks.Add(chunk);

            if (ChunkCache.ContainsKey(chunk)) // Get from cache
            {
                return ChunkCache[chunk];
            }
            else if(!DebugUnlinkDatabase && References.Server.Database.TryGetChunk(chunk.x, chunk.y, out byte[] bytes)) // Get from database
            {
                BlockData[,] blocks = BytesToChunk(bytes);
                ChunkCache[chunk] = blocks;
                return blocks;
            }
            else // Generate a new chunk
            {
                BlockData[,] blocks = References.Generator.GenerateChunk(chunk);
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

            foreach(var vkp in ChunkCache.ToHashSet())
            {
                Vector2Int chunk = vkp.Key;
                BlockData[,] data = vkp.Value;

                // Flush data

                byte[] bytes = ChunkToBytes(data);
                References.Server.Database.SetChunk(chunk.x, chunk.y, bytes);

                // Remove disabled chunks from cache

                if (!ReferencedChunks.Contains(chunk))
                    ChunkCache.Remove(chunk);
            }
        }

        private BlockData[,] BytesToChunk(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 5 * 16 * 16)
                throw new ArgumentException("Wrong bytes size!");

            BlockData[,] returns = new BlockData[16, 16];

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    int ind = 5 * (x * 16 + y);
                    BlockData block = BlockData.Deserialize(bytes, ind);
                    returns[x, y] = block;
                }

            return returns;
        }

        private byte[] ChunkToBytes(BlockData[,] Chunk)
        {
            if (Chunk.GetLength(0) != 16 || Chunk.GetLength(1) != 16)
                throw new ArgumentException("Wrong array size!");

            byte[] bytes = new byte[5 * 16 * 16];

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    int ind_s = 5 * (x * 16 + y);

                    BlockData block = Chunk[x, y];
                    if (block == null || block.Front == null || block.Back == null)
                        throw new System.ArgumentException("Chunk array cannot have null places!");

                    Buffer.BlockCopy(block.Serialize(), 0, bytes, ind_s, 5);
                }

            return bytes;
        }
    }
}
