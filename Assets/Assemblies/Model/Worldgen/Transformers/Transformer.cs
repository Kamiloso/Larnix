using System;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Providers;

namespace Larnix.Model.Worldgen.Transformers;

internal interface ITransformer
{
    Type InputType { get; }
    Type OutputType { get; }
    Array Rebuild(Vec2Int chunk, Array input);
}

internal abstract class Transformer<TIn, TOut> : ITransformer
{
    protected UsefulBag UsefulBag { get; }
    protected Dictionary<string, ValueProvider> Providers => UsefulBag.Providers;
    protected Generator Generator => UsefulBag.Generator;
    protected Seed Seed => UsefulBag.Seed;

    public abstract TOut[,] Rebuild(Vec2Int chunk, TIn[,] chunkIn);

    Type ITransformer.InputType => typeof(TIn);
    Type ITransformer.OutputType => typeof(TOut);
    Array ITransformer.Rebuild(Vec2Int chunk, Array input)
    {
        if (input is not TIn[,] castedInput)
            throw new ArgumentException("Input type does not match.");

        return Rebuild(chunk, castedInput);
    }

    protected Transformer(UsefulBag usefulBag)
    {
        UsefulBag = usefulBag ?? throw new ArgumentNullException(nameof(usefulBag));
    }
}
