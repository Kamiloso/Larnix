#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Payload;

internal static class SocketSanitizer
{
    public static FixedString32 ToGoodNickname(FixedString32 nickname)
    {
        nickname = Sanitizer.Filter(nickname);
        return SocketInfo.IsValidNickname(nickname)
            ? nickname
            : new FixedString32(SocketInfo.ReservedNickname);
    }

    public static FixedString64 ToGoodPassword(FixedString64 password)
    {
        password = Sanitizer.Filter(password);
        return SocketInfo.IsValidPassword(password)
            ? password
            : new FixedString64(SocketInfo.ReservedPassword);
    }

    public static FixedString256 ToGoodMotd(FixedString256 text)
    {
        text = Sanitizer.Filter(text);
        return SocketInfo.IsValidMotd(text)
            ? text
            : new FixedString256(SocketInfo.DefaultMotd);
    }
}
