using Larnix.Core;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets;
using System;
using System.IO;
using Version = Larnix.Model.Version;

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

    void Send(string nickname, Payload_Legacy payload);
    void Broadcast(Payload_Legacy payload);
    void SendFast(string nickname, Payload_Legacy payload);
    void BroadcastFast(Payload_Legacy payload);
    void Close();
}

internal class Server : IServer
{
    public ServerType ServerType { get; }
    public string WorldPath { get; }
    private Action CloseServer { get; }

    public ushort Port => QuickServer.Port;
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
            Echo.SetTitle("Larnix Server " + Version.Current);
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

    public void Send(string nickname, Payload_Legacy payload) => QuickServer.Send(nickname, payload);
    public void Broadcast(Payload_Legacy payload) => QuickServer.Broadcast(payload);
    public void SendFast(string nickname, Payload_Legacy payload) => QuickServer.Send(nickname, payload, false);
    public void BroadcastFast(Payload_Legacy payload) => QuickServer.Broadcast(payload, false);
    public void Close() => CloseServer();
}
