#nullable enable
using Larnix.Core;
using Larnix.Model.Database;
using System;

namespace Larnix.Server.Data;

internal interface IDataSaver : ITickable
{
    public event Action? SavingWorld;
    void SaveWorld();
}

internal class DataSaver : IDataSaver
{
    public event Action? SavingWorld;
    private Config Config => GlobRef.Get<Config>();
    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IClock Clock => GlobRef.Get<IClock>();

    public void Tick(float deltaTime)
    {
        if (Clock.FixedFrame % Config.PeriodicTasks_DataSavingPeriodFrames == 0)
        {
            SaveWorld();
        }
    }

    public void SaveWorld()
    {
        Db?.Handle.AsTransaction(() => SavingWorld?.Invoke());
    }
}
