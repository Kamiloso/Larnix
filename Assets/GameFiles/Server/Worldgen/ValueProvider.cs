using LibNoise.Generator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Server.Worldgen
{
    public enum ProviderType
    {
        Perlin,
        Function,
        Condition,
    }

    public class ValueProvider
    {
        public readonly ProviderType Type;
        private Func<double, double, double, double> Value;

        private ValueProvider(ProviderType type) => Type = type;
        public double GetValue(double x = 0, double y = 0, double z = 0) => Value(x, y, z);

        public ValueProvider CastType(ProviderType type)
        {
            ValueProvider provider = new ValueProvider(type);
            provider.Value = (x, y, z) => GetValue(x, y, z);
            return provider;
        }

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

        public static ValueProvider CreateCondition(ValueProvider baseProvider, double min, double max, double width)
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
                    return 0.0;

                if (v >= min && v <= max)
                    return 1.0;

                double t;
                if (v < min)
                {
                    t = (v - leftStart) / (min - leftStart);
                    double smooth = t * t * (3.0 - 2.0 * t);
                    return smooth;
                }
                else // v > max
                {
                    t = (v - max) / (rightEnd - max);
                    double smooth = t * t * (3.0 - 2.0 * t);
                    return 1.0 - smooth;
                }
            };

            return provider;
        }

        public ValueProvider Stretch(double stretch_x = 1.0, double stretch_y = 1.0, double stretch_z = 1.0)
        {
            ValueProvider provider = new ValueProvider(Type);
            provider.Value = (x, y, z) => GetValue(x / stretch_x, y / stretch_y, z / stretch_z);
            return provider;
        }

        public ValueProvider Negate()
        {
            if (Type != ProviderType.Condition)
                throw new Exception("Only conditional providers are allowed in Negate() operation.");

            ValueProvider provider = new ValueProvider(ProviderType.Condition);
            provider.Value = (x, y, z) => 1.0 - GetValue(x, y, z);
            return provider;
        }

        public ValueProvider And(ValueProvider condition)
        {
            if (Type != ProviderType.Condition || condition.Type != ProviderType.Condition)
                throw new Exception("Only conditional providers are allowed in And() operation.");

            ValueProvider provider = new ValueProvider(ProviderType.Condition);
            provider.Value = (x, y, z) => Math.Min(GetValue(x, y, z), condition.GetValue(x, y, z));
            return provider;
        }

        public ValueProvider Or(ValueProvider condition)
        {
            if (Type != ProviderType.Condition || condition.Type != ProviderType.Condition)
                throw new Exception("Only conditional providers are allowed in Or() operation.");

            ValueProvider provider = new ValueProvider(ProviderType.Condition);
            provider.Value = (x, y, z) => Math.Max(GetValue(x, y, z), condition.GetValue(x, y, z));
            return provider;
        }

        public ValueProvider If(ValueProvider condition)
        {
            ValueProvider provider = new ValueProvider(ProviderType.Condition);
            provider.Value = (x, y, z) => GetValue(x, y, z) * condition.GetValue(x, y, z);
            return provider;
        }
    }
}
