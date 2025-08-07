using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

namespace Larnix.Client
{
    public class Debug : MonoBehaviour
    {
        [SerializeField] public bool ShowDebugInfo;
        [SerializeField] public bool AdvancedDebugKeys;
        [SerializeField] public bool SpawnWildpigsWithZ;
        [SerializeField] public bool UnlinkTerrainData;
        [SerializeField] public bool NoiseDisplay;
        [SerializeField] public bool SpectatorMode;

        [SerializeField] TextMeshProUGUI DebugF3;

        private int FixedFrame = 0;
        private float? LastFPS = null;

        private void Awake()
        {
            References.Debug = this;
            Server.References.Debug = this;
        }

        private void OnDestroy()
        {
            References.Debug = null;
            Server.References.Debug = null;
        }

        private void FixedUpdate()
        {
            FixedFrame++;
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.F3))
                ShowDebugInfo = !ShowDebugInfo;

            if(AdvancedDebugKeys)
            {
                if(Input.GetKeyDown(KeyCode.N))
                    SpectatorMode = !SpectatorMode;
            }
        }

        private void LateUpdate()
        {
            // Coordinates text update (temporary)

            if (LastFPS == null || FixedFrame % 15 == 0)
                LastFPS = (float)(Math.Round(1f / Time.deltaTime * 10f) / 10f);

            Vector2 playerPos = References.MainPlayer.GetPosition();

            string debugText =
                $"FPS: {LastFPS}\n" +
                $"X: {playerPos.x}\n" +
                $"Y: {playerPos.y}\n";

            if (NoiseDisplay)
            {
                debugText += $"\n" + (References.Client.IsMultiplayer ?
                    "Cannot access noise info on a remote server." :
                    Server.References.Generator.GetNoiseInfo(Blocks.ChunkMethods.CoordsToBlock(References.MainPlayer.GetPosition())));
            }

            DebugF3.text = ShowDebugInfo && References.MainPlayer.IsAlive ? debugText : "";
        }
    }
}
