using System;

namespace Larnix.Core.Coroutines
{
    public interface IBox
    {
        Box<object> AsObject();
        Box<U> Cast<U>();
    }

    public class Box<T> : IBox
    {
        public T Value { get; init; }
        public Box(T value) => Value = value;

        public Box<object> AsObject()
        {
            return Cast<object>();
        }

        public Box<U> Cast<U>()
        {
            if (Value is null && default(U) is null)
                return new Box<U>(default);

            if (Value is U casted)
                return new Box<U>(casted);

            throw new InvalidCastException($"Cannot cast value of type {typeof(T)} to {typeof(U)}.");
        }

        public override bool Equals(object obj) => obj as Box<T> == this;
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        
        public static bool operator ==(Box<T> left, Box<T> right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Value?.Equals(right.Value) ?? right.Value is null;
        }
        public static bool operator !=(Box<T> left, Box<T> right) => !(left == right);

        public static explicit operator Box<T>(T value) => new Box<T>(value);
        public static explicit operator T(Box<T> box) => box.Value;
    }
}
