#nullable enable
using System;
using System.Threading.Tasks;

namespace Larnix.Socket.Networking;

// WARNING: This class should be fully thread-safe!

internal class RelayClient : INetworkInteractions, IDisposable
{
    public Task<string?> ForeignAddressTask { get; }

    private volatile RelayConnection? _relay;

    private readonly object _stateChangeLock = new();
    private bool _disposed = false;

    public RelayClient(string? address)
    {
        ForeignAddressTask = address != null
            ? Task.Run(() => EstablishRelayConnectionAsync(address))
            : Task.FromResult<string?>(null);
    }

    private async Task<string?> EstablishRelayConnectionAsync(string address)
    {
        RelayConnection? estRelay = await RelayConnection.EstablishRelayAsync(address);

        lock (_stateChangeLock)
        {
            if (!_disposed)
            {
                _relay = estRelay;
            }
            else
            {
                estRelay?.Dispose();
            }
        }

        RelayConnection? relay = _relay;
        return relay?.ForeignAddress;
    }
    
    public void Send(DataBox payload)
    {
        RelayConnection? relay = _relay;
        relay?.Send(payload);
    }

    public bool TryReceive(out DataBox result)
    {
        RelayConnection? relay = _relay;
        if (relay != null)
        {
            return relay.TryReceive(out result);
        }
        else
        {
            result = null!;
            return false;
        }
    }

    public void Dispose()
    {
        lock (_stateChangeLock)
        {
            if (_disposed) return;
            _disposed = true;

            _relay?.Dispose();
        }
    }
}
