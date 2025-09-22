using System.Collections.Generic;
using System;

namespace Larnix.Relay
{
    public static class Config
    {
        // Basic config
        public const int MaxServersGlobally = 10; // at least 100 KB/s per server recommended
        public const int MaxServersPerIP = 1;
        public const int ServerLifetime = 15_000; // miliseconds
        public const int MaxTransferPerSecond = 1024 * 1024; // MB/s (will be split between all active servers)
        public const int MaxMessageLength = 1450; // bytes, 1500 = MTU

        // Socket on server port
        public const ushort ServerPort = 27681;
        public const int ReceiveBufferServer = 5 * 1024 * 1024; // bytes
        public const int SendBufferServer = 2 * 1024 * 1024; // bytes

        // Sockets on client ports - ports must be free at all times
        public const ushort MinPort = 30_100;
        public const ushort MaxPort = 30_999;
        public const int ReceiveBufferClient = 512 * 1024; // bytes
        public const int SendBufferClient = 256 * 1024; // bytes

        // Debugging
        public const int ArtificialPing = 0; // miliseconds
        public const int ArtificialJitter = 0; // miliseconds (ping randomness)
    }
}
