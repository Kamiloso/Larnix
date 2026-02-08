using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Profiling;
using Larnix.Core.Vectors;
using Larnix.Core;

namespace Larnix.Client
{
    public class Debugger : MonoBehaviour
    {
        private Client Client => Ref.Client;
        private MainPlayer MainPlayer => Ref.MainPlayer;

        [SerializeField] public bool ShowDebugInfo;
        [SerializeField] public bool AdvancedDebugKeys;
        [SerializeField] public bool SpectatorMode;
        [SerializeField] public bool ClientBlockSwap;
        [SerializeField] TextMeshProUGUI DebugF3;

        private float? _lastFPS = null;
        private float _lastPing = 0f;
        private List<float> _frameTimes = new();

        private void Awake()
        {
            Ref.Debugger = this;
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

            _frameTimes.Add(Time.deltaTime);
            if (_lastFPS == null || Client.FixedFrame % 25 == 0)
            {
                _lastFPS = (float)(Math.Round(1f / _frameTimes.Average() * 10f) / 10f);
                _frameTimes.Clear();
            }

            // Ping calculate

            if (_lastPing == 0f || Client.FixedFrame % 25 == 0)
            {
                _lastPing = (float)(Math.Round(Client.GetPing() * 10f) / 10f);
            }

            // Allocations

            long used = Profiler.GetMonoUsedSizeLong();
            long heap = Profiler.GetMonoHeapSizeLong();

            float usedMB = used / (1024f * 1024f);
            float heapMB = heap / (1024f * 1024f);
            float percent = (heap > 0) ? (used / (float)heap) * 100f : 0f;

            string allocations = $"{usedMB:F2} / {heapMB:F2} MB ({percent:F1}%)";

            // Coordinates text update (temporary)

            Vec2 playerPos = MainPlayer.Position;

            string debugText =
                $"FPS: {_lastFPS}\n" +
                $"Ping: {_lastPing} ms\n" +
                $"Memory: {allocations}\n" +
                $"X: {playerPos.x}\n" +
                $"Y: {playerPos.y}\n";

            DebugF3.text = ShowDebugInfo && MainPlayer.IsAlive ? debugText : "";
        }
    }
}
