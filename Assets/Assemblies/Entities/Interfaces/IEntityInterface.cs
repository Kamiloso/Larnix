using System.Collections;
using System.Collections.Generic;
using Larnix.GameCore.Physics;
using Larnix.GameCore.Json;

namespace Larnix.Entities.All;

public interface IEntityInterface
{
    Entity This => (Entity)this;
    PhysicsManager Physics => This.Physics;
    Storage Data => This.EntityData.NBT;
}
