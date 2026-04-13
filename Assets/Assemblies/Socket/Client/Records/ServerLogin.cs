#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Client.Records;

public record ServerLogin(
    FixedString32 Nickname,
    FixedString64 Password
    );
