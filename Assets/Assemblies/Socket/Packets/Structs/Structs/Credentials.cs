#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Payload.Structs.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct Credentials : ISanitizable<Credentials>
{
    public FixedString32 Nickname { get; }
    public FixedString64 Password { get; }
    public long ServerSecret { get; }
    public long ChallengeId { get; }
    public long Timestamp { get; }
    public long RunId { get; }

    public bool IsRegister() => ChallengeId == 0;
    public bool IsLogin() => ChallengeId != 0;

    public Credentials(in FixedString32 nickname, in FixedString64 password, long serverSecret, long challengeId, long timestamp, long runId)
    {
        Nickname = Validation.IsGoodNickname(nickname) ? nickname : new FixedString32(GameInfo.ReservedNickname);
        Password = Validation.IsGoodPassword(password) ? password : new FixedString64(GameInfo.ReservedPassword);
        ServerSecret = serverSecret;
        ChallengeId = challengeId;
        Timestamp = timestamp;
        RunId = runId;
    }

    public Credentials Sanitize()
    {
        return new Credentials(Nickname, Password, ServerSecret, ChallengeId, Timestamp, RunId);
    }
}
