using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Profiling;
using Larnix.Core.Vectors;
using Larnix.Worldgen.Biomes;
using Larnix.Core;

namespace Larnix.Client
{
    public class Debugger : MonoBehaviour
    {
        private Client Client => GlobRef.Get<Client>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();

        [SerializeField] public bool ShowDebugInfo;
        [SerializeField] public bool AdvancedDebugKeys;
        [SerializeField] public bool SpectatorMode;
        [SerializeField] public bool ClientBlockSwap;
        [SerializeField] TextMeshProUGUI DebugF3;

        private BiomeID _currentBiome;
        private long _serverTick;
        private float _tps;

        private float? _lastFPS = null;
        private float? _lastTPS = null;
        private float _lastPing = 0f;
        private List<float> _frameTimes = new();
        private List<float> _tpsSamples = new();

        private void Awake()
        {
            GlobRef.Set(this);
        }

        public void InfoUpdate(long serverTick, BiomeID biomeID, float tps)
        {
            _serverTick = serverTick;
            _currentBiome = biomeID;
            _tps = tps;
            _tpsSamples.Add(tps);
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

            // TPS calculate (average like FPS)
            if (_lastTPS == null || Client.FixedFrame % 25 == 0)
            {
                if (_tpsSamples.Count > 0)
                {
                    _lastTPS = (float)(Math.Round(_tpsSamples.Average() * 10f) / 10f);
                    _tpsSamples.Clear();
                }
                else
                {
                    _lastTPS = _tps;
                }
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
                $"Y: {playerPos.y}\n" +
                $"Biome: {_currentBiome}\n" +
                $"World Age: {TextAge(_serverTick)}\n" +
                $"TPS: {(_lastTPS ?? _tps):F1}\n";

            DebugF3.text = ShowDebugInfo && MainPlayer.IsAlive ? debugText : "";
        }

        private static string TextAge(long ticks)
        {
            long seconds = ticks / 50;
            long minutes = seconds / 60;
            long hours = minutes / 60;
            long days = hours / 24;

            seconds %= 60;
            minutes %= 60;
            hours %= 24;

            if (days == 0)
            {
                if (hours == 0)
                {
                    if (minutes == 0)
                    {
                        return $"{seconds}s";
                    }
                    return $"{minutes}m {seconds}s";
                }
                return $"{hours}h {minutes}m {seconds}s";
            }
            return $"{days}d {hours}h {minutes}m {seconds}s";
        }
    }
}
