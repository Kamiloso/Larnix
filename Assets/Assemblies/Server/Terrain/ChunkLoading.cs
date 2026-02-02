using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Socket.Packets;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Socket.Backend;
using Larnix.Core.References;
using Larnix.Packets;

namespace Larnix.Server.Terrain
{
    internal class ChunkLoading : Singleton
    {
        public const float UNLOADING_TIME = 4f; // seconds

        public readonly WorldAPI WorldAPI;
        private Server Server => Ref<Server>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private QuickServer QuickServer => Ref<QuickServer>();
        private EntityManager EntityManager => Ref<EntityManager>();

        private readonly Dictionary<Vec2Int, ChunkContainer> Chunks = new();

        public enum LoadState { None, Loading, Active }
        private class ChunkContainer
        {
            public LoadState State { get { return Instance == null ? LoadState.Loading : LoadState.Active; } }
            public float UnloadTime;
            public ChunkServer Instance;
        }

        public ChunkLoading(Server server) : base(server)
        {
            WorldAPI = new WorldAPI(server);
        }

        public override void FrameUpdate()
        {
            var activeChunks = Chunks.Where(kv => ChunkState(kv.Key) == LoadState.Active).ToList();
            var orderedChunks = activeChunks.OrderBy(kv => kv.Key.y).ThenBy(kv => kv.Key.x).ToList();
            var shuffledChunks = activeChunks.OrderBy(_ => Common.Rand().NextDouble()).ToList();

            foreach (var kvp in activeChunks) kvp.Value.Instance.INVOKE_PreFrame(); // START
            foreach (var kvp in orderedChunks) kvp.Value.Instance.INVOKE_Conway(); // 1
            foreach (var kvp in orderedChunks) kvp.Value.Instance.INVOKE_SequentialEarly(); // 2
            foreach (var kvp in shuffledChunks) kvp.Value.Instance.INVOKE_Random(); // 3
            foreach (var kvp in orderedChunks) kvp.Value.Instance.INVOKE_SequentialLate(); // 4
            foreach (var kvp in activeChunks) kvp.Value.Instance.INVOKE_PostFrame(); // END

            // Chunk changes broadcasting
            const int BROADCAST_FIXED_PERIOD = 8;
            if (Server.FixedFrame % BROADCAST_FIXED_PERIOD == 0)
            {
                BroadcastChunkChanges();
            }
        }

        public override void EarlyFrameUpdate()
        {
            // Chunk stimulating

            foreach(var chunk in GetStimulatedChunks())
            {
                ChunkStimulate(chunk);
            }

            // Chunk unloading

            foreach (var vkp in Chunks.ToList())
            {
                var chunk = vkp.Key;
                var timeLeft = vkp.Value.UnloadTime;

                if (timeLeft <= 0f)
                    ChunkUnload(chunk);
            }

            // Chunk activating

            foreach (var chunk in SortByPriority(Chunks.Keys.Where(ch => ChunkState(ch) == LoadState.Loading).ToList()))
            {
                var state = ChunkState(chunk);

                if (state == LoadState.Loading)
                    ChunkActivate(chunk);

                break; // only one chunk per frame
            }

            // Chunk unloading countdown

            foreach (var vkp in Chunks.ToList())
            {
                Chunks[vkp.Key].UnloadTime -= Common.FIXED_TIME;
            }
        }

        private void BroadcastChunkChanges()
        {
            // Updating player chunk data

            foreach (string nickname in PlayerManager.GetAllPlayerNicknames())
            {
                Vec2Int chunkpos = BlockUtils.CoordsToChunk(PlayerManager.GetPlayerRenderingPosition(nickname));
                var player_state = PlayerManager.GetPlayerState(nickname);

                HashSet<Vec2Int> chunksMemory = PlayerManager.LoadedChunksCopy(nickname);
                HashSet<Vec2Int> chunksNearby = BlockUtils.GetNearbyChunks(chunkpos, BlockUtils.LOADING_DISTANCE)
                    .Where(c => ChunkState(c) == LoadState.Active)
                    .ToHashSet();

                HashSet<Vec2Int> added = new(chunksNearby);
                added.ExceptWith(chunksMemory);

                HashSet<Vec2Int> removed = new(chunksMemory);
                removed.ExceptWith(chunksNearby);

                // send added
                foreach (var chunk in added)
                {
                    BlockData2[,] chunkArray = Chunks[chunk].Instance.ActiveChunkReference;
                    Payload packet = new ChunkInfo(chunk, chunkArray);
                    QuickServer.Send(nickname, packet);
                }

                // send removed
                foreach (var chunk in removed)
                {
                    Payload packet = new ChunkInfo(chunk, null);
                    QuickServer.Send(nickname, packet);
                }

                PlayerManager.UpdateClientChunks(nickname, chunksNearby);
            }
        }

