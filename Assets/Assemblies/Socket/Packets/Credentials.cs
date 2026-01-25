using Larnix.Core.Utils;
using Larnix.Socket.Packets.Control;
using Version = Larnix.Core.Version;

namespace Larnix.Socket.Packets
{
    internal class Credentials
    {
        public readonly String32 Nickname;
        public readonly String64 Password;
        public readonly long ServerSecret;
        public readonly long ChallengeID;
        public readonly long Timestamp;
        public readonly long RunID;

        public readonly String64? NewPassword;

        internal Credentials(P_LoginTry infoPacket, String64? newPassword = null)
        {
            Nickname = infoPacket.Nickname;
            Password = infoPacket.Password;
            ServerSecret = infoPacket.ServerSecret;
            ChallengeID = infoPacket.ChallengeID;
            Timestamp = infoPacket.Timestamp;
            RunID = infoPacket.RunID;

            NewPassword = newPassword;
        }
    }
}
