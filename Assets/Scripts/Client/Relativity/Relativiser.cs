using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Client.Relativity
{
    public class Relativiser : MonoBehaviour
    {
        private Vec2 _position;
        public Vec2 Position
        {
            get => _position;
            set
            {
                _position = value;
                transform.SetLarnixPos(_position);
            }
        }

        public bool IsLatePhase { get; set; } = true;

        private void Update()
        {
            if (!IsLatePhase)
                transform.SetLarnixPos(Position);
        }

        private void LateUpdate()
        {
            if (IsLatePhase)
                transform.SetLarnixPos(Position);
        }
    }
}
