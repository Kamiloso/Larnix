using LibNoise.Generator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Server.Worldgen
{
    public enum ProviderType
    {
        Perlin,
        Function,
        Condition,
        And,
        Or,
    }

    public class ValueProvider
    {
        public readonly ProviderType Type;
        private Func<double, double, double, double> Value;

        private ValueProvider(ProviderType type) => Type = type;
        public double GetValue(double x = 0, double y = 0, double z = 0) => Value(x, y, z);

        public static ValueProvider CreatePerlin(Perlin perlin, double min, double max, int dim)
        {
            ValueProvider provider = new ValueProvider(ProviderType.Perlin);
            provider.Value = (x, y, z) => (perlin.GetValue(dim >= 1 ? x : 0, dim >= 2 ? y : 0, dim >= 3 ? z : 0) + 1.0) / 2.0 * (max - min) + min;
            return provider;
        }

        public static ValueProvider CreateFunction(Func<double, double, double, double> function)
        {
            ValueProvider provider = new ValueProvider(ProviderType.Function);
            provider.Value = (x, y, z) => function(x, y, z);
            return provider;
        }

        public static ValueProvider CreateCondition(ValueProvider baseProvider, double min, double max, double width, bool isInner)
        {
            ValueProvider provider = new ValueProvider(ProviderType.Condition);
            provider.Value = (x, y, z) =>
            {
                double v = baseProvider.GetValue(x, y, z);

                double leftStart = min - width;
                double rightEnd = max + width;

                if (leftStart > min) leftStart = double.MinValue;
                if (rightEnd < max) rightEnd = double.MaxValue;

                if (v <= leftStart || v >= rightEnd)
                    return isInner ? 0.0 : 1.0;

                if (v >= min && v <= max)
                    return isInner ? 1.0 : 0.0;

                double t;
                if (v < min)
                {
                    t = (v - leftStart) / (min - leftStart);
                    double smooth = t * t * (3.0 - 2.0 * t);
                    return isInner ? smooth : 1.0 - smooth;
                }
                else // v > max
                {
                    t = (v - max) / (rightEnd - max);
                    double smooth = t * t * (3.0 - 2.0 * t);
                    return isInner ? 1.0 - smooth : smooth;
                }
            };

            return provider;
        }

        public static ValueProvider CreateAnd(List<ValueProvider> providers)
        {
            ValueProvider provider = new ValueProvider(ProviderType.And);
            provider.Value = (x, y, z) =>
            {
                double num = 1.0;
                foreach (var p in providers)
                {
                    num *= p.GetValue(x, y, z);
                }
                return num;
            };
            return provider;
        }

        public static ValueProvider CreateOr(List<ValueProvider> providers)
        {
            ValueProvider provider = new ValueProvider(ProviderType.Or);
            provider.Value = (x, y, z) =>
            {
                double num = 1.0;
                foreach (var p in providers)
                {
                    num *= 1.0 - p.GetValue(x, y, z);
                }
                return 1.0 - num;
            };
            return provider;
        }
    }
}
