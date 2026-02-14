using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;
using Larnix.Core.Json;

namespace Larnix.Entities.All
{
    public interface IEntityInterface
    {
        EntityServer This => (EntityServer)this;
        PhysicsManager Physics => This.Physics;
        Storage Data => This.EntityData.Data;
    }
}
