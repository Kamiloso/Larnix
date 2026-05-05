#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using System;
using Larnix.Core.Utils;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(3)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ChunkInfo : ISanitizable<ChunkInfo>
{
    public Vec2Int Chunk { get; }
    public boolsrl IsLoad { get; }

    private FixedBuffer1024<byte> Buffer1 { get; }
    private FixedBuffer256<byte> Buffer2 { get; }

    public byte[] ChunkBytes() => ArrayUtils.MegaConcat(
        Buffer1.ToArray(),
        Buffer2.ToArray()
        );

    private ChunkInfo(Vec2Int chunk, boolsrl isLoad, in FixedBuffer1024<byte> buffer1, in FixedBuffer256<byte> buffer2)
    {
        Chunk = LarnixSanitizer.ToWorldChunk(chunk);
        IsLoad = Sanitizer.Filter(isLoad);
        Buffer1 = Sanitizer.Filter(buffer1);
        Buffer2 = Sanitizer.Filter(buffer2);
    }

    public static ChunkInfo MakeLoadInfo(Vec2Int chunk, byte[] chunkBytes)
    {
        if (chunkBytes.Length > 1280)
            throw new ArgumentException($"Chunk bytes cannot exceed size of {1280} bytes.");

        FixedBuffer1024<byte> buffer1 = new();
        buffer1.AddRange(chunkBytes.AsSpan(
            start: 0,
            length: Math.Min(1024, chunkBytes.Length))
            );

        FixedBuffer256<byte> buffer2 = new();
        buffer2.AddRange(chunkBytes.AsSpan(
            start: 1024,
            length: Math.Max(0, chunkBytes.Length - 1024))
            );

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
