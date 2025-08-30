using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;

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
        private float LastPing = 0f;
        private List<float> FrameTimes = new();

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
            // FPS calculate

            FrameTimes.Add(Time.deltaTime);
            if (LastFPS == null || FixedFrame % 25 == 0)
            {
                LastFPS = (float)(Math.Round(1f / FrameTimes.Average() * 10f) / 10f);
                FrameTimes.Clear();
            }

            // Ping calculate

            if (LastPing == 0f || FixedFrame % 25 == 0)
            {
                LastPing = (float)(Math.Round((References.Client.LarnixClient?.GetPing() ?? 0f) * 10f) / 10f);
            }

            // Coordinates text update (temporary)

            Vector2 playerPos = References.MainPlayer.GetPosition();

            string debugText =
                $"FPS: {LastFPS}\n" +
                $"Ping: {LastPing} ms\n" +
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
