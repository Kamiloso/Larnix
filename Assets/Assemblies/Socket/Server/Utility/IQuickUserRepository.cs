#nullable enable

namespace Larnix.Socket.Server.Utility;

public interface IQuickUserRepository
{
    long NextFreeUid();
    void SaveUser(QuickUser user);
    QuickUser? FindByNickname(string nickname);
}
