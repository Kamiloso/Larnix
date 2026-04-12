#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Helpers.Records;

public record PasswordChangeData(
    string Address,
    string Authcode,
    FixedString32 Nickname,
    FixedString64 Password,
    FixedString64 NewPassword

    ) : FullLoginData(Address, Authcode, Nickname, Password);
