#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Helpers.Records;

public record ServerLogin(
    FixedString32 Nickname,
    FixedString64 Password
    );
