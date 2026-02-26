using Larnix.Core.DbStructs;
using Larnix.Core.Utils;

namespace Larnix.Socket.Backend
{
    public readonly struct QuickConfig
    {
        public readonly ushort Port;
        public readonly ushort MaxClients;
        public readonly bool IsLoopback;
        public readonly string DataPath;
        public readonly String256 Motd;
        public readonly String32 HostUser;
        public readonly int MaskIPv4;
        public readonly int MaskIPv6;
        public readonly bool AllowRegistration;
        public readonly IDbUserAccess UserAccess;

        public QuickConfig(ushort port, ushort maxClients, bool isLoopback, string dataPath,
            String256 motd, String32 hostUser, int maskIPv4, int maskIPv6, bool allowRegistration, IDbUserAccess userAccess)
        {
            Port = port;
            MaxClients = maxClients;
            IsLoopback = isLoopback;
            DataPath = dataPath;
            Motd = motd;
            HostUser = hostUser;
            MaskIPv4 = maskIPv4;
            MaskIPv6 = maskIPv6;
            AllowRegistration = allowRegistration;
            UserAccess = userAccess;
        }

        public QuickConfig WithPort(ushort realPort)
        {
            return new QuickConfig(
                port: realPort,
                maxClients: MaxClients,
                isLoopback: IsLoopback,
                dataPath: DataPath,
                motd: Motd,
                hostUser: HostUser,
                maskIPv4: MaskIPv4,
                maskIPv6: MaskIPv6,
                allowRegistration: AllowRegistration,
                userAccess: UserAccess
                );
        }
    }
}
