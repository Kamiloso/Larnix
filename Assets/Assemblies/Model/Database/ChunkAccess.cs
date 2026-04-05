#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Database.Connection;

namespace Larnix.Model.Database;

public interface IChunkAccess
{
    void SetChunk(Vec2Int chunk, ChunkData chunkData);
    bool TryGetChunk(Vec2Int chunk, out ChunkData? chunkData);
}

internal class ChunkAccess : IChunkAccess
{
    private readonly IDbHandle _db;
    public ChunkAccess(IDbHandle db) => _db = db;

    public void SetChunk(Vec2Int chunk, ChunkData chunkData)
    {
        string cmd = @"
            INSERT OR REPLACE INTO chunks
                (chunk_x, chunk_y, block_bytes, nbt)
                VALUES ($p1, $p2, $p3, $p4);
        ";

        _db.Execute(cmd, chunk.x, chunk.y, chunkData.Serialize(), chunkData.ExportData());
    }

    public bool TryGetChunk(Vec2Int chunk, out ChunkData? chunkData)
    {
        string cmd = @"
            SELECT block_bytes, nbt
                FROM chunks
                WHERE chunk_x = $p1 AND chunk_y = $p2;
        ";

        DbRecord? record = _db.QuerySingle(cmd, chunk.x, chunk.y);
        if (record is not null)
        {
            byte[]? bytes = record.Get<byte[]>("block_bytes");
            string? nbt = record.Get<string>("nbt");

            if (bytes is not null && nbt is not null)
            {
                chunkData = ChunkData.Deserialize(bytes);
                chunkData.ImportData(nbt);
                return true;
            }
        }

        chunkData = default;
        return false;
    }
}
