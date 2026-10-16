using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Frame spike checker. Measures how long each frame takes, flags frames over a threshold,
    /// and on a spike reports - to the MelonLoader console/log - how much of that frame the mod's
    /// own Update() cost (with a per-file breakdown of the drug-effect managers) plus which drug
    /// effects were active. That splits every spike into "one of my files is hot" versus "this is
    /// rendering / other game code" - the point of the freecam lag tests.
    ///
    /// Off by default. Drive it with the <c>wvc_perf</c> console command. While off, each timed
    /// section costs one timestamp read and an early-out; nothing is allocated.
    /// </summary>
    public static class WvcPerfMonitor
    {
        public const string Prefix = "[WVC Perf] ";

        private static bool _enabled;
        private static float _spikeThresholdMs = 40f;   // ~25fps
        private static float _reportInterval = 0.75f;   // min seconds between printed spike reports
        private const int WarmupFrames = 10;            // ignore first frames (scene load / shader compile)
        private const int RingSize = 600;               // ~10s of history at 60fps

        private static readonly float[] _frameRing = new float[RingSize];
        private static int _ringCount;
        private static int _ringIndex;
        private static long _totalFrames;
        private static long _spikeCount;
        private static float _worstFrameMs;
        private static int _warmup;
        private static float _lastReportTime = -999f;

        // Sections recorded during the frame currently being built (read by Tick() next frame).
        private static readonly Dictionary<string, float> _frameSections = new Dictionary<string, float>();

        // Cumulative peaks and "how often was this the hottest file" across every spike.
        private static readonly Dictionary<string, float> _peakMs = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> _hotHits = new Dictionary<string, int>();

        private static readonly Dictionary<string, Func<bool>> _probes = new Dictionary<string, Func<bool>>();
        private static readonly List<string> _activeScratch = new List<string>();
        private static readonly List<KeyValuePair<string, float>> _sortedScratch = new List<KeyValuePair<string, float>>();

        private static float _lastModUpdateMs;

        public static bool Enabled => _enabled;
        public static float SpikeThresholdMs => _spikeThresholdMs;
        public static long SpikeCount => _spikeCount;
        public static long TotalFrames => _totalFrames;

        /// <summary>Timestamp for manual begin/end timing (0 while disabled).</summary>
        public static long Now() => _enabled ? Stopwatch.GetTimestamp() : 0L;

        /// <summary>Accumulates elapsed time from <paramref name="start"/> into <paramref name="name"/> for this frame.</summary>
        public static void End(string name, long start)
        {
            if (!_enabled || name == null || start == 0L)
                return;

            long dt = Stopwatch.GetTimestamp() - start;
            float ms = (float)(dt * 1000.0 / Stopwatch.Frequency);

            _frameSections.TryGetValue(name, out float acc);
            _frameSections[name] = acc + ms;
        }

        /// <summary>Allocation-free timing scope: <c>using (WvcPerfMonitor.Section("Name")) SomeCall();</c></summary>
        public struct Scope : IDisposable
        {
            private readonly string _name;
            private readonly long _start;

            internal Scope(string name)
            {
                _name = name;
                _start = WvcPerfMonitor.Now();
            }

            public void Dispose() => WvcPerfMonitor.End(_name, _start);
        }

        public static Scope Section(string name) => new Scope(name);

        /// <summary>Registers a probe reporting whether a drug effect is currently running.</summary>
        public static void RegisterActive(string name, Func<bool> isActive)
        {
            if (name == null || isActive == null)
                return;
            _probes[name] = isActive;
        }

        public static void SetEnabled(bool on)
        {
            if (on && !_enabled)
                ResetStats();
            _enabled = on;
            if (_enabled)
                WvcLog.Always(Prefix + "ON - threshold " + _spikeThresholdMs.ToString("0") +
                              "ms, report every " + _reportInterval.ToString("0.0") + "s. Play and watch for spikes.");
            else
                WvcLog.Always(Prefix + "OFF.");
        }

        public static void SetThreshold(float ms)
        {
            _spikeThresholdMs = Mathf.Clamp(ms, 5f, 1000f);
            WvcLog.Always(Prefix + "Spike threshold = " + _spikeThresholdMs.ToString("0") + "ms.");
        }

        public static void SetReportInterval(float seconds)
        {
            _reportInterval = Mathf.Clamp(seconds, 0.05f, 30f);
            WvcLog.Always(Prefix + "Report interval = " + _reportInterval.ToString("0.00") + "s.");
        }

        public static void ResetStats()
        {
            _ringCount = 0;
            _ringIndex = 0;
            _totalFrames = 0;
            _spikeCount = 0;
            _worstFrameMs = 0f;
            _warmup = 0;
            _frameSections.Clear();
            _peakMs.Clear();
            _hotHits.Clear();
            _lastModUpdateMs = 0f;
        }

        /// <summary>
        /// Called once per frame at the top of <c>MainMod.OnUpdate()</c>. The section data left in
        /// the buffer belongs to the frame that <c>Time.unscaledDeltaTime</c> is about to report on,
        /// so a spike here is reported against that frame's work, then the buffer is cleared.
        /// </summary>
        public static void Tick()
        {
            if (!_enabled)
                return;

            float frameMs = Time.unscaledDeltaTime * 1000f;

            // Warm-up and loading hiccups are not gameplay spikes; drop them and the frame's data.
            if (_warmup < WarmupFrames || frameMs > 1000f)
            {
                _warmup++;
                _frameSections.Clear();
                return;
            }

            _totalFrames++;

            _frameRing[_ringIndex] = frameMs;
            _ringIndex = (_ringIndex + 1) % _frameRing.Length;
            if (_ringCount < _frameRing.Length)
                _ringCount++;
            if (frameMs > _worstFrameMs)
                _worstFrameMs = frameMs;

            _frameSections.TryGetValue("Mod.Update", out _lastModUpdateMs);

            if (frameMs >= _spikeThresholdMs)
            {
                _spikeCount++;
                UpdatePeaksAndHits();

                if (Time.realtimeSinceStartup - _lastReportTime >= _reportInterval)
                {
                    _lastReportTime = Time.realtimeSinceStartup;
                    ReportSpike(frameMs);
                }
            }

            _frameSections.Clear();
        }

        // ---- reporting -------------------------------------------------------------------------

        private static void UpdatePeaksAndHits()
        {
            string hottest = null;
            float hottestMs = 0f;

            foreach (var kv in _frameSections)
            {
                if (kv.Key == "Mod.Update")
                    continue;

                if (kv.Value > hottestMs)
                {
                    hottestMs = kv.Value;
                    hottest = kv.Key;
                }

                if (!_peakMs.TryGetValue(kv.Key, out float peak) || kv.Value > peak)
                    _peakMs[kv.Key] = kv.Value;
            }

            if (hottest != null)
                _hotHits[hottest] = (_hotHits.TryGetValue(hottest, out int c) ? c : 0) + 1;
        }

        private static void ReportSpike(float frameMs)
        {
            float fps = frameMs > 0.001f ? 1000f / frameMs : 0f;
            float modPct = frameMs > 0.001f ? _lastModUpdateMs / frameMs * 100f : 0f;

            var sb = new StringBuilder(256);
            sb.Append(Prefix).Append("SPIKE ").Append(frameMs.ToString("0.0")).Append("ms (")
              .Append(fps.ToString("0.0")).Append("fps) #").Append(_spikeCount)
              .Append(" worst=").Append(_worstFrameMs.ToString("0.0")).Append("ms | Mod.Update=")
              .Append(_lastModUpdateMs.ToString("0.0")).Append("ms (").Append(modPct.ToString("0")).Append("%)");
            WvcLog.Always(sb.ToString());

            // Per-file breakdown for this frame (these run *inside* Mod.Update).
            _sortedScratch.Clear();
            foreach (var kv in _frameSections)
            {
                if (kv.Key == "Mod.Update")
                    continue;
                _sortedScratch.Add(kv);
            }
            _sortedScratch.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (_sortedScratch.Count > 0)
            {
                var top = new StringBuilder(160);
                top.Append(Prefix).Append("  files this frame: ");
                int show = Math.Min(6, _sortedScratch.Count);
                for (int i = 0; i < show; i++)
                {
                    if (i > 0) top.Append(", ");
                    top.Append(_sortedScratch[i].Key).Append('=')
                       .Append(_sortedScratch[i].Value.ToString("0.00")).Append("ms");
                }
                WvcLog.Always(top.ToString());
            }

            // Active drug effects - catches render/shader cost that never touches Update().
            SampleActive();
            WvcLog.Always(Prefix + "  active effects: " +
                          (_activeScratch.Count > 0 ? string.Join(", ", _activeScratch) : "none"));

            if (modPct < 15f)
            {
                WvcLog.Always(Prefix + "  verdict: mod Update is only " + modPct.ToString("0") +
                              "% of the frame -> spike is rendering / other game code, not a mod file.");
            }
            else
            {
                string hot = _sortedScratch.Count > 0 ? _sortedScratch[0].Key : "(no section)";
                WvcLog.Always(Prefix + "  verdict: mod Update is " + modPct.ToString("0") +
                              "% of the frame -> hot file: " + hot);
            }
        }

        private static void SampleActive()
        {
            _activeScratch.Clear();
            foreach (var kv in _probes)
            {
                try
                {
                    if (kv.Value())
                        _activeScratch.Add(kv.Key);
                }
                catch
                {
                    // a bad probe must never break the frame
                }
            }
        }

        /// <summary>Snapshot for <c>wvc_perf status</c>.</summary>
        public static string Status()
        {
            ComputeStats(out float avgMs, out float minMs, out float maxMs);
            float avgFps = avgMs > 0.001f ? 1000f / avgMs : 0f;
            float bestFps = minMs > 0.001f ? 1000f / minMs : 0f; // best fps = shortest frame
            float worstFps = maxMs > 0.001f ? 1000f / maxMs : 0f; // worst fps = longest frame

            var sb = new StringBuilder(256);
            sb.Append("enabled=").Append(_enabled ? "on" : "off")
              .Append(" threshold=").Append(_spikeThresholdMs.ToString("0")).Append("ms")
              .Append(" frames=").Append(_totalFrames)
              .Append(" spikes=").Append(_spikeCount)
              .Append(" worst=").Append(_worstFrameMs.ToString("0.0")).Append("ms")
              .Append(" | avg ").Append(avgFps.ToString("0.0")).Append("fps (")
              .Append(avgMs.ToString("0.0")).Append("ms), best ").Append(bestFps.ToString("0"))
              .Append("fps, worst ").Append(worstFps.ToString("0")).Append("fps");

            SampleActive();
            sb.Append(" | active: ")
              .Append(_activeScratch.Count > 0 ? string.Join(", ", _activeScratch) : "none");

            return sb.ToString();
        }

        /// <summary>Dump for <c>wvc_perf report</c>: current frame's files + all-time hot files.</summary>
        public static string Report()
        {
            var sb = new StringBuilder(320);
            sb.Append("this frame: ");
            _sortedScratch.Clear();
            foreach (var kv in _frameSections)
                _sortedScratch.Add(kv);
            _sortedScratch.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (_sortedScratch.Count == 0)
            {
                sb.Append("(nothing recorded yet - play a frame first)");
            }
            else
            {
                for (int i = 0; i < _sortedScratch.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(_sortedScratch[i].Key).Append('=')
                       .Append(_sortedScratch[i].Value.ToString("0.00")).Append("ms");
                }
            }

            if (_peakMs.Count > 0)
            {
                _sortedScratch.Clear();
                foreach (var kv in _peakMs)
                    _sortedScratch.Add(kv);
                _sortedScratch.Sort((a, b) => b.Value.CompareTo(a.Value));

                sb.Append(" | hottest ever: ");
                int show = Math.Min(6, _sortedScratch.Count);
                for (int i = 0; i < show; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(_sortedScratch[i].Key).Append(" peak=")
                       .Append(_sortedScratch[i].Value.ToString("0.00")).Append("ms");
                    if (_hotHits.TryGetValue(_sortedScratch[i].Key, out int hits))
                        sb.Append(" x").Append(hits);
                }
            }

            return sb.ToString();
        }

        private static void ComputeStats(out float avgMs, out float minMs, out float maxMs)
        {
            avgMs = 0f;
            minMs = 0f;
            maxMs = 0f;
            if (_ringCount == 0)
                return;

            float sum = 0f;
            float min = float.MaxValue;
            float max = 0f;
            for (int i = 0; i < _ringCount; i++)
            {
                float v = _frameRing[i];
                sum += v;
                if (v < min) min = v;
                if (v > max) max = v;
            }

            avgMs = sum / _ringCount;
            minMs = min;
            maxMs = max;
        }


    }
}
