#nullable enable
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Socket.Tools;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Credentials : ISanitizable<Credentials>
{
    public FixedString32 Nickname { get; }
    public FixedString64 Password { get; }
    public long ServerSecret { get; }
    public long ChallengeId { get; }
    public long Timestamp { get; }
    public long RunId { get; }

    public bool IsRegister() => ChallengeId == 0;
    public bool IsLogin() => ChallengeId != 0;
    public bool IsLoopbackOnly() =>
        Nickname == SocketInfo.ReservedNickname ||
        Password == SocketInfo.ReservedPassword;

    public Credentials(in FixedString32 nickname, in FixedString64 password, long serverSecret, long challengeId, long timestamp, long runId)
    {
        Nickname = SocketSanitizer.ToGoodNickname(nickname);
        Password = SocketSanitizer.ToGoodPassword(password);
        ServerSecret = serverSecret;
        ChallengeId = challengeId;
        Timestamp = timestamp;
        RunId = runId;
    }

    public Credentials Sanitize()
    {
        return new Credentials(Nickname, Password, ServerSecret, ChallengeId, Timestamp, RunId);
    }

    public (FixedString32 nickname, FixedString64 password, long challengeId) ExtractLoginData()
    {
        return (Nickname, Password, ChallengeId);
    }
}
