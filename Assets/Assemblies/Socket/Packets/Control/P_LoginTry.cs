using Larnix.GameCore.Utils;
using Larnix.Core.Binary;
using Larnix.Core.Misc;

namespace Larnix.Socket.Packets.Control
{
    internal sealed class P_LoginTry : Payload
    {
        private const int SIZE = 32 + 64 + 64 + 8 + 8 + 8 + 8;

        public String32 Nickname => Primitives.FromBytes<String32>(Bytes, 0); // 32B
        public String64 Password => Primitives.FromBytes<String64>(Bytes, 32); // 64B
        public String64 NewPassword => Primitives.FromBytes<String64>(Bytes, 96); // 64B
        public long ServerSecret => Primitives.FromBytes<long>(Bytes, 160); // 8B
        public long ChallengeID => Primitives.FromBytes<long>(Bytes, 168); // 8B
        public long Timestamp => Primitives.FromBytes<long>(Bytes, 176); // 8B
        public long RunID => Primitives.FromBytes<long>(Bytes, 184); // 8B

        public P_LoginTry(in String32 nickname, in String64 password, long serverSecret,
            long challengeID, long timestamp, long runID, in String64? newPassword = null, byte code = 0)
        {
            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(nickname),
                Primitives.GetBytes(password),
                Primitives.GetBytes(newPassword ?? password),
                Primitives.GetBytes(serverSecret),
                Primitives.GetBytes(challengeID),
                Primitives.GetBytes(timestamp),
                Primitives.GetBytes(runID)
                ), code);
        }

        public bool IsPasswordChange()
        {
            return Password != NewPassword;
        }

        protected override bool IsValid()
        {
            return Bytes.Length == SIZE &&
                Validation.IsGoodNickname(Nickname) &&
                Validation.IsGoodPassword(Password) &&
                Validation.IsGoodPassword(NewPassword);
        }
    }
}
