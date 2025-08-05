using Larnix;
using Larnix.Physics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Larnix.Physics
{
    public class StaticCollider
    {
        public Vector2 Center { get; private set; }
        public Vector2 Size { get; private set; }

        public StaticCollider(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }

        public void MakeOffset(Vector2 POS)
        {
            Center += POS;
        }
    }
}
