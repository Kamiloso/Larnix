using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client
{
    public class Smoother
    {
        private const int IncludeCount = 3;
        private const float CorrectionProportion = 0.15f;
        private const float MaxPositionDifference = 1.0f;

        private readonly List<double> Times = new List<double>();
        private readonly List<Vector2> Positions = new List<Vector2>();
        private readonly List<float> Rotations = new List<float>();

        private Vector2 LocalPosition;
        private float LocalRotation;

        private Vector2 VelocityPosition = Vector2.zero;
        private float VelocityRotation = 0f;

        public Smoother(double initialTime, Vector2 initialPosition, float initialRotation)
        {
            LocalPosition = initialPosition;
            LocalRotation = initialRotation;

            AddRecord(initialTime, initialPosition, initialRotation);
        }

        public void AddRecord(double time, Vector2 position, float rotation)
        {
            Times.Add(time);
            Positions.Add(position);
            Rotations.Add(rotation);

            if (Times.Count > IncludeCount)
            {
                Times.RemoveAt(0);
                Positions.RemoveAt(0);
                Rotations.RemoveAt(0);
            }

            if (Times.Count >= 2)
            {
                double diffTime = Times[Times.Count - 1] - Times[0];
                if (diffTime > 0f)
                {
                    Vector2 diffPos = Positions[Positions.Count - 1] - Positions[0];
                    float diffRot = GetAngleDifference(Rotations[Rotations.Count - 1], Rotations[0]);

                    VelocityPosition = diffPos / (float)diffTime;
                    VelocityRotation = diffRot / (float)diffTime;
                }
            }
        }

        public void UpdateSmooth(float deltaTime)
        {
            // Prediction
            LocalPosition += VelocityPosition * deltaTime;
            LocalRotation += VelocityRotation * deltaTime;
            LocalRotation = ReduceAngle(LocalRotation);

            // Correction
            if(Times.Count >= 2)
            {
                Vector2 targetPosition = Positions[Positions.Count - 1];
                float targetRotation = Rotations[Rotations.Count - 1];

                float FrameProportion = 1 - Mathf.Pow(1 - CorrectionProportion, 100 * Time.deltaTime);

                LocalPosition += (targetPosition - LocalPosition) * FrameProportion;
                LocalRotation += GetAngleDifference(targetRotation, LocalRotation) * FrameProportion;
                LocalRotation = ReduceAngle(LocalRotation);

                if (Vector2.Distance(LocalPosition, targetPosition) > MaxPositionDifference)
                {
                    LocalPosition = targetPosition;
                    ResetPredictor();
                }
            }
        }

        public Vector2 GetSmoothedPosition() => LocalPosition;
        public float GetSmoothedRotation() => LocalRotation;

        private void ResetPredictor()
        {
            Times.Clear();
            Positions.Clear();
            Rotations.Clear();

            VelocityPosition = Vector2.zero;
            VelocityRotation = 0f;
        }

        private static float GetAngleDifference(float a, float b)
        {
            float diff = ReduceAngle(a - b);
            return diff <= 180f ? diff : diff - 360f;
        }

        private static float ReduceAngle(float a)
        {
            float result = a % 360f;
            return result < 0f ? result + 360f : result;
        }
    }
}
