#nullable enable
using Larnix.Socket.Limiting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Security.Encryption;

namespace Larnix.Socket.Server.Channel;

internal class AsyncDecryptor
{
    private readonly MainServices _services;

    public AsyncDecryptor(MainServices services)
    {
        _services = services;
    }

    public IEnumerable<(bool Success, byte[] Decrypted)?> Decrypt(string cidr, byte[] data, IDecryptor? key)
    {
        var limiter = _services.Limiters.ConcurrentRsaDecryptions;
        using var _ = LimitHolder.Acquire(limiter, cidr, out bool acquired);

        if (!acquired)
        {
            yield break; // couldn't acquire, ignore request
        }

        Task<(bool, byte[])> decryption = Task.Run(() =>
        {
            bool success = NetworkSerializer.TryDecryptNetworkBytes(data, key, out byte[] decrypted);
            return (success, decrypted);
        });

        while (!decryption.IsCompleted)
        {
            yield return null; // wait for decryption to complete
        }

        yield return decryption.Result;
    }
}
