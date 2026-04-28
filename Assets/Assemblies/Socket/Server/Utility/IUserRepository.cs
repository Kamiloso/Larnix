#nullable enable

namespace Larnix.Socket.Server.Utility;

public interface IUserRepository
{
    long NextFreeUid();
    void SaveUser(User user);
    User? FindByUid(long uid);
    User? FindByNickname(string nickname);
}
