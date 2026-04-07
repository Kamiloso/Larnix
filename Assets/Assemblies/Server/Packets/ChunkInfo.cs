#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using Larnix.Socket.Packets;
using System;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Server.Packets;

public sealed class ChunkInfo : Payload
{
    private static int CHUNK_SIZE => BlockUtils.CHUNK_SIZE;
    private static int MIN_SIZE => Binary<Vec2Int>.Size;
    private static int MAX_SIZE => Binary<Vec2Int>.Size + CHUNK_SIZE * CHUNK_SIZE * Binary<BlockHeader2>.Size;

    public Vec2Int Chunkpos => Binary<Vec2Int>.Deserialize(Bytes, 0);
    public ChunkView? Chunk => Bytes.Length != MIN_SIZE ?
        ChunkView.Deserialize(Bytes, Binary<Vec2Int>.Size) : null; // 0B - 1280B

    /// <summary>
    /// Chunk load / unload packet constructor. Unload when chunk is null, load otherwise.
    /// </summary>
    public ChunkInfo(Vec2Int chunkpos, ChunkView? chunk, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<Vec2Int>.Serialize(chunkpos),
            chunk?.Serialize() ?? Array.Empty<byte>()
            ), code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length >= MIN_SIZE && Bytes.Length <= MAX_SIZE &&
            Chunkpos.x >= BlockUtils.MIN_CHUNK && Chunkpos.x <= BlockUtils.MAX_CHUNK &&
            Chunkpos.y >= BlockUtils.MIN_CHUNK && Chunkpos.y <= BlockUtils.MAX_CHUNK;
    }
}
