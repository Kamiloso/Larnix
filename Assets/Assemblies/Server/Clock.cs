#nullable enable
using System;
using Larnix.Core;
using Larnix.Model.Database;
using Larnix.Server.Data;

namespace Larnix.Server;

internal interface IClock : ITickable
{
    long ServerTick { get; }
    uint FixedFrame { get; }
    float DeltaTime { get; }
    float TPS { get; }
}

internal class Clock : IClock
{
    public long ServerTick { get; private set; }
    public uint FixedFrame { get; private set; }

    private float? _deltaTime = null;
    public float DeltaTime => _deltaTime ?? throw new InvalidOperationException("Delta time uninitialized.");
    public float TPS => 1f / DeltaTime;

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IDataSaver DataSaver => GlobRef.Get<IDataSaver>();

    public Clock()
    {
        FixedFrame = 1;
        ServerTick = Db.Values.Get("server_tick") ?? 0L;
        DataSaver.SavingAll += SaveServerTick;
    }

    public void Tick(float deltaTime)
    {
        _deltaTime = deltaTime;
        FixedFrame++;
        ServerTick++;
    }

    private void SaveServerTick()
    {
        Db.Values.Put("server_tick", ServerTick);
    }
}
