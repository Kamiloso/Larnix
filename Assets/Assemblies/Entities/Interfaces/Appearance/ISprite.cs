using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Entities
{
    public interface ISprite : IEntityInterface
    {
        public Vector2Int SPRITE_SIZE();
        public Vec2 SPRITE_OFFSET();

        public Sprite CLIENT_GetSprite(int y = 0, int animation = 0)
        {
            EntityID entityID = This.EntityData.ID;
            return Sprites.GetSprite(entityID, y, animation);
        }
    }
}
