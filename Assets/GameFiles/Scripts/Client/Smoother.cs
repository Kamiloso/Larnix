using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client
{
    public class Smoother
    {
        private readonly int IncludeCount;

        private readonly List<float> Times = new List<float>();
        private readonly List<Vector2> Positions = new List<Vector2>();
        private readonly List<float> Rotations = new List<float>();

        Smoother(int includeCount)
        {
            IncludeCount = includeCount;
        }

        public void AddRecord(float time, Vector2 position, float rotation)
        {
            Times.Add(time);
            Positions.Add(position);
            Rotations.Add(rotation);

            if(Times.Count > IncludeCount)
            {
                Times.RemoveAt(0);
                Positions.RemoveAt(0);
                Rotations.RemoveAt(0);
            }
        }

        public Vector2 GetPosition(float time)
        {
            return new Vector2(0f, 0f);
        }

        public float GetRotation(float time)
        {
            return 0.0f;
        }
    }
}
