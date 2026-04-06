#nullable enable
using Larnix.Model.Json;
using Larnix.Model.Interfaces;

namespace Larnix.Model.Entities.All;

public interface IEntityInterface
{
    Entity This => (Entity)this;
    IPhysicsManager Physics => This.Physics;
    Storage Data => This.EntityData.NBT;
}
