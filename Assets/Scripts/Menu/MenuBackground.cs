using System.Collections.Generic;
using UnityEngine;
using Larnix.Model.Worldgen;
using Larnix.Client.Terrain;
using Larnix.Model.Utils;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Background;
using Larnix.Model.Enums;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Utils;

namespace Larnix.Menu
{
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] BasicGridManager BasicGridManager;
        [SerializeField] Vector3 CameraSpeed;

        private Generator Generator;
        private Sky Sky => GlobRef.Get<Sky>();

        private HashSet<Vec2Int> ActiveChunks = new();
        private bool firstGeneration = true;

        private void Start()
        {
            long seed = RandUtils.SecureLong();
            Generator = new Generator(seed);
        }

        private void Update()
        {
            HashSet<Vec2Int> NearbyChunks = BlockUtils.GetNearbyChunks(
                BlockUtils.CoordsToChunk(VectorExtensions.ConstructVec2(Camera.transform.position, Vec2.Zero)), 2);

            HashSet<Vec2Int> ToAdd = new HashSet<Vec2Int>(NearbyChunks);
            ToAdd.ExceptWith(ActiveChunks);

            HashSet<Vec2Int> ToRemove = new HashSet<Vec2Int>(ActiveChunks);
            ToRemove.ExceptWith(NearbyChunks);

            foreach(var chunk in ToAdd)
            {
                ChunkView chunkView = Generator.GenerateChunk(chunk).HeaderView;
                BasicGridManager.AddChunk(chunk, chunkView, firstGeneration);
                ActiveChunks.Add(chunk);
            }

            foreach(var chunk in ToRemove)
            {
                BasicGridManager.RemoveChunk(chunk);
                ActiveChunks.Remove(chunk);
            }

            Vec2 camPos = VectorExtensions.ConstructVec2((Vector2)Camera.transform.position, Vec2.Zero);
            Sky.UpdateSky(
                biomeID: Generator.BiomeAt(camPos),
                skyColor: Generator.SkyColorAt(camPos),
                weather: WeatherID.Clear
                );

            firstGeneration = false;
        }

        private void LateUpdate()
        {
            Camera.transform.position += CameraSpeed * Time.deltaTime;
        }
    }
}
