#nullable enable
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Database.Connection;

namespace Larnix.Model.Database;

public interface IChunkAccess
{
    void SetChunk(int x, int y, ChunkData chunk);
    bool TryGetChunk(int x, int y, out ChunkData? chunk);
}

internal class ChunkAccess : IChunkAccess
{
    private readonly IDbHandle _db;
    public ChunkAccess(IDbHandle db) => _db = db;

    public void SetChunk(int x, int y, ChunkData chunk)
    {
        string cmd = @"
            INSERT OR REPLACE INTO chunks
                (chunk_x, chunk_y, block_bytes, nbt)
                VALUES ($p1, $p2, $p3, $p4);
        ";

        _db.Execute(cmd, x, y, chunk.Serialize(), chunk.ExportData());
    }

    public bool TryGetChunk(int x, int y, out ChunkData? chunk)
    {
        string cmd = @"
            SELECT block_bytes, nbt
                FROM chunks
                WHERE chunk_x = $p1 AND chunk_y = $p2;
        ";

        DbRecord? record = _db.QuerySingle(cmd, x, y);
        if (record is not null)
        {
            byte[]? bytes = record.Get<byte[]>("block_bytes");
            string? nbt = record.Get<string>("nbt");

            if (bytes is not null && nbt is not null)
            {
                chunk = ChunkData.Deserialize(bytes);
                chunk.ImportData(nbt);
                return true;
            }
        }

        chunk = default;
        return false;
    }
}
