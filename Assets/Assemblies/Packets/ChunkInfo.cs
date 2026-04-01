#nullable enable
using Larnix.Core.Vectors;
using Larnix.GameCore.Utils;
using Larnix.Blocks.Structs;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;
using Larnix.Core.Misc;
using Larnix.GameCore.Structs;
using System;

namespace Larnix.Packets;

public sealed class ChunkInfo : Payload
{
    private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
    private const int MIN_SIZE = Vec2Int.SIZE;
    private const int MAX_SIZE = Vec2Int.SIZE + CHUNK_SIZE * CHUNK_SIZE * BlockHeader2.SIZE;

    public Vec2Int Chunkpos => Structures.FromBytes<Vec2Int>(Bytes, 0); // Vec2Int.SIZE
    public ChunkView? Chunk => Bytes.Length != MIN_SIZE ?
        ChunkView.Deserialize(Bytes, Vec2Int.SIZE) : null; // 0B - 1280B

    /// <summary>
    /// Chunk load / unload packet constructor.
    /// </summary>
    public ChunkInfo(Vec2Int chunkpos, ChunkView? chunk, byte code = 0)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Structures.GetBytes(chunkpos),
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
