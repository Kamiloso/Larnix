#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Larnix.Core.Serialization;

public enum TestState : byte { None = 0, Active = 1 }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Vector3(float X, float Y, float Z);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerData(int Id, TestState State);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct HardwareState
{
    public long Uptime;
    public fixed byte Mac[6];
}

public struct InvalidLayoutStruct { public int Id; }
[StructLayout(LayoutKind.Explicit)] public struct ExplicitStruct { [FieldOffset(0)] public int Id; }
[StructLayout(LayoutKind.Sequential, Pack = 2)] public struct BadPackStruct { public byte A; public short B; }
[StructLayout(LayoutKind.Sequential, Pack = 1)] public struct EmptyStruct { }
[StructLayout(LayoutKind.Sequential, Pack = 1)] public struct StructWithBool { public bool Flag; }
[StructLayout(LayoutKind.Sequential, Pack = 1)] public struct StructWithDecimal { public decimal Val; }

[TestFixture]
public class BinaryTests
{
    [Test]
    public void SupportedTypes_InitializeSuccessfully()
    {
        Assert.That(Binary<int>.Size, Is.GreaterThan(0));
        Assert.That(Binary<Vector3>.Size, Is.GreaterThan(0));
        Assert.That(Binary<PlayerData>.Size, Is.GreaterThan(0));
        Assert.That(Binary<HardwareState>.Size, Is.GreaterThan(0));
    }

    [Test]
    public void UnsupportedTypes_ThrowTypeInitializationException()
    {
        static void AssertUnsupported<T>() where T : unmanaged
        {
            var ex = Assert.Throws<TypeInitializationException>(() => _ = Binary<T>.Size);
            Assert.That(ex?.InnerException, Is.InstanceOf<NotSupportedException>(),
                $"Type {typeof(T).Name} did not throw the expected NotSupportedException.");
        }

        AssertUnsupported<bool>();
        AssertUnsupported<decimal>();
        AssertUnsupported<InvalidLayoutStruct>();
        AssertUnsupported<ExplicitStruct>();
        AssertUnsupported<BadPackStruct>();
        AssertUnsupported<EmptyStruct>();
        AssertUnsupported<StructWithBool>();
        AssertUnsupported<StructWithDecimal>();
    }

    [Test]
    public void SerializeDeserialize_RecordStruct_MatchesOriginal()
    {
        var original = new Vector3(1.5f, -2.0f, 3.14f);

        var bytes = Binary<Vector3>.Serialize(in original);
        var deserialized = Binary<Vector3>.Deserialize(bytes);

        Assert.That(bytes, Has.Length.EqualTo(Binary<Vector3>.Size));
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void SerializeDeserialize_ReadonlyRecordStruct_MatchesOriginal()
    {
        var original = new PlayerData(42, TestState.Active);

        var bytes = Binary<PlayerData>.Serialize(in original);
        var deserialized = Binary<PlayerData>.Deserialize(bytes);

        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public unsafe void SerializeDeserialize_FixedBuffer_MatchesOriginal()
    {
        var original = new HardwareState { Uptime = 999 };
        original.Mac[0] = 0xFF;
        original.Mac[5] = 0xAA;

        var bytes = Binary<HardwareState>.Serialize(in original);
        var deserialized = Binary<HardwareState>.Deserialize(bytes);

        Assert.That(deserialized.Uptime, Is.EqualTo(original.Uptime));
        Assert.That(deserialized.Mac[0], Is.EqualTo(0xFF));
        Assert.That(deserialized.Mac[5], Is.EqualTo(0xAA));
    }

    [Test]
    public void SerializeDeserializeArray_MatchesOriginal()
    {
        var original = new[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };

        var bytes = Binary<Vector3>.SerializeArray(original);
        var deserialized = Binary<Vector3>.DeserializeArray(bytes, original.Length);

        Assert.That(deserialized, Has.Length.EqualTo(2));
        Assert.That(deserialized, Is.EquivalentTo(original));
    }

    [Test]
    public void Deserialize_ValidOffset_ExtractsCorrectly()
    {
        var combined = Binary<Vector3>.Serialize(new Vector3(1, 1, 1))
            .Concat(Binary<Vector3>.Serialize(new Vector3(2, 2, 2)))
            .ToArray();

        var result = Binary<Vector3>.Deserialize(combined, Binary<Vector3>.Size);

        Assert.That(result, Is.EqualTo(new Vector3(2, 2, 2)));
    }

    [TestCase(-1)]
    [TestCase(1)]
    public void Deserialize_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset)
    {
        var bytes = new byte[Binary<int>.Size];
        Assert.Throws<ArgumentOutOfRangeException>(() => Binary<int>.Deserialize(bytes, offset));
    }

    [Test]
    public void DeserializeArray_ValidOffset_ExtractsSubset()
    {
        var bytes = Binary<int>.SerializeArray(new[] { 10, 20, 30, 40 });

        var result = Binary<int>.DeserializeArray(bytes, 2, Binary<int>.Size * 1);

        Assert.That(result, Is.EquivalentTo(new[] { 20, 30 }));
    }

    [TestCase(-1, 0)]
    [TestCase(10, 0)]
    [TestCase(2, -1)]
    [TestCase(2, 20)]
    public void DeserializeArray_InvalidParams_ThrowsArgumentOutOfRangeException(int count, int offset)
    {
        var bytes = new byte[16]; // 4 ints
        Assert.Throws<ArgumentOutOfRangeException>(() => Binary<int>.DeserializeArray(bytes, count, offset));
    }
}
