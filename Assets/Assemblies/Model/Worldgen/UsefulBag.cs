using System;
using System.Collections.Generic;
using Larnix.Model.Worldgen.Providers;

namespace Larnix.Model.Worldgen;

internal class UsefulBag
{
    public Seed Seed => Generator.Seed;
    public Generator Generator { get; }
    public Dictionary<string, ValueProvider> Providers { get; }

    public UsefulBag(Generator generator)
    {
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
        Providers = new();
    }

    public T Get<T>(string key) where T : ValueProvider
    {
        return (T)Providers[key];
    }

    public UsefulBag Copy()
    {
        var newBag = new UsefulBag(Generator);
        foreach (var kvp in Providers)
        {
            newBag.Providers[kvp.Key] = kvp.Value;
        }
        return newBag;
    }
}
