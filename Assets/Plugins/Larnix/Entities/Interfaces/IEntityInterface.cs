using System.Collections;
using System.Collections.Generic;
using Larnix.Physics;

namespace Larnix.Entities
{
    public interface IEntityInterface
    {
        EntityServer This => (EntityServer)this;
        PhysicsManager Physics => This.Physics;
    }
}
