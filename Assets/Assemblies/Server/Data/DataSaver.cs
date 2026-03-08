using System;
using Larnix.GameCore;
using Larnix.GameCore.Utils;
using Larnix.Socket.Backend;
using Larnix.Server.Configuration;
using Larnix.Server.Data.SQLite;
using Larnix.Core.Interfaces;
using Larnix.GameCore.Json;
using Larnix.Core;
using Version = Larnix.GameCore.Version;

namespace Larnix.Server.Data
{
    public class DataSaver : IDisposable, ITickable
    {
        private string WorldPath { get; }

        private WorldMeta _worldMeta;
        private WorldMeta WorldMeta
        {
            get => _worldMeta;
            set
            {
                _worldMeta = value;
                WorldMeta.SaveToFolder(WorldPath, _worldMeta);
            }
        }

        public Version Version => WorldMeta.Version;
        public String32 HostNickname
        {
            get => (String32)WorldMeta.Nickname;
            private set => WorldMeta = new WorldMeta(WorldMeta.Version, value);
        }

        private Server Server => GlobRef.Get<Server>();
        private Database Database => GlobRef.Get<Database>();
        private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
        private Clock Clock => GlobRef.Get<Clock>();
        private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
        private BlockDataManager BlockDataManager => GlobRef.Get<BlockDataManager>();
        private IUserManager UserManager => GlobRef.Get<IUserManager>();

        private bool _disposed = false;

        public DataSaver(string worldPath)
        {
            WorldPath = worldPath;

            GlobRef.Set(Config.FromDirectory<ServerConfig>(WorldPath));
            GlobRef.Set(new Database(WorldPath));

            GlobRef.New<BlockDataManager>();
            GlobRef.New<EntityDataManager>();
            
            var meta = WorldMeta.ReadFromFolder(WorldPath);
            WorldMeta = new WorldMeta(Version.Current, meta.Nickname);
        }

        /// <summary>
        /// Never call this if server is not in headless mode.
        /// This may ask the user to change the password using the console and cause the deadlock!
        /// </summary>
        public bool EnsureDetachedServer()
        {
            if (Server.Type != ServerType.Remote)
                throw new InvalidOperationException("EnsureDetachedServer() should only be called for headless servers.");

            if (HostNickname != Common.ReservedNickname)
            {
                Echo.LogRaw("This world was previously in use by " + HostNickname + ".\n");
                Echo.LogRaw("Choose a password for this player to start the server.\n");

                bool changeSuccess = false;
                do
                {
                    Echo.LogRaw("> ");
                    string password = Echo.ReadLineSync();

                    if (Validation.IsGoodPassword(password))
                    {
                        UserManager.TryChangePasswordOrAddUserSync(HostNickname, password);
                        HostNickname = (String32)Common.ReservedNickname;
                        changeSuccess = true;
                    }
                    else
                    {
                        Echo.LogRaw(Validation.WrongPasswordInfo + "\n");
                    }
                    
                } while (!changeSuccess);

                Echo.LogRaw("Password changed.\n");
                return true;
            }
            return false;
        }

        public void Tick(float deltaTime)
        {
            if (Clock.FixedFrame % ServerConfig.PeriodicTasks_DataSavingPeriodFrames == 0)
            {
                SaveAll();
            }
        }

        public void SaveAll()
        {
            if (Database != null)
            {
                Database.BeginTransaction();
                try
                {
                    EntityDataManager.FlushIntoDatabase();
                    BlockDataManager.FlushIntoDatabase();
                    Database.SetServerTick(Clock.ServerTick);

                    Database.CommitTransaction();
                }
                catch
                {
                    Database.RollbackTransaction();
                    throw;
                }
            }
        }

        public void Dispose() => Dispose(false);
        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (!emergency)
                {
                    SaveAll();
                    Echo.Log("Data has been saved.");
                }

                Database?.Dispose();
            }
        }
    }
}
