#nullable enable
using Larnix.Core;
using Larnix.Socket.Server.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Larnix.Server.Network;

internal class BanProvider : IBanProvider
{
    private Config Config => GlobRef.Get<Config>();

    public bool IsBannedNickname(string nickname)
    {
        return Config.Administration_Banned.Contains(nickname);
    }

    public bool IsBannedIp(IPAddress address)
    {
        return Config.Administration_Banned.Any(entry => IsInNetworkString(address, entry));
    }

    private static bool IsInNetworkString(IPAddress address, string networkString)
    {
        int index = networkString.IndexOf('/');

        if (index == -1) // single IP
        {
            return IPAddress.TryParse(networkString, out IPAddress singleIP) &&
                singleIP.Equals(address);
        }

        ReadOnlySpan<char> ipSpan = networkString.AsSpan(0, index);
        ReadOnlySpan<char> prefixSpan = networkString.AsSpan(index + 1);

        if (IPAddress.TryParse(ipSpan, out IPAddress networkIp) &&
            address.AddressFamily == networkIp.AddressFamily &&
            int.TryParse(prefixSpan, out int prefixLength))
        {
            int maxPrefix = networkIp.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            if (prefixLength >= 0 && prefixLength <= maxPrefix)
            {
                int fullBytes = prefixLength / 8;
                int remainingBits = prefixLength % 8;

                Span<byte> networkBytes = stackalloc byte[16];
                Span<byte> addressBytes = stackalloc byte[16];

                networkIp.TryWriteBytes(networkBytes, out _);
                address.TryWriteBytes(addressBytes, out _);

                for (int i = 0; i < fullBytes; i++)
                {
                    if (networkBytes[i] != addressBytes[i])
                        return false;
                }

                if (remainingBits > 0)
                {
                    int mask = 0xFF << 8 - remainingBits & 0xFF;
                    if ((networkBytes[fullBytes] & mask) != (addressBytes[fullBytes] & mask))
                        return false;
                }

                return true;
            }
        }

        return false;
    }
}
