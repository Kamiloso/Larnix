#nullable enable
using Larnix.Socket.Security;
using System;
using System.Linq;

namespace Larnix.Socket;

public static class SocketInfo
{
    public static ushort DefaultPort => 27682;
    public static ushort DefaultRelayPort => 27681;

    public static string ReservedNickname => "Player";
    public static string ReservedPassword => "SGP_PASSWORD\x01";
    public static string DefaultMotd => "Welcome to the server!";

    public static string WrongNicknameInfo => "Nickname should be 3-16 characters and only use: letters, digits, _ or -.";
    public static string WrongPasswordInfo => "Password should be 7-32 characters and not use white spaces or NULL (0x00).";
    public static string WrongMotdInfo => "Motd should be at most 256 characters and not use NULL (0x00).";

    public static string PathPrivateKey => "qck_private_key";
    public static string PathServerSecret => "qck_server_secret";

    public static bool IsValidNickname(string nickname)
    {
        if (nickname.Length is < 3 or > 16) return false;
        if (nickname.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-'))) return false;
        return true;
    }

    public static bool IsValidPassword(string password)
    {
        if (password.Length is < 7 or > 32) return false;
        if (password.Any(c => char.IsWhiteSpace(c) || c == '\0')) return false;
        return true;
    }

    public static bool IsValidMotd(string motd)
    {
        if (motd.Length > 256) return false;
        if (motd.Any(c => c == '\0')) return false;
        return true;
    }

    public static bool IsValidAuthcode(string authcode)
    {
        try
        {
            _ = new Authcode(authcode);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
