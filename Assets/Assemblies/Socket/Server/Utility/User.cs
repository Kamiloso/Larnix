#nullable enable

namespace Larnix.Socket.Server.Utility;

public record User(
    long Uid,
    string Nickname,
    string PasswordHash,
    long ChallengeId
    )
{
    public User AfterLogin() => this with { ChallengeId = ChallengeId + 1 };
    public User AfterNicknameChange(string newNickname) => this with { Nickname = newNickname };
    public User AfterPasswordChange(string newPasswordHash) => this with { PasswordHash = newPasswordHash };

    public static User CreateAccount(long uid, string nickname, string passwordHash)
    {
        return new User(uid, nickname, passwordHash, 1000); // 1000 = arbitrary big number
    }
}
