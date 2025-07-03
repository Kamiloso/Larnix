using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Larnix.Socket
{
    public static class DnsResolver
    {
        public static IPEndPoint ResolveString(string address)
        {
            var uri = new Uri($"udp://{address}");
            var ipAddresses = Dns.GetHostAddresses(uri.Host);
            return new IPEndPoint(ipAddresses[0], uri.Port);
        }
    }
}
