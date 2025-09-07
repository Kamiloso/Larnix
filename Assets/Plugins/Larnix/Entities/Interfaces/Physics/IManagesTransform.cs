using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public interface IManagesTransform : IEntityInterface
    {
        void ApplyTransformToSystem(Vec2 position, float rotation);
    }
}
