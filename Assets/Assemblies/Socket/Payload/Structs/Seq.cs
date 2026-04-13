#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Seq(int Value)
{
    public static Seq operator++(Seq seq) => new(unchecked(seq.Value + 1));
    public static Seq operator--(Seq seq) => new(unchecked(seq.Value - 1));

    public static Seq operator+(Seq seq, int value) => new(unchecked(seq.Value + value));
    public static Seq operator-(Seq seq, int value) => new(unchecked(seq.Value - value));

    public static int operator -(Seq seq1, Seq seq2) => unchecked(seq1.Value - seq2.Value);

    public static bool operator>(Seq a, Seq b) => unchecked(a - b > 0);
    public static bool operator<(Seq a, Seq b) => unchecked(a - b < 0);
    public static bool operator>=(Seq a, Seq b) => unchecked(a - b >= 0);
    public static bool operator<=(Seq a, Seq b) => unchecked(a - b <= 0);

    public override string ToString() => Value.ToString();
}
