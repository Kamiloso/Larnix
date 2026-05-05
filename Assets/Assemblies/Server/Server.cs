#nullable enable
using Larnix.Core;
using Larnix.Server.Data;
using Larnix.Server.Transmission;
using Larnix.Socket.Payload;
using Larnix.Socket.Server;
using System;
using System.Threading.Tasks;
using Larnix.Socket.Server.Interfaces;
using Larnix.Core.Serialization;
using Larnix.Socket.Server.Utility;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using Larnix.Model;

namespace Larnix.Server;

internal interface IServer : ITickable, IDisposable
{
    ushort Port { get; }
    string LocalAddress { get; }
    string Authcode { get; }
    ushort PlayerCount { get; }
    ushort MaxPlayers { get; }

    void Send<T>(string nickname, T payload) where T : unmanaged;
    void Broadcast<T>(T payload) where T : unmanaged;
    void SendUnreliable<T>(string nickname, T payload) where T : unmanaged;
    void BroadcastUnreliable<T>(T payload) where T : unmanaged;
    void KickRequest(string nickname);

    void OnConnected(ConnectedHandler execute);
    void OnDisconnected(DisconnectedHandler execute);
    void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged;

    void Close();
}

internal class Server : IServer
{
    public ushort Port => _quickServer.Port;
    public string LocalAddress => $"localhost:{Port}";
    public string Authcode => _quickServer.Authcode;
    public ushort PlayerCount => _quickServer.PlayerCount;
    public ushort MaxPlayers => _quickServer.MaxPlayers;

    private ServerConfig Config => GlobRef.Get<ServerConfig>();
    private IServerInfo ServerInfo => GlobRef.Get<IServerInfo>();
    private IWorldMetaManager WorldMetaManager => GlobRef.Get<IWorldMetaManager>();

    private readonly QuickServer _quickServer;
    private readonly Receiver _receiver;

    private readonly RunSuggestions _suggestions;
    private readonly Action _closeServer;

    public Server(RunSuggestions suggestions, Action closeServer, out Task<string?>? relayTask)
    {
        _suggestions = suggestions;
        _closeServer = closeServer;

        Task<QuickServer> loadingServer = QuickServer.CreateServerAsync(
            new QuickSettings(
                port: Config.Port,
                maxPlayers: Config.MaxPlayers,
                isLoopback: ServerInfo.Type == ServerType.Local,
                enableRegister: Config.Network_AllowRegistration,
                motd: new FixedString256(Config.Motd),
                hostUser: WorldMetaManager.HostNickname,
                version: GameInfo.Version,
                interfaces: new QuickSettings.InterfacesStruct(
                    SecretRepository: GlobRef.Get<IValueRepository>(),
                    UserRepository: GlobRef.Get<IUserRepository>(),
                    PasswordHasher: GlobRef.Get<IPasswordHasher>(),
                    BanProvider: GlobRef.Get<IBanProvider>()
                    ),
                security: null, // TODO: add configuration
                relayAddress: suggestions.RelayAddress
                ));

        _quickServer = loadingServer.Result; // sync is temporary, for simplicity
        _receiver = new Receiver();

        relayTask = EstablishRelayTask(suggestions.RelayAddress);
    }

    private Task<string?>? EstablishRelayTask(string? suggestion)
    {
        bool remoteUse = ServerInfo.Type == ServerType.Remote && Config.Network_UseRelay;
        bool hostUse = ServerInfo.Type == ServerType.Host && suggestion != null;

        return remoteUse || hostUse
            ? Task.FromResult(_quickServer.RelayAddress)
            : null;
    }

    public void Tick(float deltaTime)
    {
        _quickServer.Tick(deltaTime);
        _receiver.Tick(deltaTime);
    }

    public void Send<T>(string nickname, T payload) where T : unmanaged => _quickServer.Send(nickname, payload);
    public void Broadcast<T>(T payload) where T : unmanaged => _quickServer.Broadcast(payload);
    public void SendUnreliable<T>(string nickname, T payload) where T : unmanaged => _quickServer.SendUnreliable(nickname, payload);
    public void BroadcastUnreliable<T>(T payload) where T : unmanaged => _quickServer.BroadcastUnreliable(payload);
    public void KickRequest(string nickname) => _quickServer.KickRequest(nickname);

    public void OnConnected(ConnectedHandler execute) => _quickServer.OnConnected(execute);
    public void OnDisconnected(DisconnectedHandler execute) => _quickServer.OnDisconnected(execute);
    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged => _quickServer.OnReceive(execute);

    public void Close() => _closeServer.Invoke();

    public void Dispose()
    {
        _quickServer.Dispose();
    }
}
