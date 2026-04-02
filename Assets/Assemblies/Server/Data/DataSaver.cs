#nullable enable
using Larnix.Core;
using Larnix.Model.Database;

namespace Larnix.Server.Data;

internal interface IDataSaver : ITickable
{
    void SaveAll();
}

internal class DataSaver : IDataSaver
{
    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IClock Clock => GlobRef.Get<IClock>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
    private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
    private BlockDataManager BlockDataManager => GlobRef.Get<BlockDataManager>();

    public void Tick(float deltaTime)
    {
        if (Clock.FixedFrame % ServerConfig.PeriodicTasks_DataSavingPeriodFrames == 0)
        {
            SaveAll();
        }
    }

    public void SaveAll()
    {
        if (Db == null) return;

        Db.Handle.AsTransaction(() =>
        {
            EntityDataManager.FlushIntoDatabase();
            BlockDataManager.FlushIntoDatabase();
            Db.Values.Put("server_tick", Clock.ServerTick);
        });
    }
}
