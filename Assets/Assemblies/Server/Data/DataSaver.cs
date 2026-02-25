using System;
using Larnix.Core;
using Larnix.Core.Utils;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using Larnix.Socket.Backend;

namespace Larnix.Server.Data
{
    public class DataSaver : IDisposable2, ITickable
    {
        private string WorldPath { get; init; }

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
        private Config Config => GlobRef.Get<Config>();
        private Clock Clock => GlobRef.Get<Clock>();
        private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
        private BlockDataManager BlockDataManager => GlobRef.Get<BlockDataManager>();
        private UserManager UserManager => GlobRef.Get<UserManager>();

        private bool _disposed = false;

        public DataSaver(string worldPath)
        {
            WorldPath = worldPath;

            GlobRef.Set(Config.Obtain(WorldPath));
            GlobRef.Set(new Database(WorldPath));
            
            GlobRef.Set(new BlockDataManager());
            GlobRef.Set(new EntityDataManager());
            
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
                Core.Debug.LogRaw("This world was previously in use by " + HostNickname + ".\n");
                Core.Debug.LogRaw("Choose a password for this player to start the server.\n");

                bool changeSuccess = false;
                do
                {
                    Core.Debug.LogRaw("> ");
                    string password = Console.GetInputSync();

                    if (Validation.IsGoodPassword(password))
                    {
                        UserManager.ChangePasswordSync(HostNickname, password);
                        HostNickname = (String32)Common.ReservedNickname;
                        changeSuccess = true;
                    }
                    else
                    {
                        Core.Debug.LogRaw(Validation.WrongPasswordInfo + "\n");
                    }
                    
                } while (!changeSuccess);

                Core.Debug.LogRaw("Password changed.\n");
                return true;
            }
            return false;
        }

        public void Tick(float deltaTime)
        {
            if (Clock.FixedFrame % Config.DataSavingPeriodFrames == 0)
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

            Config.Save(WorldPath);
        }

        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (!emergency)
                {
                    SaveAll();
                    Core.Debug.Log("Data has been saved.");
                }

                Database?.Dispose();
            }
        }
    }
}
