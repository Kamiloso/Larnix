#nullable enable
using Larnix.Model.Blocks;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Server.Commands;
using Larnix.Core;
using Larnix.Model;
using BreakMode = Larnix.Model.Blocks.IWorldAPI.BreakMode;

namespace Larnix.Server.Terrain;

internal class WorldAPI : IWorldAPI
{
    private Chunks Chunks => GlobRef.Get<Chunks>();
    private IAtomicChunks AtomicChunks => GlobRef.Get<IAtomicChunks>();
    private ICmdManager CmdManager => GlobRef.Get<ICmdManager>();
    private IClock Clock => GlobRef.Get<IClock>();

    public long ServerTick => Clock.ServerTick;

    public bool IsChunkLoaded(Vec2Int chunk, bool atomic = false)
    {
        return atomic ?
            AtomicChunks.IsAtomicLoaded(chunk) :
            Chunks.IsChunkLoaded(chunk);
    }

    public Block? GetBlock(Vec2Int POS, bool isFront)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

        return Chunks.GetChunk(chunk)?.GetBlock(pos, isFront);
    }

    public Block? ReplaceBlock(Vec2Int POS, bool isFront, BlockData1 blockTemplate,
        BreakMode breakMode = BreakMode.Replace)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

        BlockData1 blockDeepCopy = blockTemplate.DeepCopy();
        return Chunks.GetChunk(chunk)?.UpdateBlock(pos, isFront, blockDeepCopy, breakMode);
    }

    public Block? MutateBlockVariant(Vec2Int POS, bool isFront, byte variant)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

        BlockData1 blockData = GetBlock(POS, isFront)!.BlockData;
        blockData.Variant = variant;
        return Chunks.GetChunk(chunk)?.UpdateBlockMutated(pos, isFront);
    }

    public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item)
    {
        BlockData1? frontBlock = GetBlock(POS, true)?.BlockData;
        BlockData1? backBlock = GetBlock(POS, false)?.BlockData;

        if (frontBlock is null || backBlock is null) return false;

        BlockHeader2 h_blockPair = new(frontBlock.Header, backBlock.Header);
        BlockHeader1 h_item = item.Header;

        return BlockInteractions.CanBePlaced(h_blockPair, h_item, front);
    }

    public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool)
    {
        BlockData1? frontBlock = GetBlock(POS, true)?.BlockData;
        BlockData1? backBlock = GetBlock(POS, false)?.BlockData;

        if (frontBlock is null || backBlock is null) return false;

        BlockHeader2 h_blockPair = new(frontBlock.Header, backBlock.Header);
        BlockHeader1 h_item = item.Header;
        BlockHeader1 h_tool = tool.Header;

        return BlockInteractions.CanBeBroken(h_blockPair, h_item, h_tool, front);
    }

    public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item)
    {
        ReplaceBlock(POS, front, item, BreakMode.Effects);
    }

    public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool)
    {
        BlockData1? oldBlock = GetBlock(POS, front)?.BlockData;
        if (oldBlock is null) return;

        // TODO: Drop items code here

        ReplaceBlock(POS, front, BlockData1.Air, BreakMode.Effects);
    }

    public (CmdResult, string) ExecuteCommand(string command, string? sender = null)
    {
        return CmdManager.ExecuteCommand(command, sender);
    }
}
