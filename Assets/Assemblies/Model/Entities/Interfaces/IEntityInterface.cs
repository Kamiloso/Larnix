using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Physics;
using Larnix.Model.Json;

namespace Larnix.Model.Entities.All;

public interface IEntityInterface
{
    Entity This => (Entity)this;
    PhysicsManager Physics => This.Physics;
    Storage Data => This.EntityData.NBT;
}
