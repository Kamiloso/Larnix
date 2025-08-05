using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Physics
{
    public enum Side { Left, Right, Top, Bottom };
    public struct PhysicsReport
    {
        public Vector2? Position;
        public bool OnGround;
        public bool OnLeftWall;
        public bool OnRightWall;
        public bool OnCeil;

        public void Merge(PhysicsReport other)
        {
            Position = other.Position;
            OnGround |= other.OnGround;
            OnLeftWall |= other.OnLeftWall;
            OnRightWall |= other.OnRightWall;
            OnCeil |= other.OnCeil;
        }
    }
}
