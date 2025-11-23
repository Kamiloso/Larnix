using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Profiling;
using Larnix.Core.Vectors;

namespace Larnix.Client
{
    public class Debug : MonoBehaviour
    {
        [SerializeField] public bool ShowDebugInfo;
        [SerializeField] public bool AdvancedDebugKeys;
        [SerializeField] public bool SpectatorMode;
        [SerializeField] public bool ClientBlockSwap;

        [SerializeField] TextMeshProUGUI DebugF3;

        private int FixedFrame = 0;
        private float? LastFPS = null;
        private float LastPing = 0f;
        private List<float> FrameTimes = new();

        private void Awake()
        {
            Ref.Debug = this;
        }

        private void OnDestroy()
        {
            Ref.Debug = null;
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
                LastPing = (float)(Math.Round((Ref.Client.LarnixClient?.GetPing() ?? 0f) * 10f) / 10f);
            }

            // Allocations

            long used = Profiler.GetMonoUsedSizeLong();
            long heap = Profiler.GetMonoHeapSizeLong();

            float usedMB = used / (1024f * 1024f);
            float heapMB = heap / (1024f * 1024f);
            float percent = (heap > 0) ? (used / (float)heap) * 100f : 0f;

            string allocations = $"{usedMB:F2} / {heapMB:F2} MB";

            // Coordinates text update (temporary)

            Vec2 playerPos = Ref.MainPlayer.Position;

            string debugText =
                $"FPS: {LastFPS}\n" +
                $"Ping: {LastPing} ms\n" +
                $"Memory: {allocations}\n" +
                $"X: {playerPos.x}\n" +
                $"Y: {playerPos.y}\n";

            DebugF3.text = ShowDebugInfo && Ref.MainPlayer.IsAlive ? debugText : "";
        }
    }
}
