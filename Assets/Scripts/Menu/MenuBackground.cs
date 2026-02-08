using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Worldgen;
using Larnix.Client.Terrain;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Background;
using Larnix.Core;

namespace Larnix.Menu
{
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] BasicGridManager BasicGridManager;
        [SerializeField] Vector3 CameraSpeed;

        private Generator Generator;
        private Sky Sky => Ref.Sky;

        private HashSet<Vec2Int> ActiveChunks = new();
        private bool firstGeneration = true;

        private void Start()
        {
            Generator = new Generator(Common.Rand().Next());
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
                BasicGridManager.AddChunk(chunk, Generator.GenerateChunk(chunk), firstGeneration);
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
                weather: Weather.Clear
                );

            firstGeneration = false;
        }

        private void LateUpdate()
        {
            Camera.transform.position += CameraSpeed * Time.deltaTime;
        }
    }
}
