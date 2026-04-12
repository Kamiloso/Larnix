#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Larnix.Socket;

internal static class DnsResolver
{
    public static async Task<IPEndPoint?> ResolveAsync(string? address, ushort defaultPort)
    {
        if (address == null) return null;

        if (address.EndsWith(']') || !address.Contains(':'))
        {
            address += ":" + defaultPort;
        }

        if (address.Count(c => c == ':') >= 2 && !address.StartsWith("[") && !address.EndsWith("]"))
        {
            address = '[' + address + "]:" + defaultPort;
        }

        string? iface = null;
        int p1 = address.IndexOf('%');
        if (p1 != -1)
        {
            int p2 = p1 + 1;
            while (p2 < address.Length && char.IsDigit(address[p2]))
            {
                p2++;
            }

            iface = address.Substring(p1 + 1, p2 - p1 - 1);
            address = address.Remove(p1, p2 - p1);
        }

        try
        {
            var uri = new Uri($"udp://{address}");
            var ipAddresses = await Dns.GetHostAddressesAsync(uri.Host);
            
            if (iface != null)
            {
                ipAddresses[0].ScopeId = int.Parse(iface);
            }

            return new IPEndPoint(ipAddresses[0], uri.Port);
        }
        catch
        {
            return null;
        }
    }
}
