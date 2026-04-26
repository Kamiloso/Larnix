#nullable enable

namespace Larnix.Socket.Backend.Utility;

public interface IUserRepository
{
    void Save(User user);
    User? ReadByUid(long uid);
}
