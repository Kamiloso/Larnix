#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Socket.Payload;
using Larnix.Socket.Server;
using System;
using System.IO;

namespace Larnix.Server;

internal interface IServer
{
    ServerType ServerType { get; }
    ushort Port { get; }
    string LocalAddress { get; }
    string Authcode { get; }
    string WorldPath { get; }
    string SocketPath { get; }

    void PrintHelloToConsole();

    void Send<T>(string nickname, T payload) where T : unmanaged;
    void Broadcast<T>(T payload) where T : unmanaged;
    void SendUnreliable<T>(string nickname, T payload) where T : unmanaged;
    void BroadcastUnreliable<T>(T payload) where T : unmanaged;
    void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged;

    void Close();
}

internal class Server : IServer
{
    public ServerType ServerType { get; }
    public string WorldPath { get; }
    private Action CloseServer { get; }

    public ushort Port => QuickServer.Settings.Port;
    public string LocalAddress => "localhost:" + Port;
    public string Authcode => QuickServer.Authcode;
    public string SocketPath => Path.Combine(WorldPath, "Socket");

    private QuickServer QuickServer => GlobRef.Get<QuickServer>();

    public Server(ServerType serverType, string worldPath, Action closeServer)
    {
        ServerType = serverType;
        WorldPath = worldPath;
        CloseServer = closeServer;
    }

    public void PrintHelloToConsole()
    {
        if (ServerType == ServerType.Remote)
        {
            Echo.SetTitle("Larnix Server " + GameInfo.Version);
            Echo.PrintBorder();

            Echo.LogRaw($"Socket created on port: {Port}\n");
            Echo.LogRaw($"Authcode: {Authcode}\n");
            Echo.PrintBorder();
        }
        else
        {
            Echo.Log($"Port: {Port} | Authcode: {Authcode}");
        }
    }

    public void Send<T>(string nickname, T payload) where T : unmanaged => QuickServer.Send(nickname, payload);
    public void Broadcast<T>(T payload) where T : unmanaged => QuickServer.Broadcast(payload);
    public void SendUnreliable<T>(string nickname, T payload) where T : unmanaged => QuickServer.SendUnreliable(nickname, payload);
    public void BroadcastUnreliable<T>(T payload) where T : unmanaged => QuickServer.BroadcastUnreliable(payload);
    public void OnReceive<T>(CmdSenderHandler<T>? execute) where T : unmanaged => QuickServer.OnReceive(execute);

    public void Close() => CloseServer();
}
