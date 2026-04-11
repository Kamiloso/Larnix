#nullable enable
using System;
using NUnit.Framework;
using System.Runtime.InteropServices;
using Larnix.Core.Serialization;

public enum TestEnum : byte
{
    None = 0,
    Alpha = 1,
    Beta = 2,
    Gamma = 3
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct TestStruct
{
    public readonly int Id;
    public readonly byte Value;

    public TestStruct(int id, byte value)
    {
        Id = id;
        Value = value;
    }
}

public struct InvalidTestStruct
{
    public int X;
    public double Y;
}

[TestFixture]
public class FixedBufferTests
{
    [Test]
    public void Capacity_CalculatedCorrectly_ForDifferentTypes()
    {
        var intBuffer = new FixedBuffer32<int>(); // 4 bytes
        var enumBuffer = new FixedBuffer32<TestEnum>(); // 1 byte
        var structBuffer = new FixedBuffer32<TestStruct>(); // 5 bytes (4 + 1)

        Assert.That(intBuffer.Capacity, Is.EqualTo(8));  // 32 / 4
        Assert.That(enumBuffer.Capacity, Is.EqualTo(32)); // 32 / 1
        Assert.That(structBuffer.Capacity, Is.EqualTo(6)); // 32 / 5
    }

    [Test]
    public void PushAndToArray_WorksForPrimitives()
    {
        var buffer = new FixedBuffer32<int>();

        buffer.Push(10);
        buffer.Push(20);
        buffer.Push(30);

        Assert.That(buffer.Count, Is.EqualTo(3));
        Assert.That(buffer.IsFull, Is.False);

        int[] result = buffer.ToArray();
        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30 }));
    }

    [Test]
    public void PushAndToArray_WorksForEnums()
    {
        var buffer = new FixedBuffer32<TestEnum>();

        buffer.Push(TestEnum.Alpha);
        buffer.Push(TestEnum.Gamma);

        Assert.That(buffer.Count, Is.EqualTo(2));

        TestEnum[] result = buffer.ToArray();
        Assert.That(result, Is.EqualTo(new[] { TestEnum.Alpha, TestEnum.Gamma }));
    }

    [Test]
    public void PushAndToArray_WorksForUnmanagedStructs()
    {
        var buffer = new FixedBuffer32<TestStruct>();
        var s1 = new TestStruct(1, 255);
        var s2 = new TestStruct(2, 128);

        buffer.Push(s1);
        buffer.Push(s2);

        Assert.That(buffer.Count, Is.EqualTo(2));

        TestStruct[] result = buffer.ToArray();
        Assert.That(result, Is.EqualTo(new[] { s1, s2 }));
    }

    [Test]
    public void Push_WhenFull_ThrowsInvalidOperationException()
    {
        var buffer = new FixedBuffer32<int>(); // Capacity = 8

        for (int i = 0; i < 8; i++)
        {
            buffer.Push(i);
        }

        Assert.That(buffer.IsFull, Is.True);
        Assert.Throws<InvalidOperationException>(() => buffer.Push(99));
    }

    [Test]
    public void Clear_ResetsSizeAndMemory()
    {
        var buffer = new FixedBuffer32<int>();
        buffer.Push(42);
        buffer.Push(84);

        buffer.Clear();

        Assert.That(buffer.Count, Is.EqualTo(0));
        Assert.That(buffer.IsFull, Is.False);
        Assert.That(buffer.ToArray(), Is.Empty);
    }

    [Test]
    public void Equals_IdenticalBuffers_ReturnsTrue()
    {
        var buffer1 = new FixedBuffer32<int>();
        var buffer2 = new FixedBuffer32<int>();

        buffer1.Push(10);
        buffer1.Push(20);

        buffer2.Push(10);
        buffer2.Push(20);

        Assert.That(buffer1.Equals(buffer2), Is.True);
        Assert.That(buffer1 == buffer2, Is.True);
        Assert.That(buffer1.GetHashCode(), Is.EqualTo(buffer2.GetHashCode()));
    }

    [Test]
    public void Equals_DifferentSizeBuffers_ReturnsFalse()
    {
        var buffer1 = new FixedBuffer32<int>();
        var buffer2 = new FixedBuffer32<int>();

        buffer1.Push(10);

        buffer2.Push(10);
        buffer2.Push(20);

        Assert.That(buffer1.Equals(buffer2), Is.False);
        Assert.That(buffer1 != buffer2, Is.True);
    }

    [Test]
    public void Equals_DifferentContentBuffers_ReturnsFalse()
    {
        var buffer1 = new FixedBuffer32<int>();
        var buffer2 = new FixedBuffer32<int>();

        buffer1.Push(10);
        buffer2.Push(99);

        Assert.That(buffer1.Equals(buffer2), Is.False);
        Assert.That(buffer1 != buffer2, Is.True);
    }

    [Test]
    public void EqualsAndHashCode_IgnoreGarbageDataAfterClear()
    {
        var cleanBuffer = new FixedBuffer32<int>();
        cleanBuffer.Push(5);

        var dirtyBuffer = new FixedBuffer32<int>();
        dirtyBuffer.Push(5);
        dirtyBuffer.Push(999);
        dirtyBuffer.Push(1234);

        dirtyBuffer.Clear();
        dirtyBuffer.Push(5);

        Assert.That(dirtyBuffer.Equals(cleanBuffer), Is.True);
        Assert.That(dirtyBuffer == cleanBuffer, Is.True);
        Assert.That(dirtyBuffer.GetHashCode(), Is.EqualTo(cleanBuffer.GetHashCode()));
    }

    [Test]
    public void Equals_WithObjectBox_WorksCorrectly()
    {
        var buffer1 = new FixedBuffer32<int>();
        buffer1.Push(7);

        var buffer2 = new FixedBuffer32<int>();
        buffer2.Push(7);

        object boxedBuffer2 = buffer2;

        Assert.That(buffer1.Equals(boxedBuffer2), Is.True);
    }

    [Test]
    public void StaticConstructor_WithIncompatibleType_ThrowsTypeInitializationException()
    {
        var exception = Assert.Throws<TypeInitializationException>(() =>
        {
            var buffer = new FixedBuffer32<InvalidTestStruct>();
            _ = buffer.IsFull; // trigger static constructor
        });
    }
}