        public LoadState ChunkState(Vec2Int chunk)
        {
            if(Chunks.TryGetValue(chunk, out var container))
                return container.State;
            return LoadState.None;
        }

        public bool IsEntityInZone(EntityAbstraction entity, LoadState state)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(entity.EntityData.Position);
            return ChunkState(chunk) == state;
        }

        public bool TryGetChunk(Vec2Int chunk, out ChunkServer chunkObject)
        {
            var state = ChunkState(chunk);
            switch (state)
            {
                case LoadState.None:
                case LoadState.Loading:
                    chunkObject = null;
                    return false;

                case LoadState.Active:
                    chunkObject = Chunks[chunk].Instance;
                    return true;

                default:
                    throw new NotImplementedException("Impossible chunk state!");
            }
        }

        public bool TryForceLoadChunk(Vec2Int chunk)
        {
            if (BlockUtils.GetNearbyChunks(chunk, 0).Count == 0)
                return false;

            if(ChunkState(chunk) == LoadState.None)
                ChunkStimulate(chunk);

            if(ChunkState(chunk) == LoadState.Loading)
                ChunkActivate(chunk);

            return true;
        }

        private void ChunkStimulate(Vec2Int chunk)
        {
            LoadState state = ChunkState(chunk);
            switch(state)
            {
                case LoadState.None:
                    Chunks[chunk] = new ChunkContainer
                    {
                        UnloadTime = UNLOADING_TIME,
                        Instance = null
                    };
                    EntityManager.LoadEntitiesByChunk(chunk); // passive load (without instance)
                    break;

                case LoadState.Loading:
                case LoadState.Active:
                    Chunks[chunk].UnloadTime = UNLOADING_TIME;
                    break;

                default:
                    throw new NotImplementedException("Impossible chunk state!");
            }
        }

        private void ChunkActivate(Vec2Int chunk)
        {
            // Check if loading

            LoadState state = ChunkState(chunk);
            if (state != LoadState.Loading)
                throw new InvalidOperationException($"Chunk {chunk} must be in loading state to activate it!");

            // Create block chunk

            Chunks[chunk].Instance = new ChunkServer(this, chunk);
        }

        private void ChunkUnload(Vec2Int chunk)
        {
            // Remove chunks entry

            LoadState state = ChunkState(chunk);
            switch(state)
            {
                case LoadState.Loading:
                    Chunks.Remove(chunk);
                    break;

                case LoadState.Active:
                    Chunks[chunk].Instance.Dispose();
                    Chunks.Remove(chunk);
                    break;

                default:
                    throw new InvalidOperationException($"Chunk {chunk} cannot be unloaded!");
            }

            // Unload entities

            // --- entities will unload from EntityManager just after chunk update ---
        }

        private List<Vec2Int> SortByPriority(List<Vec2Int> chunkList)
        {
            List<(Vec2Int chunk, int distance)> pairs = new();

            foreach(var chunk in chunkList)
            {
                int dist_min = int.MaxValue;

                foreach(string nickname in PlayerManager.GetAllPlayerNicknames())
                {
                    int dist = GeometryUtils.ManhattanDistance(
                        chunk,
                        BlockUtils.CoordsToChunk(PlayerManager.GetPlayerRenderingPosition(nickname))
                        );

                    if (dist < dist_min)
                        dist_min = dist;
                }

                pairs.Add((chunk, dist_min));
            }

            pairs.Sort((a, b) => a.distance - b.distance);
            return pairs.Select(c => c.chunk).ToList();
        }

        private HashSet<Vec2Int> GetStimulatedChunks()
        {
            HashSet<Vec2Int> targetLoads = new();

            List<Vec2> positions = new();
            foreach (string nickname in PlayerManager.GetAllPlayerNicknames())
                positions.Add(PlayerManager.GetPlayerRenderingPosition(nickname));

            foreach (Vec2Int center in BlockUtils.GetCenterChunks(positions))
            {
                targetLoads.UnionWith(BlockUtils.GetNearbyChunks(center, BlockUtils.LOADING_DISTANCE));
            }

            return targetLoads;
        }
    }
}
