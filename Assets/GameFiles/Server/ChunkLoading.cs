using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Server
{
    public class ChunkLoading : MonoBehaviour
    {
        private const int MinChunk = -16384;
        private const int MaxChunk = 16383;

        // Chunks can be unloaded, lazy loaded or alive
        // UNLOADED - Everything is unloaded
        // LAZY LOADED - Blocks are loaded, but entities are unloaded
        // LOADED - Blocks and entities are loaded

        public enum LoadState : byte
        {
            None,
            Lazy,
            Full
        }

        private readonly Dictionary<Vector2Int, LoadState> LoadedChunks = new();
        private readonly Dictionary<Vector2Int, float> UnloadTimer = new();
        private readonly Dictionary<Vector2Int, ChunkServer> Chunks = new();

        private const int LazyLoadingDistance = FullLoadingDistance + 1;
        private const int FullLoadingDistance = 3;
        private const float UnloadingTime = 4f; // seconds

        private void Awake()
        {
            References.ChunkLoading = this;
        }

        public void FromEarlyUpdate() // 1
        {
            // Chunk stimulating

            HashSet<Vector2Int> lazyAndFullStimulate = GetStimulatedChunks(LoadState.Lazy, LazyLoadingDistance);
            HashSet<Vector2Int> fullStimulate = GetStimulatedChunks(LoadState.Full, FullLoadingDistance);

            foreach(var chunk in fullStimulate)
            {
                StimulateChunk(chunk, LoadState.Full);
            }

            foreach(var chunk in lazyAndFullStimulate)
                if(!fullStimulate.Contains(chunk))
                {
                    StimulateChunk(chunk, LoadState.Lazy);
                }

            // Chunk unloading

            List<Vector2Int> unloadList = new List<Vector2Int>();

            foreach(var vkp in UnloadTimer) // find
            {
                Vector2Int chunk = vkp.Key;
                float timeLeft = vkp.Value;

                if (timeLeft <= 0f)
                    unloadList.Add(chunk);
            }

            foreach (var chunk in unloadList) // unload
            {
                UnloadChunk(chunk);
            }

            foreach(var chunk in LoadedChunks.Keys.ToList()) // countdown
            {
                UnloadTimer[chunk] -= Time.deltaTime;
            }
        }

        public bool IsEntityInAliveZone(EntityController entity)
        {
            Vector2Int chunk = CoordsToChunk(entity.EntityData.Position);
            if (LoadedChunks.ContainsKey(chunk))
            {
                LoadState state = LoadedChunks[chunk];
                return state >= LoadState.Full;
            }
            return false;
        }

        private void StimulateChunk(Vector2Int chunk, LoadState loadState)
        {
            // Create block chunk

            if (!LoadedChunks.ContainsKey(chunk))
            {
                GameObject chunkObj = Prefabs.CreateChunk(Prefabs.Mode.Server);
                chunkObj.transform.position = ChunkStartingCoords(chunk);
                chunkObj.transform.SetParent(transform, false);
                chunkObj.name = "Chunk [" + chunk.x + ", " + chunk.y + "]";

                ChunkServer chunkServer = chunkObj.GetComponent<ChunkServer>();
                chunkServer.Initialize(chunk);
                Chunks.Add(chunk, chunkServer);
            }

            // Load entities

            bool notYetFull = (!LoadedChunks.ContainsKey(chunk) || LoadedChunks[chunk] == LoadState.Lazy);

            if (notYetFull && loadState == LoadState.Full)
                References.EntityManager.LoadEntitiesByChunk(chunk);

            // Mark in dictionaries

            LoadedChunks[chunk] = notYetFull ? loadState : LoadState.Full;

            UnloadTimer[chunk] = UnloadingTime;
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            // Remove block chunk

            Destroy(Chunks[chunk].gameObject);
            Chunks.Remove(chunk);

            // --- Entities will unload from EntityManager in the same frame ---

            // Remove from dictionaries

            LoadedChunks.Remove(chunk);
            UnloadTimer.Remove(chunk);

            // Mark nearby chunks as lazy

            foreach(var around in GetNearbyChunks(chunk, 1))
            {
                if (LoadedChunks.ContainsKey(around) && LoadedChunks[around] == LoadState.Full)
                    LoadedChunks[around] = LoadState.Lazy;
            }
        }

        private HashSet<Vector2Int> GetStimulatedChunks(LoadState loadState, int simDistance)
        {
            HashSet<Vector2Int> targetLoads = new HashSet<Vector2Int>();

            List<EntityController> playerControllers = References.EntityManager.GetAllPlayerControllers();
            foreach (Vector2Int center in GetCenterChunks(playerControllers))
            {
                targetLoads.UnionWith(GetNearbyChunks(center, simDistance));
            }

            return targetLoads;
        }

        public static HashSet<Vector2Int> GetCenterChunks(List<EntityController> entityControllers)
        {
            HashSet<Vector2Int> returns = new HashSet<Vector2Int>();
            foreach(EntityController controller in entityControllers)
            {
                returns.Add(CoordsToChunk(controller.EntityData.Position));
            }
            return returns;
        }

        public static HashSet<Vector2Int> GetNearbyChunks(Vector2Int center, int simDistance)
        {
            int min_x = center.x - simDistance; if (min_x < MinChunk) min_x = MinChunk;
            int max_x = center.x + simDistance; if (max_x > MaxChunk) max_x = MaxChunk;
            int min_y = center.y - simDistance; if (min_y < MinChunk) min_y = MinChunk;
            int max_y = center.y + simDistance; if (max_y > MaxChunk) max_y = MaxChunk;

            HashSet<Vector2Int> returns = new HashSet<Vector2Int>();
            for (int x = min_x; x <= max_x; x++)
                for (int y = min_y; y <= max_y; y++)
                {
                    returns.Add(new Vector2Int(x, y));
                }

            return returns;
        }

        public static Vector2Int CoordsToChunk(Vector2 position)
        {
            return new Vector2Int(
                (int)System.Math.Floor(position.x / 16),
                (int)System.Math.Floor(position.y / 16)
                );
        }

        public static Vector2 ChunkStartingCoords(Vector2Int chunk)
        {
            return 16 * new Vector2(chunk.x, chunk.y);
        }
    }
}
