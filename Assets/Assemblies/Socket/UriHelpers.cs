#nullable enable
using System;

namespace Larnix.Socket;

internal static class UriHelpers
{
    public static string FormatAddress(string address, ushort port)
    {
        UriBuilder uri = new("udp://" + address)
        {
            Port = port
        };

        return uri.ToString()
            .Replace("udp://", "")
            .Replace("/", "");
    }
}
