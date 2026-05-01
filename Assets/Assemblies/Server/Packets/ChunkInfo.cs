#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using System;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x03)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ChunkInfo : ISanitizable<ChunkInfo>
{
    public Vec2Int Chunk { get; }

    private readonly byte _isLoad;
    public bool IsLoad => _isLoad != 0;

    private FixedBuffer1024<byte> Buffer1 { get; }
    private FixedBuffer256<byte> Buffer2 { get; }

    public byte[] ChunkBytes() => ArrayUtils.MegaConcat(
        Buffer1.ToArray(),
        Buffer2.ToArray()
        );

    private ChunkInfo(Vec2Int chunk, bool isLoad, in FixedBuffer1024<byte> buffer1, in FixedBuffer256<byte> buffer2)
    {
        Chunk = BlockUtils.ChunkInWorld(chunk) ? chunk : Vec2Int.Zero;
        _isLoad = (byte)(isLoad ? 1 : 0);
        Buffer1 = buffer1;
        Buffer2 = buffer2;
    }

    public static ChunkInfo MakeLoadInfo(Vec2Int chunk, byte[] chunkBytes)
    {
        if (chunkBytes.Length > 1280)
            throw new ArgumentException($"Chunk bytes cannot exceed {1280} bytes.");

        FixedBuffer1024<byte> buffer1 = new();
        for (int ptr = 0; ptr < 1024 && ptr < chunkBytes.Length; ptr++)
        {
            buffer1.Add(chunkBytes[ptr]);
        }

        FixedBuffer256<byte> buffer2 = new();
        for (int ptr = 1024; ptr < 1280 && ptr < chunkBytes.Length; ptr++)
        {
            buffer2.Add(chunkBytes[ptr]);
        }

        return new ChunkInfo(chunk, true, buffer1, buffer2);
    }

    public static ChunkInfo MakeUnloadInfo(Vec2Int chunk)
    {
        return new ChunkInfo(
            chunk, false,
            new FixedBuffer1024<byte>(),
            new FixedBuffer256<byte>()
            );
    }

    public ChunkInfo Sanitize()
    {
        return new ChunkInfo(Chunk, IsLoad, Buffer1, Buffer2);
    }
}
