#nullable enable

namespace Larnix.Server.Run.Records;

public record RunAnswer(
    string Address,
    string Authcode
    )
{
    public ushort Port { get; } = ushort.TryParse(Address.Split(':')[^1], out var value) ? value : (ushort)0;
    public string? RelayAddress { get; init; }
}
