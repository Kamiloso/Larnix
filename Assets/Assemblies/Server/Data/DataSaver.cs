#nullable enable
using Larnix.Core;
using Larnix.Model.Database;
using System;

namespace Larnix.Server.Data;

internal interface IDataSaver : ITickable
{
    public event Action? SavingAll;
    void SaveAll();
}

internal class DataSaver : IDataSaver
{
    public event Action? SavingAll;

    private Config Config => GlobRef.Get<Config>();
    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IClock Clock => GlobRef.Get<IClock>();

    public void Tick(float deltaTime)
    {
        if (Clock.FixedFrame % Config.PeriodicTasks_DataSavingPeriodFrames == 0)
        {
            SaveAll();
        }
    }

    public void SaveAll()
    {
        Db?.Handle.AsTransaction(() => SavingAll?.Invoke());
    }
}
