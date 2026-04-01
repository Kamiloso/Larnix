using Larnix.Blocks;
using Larnix.GameCore.Utils;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Server.Commands;
using Larnix.Core;
using Larnix.Server.Terrain;
using BreakMode = Larnix.Blocks.IWorldAPI.BreakMode;
using ResultType = Larnix.GameCore.ICmdExecutor.CmdResult;
using Larnix.GameCore.Structs;

namespace Larnix.Server.APIs;

internal class WorldAPI : IWorldAPI
{
    private Chunks Chunks => GlobRef.Get<Chunks>();
    private AtomicChunks AtomicChunks => GlobRef.Get<AtomicChunks>();
    private CmdManager Commands => GlobRef.Get<CmdManager>();
    private Clock Clock => GlobRef.Get<Clock>();

    public long ServerTick => Clock.ServerTick;

    public bool IsChunkLoaded(Vec2Int chunk, bool atomic = false)
    {
        return atomic ?
            AtomicChunks.IsAtomicLoaded(chunk) :
            Chunks.IsChunkLoaded(chunk);
    }

    public Block GetBlock(Vec2Int POS, bool isFront)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        if (Chunks.TryGetChunk(chunk, out var chunkObject))
        {
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
            return chunkObject.GetBlock(pos, isFront);
        }
        return null;
    }

    public Block ReplaceBlock(Vec2Int POS, bool isFront, BlockData1 blockTemplate,
        BreakMode breakMode = BreakMode.Replace)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        if (Chunks.TryGetChunk(chunk, out var chunkObject))
        {
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
            BlockData1 blockDeepCopy = blockTemplate.DeepCopy();
            return chunkObject.UpdateBlock(pos, isFront, blockDeepCopy, breakMode);
        }
        return null;
    }

    public Block MutateBlockVariant(Vec2Int POS, bool isFront, byte variant)
    {
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        if (Chunks.TryGetChunk(chunk, out var chunkObject))
        {
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
            BlockData1 blockData = GetBlock(POS, isFront).BlockData;
            blockData.Variant = variant;
            return chunkObject.UpdateBlockMutated(pos, isFront);
        }
        return null;
    }

    public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item)
    {
        BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
        BlockData1 backBlock = GetBlock(POS, false)?.BlockData;

        if (frontBlock != null && backBlock != null)
        {
            BlockHeader2 h_blockPair = new(frontBlock.Header, backBlock.Header);
            BlockHeader1 h_item = item.Header;

            return BlockInteractions.CanBePlaced(h_blockPair, h_item, front);
        }
        return false;
    }

    public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool)
    {
        BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
        BlockData1 backBlock = GetBlock(POS, false)?.BlockData;

        if (frontBlock != null && backBlock != null)
        {
            BlockHeader2 h_blockPair = new(frontBlock.Header, backBlock.Header);
            BlockHeader1 h_item = item.Header;
            BlockHeader1 h_tool = tool.Header;

            return BlockInteractions.CanBeBroken(h_blockPair, h_item, h_tool, front);
        }
        return false;
    }

    public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item)
    {
        ReplaceBlock(POS, front, item, BreakMode.Effects);
    }

    public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool)
    {
        BlockData1 oldBlock = GetBlock(POS, front)?.BlockData;
        if (oldBlock == null) return;

        // TODO: Drop items code here

        ReplaceBlock(POS, front, BlockData1.Air, BreakMode.Effects);
    }

    public (ResultType, string) ExecuteCommand(string command, string sender = null)
    {
        return Commands.ExecuteCommand(command, sender);
    }
}
