using Larnix.Entities.Structs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public class None : EntityServer, IEntityInterface
    {
        public None(ulong uid, EntityData entityData)
            : base(uid, entityData) { }
    }
}
