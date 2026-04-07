#nullable enable
using Larnix.Core.Vectors;
using System;

namespace Larnix.Model.Blocks;

public enum BlockOrder
{
    PreFrame, PreFrameSelfMutations,
    Conway,
    Sequential, Random,
    ElectricPropagation, ElectricFinalize, ElectricDevices,
    SequentialLate, RandomLate,
    TechCmdExecute,
}

public interface IChunkEvents
{
    public void Subscribe(Vec2Int pos, bool front, BlockOrder type, Action action, bool isSecAtomic);
    public void Unsubscribe(BlockOrder type, Action action);
}
