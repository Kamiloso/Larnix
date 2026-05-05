#nullable enable
using Larnix.Socket;
using System;
using System.Linq;

namespace Larnix.Model.Utils;

public static class Validation
{
    public static string WrongNicknameInfo => SocketInfo.WrongNicknameInfo;
    public static string WrongPasswordInfo => SocketInfo.WrongPasswordInfo;
    public static string WrongWorldNameInfo => "World name should be 1-32 characters, be already trimmed and only use: letters, digits, space, _ or -.";

    public static bool IsGoodNickname(string nickname) => SocketInfo.IsValidNickname(nickname);
    public static bool IsGoodPassword(string password) => SocketInfo.IsValidPassword(password);
    public static bool IsGoodWorldName(string worldName)
    {
        string[] denyWorldNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        if (worldName.Length is < 1 or > 32) return false;
        if (worldName.Any(c => !(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' '))) return false;
        if (worldName != worldName.Trim()) return false;
        if (denyWorldNames.Contains(worldName.ToUpperInvariant())) return false;
        return true;
    }
}
