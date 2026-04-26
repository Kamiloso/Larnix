#nullable enable

namespace Larnix.Socket.Backend.Utility;

public record User(
    long Uid,
    string Username,
    string PasswordHash,
    long ChallengeId
    )
{
    public User AfterLogin() => this with { ChallengeId = ChallengeId + 1 };
    public User AfterUsernameChange(string newUsername) => this with { Username = newUsername };
    public User AfterPasswordChange(string newPasswordHash) => this with { PasswordHash = newPasswordHash };
}
