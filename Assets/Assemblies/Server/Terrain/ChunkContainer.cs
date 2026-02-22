using Larnix.Core.Vectors;
using System;
using System.Collections.Generic;

namespace Larnix.Server.Terrain
{
    internal class ChunkContainer
    {
        public Vec2Int Chunkpos { get; init; }
        public ChunkLoadState State { get; private set; }
        public Chunk Instance { get; private set; }

        private AtomicChunks AtomicChunks => Ref.AtomicChunks;
        
        private float _unloadTime;
        private float UNLOADING_TIME => 1f;

        public ChunkContainer(Vec2Int chunkpos)
        {
            Chunkpos = chunkpos;
            State = ChunkLoadState.Loading;
            _unloadTime = UNLOADING_TIME;
        }

        public void Activate(Chunk instance)
        {
            if (Instance != null)
                throw new InvalidOperationException($"Chunk {Chunkpos} already initialized.");

            State = ChunkLoadState.Active;
            Instance = instance;
        }

        public void Tick(float deltaTime) => _unloadTime -= deltaTime;
        public void Stimulate() => _unloadTime = UNLOADING_TIME;

        public bool ShouldUnload(Func<Vec2Int, ChunkContainer> chunkLookup)
        {
            IEnumerable<Vec2Int> atomicGroup = AtomicChunks.GetAtomicSet(Chunkpos) ??
                new[] { Chunkpos };
            
            foreach (var chunk in atomicGroup)
            {
                if (chunkLookup(chunk)._unloadTime > 0f)
                    return false;
            }
            return true;
        }
    }
}
