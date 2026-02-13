using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Client.Relativity
{
    public class Relativiser : MonoBehaviour
    {
        public Vec2 Position { get; set; }

        private void LateUpdate()
        {
            transform.SetLarnixPos(Position);
        }
    }
}
