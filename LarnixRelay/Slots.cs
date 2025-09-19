using System.Collections.Generic;
using System;

namespace Larnix.Relay
{
    public static class Slots
    {
        private static HashSet<ushort> ReservedPorts = new HashSet<ushort>();
        private static ushort _portEyes = Config.MaxPort; // the last used port
        private static object _lock = new object();

        public static ushort ReserveClientPort(ushort preferredPort)
        {
            lock (_lock)
            {
                if (ReservedPorts.Add(preferredPort))
                    return preferredPort;

                int range = Config.MaxPort - Config.MinPort + 1;
                for (int i = 0; i < range; i++)
                {
                    _portEyes = (ushort)(_portEyes < Config.MaxPort ? _portEyes + 1 : Config.MinPort);

                    if (ReservedPorts.Add(_portEyes))
                        return _portEyes;
                }
            }

            throw new NoAvailablePortException();
        }

        public static void DisposeClientPort(ushort port)
        {
            lock (_lock)
            {
                if (ReservedPorts.Contains(port))
                {
                    ReservedPorts.Remove(port);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot dispose port {port}");
                }
            }
        }
    }
}
