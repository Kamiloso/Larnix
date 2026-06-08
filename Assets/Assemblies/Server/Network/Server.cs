#nullable enable
using Larnix.Core;
using Larnix.Server.Data;
using Larnix.Socket.Payload;
using Larnix.Socket.Server;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Server.Interfaces;
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Server.Repositories;
using Larnix.Server.Run.Records;
using Larnix.Socket.Server.Configuration;

namespace Larnix.Server.Network;

internal interface IServer : ITickable, IDisposable
{
    ushort Port { get; }
    string Authcode { get; }
    ushort PlayerCount { get; }
    ushort MaxPlayers { get; }

    void Send<T>(string nickname, T payload) where T : unmanaged;
    void SendUnreliable<T>(string nickname, T payload) where T : unmanaged;
    void Broadcast<T>(T payload) where T : unmanaged;
    void BroadcastUnreliable<T>(T payload) where T : unmanaged;

    void OnConnected(ConnectedHandler execute);
    void OnDisconnected(DisconnectedHandler execute);
    void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged;

    void KickRequest(string nickname);
    void ResetLimiters();
}

internal class Server : IServer
{
    public ushort Port => _quickServer.Port;
    public string Authcode => _quickServer.Authcode;
    public ushort PlayerCount => _quickServer.PlayerCount;
    public ushort MaxPlayers => _quickServer.MaxPlayers;

    private Config Config => GlobRef.Get<Config>();
    private RunInfo RunInfo => GlobRef.Get<RunInfo>();
    private RunCallbacks RunCallbacks => GlobRef.Get<RunCallbacks>();
    private IWorldMetaManager WorldMetaManager => GlobRef.Get<IWorldMetaManager>();

    private IValueRepository ValueRepository => GlobRef.Get<IValueRepository>();
    private IUserRepository UserRepository => GlobRef.Get<IUserRepository>();
    private IPasswordHasher PasswordHasher => GlobRef.Get<IPasswordHasher>();
    private IBanProvider BanProvider => GlobRef.Get<IBanProvider>();

    private readonly QuickServer _quickServer;
    private readonly Receiver _receiver;

    private bool _disposed = false;

    public Server()
    {
        Task<QuickServer> loadingServer = QuickServer.CreateServerAsync(
            new QuickSettings(
                Port: GetPort(),
                MaxPlayers: Config.MaxPlayers,
                IsLoopback: RunInfo.Mode == RunMode.Local,
                EnableRegister: Config.Network_AllowRegistration,
                Motd: new FixedString256(Config.Motd),
                HostUser: WorldMetaManager.HostNickname,
                Version: GameInfo.Version,
                RelayAddress: GetRelayAddress()
                ),
            new QuickInterfaces(
                SecretRepository: ValueRepository,
                UserRepository: UserRepository,
                PasswordHasher: PasswordHasher,
                BanProvider: BanProvider
                )
            // IMPORTANT:
            // TODO: add security to config
            );

        _quickServer = loadingServer.Result; // sync is temporary, for simplicity
        _receiver = new Receiver(this);

        RunCallbacks.Answer(
            new RunAnswer(
                Address: $"localhost:{Port}",
                Authcode: Authcode
            ){
                RelayAddress = _quickServer.RelayAddress
            });
    }

    private ushort GetPort()
    {
        return RunInfo.Mode switch
        {
            RunMode.Local or RunMode.Host => 0,
            RunMode.Remote => Config.Port,
            _ => default
        };
    }

    private string? GetRelayAddress()
    {
        return RunInfo.Mode switch
        {
            RunMode.Local => null,
            RunMode.Host => RunInfo.RelayAddress,
            RunMode.Remote when Config.Network_UseRelay => Config.Network_RelayAddress,
            _ => default
        };
    }

    public void Tick(float deltaTime)
    {
        _quickServer.Tick(deltaTime);
        _receiver.Tick(deltaTime);
    }

    public void Send<T>(string nickname, T payload) where T : unmanaged => _quickServer.Send(nickname, payload);
    public void SendUnreliable<T>(string nickname, T payload) where T : unmanaged => _quickServer.SendUnreliable(nickname, payload);
    public void Broadcast<T>(T payload) where T : unmanaged => _quickServer.Broadcast(payload);
    public void BroadcastUnreliable<T>(T payload) where T : unmanaged => _quickServer.BroadcastUnreliable(payload);

    public void OnConnected(ConnectedHandler execute) => _quickServer.OnConnected(execute);
    public void OnDisconnected(DisconnectedHandler execute) => _quickServer.OnDisconnected(execute);
    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged => _quickServer.OnReceive(execute);

    public void KickRequest(string nickname) => _quickServer.KickRequest(nickname);
    public void ResetLimiters() => _quickServer.ResetLimiters();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _quickServer.Dispose();
    }
}
