#nullable enable

namespace Larnix.Model.Physics.Structs;

public readonly struct InputData
{
    public bool Left { get; init; }
    public bool Right { get; init; }
    public bool Jump { get; init; }

    public InputData(in InputData original)
    {
        Left = original.Left;
        Right = original.Right;
        Jump = original.Jump;
    }
}
