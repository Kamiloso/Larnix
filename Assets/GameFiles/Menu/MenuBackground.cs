using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Worldgen;
using Larnix.Client.Terrain;
using Larnix.Blocks;
using Larnix.Core;

namespace Larnix.Menu
{
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] GridManager GridManager;
        [SerializeField] Vector3 CameraSpeed;

        private Generator Generator;

        private HashSet<Vector2Int> ActiveChunks = new();
        private bool firstGeneration = true;

        private void Start()
        {
            Generator = new Generator(Common.Rand().Next());
        }

        private void Update()
        {
            HashSet<Vector2Int> NearbyChunks = Server.Terrain.ChunkLoading.GetNearbyChunks(
                ChunkMethods.CoordsToChunk(Camera.transform.position), 2);

            HashSet<Vector2Int> ToAdd = new HashSet<Vector2Int>(NearbyChunks);
            ToAdd.ExceptWith(ActiveChunks);

            HashSet<Vector2Int> ToRemove = new HashSet<Vector2Int>(ActiveChunks);
            ToRemove.ExceptWith(NearbyChunks);

            foreach(var chunk in ToAdd)
            {
                GridManager.AddChunk(chunk, Generator.GenerateChunk(chunk), firstGeneration);
                ActiveChunks.Add(chunk);
            }

            foreach(var chunk in ToRemove)
            {
                GridManager.RemoveChunk(chunk);
                ActiveChunks.Remove(chunk);
            }

            Camera.backgroundColor = Generator.SkyColorAt((Vector2)Camera.transform.position);

            firstGeneration = false;
        }

        private void LateUpdate()
        {
            Camera.transform.position += CameraSpeed * Time.deltaTime;
        }
    }
}
