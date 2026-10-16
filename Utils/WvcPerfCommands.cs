using System;
using System.Collections.Generic;
using System.Globalization;
using MelonLoader;
using S1API.Console;

using NativeConsole = Il2CppScheduleOne.Console;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// <c>wvc_perf</c> - controls the frame spike checker (<see cref="WvcPerfMonitor"/>).
    ///
    /// S1API finds <see cref="BaseConsoleCommand"/> subclasses when the console wakes up, so no
    /// registration call is needed (same road the weather and casino commands travel). Output goes
    /// to the in-game console and the MelonLoader log so it lands in the bug-report file too.
    /// </summary>
    public class PerfCommand : BaseConsoleCommand
    {
        public override string CommandWord => "wvc_perf";

        public override string CommandDescription =>
            "Frame spike checker: on/off, threshold &lt;ms&gt;, interval &lt;s&gt;, status, report, reset.";

        public override string ExampleUsage =>
            "wvc_perf on | off | threshold 50 | interval 1 | status | report | reset";

        private static void Report(string message)
        {
            try { NativeConsole.LogWarning(WvcPerfMonitor.Prefix + message); }
            catch (Exception) { }
            try { WvcLog.Always(WvcPerfMonitor.Prefix + message); }
            catch (Exception) { }
        }

        private static List<string> Clean(List<string> args)
        {
            var pieces = new List<string>();
            if (args == null)
                return pieces;

            for (int i = 0; i < args.Count; i++)
            {
                string piece = args[i];
                if (string.IsNullOrWhiteSpace(piece))
                    continue;
                piece = piece.Trim();
                if (string.Equals(piece, "wvc_perf", StringComparison.OrdinalIgnoreCase))
                    continue;
                pieces.Add(piece);
            }
            return pieces;
        }

        private static bool TryFloat(string raw, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            raw = raw.Replace("ms", "").Replace("s", "").Replace("f", "").Trim();
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public override void ExecuteCommand(List<string> args)
        {
            try
            {
                List<string> parts = Clean(args);
                string sub = parts.Count > 0 ? parts[0].ToLowerInvariant() : "status";

                switch (sub)
                {
                    case "on":
                    case "enable":
                        WvcPerfMonitor.SetEnabled(true);
                        Report("watch the console while you play; spikes print automatically.");
                        break;

                    case "off":
                    case "disable":
                        WvcPerfMonitor.SetEnabled(false);
                        break;

                    case "toggle":
                        WvcPerfMonitor.SetEnabled(!WvcPerfMonitor.Enabled);
                        break;

                    case "threshold":
                        if (parts.Count > 1 && TryFloat(parts[1], out float th))
                            WvcPerfMonitor.SetThreshold(th);
                        else
                            Report("usage: wvc_perf threshold <ms>  (current " +
                                   WvcPerfMonitor.SpikeThresholdMs.ToString("0") + "ms)");
                        break;

                    case "interval":
                        if (parts.Count > 1 && TryFloat(parts[1], out float iv))
                            WvcPerfMonitor.SetReportInterval(iv);
                        else
                            Report("usage: wvc_perf interval <seconds>");
                        break;

                    case "status":
                        Report(WvcPerfMonitor.Status());
                        break;

                    case "report":
                        Report(WvcPerfMonitor.Report());
                        break;

                    case "reset":
                        WvcPerfMonitor.ResetStats();
                        Report("stats cleared.");
                        break;

                    default:
                        Report("unknown '" + sub + "'. Try: on, off, threshold <ms>, interval <s>, status, report, reset");
                        break;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Perf] wvc_perf exception: " + ex);
            }
        }
    }
}
