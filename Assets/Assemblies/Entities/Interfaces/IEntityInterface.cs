using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;
using Larnix.Core.Json;

namespace Larnix.Entities
{
    public interface IEntityInterface
    {
        EntityServer This => (EntityServer)this;
        PhysicsManager Physics => This.Physics;
        Storage Data => This.EntityData.Data;
    }
}
