using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using System.Linq;
using Larnix.Server.Data;
using Larnix.Worldgen;
using Larnix.Core.Vectors;
using System;

namespace Larnix.Server.Terrain
{
    internal class BlockDataManager : Singleton
    {
        private readonly Dictionary<Vec2Int, BlockData2[,]> _chunkCache = new();
        private readonly HashSet<Vec2Int> _referencedChunks = new();

        private Database Database => Ref<Database>();
        private Generator Generator => Ref<Generator>();

        private bool _debugUnlinkDatabase = false;

        public BlockDataManager(Server server) : base(server) {}

        /// <summary>
        /// Modify this reference during FixedUpdate time and it will automatically update in this script.
        /// Don't forget to DisableChunkReference(...) when unloading chunk!
        /// </summary>
        public BlockData2[,] ObtainChunkReference(Vec2Int chunk)
        {
            if (_referencedChunks.Contains(chunk))
                throw new InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

            _referencedChunks.Add(chunk);

            BlockData2[,] blocks;

            if (_chunkCache.ContainsKey(chunk)) // Get from cache
            {
                blocks = _chunkCache[chunk];
                return blocks;
            }
            else if(!_debugUnlinkDatabase && Database.TryGetChunk(chunk.x, chunk.y, out blocks)) // Get from database
            {
                _chunkCache[chunk] = blocks;
                return blocks;
            }
            else // Generate a new chunk
            {
                blocks = Generator.GenerateChunk(chunk);
                _chunkCache[chunk] = blocks;
                return blocks;
            }
        }

        public void ReturnChunkReference(Vec2Int chunk)
        {
            if (!_referencedChunks.Contains(chunk))
                throw new InvalidOperationException($"Reference to chunk {chunk} is not marked as active!");

            _referencedChunks.Remove(chunk);
        }

        public void FlushIntoDatabase()
        {
            if (!_debugUnlinkDatabase)
            {
                foreach(var kvp in _chunkCache.ToList())
                {
                    Vec2Int chunk = kvp.Key;
                    BlockData2[,] data = kvp.Value;

                    // flush data
                    Database.SetChunk(chunk.x, chunk.y, data);

                    // remove disabled chunks from cache
                    if (!_referencedChunks.Contains(chunk))
                        _chunkCache.Remove(chunk);
                }
            }
        }
    }
}
