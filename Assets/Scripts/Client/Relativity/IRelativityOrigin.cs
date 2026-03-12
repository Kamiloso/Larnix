using UnityEngine;
using System;
using Larnix.Core.Vectors;

namespace Larnix.Client.Relativity
{
    public interface IRelativityOrigin
    {
        Vec2 OriginOffset { get; }

        public Vector2 ToUnityPos(Vec2 position)
        {
            Vec2 origin = OriginOffset;
            return position.ExtractPosition(origin);
        }

        public Vec2 ToLarnixPos(Vector2 position)
        {
            Vec2 origin = OriginOffset;
            return VectorExtensions.ConstructVec2(position, origin);
        }
    }
}
