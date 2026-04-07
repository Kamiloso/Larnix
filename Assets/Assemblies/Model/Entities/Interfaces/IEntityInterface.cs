#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Json;
using IIPhysics = Larnix.Model.Physics.IPhysics;

namespace Larnix.Model.Entities.All;

public interface IEntityInterface
{
    Entity This => (Entity)this;

    Vec2 Position => This.EntityData.Position;
    float Rotation => This.EntityData.Rotation;
    Storage Data => This.EntityData.NBT;

    IIPhysics Physics => This.Interfaces.Physics;
    ICmdExecutor CmdExecutor => This.Interfaces.CmdExecutor;
}
