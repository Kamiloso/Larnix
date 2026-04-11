#nullable enable
using Larnix.Model.Json;

namespace Larnix.Model.Blocks.Structs;

public class BlockData1
{
    public BlockHeader1 Header { get; private set; }
    public Storage NBT { get; private set; }

    public BlockID ID => Header.Id;
    public byte Variant
    {
        get => Header.Variant;
        set => Header = new BlockHeader1(ID, value);
    }

    public static BlockData1 Air => new(BlockHeader1.Air);
    public static BlockData1 UltimateTool => new(BlockHeader1.UltimateTool);

    public BlockData1(in BlockHeader1 header, Storage? nbt = null)
    {
        Header = header;
        NBT = nbt ?? new();
    }

    public BlockData1(BlockID id, byte variant, Storage? data = null)
    {
        Header = new BlockHeader1(id, variant);
        NBT = data ?? new();
    }

    public BlockData1 DeepCopy()
    {
        return new BlockData1(
            Header, NBT.DeepCopy()
        );
    }

    public override string ToString()
    {
        return Header.ToString();
    }
}
