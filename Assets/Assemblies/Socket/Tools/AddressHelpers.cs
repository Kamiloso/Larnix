#nullable enable
using System;

namespace Larnix.Socket.Helpers;

internal static class AddressHelpers
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
