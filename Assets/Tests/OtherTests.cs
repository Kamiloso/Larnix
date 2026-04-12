#nullable enable
using Larnix.Socket.Payload;
using NUnit.Framework;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;
using Larnix.Socket.Security.Keys;
using System.Linq;
using System;
using Larnix.Core.Serialization;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Blocks;
using Larnix.Socket.Payload;

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

public class OtherTests
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

        byte[] bytes = NetworkSerializer.ToBytes(header, cmd, DummyKey.Instance);

        bool successHeader = NetworkSerializer.TryPlainHeaderFromBytes(bytes, out PayloadHeader outHeader);
        Assert.IsTrue(successHeader);
        Assert.AreEqual(header, outHeader);

        bool successDecrypt = NetworkSerializer.TryDecryptNetworkBytes(bytes, DummyKey.Instance, out byte[]? decrypted);
        Assert.IsTrue(successDecrypt);

        bool successPayload = NetworkSerializer.TryDecryptedBytesAs(decrypted!, out PayloadHeader outHeader2, out TestCommand1 outCmd);
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
            NetworkSerializer.ToBytes(header, cmd, DummyKey.Instance);
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
            NetworkSerializer.ToBytes(header, cmd, DummyKey.Instance);
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

    [Test]
    public void Structs_ComparesDefinedStructsAsExpected()
    {
        static void Compare<T>(Func<T> ctor1, Func<T> ctor2) where T : unmanaged
        {
            T o1 = ctor1();
            T o2 = ctor1();
            T o3 = ctor2();

            Assert.AreEqual(o1, o2);
            Assert.AreNotEqual(o1, o3);
        }

        Compare<Vec2Int>(() => new(1, 2), () => new(3, 4));
        Compare<BlockHeader1>(() => new(BlockID.Stone), () => new(BlockID.Stone, 1));
        Compare<BlockHeader2>(() => new(new(BlockID.Stone), new()), () => new(new(), new(BlockID.Sand)));
    }

    [Test]
    public void FixedBuffer_WrongInternalsComparisons()
    {
        byte[] data1 = new byte[] { /* Size */ 2, 0, /* Contents */ 3, 4, 5, 6, 0, 0, 0, 0 };
        byte[] data2 = new byte[] { /* Size */ 2, 0, /* Contents */ 3, 4, 5, 6, 7, 8, 9, 10 };

        FixedBuffer8<short> buffer1 = Binary<FixedBuffer8<short>>.Deserialize(data1);
        FixedBuffer8<short> buffer2 = Binary<FixedBuffer8<short>>.Deserialize(data2);

        Assert.That(buffer1.Count == 2);
        Assert.That(buffer2.Count == 2);

        Assert.That(buffer1.Capacity == 4);
        Assert.That(buffer2.Capacity == 4);

        Assert.AreEqual(buffer1, buffer2);
    }
}
