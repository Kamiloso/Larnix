using Larnix.Core.Vectors;

namespace Larnix.GameCore.Physics.Structs;

public readonly struct PhysicsProperties
{
    public double Gravity { get; init; }
    public double ControlForce { get; init; }
    public double HorizontalDrag { get; init; }
    public double JumpSize { get; init; }
    public double MaxVerticalVelocity { get; init; }
    public double MaxHorizontalVelocity { get; init; }

    public PhysicsProperties(in PhysicsProperties original)
    {
        Gravity = original.Gravity;
        ControlForce = original.ControlForce;
        HorizontalDrag = original.HorizontalDrag;
        JumpSize = original.JumpSize;
        MaxVerticalVelocity = original.MaxVerticalVelocity;
        MaxHorizontalVelocity = original.MaxHorizontalVelocity;
    }
}
