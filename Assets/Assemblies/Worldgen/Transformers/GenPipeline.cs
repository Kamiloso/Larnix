using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.GameCore.Utils;

namespace Larnix.Worldgen.Transformers;

public class GenPipeline
{
    private Func<Vec2Int, Array> _function = _ => ChunkIterator.Array2D<object>();
    private Type _lastType = typeof(object);

    public GenPipeline(params ITransformer[] transformers)
    {
        foreach (ITransformer trn in transformers)
        {
            Type inputType = trn.InputType;
            Type outputType = trn.OutputType;

            if (_lastType != inputType)
            {
                throw new ArgumentException(
                    $"Non-matching input type for type {trn.GetType()}. Expected {_lastType}, but got {inputType}.");
            }

            var prevFunc = _function;
            _function = chunk =>
            {
                Array prevResult = prevFunc(chunk);
                return trn.Rebuild(chunk, prevResult);
            };

            _lastType = outputType;
        }

        if (_lastType != typeof(BlockData2))
            throw new ArgumentException($"Last transformer must output {nameof(BlockData2)}, but got {_lastType}.");
    }

    public BlockData2[,] Run(Vec2Int chunk)
    {
        return (BlockData2[,])_function(chunk);
    }
}
