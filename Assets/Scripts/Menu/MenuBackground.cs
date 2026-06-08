using System.Collections.Generic;
using UnityEngine;
using Larnix.Model.Worldgen;
using Larnix.Client.Terrain;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Background;
using Larnix.Model.Enums;
using Larnix.Core.Utils;
using Larnix.Model.Blocks.Chunks;
using Larnix.Model.Blocks;

namespace Larnix.Menu
{
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] BasicGridManager BasicGridManager;
        [SerializeField] Vector3 CameraSpeed;

        private Sky Sky => GlobRef.Get<Sky>();

        private readonly HashSet<Vec2Int> _activeChunks = new();
        private bool _firstGeneration = true;

        private Generator _generator;

        private void Start()
        {
            long seed = RandUtils.SecureLong();
            _generator = new Generator(seed);
        }

        private void Update()
        {
            Vector2 unityPosition = Camera.transform.position;
            Vec2 camPosition = VectorExtensions.ConstructVec2(unityPosition, Vec2.Zero);
            Vec2Int camChunk = BlockHelpers.CoordsToChunk(camPosition);

            HashSet<Vec2Int> nearbyChunks = BlockHelpers.GetNearbyChunks(camChunk, BlockHelpers.LOADING_DISTANCE);

            var ToAdd = new HashSet<Vec2Int>(nearbyChunks);
            ToAdd.ExceptWith(_activeChunks);

            var ToRemove = new HashSet<Vec2Int>(_activeChunks);
            ToRemove.ExceptWith(nearbyChunks);

            foreach(var chunk in ToAdd)
            {
                ChunkData chunkData = _generator.GenerateChunk(chunk);
                BasicGridManager.AddChunk(chunk, chunkData, _firstGeneration);
                _activeChunks.Add(chunk);
            }

            foreach(var chunk in ToRemove)
            {
                BasicGridManager.RemoveChunk(chunk);
                _activeChunks.Remove(chunk);
            }

            Sky.UpdateSky(
                biomeID: _generator.BiomeAt(camPosition),
                skyColor: _generator.SkyColorAt(camPosition),
                weather: WeatherID.Clear
                );

            _firstGeneration = false;
        }

        private void LateUpdate()
        {
            Camera.transform.position += CameraSpeed * Time.deltaTime;
        }
    }
}
