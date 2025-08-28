using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Socket.Commands;
using Larnix.Socket;
using Larnix.Server.Entities;
using Larnix.Socket.Channel;

namespace Larnix.Server.Terrain
{
    public class ChunkLoading : MonoBehaviour
    {
        public const int LOADING_DISTANCE = 2; // chunks
        public const float UNLOADING_TIME = 4f; // seconds
        public const float PLAYER_SENDING_PERIOD = 0.15f; // seconds

        private readonly Dictionary<Vector2Int, ChunkContainer> Chunks = new();

        private static readonly BlockData[,] PreAllocatedChunkArray = new BlockData[16, 16];

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

        private void Awake()
        {
            References.ChunkLoading = this;

            if (PreAllocatedChunkArray[0, 0] == null) // pre-allocating chunk array
            {
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                        PreAllocatedChunkArray[x, y] = new BlockData();
            }
        }

        private void Start()
        {
            StartCoroutine(DataSendingCoroutine());
        }

        public void FromFixedUpdate() // FIX-1
        {
            var activeChunks = Chunks.Where(kv => ChunkState(kv.Key) == LoadState.Active).ToList();
            var orderedChunks = activeChunks.OrderBy(kv => kv.Key.y).ThenBy(kv => kv.Key.x).ToList();
            var shuffledChunks = activeChunks.OrderBy(_ => UnityEngine.Random.value).ToList();

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
        }

        public void FromEarlyUpdate() // 1
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
                Chunks[vkp.Key].UnloadTime -= Time.deltaTime;
            }
        }

        private IEnumerator DataSendingCoroutine()
        {
            while(true)
            {
                // Updating player chunk data

                foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
                {
                    Vector2Int chunkpos = ChunkMethods.CoordsToChunk(References.PlayerManager.GetPlayerRenderingPosition(nickname));
                    var player_state = References.PlayerManager.GetPlayerState(nickname);

                    HashSet<Vector2Int> chunksMemory = References.PlayerManager.ClientChunks[nickname];
                    HashSet<Vector2Int> chunksNearby = GetNearbyChunks(chunkpos, LOADING_DISTANCE).Where(c => ChunkState(c) == LoadState.Active).ToHashSet();

                    HashSet<Vector2Int> added = new(chunksNearby);
                    added.ExceptWith(chunksMemory);

                    HashSet<Vector2Int> removed = new(chunksMemory);
                    removed.ExceptWith(chunksNearby);

                    // send added
                    foreach (var chunk in added)
                    {
                        Chunks[chunk].Instance.MoveChunkIntoArray(PreAllocatedChunkArray);
                        ChunkInfo chunkInfo = new ChunkInfo(chunk, PreAllocatedChunkArray);
                        if (!chunkInfo.HasProblems)
                        {
                            Packet packet = chunkInfo.GetPacket();
                            References.Server.Send(nickname, packet);
                        }
                        else throw new Exception("Couldn't construct ChunkInfo command! [SEND]");
                    }

                    // send removed
                    foreach (var chunk in removed)
                    {
                        ChunkInfo chunkInfo = new ChunkInfo(chunk, null);
                        if(!chunkInfo.HasProblems)
                        {
                            Packet packet = chunkInfo.GetPacket();
                            References.Server.Send(nickname, packet);
                        }
                        else throw new Exception("Couldn't construct ChunkInfo command! [FORGET]");
                    }

                    References.PlayerManager.ClientChunks[nickname] = chunksNearby;
                }

                yield return new WaitForSeconds(PLAYER_SENDING_PERIOD);
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
            Vector2Int chunk = ChunkMethods.CoordsToChunk(entity.EntityData.Position);
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
            if (GetNearbyChunks(chunk, 0).Count == 0)
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
                    References.EntityManager.LoadEntitiesByChunk(chunk); // passive load (without instance)
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

            Chunks[chunk].Instance = new ChunkServer(chunk);
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

                foreach(string nickname in References.PlayerManager.PlayerUID.Keys)
                {
                    int dist = Common.ManhattanDistance(
                        chunk,
                        ChunkMethods.CoordsToChunk(References.PlayerManager.GetPlayerRenderingPosition(nickname))
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

            List<Vector2> positions = new();
            foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
                positions.Add(References.PlayerManager.GetPlayerRenderingPosition(nickname));

            foreach (Vector2Int center in GetCenterChunks(positions))
            {
                targetLoads.UnionWith(GetNearbyChunks(center, LOADING_DISTANCE));
            }

            return targetLoads;
        }

        public static HashSet<Vector2Int> GetCenterChunks(List<Vector2> positions)
        {
            HashSet<Vector2Int> returns = new();
            foreach(Vector2 pos in positions)
            {
                returns.Add(ChunkMethods.CoordsToChunk(pos));
            }
            return returns;
        }

        public static HashSet<Vector2Int> GetNearbyChunks(Vector2Int center, int simDistance)
        {
            int min_x = System.Math.Clamp(center.x - simDistance, ChunkMethods.MIN_CHUNK, int.MaxValue);
            int min_y = System.Math.Clamp(center.y - simDistance, ChunkMethods.MIN_CHUNK, int.MaxValue);
            int max_x = System.Math.Clamp(center.x + simDistance, int.MinValue, ChunkMethods.MAX_CHUNK);
            int max_y = System.Math.Clamp(center.y + simDistance, int.MinValue, ChunkMethods.MAX_CHUNK);

            HashSet<Vector2Int> returns = new();
            for (int x = min_x; x <= max_x; x++)
                for (int y = min_y; y <= max_y; y++)
                {
                    returns.Add(new Vector2Int(x, y));
                }

            return returns;
        }
    }
}
