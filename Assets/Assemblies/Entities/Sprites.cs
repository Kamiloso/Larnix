using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Larnix.Core.ClientThreadUtils;
using System.ComponentModel;

namespace Larnix.Entities
{
    public static class Sprites
    {
        private static Dictionary<string, Sprite> _spriteCache = new();

        private static string ProduceKey(EntityID ID, int y, int animation)
        {
            return ID.ToString() + "-" + y + "-" + animation;
        }

        public static Sprite GetSprite(EntityID ID, int y, int animation)
        {
            if (!IsMainThreadHeuristic())
                throw new NotSupportedException("GetSprite() method can only be called from the main thread!");

            string key = ProduceKey(ID, y, animation);
            if (!_spriteCache.TryGetValue(key, out var sprite))
            {
                Vector2Int size = EntityFactory.GetSlaveInstance<ISprite>(ID).SPRITE_SIZE();
                Vector2Int start = new Vector2Int(animation * size.x, y);

                string path = "Entities/" + ID.ToString() + ".png";
                Texture2D texture = StreamingTextureLoader.Instance.LoadTextureSync(path) ??
                                    StreamingTextureLoader.PinkTexture;

                sprite = Sprite.Create(
                    texture: texture,
                    rect: new Rect(start.x, start.y, size.x, size.y),
                    pivot: new Vector2(0.5f, 0.5f),
                    pixelsPerUnit: System.Math.Max(texture.width, texture.height),
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect
                );

                _spriteCache[key] = sprite;
            }

            return sprite;
        }

        private static bool IsMainThreadHeuristic()
        {
            try
            {
                _ = Time.deltaTime;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
