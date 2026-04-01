#nullable enable
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using System.Linq;
using Larnix.Worldgen;
using Larnix.Core.Vectors;
using System;
using Larnix.Core;

namespace Larnix.Server.Data;

internal class BlockDataManager
{
    private readonly Dictionary<Vec2Int, ChunkData> _chunkCache = new();
    private readonly HashSet<Vec2Int> _referencedChunks = new();

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private Generator Generator => GlobRef.Get<Generator>();

    private static readonly bool debugUnlinkDatabase = false;

    /// <summary>
    /// Modify this reference during FixedUpdate time and it will automatically update in this script.
    /// Don't forget to DisableChunkReference(...) when unloading chunk!
    /// </summary>
    public ChunkData ObtainChunkReference(Vec2Int chunk)
    {
        if (_referencedChunks.Contains(chunk))
            throw new InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

        _referencedChunks.Add(chunk);

        ChunkData? blocks;

        if (_chunkCache.ContainsKey(chunk)) // Get from cache
        {
            blocks = _chunkCache[chunk];
            return blocks;
        }
        else if(!debugUnlinkDatabase && Db.Chunks.TryGetChunk(chunk.x, chunk.y, out blocks)) // Get from database
        {
            _chunkCache[chunk] = blocks!;
            return blocks!;
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
        if (debugUnlinkDatabase) return;

        Db.Handle.AsTransaction(() =>
        {
            foreach (var kvp in _chunkCache.ToList())
            {
                Vec2Int chunk = kvp.Key;
                ChunkData data = kvp.Value;

                // flush data
                Db.Chunks.SetChunk(chunk.x, chunk.y, data);

                // remove disabled chunks from cache
                if (!_referencedChunks.Contains(chunk))
                {
                    _chunkCache.Remove(chunk);
                }
            }
        });
    }
}
