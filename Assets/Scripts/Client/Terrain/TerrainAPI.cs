using Larnix.Model.Blocks;
using Larnix.Socket.Packets;
using Larnix.Server.Packets;
using Larnix.Core.Vectors;
using Larnix.Core;
using System;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Client.Terrain;

public static class TerrainAPI
{
    private static Client Client => GlobRef.Get<Client>();
    private static GridManager GridManager => GlobRef.Get<GridManager>();

    public static bool CanBePlaced(Vec2Int POS, BlockHeader1 item, bool front)
    {
        BlockHeader2? blockNullable = GridManager.BlockDataAtPOS(POS);
        bool isLocked = GridManager.IsBlockLocked(POS);

        if (blockNullable != null && !isLocked)
        {
            BlockHeader2 block = blockNullable.Value;
            return BlockInteractions.CanBePlaced(block, item, front);
        }
        return false;
    }

    public static bool CanBeBroken(Vec2Int POS, BlockHeader1 tool, bool front)
    {
        BlockHeader2? blockNullable = GridManager.BlockDataAtPOS(POS);
        bool isLocked = GridManager.IsBlockLocked(POS);

        if (blockNullable != null && !isLocked)
        {
            BlockHeader2 block = blockNullable.Value;

            BlockHeader1 item = front
                ? block.Front
                : block.Back;

            return BlockInteractions.CanBeBroken(block, item, tool, front);
        }
        return false;
    }

    public static void PlaceBlock(Vec2Int POS, BlockHeader1 item, bool front)
    {
        _ = GridManager.BlockDataAtPOS(POS) ??
            throw new InvalidOperationException("Cannot place a block at unloaded position!");

        long operation = GridManager.PlaceBlockClient(POS, item, front);
        SendBlockChange(POS, item, BlockHeader1.Air, front, operation, 0);
    }

    public static void BreakBlock(Vec2Int POS, BlockHeader1 tool, bool front)
    {
        BlockHeader2 oldBlock2 = GridManager.BlockDataAtPOS(POS) ??
            throw new InvalidOperationException("Cannot break a block at unloaded position!");

        BlockHeader1 oldblock1 = front
            ? oldBlock2.Front
            : oldBlock2.Back;

        long operation = GridManager.BreakBlockClient(POS, front);
        SendBlockChange(POS, oldblock1, tool, front, operation, 1);
    }

    private static void SendBlockChange(Vec2Int POS, BlockHeader1 item, BlockHeader1 tool, bool front, long operation, byte code)
    {
        Payload packet = new BlockChange(POS, item, tool, operation, front, code);
        Client.Send(packet);
    }
}
