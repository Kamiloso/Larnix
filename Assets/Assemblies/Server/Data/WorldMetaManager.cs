#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Model;
using Larnix.Model.Utils;
using Larnix.Socket.Backend;
using System;
using Version = Larnix.Model.Version;

namespace Larnix.Server.Data;

internal interface IWorldMetaManager
{
    Version Version { get; }
    FixedString32 HostNickname { get; }
    void EnsureDetachedServer();
}

internal class WorldMetaManager : IWorldMetaManager
{
    private WorldMeta _worldMeta;
    private WorldMeta WorldMeta
    {
        get => _worldMeta;
        set
        {
            _worldMeta = value;
            WorldMeta.SaveToFolder(Server.WorldPath, _worldMeta);
        }
    }

    public Version Version => WorldMeta.Version;
    public FixedString32 HostNickname
    {
        get => (FixedString32)WorldMeta.Nickname;
        private set => WorldMeta = new WorldMeta(WorldMeta.Version, value);
    }

    private IServer Server => GlobRef.Get<IServer>();
    private IUserManager UserManager => GlobRef.Get<IUserManager>();

    public WorldMetaManager()
    {
        var meta = WorldMeta.ReadFromFolder(Server.WorldPath);
        WorldMeta = new WorldMeta(Version.Current, meta.Nickname);
    }

    public void EnsureDetachedServer()
    {
        if (Server.ServerType != ServerType.Remote)
            throw new InvalidOperationException("EnsureDetachedServer() should only be called for headless servers.");

        if (HostNickname == Common.ReservedNickname)
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
                UserManager.TryChangePasswordOrAddUserSync(HostNickname, password);
                HostNickname = new FixedString32(Common.ReservedNickname);
                changeSuccess = true;
            }
            else
            {
                Echo.LogRaw(Validation.WrongPasswordInfo + "\n");
            }

        } while (!changeSuccess);

        Echo.LogRaw("Password changed.\n");
        Echo.PrintBorder();
    }
}
