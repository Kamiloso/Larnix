#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;

namespace Larnix.Socket.Tools;

public static class SocketSanitizer
{
    public static FixedString32 ToGoodNickname(in FixedString32 nickname)
    {
        return Validation.IsGoodNickname(nickname)
            ? nickname
            : new FixedString32(GameInfo.ReservedNickname);
    }

    public static FixedString64 ToGoodPassword(in FixedString64 password)
    {
        return Validation.IsGoodPassword(password)
            ? password
            : new FixedString64(GameInfo.ReservedPassword);
    }

    public static FixedString256 ToGoodMotd(in FixedString256 text)
    {
        return Validation.IsGoodText<FixedString256>(text)
            ? text
            : new FixedString256(GameInfo.DefaultMotd);
    }
}
