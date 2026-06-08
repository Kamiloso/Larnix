#nullable enable

namespace Larnix.Socket.Server.Users;

public record QuickUser(
    long Uid,
    string Nickname,
    string PasswordHash,
    long ChallengeId
    )
{
    public QuickUser AfterLogin() => this with { ChallengeId = ChallengeId + 1 };
    public QuickUser AfterNicknameChange(string newNickname) => this with { Nickname = newNickname };
    public QuickUser AfterPasswordChange(string newPasswordHash) => this with { PasswordHash = newPasswordHash };

    public static QuickUser CreateAccount(long uid, string nickname, string passwordHash)
    {
        return new QuickUser(uid, nickname, passwordHash, 1000); // 1000 = arbitrary big number
    }
}
