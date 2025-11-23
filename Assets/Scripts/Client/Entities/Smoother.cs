using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Client.Entities
{
    public class Smoother
    {
        public readonly int MaxCount;
        public readonly double Delay; // time delay behind server

        private double _time;
        private LinkedList<Record> _records = new();

        public struct Record
        {
            public double Time;
            public Vec2 Position;
            public float Rotation;
        }

        public Vec2 Position { get; private set; }
        public float Rotation { get; private set; }

        public Smoother(Record initialRecord, int maxCount = 10, double delay = 0.05)
        {
            MaxCount = maxCount;
            Delay = delay;
            _time = initialRecord.Time;
            _records.AddLast(initialRecord);
            Position = initialRecord.Position;
            Rotation = initialRecord.Rotation;
        }

        public void AddRecord(Record record)
        {
            // ignore absurd times
            if (!double.IsFinite(record.Time))
                return;

            if (_records.Count > 0)
            {
                var last = _records.Last.Value;

                // ignore older packets
                if (record.Time <= last.Time)
                    return;

                // packets with huge time jump -> reset memory
                if (record.Time - last.Time > 5.0)
                    _records.Clear();
            }

            // remove old records (>5s older than new record)
            while (_records.Count > 0 && _records.First.Value.Time < record.Time - 5.0)
                _records.RemoveFirst();

            _records.AddLast(record);

            if (_records.Count > MaxCount)
                _records.RemoveFirst();
        }

        public void UpdateSmooth(float deltaTime)
        {
            if (_records.Count < 2)
                return;

            _time += deltaTime;

            // aim to stay Delay behind the latest record
            double targetTime = _records.Last.Value.Time - Delay;
            double diff = targetTime - _time;

            // smooth time catching
            _time += diff * 0.1f;

            // find interpolation window
            var a = _records.First;
            while (a.Next != null && a.Next.Value.Time <= _time)
                a = a.Next;

            var b = a.Next;
            if (b == null)
            {
                Position = a.Value.Position;
                Rotation = a.Value.Rotation;
                return;
            }

            double t0 = a.Value.Time;
            double t1 = b.Value.Time;
            float alpha = (float)((_time - t0) / (t1 - t0));
            alpha = Mathf.Clamp01(alpha);

            // Catmull-Rom requires 4 points: p0, p1, p2, p3
            Vec2 p0 = a.Previous != null ? a.Previous.Value.Position : a.Value.Position;
            Vec2 p1 = a.Value.Position;
            Vec2 p2 = b.Value.Position;
            Vec2 p3 = b.Next != null ? b.Next.Value.Position : b.Value.Position;

            Position = CatmullRom(p0, p1, p2, p3, alpha);

            // linear rotation interpolation with wrap-around handling
            float r0 = ReduceAngle(a.Value.Rotation);
            float r1 = r0 + GetAngleDifference(b.Value.Rotation, r0);
            Rotation = Mathf.Lerp(r0, r1, alpha);
        }

        private static Vec2 CatmullRom(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return new Vec2(
                0.5f * ((2f * p1.x) +
                        (-p0.x + p2.x) * t +
                        (2f * p0.x - 5f * p1.x + 4f * p2.x - p3.x) * t2 +
                        (-p0.x + 3f * p1.x - 3f * p2.x + p3.x) * t3),
                0.5f * ((2f * p1.y) +
                        (-p0.y + p2.y) * t +
                        (2f * p0.y - 5f * p1.y + 4f * p2.y - p3.y) * t2 +
                        (-p0.y + 3f * p1.y - 3f * p2.y + p3.y) * t3)
            );
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
