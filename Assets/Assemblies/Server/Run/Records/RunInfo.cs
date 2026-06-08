#nullable enable
using System.IO;

namespace Larnix.Server.Run.Records;

public record RunInfo(
    RunMode Mode,
    string SavesPath,
    string WorldName
    )
{
    public string WorldPath { get; } = Path.Combine(SavesPath, WorldName);
    public long? Seed { get; init; }
    public string? RelayAddress { get; init; }
}
