using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Physics;

namespace Larnix.Entities
{
    public interface IEntityInterface
    {
        EntityServer This => (EntityServer)this;
        PhysicsManager Physics => This.Physics;
    }
}
