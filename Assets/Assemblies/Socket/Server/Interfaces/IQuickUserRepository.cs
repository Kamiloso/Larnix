#nullable enable
using Larnix.Socket.Server.Users;

namespace Larnix.Socket.Server.Interfaces;

public interface IQuickUserRepository
{
    long NextFreeUid();
    void SaveUser(QuickUser user);
    QuickUser? FindByNickname(string nickname);
}
