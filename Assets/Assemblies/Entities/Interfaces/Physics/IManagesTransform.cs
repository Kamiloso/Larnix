using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;

namespace Larnix.Entities.All
{
    public interface IManagesTransform : IEntityInterface
    {
        void ApplyTransformToSystem(Vec2 position, float rotation);
    }
}
