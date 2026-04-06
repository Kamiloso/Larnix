#nullable enable
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Model.Worldgen.Biomes;
using System.Collections.ObjectModel;
using Larnix.Model.Worldgen.Transformers;
using Larnix.Model.Worldgen.Transformers.Pipeline;

namespace Larnix.Model.Worldgen;

internal enum ProtoBlock : ushort
{
    Sky = 0,
    Stone = 1,
    Dirt = 2,
    Surface = 3,
    Cave = 4,
    Lake = 5
}

public interface IGenerator
{
    long Seed { get; }
    ChunkData GenerateChunk(Vec2Int chunk);
    Col32 SkyColorAt(Vec2 position);
    BiomeID BiomeAt(Vec2 position);
}

public class Generator : IGenerator
{
    public long Seed => SeedObj.Value;
    internal Seed SeedObj  { get; }
    internal ReadOnlyDictionary<BiomeID, Biome> Biomes { get; }

    private readonly UsefulBag _usefulBag;
    private readonly GenPipeline _genPipeline;

    public Generator(long seed)
    {
        SeedObj = new Seed(seed);
        Biomes = EnumFactory<BiomeID, Biome>.CreateDictionary((typeof(Seed), SeedObj));

        _usefulBag = new UsefulBag(this);
        UsefulBag ub = _usefulBag;

        _genPipeline = new GenPipeline
        (
            new IdentifyBiomes(ub),
            new BuildBaseTerrain(ub),
            new DrillCaves(ub),
            new ApplyHeaders(ub),
            new AddOreClusters(ub),
            new ApplyRealBlocks(ub)
        );
    }

    public ChunkData GenerateChunk(Vec2Int chunk)
    {
        BlockData2[,] blocks = _genPipeline.Run(chunk);

        ChunkData chunkData = new();
        ChunkIterator.Iterate((x, y) => chunkData[x, y] = blocks[x, y]);
        return chunkData;
    }

    public Col32 SkyColorAt(Vec2 position)
    {
        double temperature = _usefulBag.Providers["TEMPERATURE"].GetValue(position);

        Col32 arcticSkyColor = Biomes[BiomeID.Arctic].SkyColor;
        Col32 plainsSkyColor = Biomes[BiomeID.Plains].SkyColor;
        Col32 desertSkyColor = Biomes[BiomeID.Desert].SkyColor;

        return temperature switch
        {
            < -0.22 => arcticSkyColor,
            < -0.21 => Col32.Lerp(arcticSkyColor, plainsSkyColor, (temperature + 0.22) / 0.01),
            < 0.21  => plainsSkyColor,
            < 0.22  => Col32.Lerp(plainsSkyColor, desertSkyColor, (temperature - 0.21) / 0.01),
            _       => desertSkyColor
        };
    }

    public BiomeID BiomeAt(Vec2 position)
    {
        const string Phrase = "block_hash";
        double temperature = _usefulBag.Providers["TEMPERATURE"].GetValue(position.x, position.y);
        Vec2Int POS = BlockUtils.CoordsToBlock(position);

        return temperature switch
        {
            < -0.22 => BiomeID.Arctic,
            < -0.21 => Utils.ValueFromGradient(BiomeID.Arctic, BiomeID.Plains, (temperature + 0.22) / 0.01, SeedObj.BlockHash(POS, Phrase)),
            < 0.21  => BiomeID.Plains,
            < 0.22  => Utils.ValueFromGradient(BiomeID.Plains, BiomeID.Desert, (temperature - 0.21) / 0.01, SeedObj.BlockHash(POS, Phrase)),
            _       => BiomeID.Desert
        };
    }

    internal Biome BiomeObjectAt(Vec2 position)
    {
        return Biomes[BiomeAt(position)];
    }
}
