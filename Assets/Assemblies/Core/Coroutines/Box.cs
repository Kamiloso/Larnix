#nullable enable
using System;

namespace Larnix.Core.Coroutines;

public interface IBox
{
    Box<object> AsObject();
}

public class Box<T> : IBox
{
    public T Value { get; init; }
    public Box(T value)
    {
        if (!typeof(T).IsValueType && typeof(T) != typeof(object))
            throw new ArgumentException("Reference types are not supported!", nameof(value));

        Value = value;
    }

    public Box<object> AsObject()
    {
        return new Box<object>(Value!);
    }
}
