#nullable enable
using Larnix.Core.Limiters;
using Larnix.Socket.Server.Utility;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larnix.Socket.Server;

internal class AsyncDecryptor
{
    private readonly TrafficLimiter<string> _decryptionLimiter;

    public AsyncDecryptor(QuickSettings settings)
    {
        _decryptionLimiter = new TrafficLimiter<string>(
            maxTrafficLocal: settings.Security.Limiters.Decryptions.PerNetwork,
            maxTrafficGlobal: settings.Security.Limiters.Decryptions.Global
            );
    }

    public IEnumerable<(bool Success, byte[] Decrypted)?> Decrypt(string cidr, byte[] data, IKey? key)
    {
        using var _h1_ = LimitHolder.Acquire(_decryptionLimiter, cidr, out bool acquired);

        if (!acquired)
        {
            yield break; // couldn't acquire, ignore request
        }

        IKey? keyClone = key?.CloneKey(); // clone to avoid race conditions

        Task<(bool, byte[])> decryption = Task.Run(() =>
        {
            bool success = NetworkSerializer.TryDecryptNetworkBytes(data, keyClone, out byte[] decrypted);
            keyClone?.Dispose();
            return (success, decrypted);
        });

        while (!decryption.IsCompleted)
        {
            yield return null; // wait for decryption to complete
        }

        yield return decryption.Result;
    }
}
