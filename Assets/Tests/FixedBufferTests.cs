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

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.That(buffer.Count, Is.EqualTo(3));
        Assert.That(buffer.IsFull, Is.False);

        int[] result = buffer.ToArray();
        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30 }));
    }

    [Test]
    public void PushAndToArray_WorksForEnums()
    {
        var buffer = new FixedBuffer32<TestEnum>();

        buffer.Add(TestEnum.Alpha);
        buffer.Add(TestEnum.Gamma);

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

        buffer.Add(s1);
        buffer.Add(s2);

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
            buffer.Add(i);
        }

        Assert.That(buffer.IsFull, Is.True);
        Assert.Throws<InvalidOperationException>(() => buffer.Add(99));
    }

    [Test]
    public void Clear_ResetsSizeAndMemory()
    {
        var buffer = new FixedBuffer32<int>();
        buffer.Add(42);
        buffer.Add(84);

        buffer.Clear();

        Assert.That(buffer.Count, Is.EqualTo(0));
        Assert.That(buffer.IsFull, Is.False);
        Assert.That(buffer.ToArray(), Is.Empty);
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
