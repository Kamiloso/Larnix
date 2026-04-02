#nullable enable

namespace Larnix.Model.Physics.Structs;

public readonly struct PhysicsProperties
{
    public double Gravity { get; init; }
    public double ControlForce { get; init; }
    public double HorizontalDrag { get; init; }
    public double JumpSize { get; init; }
    public double MaxVerticalVelocity { get; init; }
    public double MaxHorizontalVelocity { get; init; }
}
