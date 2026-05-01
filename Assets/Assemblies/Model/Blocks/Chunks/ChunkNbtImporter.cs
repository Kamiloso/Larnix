#nullable enable
using Larnix.Model.Blocks;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Json;
using SimpleJSON;

namespace Larnix.Model.Blocks.Chunks;

internal static class ChunkNbtImporter
{
    public static void ImportData(ChunkData chunk, string? chunkJson)
    {
        JSONObject root = JsonUtils.ToJsonObject(chunkJson);
        ChunkIterator.Iterate((x, y) =>
        {
            string key;
            Storage? s1 = null, s2 = null;

            key = "F_" + x + "_" + y;
            if (root[key] is JSONString node1)
                s1 = Storage.FromString(node1.Value);

            key = "B_" + x + "_" + y;
            if (root[key] is JSONString node2)
                s2 = Storage.FromString(node2.Value);

            BlockData2 old = chunk[x, y];
            chunk[x, y] = new BlockData2(
                new(old.Front.ID, old.Front.Variant, s1 ?? null),
                new(old.Back.ID, old.Back.Variant, s2 ?? null)
            );
        },
        IterationOrder.XY);
    }

    public static string ExportData(ChunkData chunk)
    {
        JSONObject root = new();
        ChunkIterator.Iterate((x, y) =>
        {
            string key, value;

            key = $"F_{x}_{y}";
            if ((value = chunk[x, y].Front.NBT.ToString()) != "{}")
                root[key] = new JSONString(value);

            key = $"B_{x}_{y}";
            if ((value = chunk[x, y].Back.NBT.ToString()) != "{}")
                root[key] = new JSONString(value);
        },
        IterationOrder.XY);
        return root.ToString();
    }
}
