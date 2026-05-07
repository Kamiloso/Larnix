#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;
using Larnix.Server.Repositories;
using System;
using Version = Larnix.Core.Version;

namespace Larnix.Server.Data;

internal interface IWorldMetaManager
{
    Version Version { get; }
    FixedString32 HostNickname { get; }
    void EnsureDetachedServer();
}

internal class WorldMetaManager : IWorldMetaManager
{
    private WorldMeta _worldMeta = WorldMeta.Default;
    private WorldMeta WorldMeta
    {
        get => _worldMeta;
        set => WorldMeta.SaveToFolder(ServerInfo.WorldPath, _worldMeta = value);
    }

    public Version Version => WorldMeta.Version;
    public FixedString32 HostNickname
    {
        get => new(WorldMeta.Nickname);
        private set => WorldMeta = new WorldMeta(WorldMeta.Version, value);
    }

    private IServerInfo ServerInfo => GlobRef.Get<IServerInfo>();
    private IUserRepository UserRepository => GlobRef.Get<IUserRepository>();

    public WorldMetaManager()
    {
        WorldMeta? meta = WorldMeta.ReadFromFolder(ServerInfo.WorldPath);
        WorldMeta = new WorldMeta(GameInfo.Version, meta.Nickname);
    }

    public void EnsureDetachedServer()
    {
        if (ServerInfo.Type != ServerType.Remote)
            throw new InvalidOperationException("EnsureDetachedServer() should only be called for headless servers.");

        if (HostNickname == GameInfo.ReservedNickname)
            return;

        Echo.LogRaw($"This world was previously in use by {HostNickname}.\n");
        Echo.LogRaw($"Choose a password for this player to start the server.\n");

        bool changeSuccess = false;
        do
        {
            Echo.LogRaw("> ");
            string password = Echo.ReadLineSync();

            if (Validation.IsGoodPassword(password))
            {
                UserRepository.SetUserSync(HostNickname, password);
                HostNickname = new FixedString32(GameInfo.ReservedNickname);
                changeSuccess = true;
            }
            else
            {
                Echo.LogRaw($"{Validation.WrongPasswordInfo}\n");
            }

        } while (!changeSuccess);

        Echo.LogRaw("Password changed.\n");
        Echo.PrintBorder();
    }
}
