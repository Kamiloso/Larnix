using Larnix.Socket.Packets.Control;
using Version = Larnix.Core.Version;

namespace Larnix.Socket.Packets
{
    public class ServerInfo
    {
        private readonly A_ServerInfo _infoPacket;

        public ushort CurrentPlayers => _infoPacket.CurrentPlayers;
        public ushort MaxPlayers => _infoPacket.MaxPlayers;
        public Version GameVersion => _infoPacket.GameVersion;
        public string Motd => _infoPacket.Motd;
        public string HostUser => _infoPacket.HostUser;

        internal ServerInfo(A_ServerInfo infoPacket)
        {
            _infoPacket = infoPacket;
        }
    }
}
