#nullable enable

namespace Larnix.Socket.Server.Interfaces;

public interface IPasswordHasher
{
    public string HashPassword(string password);
    public bool VerifyPassword(string password, string storedHash);
}
