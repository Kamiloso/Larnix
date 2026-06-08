#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Model.Json;
using Larnix.Server.Chunks;

namespace Larnix.Server.Entities.Scripts;

internal interface IEntityManager : IScript { }

internal class EntityManager : IEntityManager
{
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IChunkHolders ChunkHolders => GlobRef.Get<IChunkHolders>();

    public EntityManager()
    {
        ChunkHolders.OnStartedLoading += chunk => EntityControllers.PrepareEntityControllers(chunk);
        ChunkHolders.OnFullyLoaded += chunk => EntityControllers.ActivateEntityControllers(chunk);
        ChunkHolders.OnUnloaded += chunk => EntityControllers.UnloadEntityControllers(chunk);
    }

    void IScript.Update()
    {
        // Frame update
        foreach (ulong uid in EntityControllers.Uids)
        {
            var controller = EntityControllers.GetController(uid)!;
            if (controller.IsActive)
            {
                controller.FrameUpdate();
            }
        }

        // Killing
        foreach (ulong uid in EntityControllers.Uids)
        {
            var controller = EntityControllers.GetController(uid)!;
            if (controller.IsActive)
            {
                Storage storage = controller.ActiveData.NBT;
                if (storage.RemoveFlag('K', true))
                {
                    EntityControllers.KillController(uid);
                }
            }
        }
    }
}