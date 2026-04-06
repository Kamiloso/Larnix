#nullable enable
using System.Collections.Generic;
using Larnix.Model.Blocks.Structs;
using System.Linq;
using Larnix.Model.Worldgen;
using Larnix.Core.Vectors;
using System;
using Larnix.Core;
using Larnix.Model.Database;
using Larnix.Server.Data;

namespace Larnix.Server.Chunks.Data;

internal interface IChunkRepository
{
    /// <summary>
    /// Modify this reference during FixedUpdate time and it will automatically update in this script.
    /// Don't forget to DisableChunkReference(...) when unloading chunk!
    /// </summary>
    ChunkData TakeActiveChunk(Vec2Int chunk);
    void ReturnActiveChunk(Vec2Int chunk);
}

internal class ChunkRepository : IChunkRepository
{
    private static bool DebugUnlinkDatabase => false;

    private readonly Dictionary<Vec2Int, ChunkData> _chunkCache = new();
    private readonly HashSet<Vec2Int> _referencedChunks = new();

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IDataSaver DataSaver => GlobRef.Get<IDataSaver>();
    private IGenerator Generator => GlobRef.Get<IGenerator>();

    public ChunkRepository()
    {
        DataSaver.SavingAll += FlushIntoDatabase;
    }

    public ChunkData TakeActiveChunk(Vec2Int chunk)
    {
        if (_referencedChunks.Contains(chunk))
            throw new InvalidOperationException($"Cannot get more than one reference to chunk {chunk}!");

        _referencedChunks.Add(chunk);

        ChunkData? blocks;

        // Get from cache
        if (_chunkCache.ContainsKey(chunk))
        {
            blocks = _chunkCache[chunk];
            return blocks;
        }

        // Get from database
        if (!DebugUnlinkDatabase && Db.Chunks.TryGetChunk(chunk, out blocks))
        {
            _chunkCache[chunk] = blocks!;
            return blocks!;
        }

        // Generate new chunk
        blocks = Generator.GenerateChunk(chunk);
        _chunkCache[chunk] = blocks;
        return blocks;
    }

    public void ReturnActiveChunk(Vec2Int chunk)
    {
        if (!_referencedChunks.Contains(chunk))
            throw new InvalidOperationException($"Reference to chunk {chunk} is not marked as active!");

        _referencedChunks.Remove(chunk);
    }

    private void FlushIntoDatabase()
    {
        if (DebugUnlinkDatabase) return;

        Db.Handle.AsTransaction(() =>
        {
            foreach (var kvp in _chunkCache.ToList())
            {
                Vec2Int chunk = kvp.Key;
                ChunkData data = kvp.Value;

                Db.Chunks.SetChunk(chunk, data);

                if (!_referencedChunks.Contains(chunk))
                {
                    _chunkCache.Remove(chunk);
                }
            }
        });
    }
}
