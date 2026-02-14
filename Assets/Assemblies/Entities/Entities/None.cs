using Larnix.Entities.Structs;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Entities.All
{
    public sealed class None : EntityServer, IEntityInterface
    {
        public None(ulong uid, EntityData entityData)
            : base(uid, entityData) { }
    }
}
