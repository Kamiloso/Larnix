#nullable enable
using Larnix.Socket.Packets.Payload;
using NUnit.Framework;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;
using Larnix.Socket.Security.Keys;
using System.Linq;
using System;
using Larnix.Core.Serialization;

[CmdId(1)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct TestCommand1(
    Vec2 Position,
    Vec2 Velocity,
    int Health
    );

[CmdId(2)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct TestCommand2(
    Vec2 Position,
    Vec2 Velocity,
    bool IsAlive // wrong field
    );

[CmdId(3)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TestCommand3
{
    private readonly ushort _sthElse;
    private fixed int _buffer[4];

    public TestCommand3(ushort sthElse, ReadOnlySpan<int> buffer)
    {
        _sthElse = sthElse;

        fixed (int* ptr = _buffer)
        {
            buffer.Slice(0, Math.Min(buffer.Length, 4)).CopyTo(new Span<int>(ptr, 4));
        }
    }
}

internal class DummyKey : IEncryptionKey
{
    public static DummyKey Instance { get; } = new();
    public byte[] Decrypt(byte[] ciphertext) => ciphertext[..].Reverse().ToArray();
    public byte[] Encrypt(byte[] plaintext) => plaintext[..].Reverse().ToArray();
}

public class PayloadTests
{
    [Test]
    public void StringStructs_Equality()
    {
        FixedString32 str1 = new("Hello, World!");
        FixedString32 str2 = new("Hello, World!");
        FixedString32 str3 = new("Hello and Goodbye!"); // must have the same prefix as previous

        Assert.AreEqual(str1, str2);
        Assert.AreNotEqual(str1, str3);
    }

    [Test]
    public void CommandSerialization_RoundTrip()
    {
        PayloadHeader header = new(1, 2, 0xF5);

        TestCommand1 cmd = new()
        {
            Position = new Vec2(1, 2),
            Velocity = new Vec2(3, 4),
            Health = 100
        };

        byte[] bytes = NetworkSerialization.ToBytes(header, cmd, DummyKey.Instance);

        bool successHeader = NetworkSerialization.TryPlainHeaderFromBytes(bytes, out PayloadHeader outHeader);
        Assert.IsTrue(successHeader);
        Assert.AreEqual(header, outHeader);

        bool successDecrypt = NetworkSerialization.TryDecryptNetworkBytes(bytes, DummyKey.Instance, out byte[]? decrypted);
        Assert.IsTrue(successDecrypt);

        bool successPayload = NetworkSerialization.TryDecryptedBytesAs(decrypted!, out PayloadHeader outHeader2, out TestCommand1 outCmd);
        Assert.IsTrue(successPayload);
        Assert.AreEqual(header, outHeader2);
        Assert.AreEqual(cmd, outCmd);
    }

    [Test]
    public void CommandSerialization_FailsToAcceptBool()
    {
        PayloadHeader header = new(1, 2, 0xF5);
        TestCommand2 cmd = new()
        {
            Position = new Vec2(1, 2),
            Velocity = new Vec2(3, 4),
            IsAlive = true
        };

        Assert.Throws<TypeInitializationException>(() =>
        {
            NetworkSerialization.ToBytes(header, cmd, DummyKey.Instance);
        });
    }

    [Test]
    public void CommandSerialization_AcceptsFixedBuffers()
    {
        PayloadHeader header = new(3, 4, 0xA1);

        int[] bufferData = new int[] { 10, 20, 30, 40 };
        TestCommand3 cmd = new(12345, bufferData);

        Assert.DoesNotThrow(() =>
        {
            NetworkSerialization.ToBytes(header, cmd, DummyKey.Instance);
        });
    }

    [Test]
    public void EndCompressor_WorksAsExpected()
    {
        byte[] data = new byte[] { 1, 2, 3, 4, 5, 0, 0, 0, 0 };
        byte[] compressed = EndCompressor.Compress(data);

        ushort sizeStatement = Binary<ushort>.Deserialize(compressed[^2..]);
        Assert.AreEqual(4, sizeStatement);

        byte[] decompressed = EndCompressor.Decompress(compressed);
        Assert.AreEqual(data, decompressed);
    }

    [Test]
    public void EndCompressor_WorksWhenExceedingUShortLimit()
    {
        byte[] data = new byte[70000];

        data[1] = 123;
        data[2] = 124;
        data[3] = 125;

        byte[] compressed = EndCompressor.Compress(data);

        ushort sizeStatement = Binary<ushort>.Deserialize(compressed[^2..]);
        Assert.AreEqual(ushort.MaxValue, sizeStatement);

        byte[] decompressed = EndCompressor.Decompress(compressed);
        Assert.AreEqual(data, decompressed);
    }
}
