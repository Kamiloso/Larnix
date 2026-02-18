using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Data;

namespace Larnix.Server.Terrain
{
    internal class Chunks : Singleton
    {
        public const float UNLOADING_TIME = 1f; // seconds

        private Server Server => Ref<Server>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private BlockSender BlockSender => Ref<BlockSender>();
        private Config Config => Ref<Config>();

        private readonly Dictionary<Vec2Int, ChunkContainer> _chunks = new();

        public enum LoadState { None, Loading, Active }
        private class ChunkContainer
        {
            public LoadState State => Instance == null ? LoadState.Loading : LoadState.Active;
            public float UnloadTime;
            public ChunkServer Instance;
        }

        public Chunks(Server server) : base(server) {}

        public override void EarlyFrameUpdate()
        {
            // Chunk stimulating
            foreach(var chunk in GetStimulatedChunks())
            {
                ChunkStimulate(chunk);
            }

            // Chunk unloading
            foreach (var kvp in _chunks.ToList())
            {
                var chunk = kvp.Key;
                var timeLeft = kvp.Value.UnloadTime;

                if (timeLeft <= 0f)
                    ChunkUnload(chunk);
            }

            // Chunk activating
            IEnumerable<Vec2Int> loadingChunks = _chunks.Keys
                .Where(ch => ChunkState(ch) == LoadState.Loading);

            if (TryGetHighestPriorityLoadingChunk(loadingChunks, out var nextChunk))
            {
                ChunkActivate(nextChunk);
            }

            // Chunk unloading countdown
            foreach (var kvp in _chunks.ToList())
            {
                _chunks[kvp.Key].UnloadTime -= Server.RealDeltaTime;
            }
        }

        public override void FrameUpdate()
        {
            // Invoke block events
            IEnumerator[] invokers = _chunks
                .Where(kv => IsLoadedChunk(kv.Key))
                .OrderBy(kv => kv.Key.y)
                .ThenBy(kv => kv.Key.x)
                .Select(kv => kv.Value.Instance.FrameInvoker)
                .Select(inv => inv.GetEnumerator())
                .ToArray();

            if (invokers.Length >= 1)
                while (true)
                {
                    foreach (IEnumerator inv in invokers)
                    {
                        bool result = inv.MoveNext();
                        if (!result) goto end_while_true;
                    }
                }
            end_while_true:;

            // Updating player chunk data
            if (Server.FixedFrame % Config.ChunkSendingPeriodFrames == 0)
            {
                BlockSender.BroadcastChunkChanges();
            }
        }

        public LoadState ChunkState(Vec2Int chunk)
        {
            if (_chunks.TryGetValue(chunk, out var container))
                return container.State;
            
            return LoadState.None;
        }

        public bool IsLoadedChunk(Vec2Int chunk)
        {
            return ChunkState(chunk) == LoadState.Active;
        }

        public bool IsLoadedPosition(Vec2 position)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(position);
            return IsLoadedChunk(chunk);
        }

        public bool IsEntityInZone(EntityAbstraction entity, LoadState state)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(entity.ActiveData.Position);
            return ChunkState(chunk) == state;
        }

        public ChunkServer GetChunk(Vec2Int chunk)
        {
            if (TryGetChunk(chunk, out var result))
                return result;

            throw new InvalidOperationException($"Chunk {chunk} is not active!");
        }

        public bool TryGetChunk(Vec2Int chunk, out ChunkServer result)
        {
            if (ChunkState(chunk) == LoadState.Active)
            {
                result = _chunks[chunk].Instance;
                return true;
            }

            result = null;
            return false;
        }

        private void ChunkStimulate(Vec2Int chunk)
        {
            LoadState state = ChunkState(chunk);
            switch(state)
            {
                case LoadState.None:
                    _chunks[chunk] = new ChunkContainer
                    {
                        UnloadTime = UNLOADING_TIME,
                        Instance = null
                    };
                    EntityManager.LoadEntitiesByChunk(chunk); // passive load (without instance)
                    break;

                case LoadState.Loading:
                case LoadState.Active:
                    _chunks[chunk].UnloadTime = UNLOADING_TIME;
                    break;
            }
        }

        private void ChunkActivate(Vec2Int chunk)
        {
            // Check if loading
            LoadState state = ChunkState(chunk);
            if (state != LoadState.Loading)
                throw new InvalidOperationException($"Chunk {chunk} must be in loading state to activate it!");

            // Create block chunk
            _chunks[chunk].Instance = new ChunkServer(this, chunk);
        }

        private void ChunkUnload(Vec2Int chunk)
        {
            // Remove chunks entry
            LoadState state = ChunkState(chunk);
            switch(state)
            {
                case LoadState.Loading:
                    _chunks.Remove(chunk);
                    break;

                case LoadState.Active:
                    _chunks[chunk].Instance.Dispose();
                    _chunks.Remove(chunk);
                    break;

                default:
                    throw new InvalidOperationException($"Chunk {chunk} cannot be unloaded!");
            }

            // Unload entities

            // --- entities will unload from EntityManager just after chunk update ---
        }

        private bool TryGetHighestPriorityLoadingChunk(IEnumerable<Vec2Int> chunkCandidates, out Vec2Int bestChunk)
        {
            bestChunk = default;

            var playerChunks = PlayerManager.AllPlayers()
                .Select(n => BlockUtils.CoordsToChunk(PlayerManager.RenderingPosition(n)))
                .ToList();

            if (playerChunks.Count == 0)
                return false;

            bool found = false;
            int bestDistance = int.MaxValue;

            foreach (var candidate in chunkCandidates)
            {
                int distMin = int.MaxValue;

                foreach (var pChunk in playerChunks)
                {
                    int dist = GeometryUtils.ManhattanDistance(candidate, pChunk);
                    if (dist < distMin) distMin = dist;
                    if (distMin == 0) break; // can't get better for this candidate
                }

                if (distMin < bestDistance)
                {
                    bestDistance = distMin;
                    bestChunk = candidate;
                    found = true;
                    if (bestDistance == 0) break; // optimal overall
                }
            }

            return found;
        }

        private HashSet<Vec2Int> GetStimulatedChunks()
        {
            HashSet<Vec2Int> targetLoads = new();

            IEnumerable<Vec2Int> centers = PlayerManager.AllPlayers()
                .Select(nickname => PlayerManager.RenderingPosition(nickname))
                .Select(pos => BlockUtils.CoordsToChunk(pos));

            foreach (Vec2Int center in centers)
            {
                HashSet<Vec2Int> nearbyChunks = BlockUtils.GetNearbyChunks(center, BlockUtils.LOADING_DISTANCE);
                targetLoads.UnionWith(nearbyChunks);
            }

            return targetLoads;
        }
    }
}
