using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Larnix.Socket.Packets;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Socket.Structs;
using Larnix.Socket.Backend;
using UnityEditor;
using Larnix.Server.References;

namespace Larnix.Server.Terrain
{
    internal class ChunkLoading : ServerSingleton
    {
        public const float UNLOADING_TIME = 4f; // seconds

        public WorldAPI WorldAPI { get; private set; } = null;
        private readonly Dictionary<Vector2Int, ChunkContainer> Chunks = new();

        private readonly BlockData2[,] PreAllocatedChunkArray = new BlockData2[16, 16];

        private class ChunkContainer
        {
            public LoadState State { get { return Instance == null ? LoadState.Loading : LoadState.Active; } }
            public float UnloadTime;
            public ChunkServer Instance;
        }

        public enum LoadState : byte
        {
            None,
            Loading,
            Active
        }

        public ChunkLoading(Server server) : base(server)
        {
            WorldAPI = new WorldAPI(server);

            if (PreAllocatedChunkArray[0, 0] == null) // pre-allocating chunk array
            {
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                        PreAllocatedChunkArray[x, y] = new BlockData2();
            }
        }

        public override void FrameUpdate()
        {
            var activeChunks = Chunks.Where(kv => ChunkState(kv.Key) == LoadState.Active).ToList();
            var orderedChunks = activeChunks.OrderBy(kv => kv.Key.y).ThenBy(kv => kv.Key.x).ToList();
            var shuffledChunks = activeChunks.OrderBy(_ => Common.Rand().NextDouble()).ToList();

            foreach (var kvp in activeChunks) // pre-frame configure
            {
                ChunkContainer container = kvp.Value;
                container.Instance.PreExecuteFrame();
            }

            foreach (var kvp in shuffledChunks) // actual frame random
            {
                ChunkContainer container = kvp.Value;
                container.Instance.ExecuteFrameRandom();
            }

            foreach (var kvp in orderedChunks) // actual frame sequential
            {
                ChunkContainer container = kvp.Value;
                container.Instance.ExecuteFrameSequential();
            }

            // Chunk changes broadcasting
            const int BROADCAST_FIXED_PERIOD = 8;
            if (Ref<Server>().FixedFrame % BROADCAST_FIXED_PERIOD == 0)
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

            foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
            {
                Vector2Int chunkpos = BlockUtils.CoordsToChunk(Ref<PlayerManager>().GetPlayerRenderingPosition(nickname));
                var player_state = Ref<PlayerManager>().GetPlayerState(nickname);

                HashSet<Vector2Int> chunksMemory = Ref<PlayerManager>().LoadedChunksCopy(nickname);
                HashSet<Vector2Int> chunksNearby = BlockUtils.GetNearbyChunks(chunkpos, BlockUtils.LOADING_DISTANCE)
                    .Where(c => ChunkState(c) == LoadState.Active)
                    .ToHashSet();

                HashSet<Vector2Int> added = new(chunksNearby);
                added.ExceptWith(chunksMemory);

                HashSet<Vector2Int> removed = new(chunksMemory);
                removed.ExceptWith(chunksNearby);

                // send added
                foreach (var chunk in added)
                {
                    Chunks[chunk].Instance.MoveChunkIntoArray(PreAllocatedChunkArray);
                    Payload packet = new ChunkInfo(chunk, PreAllocatedChunkArray);
                    Ref<QuickServer>().Send(nickname, packet);
                }

                // send removed
                foreach (var chunk in removed)
                {
                    Payload packet = new ChunkInfo(chunk, null);
                    Ref<QuickServer>().Send(nickname, packet);
                }

                Ref<PlayerManager>().UpdateClientChunks(nickname, chunksNearby);
            }
        }

        public LoadState ChunkState(Vector2Int chunk)
        {
            if(Chunks.TryGetValue(chunk, out var container))
                return container.State;
            return LoadState.None;
        }

        public bool IsEntityInZone(EntityAbstraction entity, LoadState state)
        {
            Vector2Int chunk = BlockUtils.CoordsToChunk(entity.EntityData.Position);
            return ChunkState(chunk) == state;
        }

        public bool TryGetChunk(Vector2Int chunk, out ChunkServer chunkObject)
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

        public bool TryForceLoadChunk(Vector2Int chunk)
        {
            if (BlockUtils.GetNearbyChunks(chunk, 0).Count == 0)
                return false;

            if(ChunkState(chunk) == LoadState.None)
                ChunkStimulate(chunk);

            if(ChunkState(chunk) == LoadState.Loading)
                ChunkActivate(chunk);

            return true;
        }

        private void ChunkStimulate(Vector2Int chunk)
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
                    Ref<EntityManager>().LoadEntitiesByChunk(chunk); // passive load (without instance)
                    break;

                case LoadState.Loading:
                case LoadState.Active:
                    Chunks[chunk].UnloadTime = UNLOADING_TIME;
                    break;

                default:
                    throw new NotImplementedException("Impossible chunk state!");
            }
        }

        private void ChunkActivate(Vector2Int chunk)
        {
            // Check if loading

            LoadState state = ChunkState(chunk);
            if (state != LoadState.Loading)
                throw new InvalidOperationException($"Chunk {chunk} must be in loading state to activate it!");

            // Create block chunk

            Chunks[chunk].Instance = new ChunkServer(this, chunk);
        }

        private void ChunkUnload(Vector2Int chunk)
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

        private List<Vector2Int> SortByPriority(List<Vector2Int> chunkList)
        {
            List<(Vector2Int chunk, int distance)> pairs = new();

            foreach(var chunk in chunkList)
            {
                int dist_min = int.MaxValue;

                foreach(string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
                {
                    int dist = GeometryUtils.ManhattanDistance(
                        chunk,
                        BlockUtils.CoordsToChunk(Ref<PlayerManager>().GetPlayerRenderingPosition(nickname))
                        );

                    if (dist < dist_min)
                        dist_min = dist;
                }

                pairs.Add((chunk, dist_min));
            }

            pairs.Sort((a, b) => a.distance - b.distance);
            return pairs.Select(c => c.chunk).ToList();
        }

        private HashSet<Vector2Int> GetStimulatedChunks()
        {
            HashSet<Vector2Int> targetLoads = new();

            List<Vec2> positions = new();
            foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
                positions.Add(Ref<PlayerManager>().GetPlayerRenderingPosition(nickname));

            foreach (Vector2Int center in BlockUtils.GetCenterChunks(positions))
            {
                targetLoads.UnionWith(BlockUtils.GetNearbyChunks(center, BlockUtils.LOADING_DISTANCE));
            }

            return targetLoads;
        }
    }
}
