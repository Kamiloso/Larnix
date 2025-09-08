using System.Collections.Generic;
using UnityEngine;
using System;

namespace Larnix.Client.Entities
{
    public class Smoother
    {
        private const int IncludeCount = 3;
        private const double SmoothTime = 0.15;
        private const double MaxPositionDifference = 5.0;

        private readonly List<double> Times = new List<double>();
        private readonly List<Vec2> Positions = new List<Vec2>();
        private readonly List<float> Rotations = new List<float>();

        private Vec2 LocalPosition;
        private float LocalRotation;

        private Vec2 VelocityPosition = Vec2.Zero;
        private float VelocityRotation = 0f;

        public Smoother(double initialTime, Vec2 initialPosition, float initialRotation)
        {
            LocalPosition = initialPosition;
            LocalRotation = initialRotation;

            AddRecord(initialTime, initialPosition, initialRotation);
        }

        public void AddRecord(double time, Vec2 position, float rotation)
        {
            if (Times.Count > 0 && time <= Times[Times.Count - 1])
                ResetPredictor(); // something is wrong, ignore previous records

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
                    Vec2 diffPos = Positions[Positions.Count - 1] - Positions[0];
                    float diffRot = GetAngleDifference(Rotations[Rotations.Count - 1], Rotations[0]);

                    VelocityPosition = diffPos / diffTime;
                    VelocityRotation = (float)(diffRot / diffTime);
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
            if(Times.Count >= 1)
            {
                Vec2 targetPosition = Positions[Positions.Count - 1];
                float targetRotation = Rotations[Rotations.Count - 1];

                double FrameProportion = 1 - Math.Exp(-deltaTime / SmoothTime);

                LocalPosition += (targetPosition - LocalPosition) * FrameProportion;
                LocalRotation += GetAngleDifference(targetRotation, LocalRotation) * (float)FrameProportion;
                LocalRotation = ReduceAngle(LocalRotation);

                if ((LocalPosition - targetPosition).Magnitude > MaxPositionDifference)
                {
                    LocalPosition = targetPosition;
                    ResetPredictor();
                }
            }
        }

        public Vec2 GetSmoothedPosition() => LocalPosition;
        public float GetSmoothedRotation() => LocalRotation;

        private void ResetPredictor()
        {
            Times.Clear();
            Positions.Clear();
            Rotations.Clear();

            VelocityPosition = Vec2.Zero;
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
