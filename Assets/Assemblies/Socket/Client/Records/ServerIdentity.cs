#nullable enable

namespace Larnix.Socket.Client.Records;

public record ServerIdentity(
    string Address,
    string Authcode
    );
