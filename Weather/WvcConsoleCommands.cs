using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using S1API.Console;

using NativeConsole = Il2CppScheduleOne.Console;

namespace CustomNPCExample.Weather
{
    /// <summary>
    /// The <c>setweather</c> console command.
    ///
    /// The game already ships a command with that word (<c>ScheduleOne.Console+SetWeather</c>), and
    /// the console answers from its own command table before anything else is asked. So a
    /// replacement is only ever reached once the game's address for the word has been taken away,
    /// which is what the patch on <c>Console.Awake</c> does.
    ///
    /// The command itself is an S1API console command: S1API looks for those when the console wakes
    /// up and routes unknown words to them, which is the same road its own commands travel.
    /// </summary>
    public static class WvcConsoleCommands
    {
        private static HarmonyLib.Harmony _harmony;
        private static bool _applied;
        private static bool _logged;

        public static bool IsRegistered { get; private set; }

        /// <summary>
        /// Hooks the console so the game's own <c>setweather</c> is removed as soon as the console
        /// exists, and so typing the word reaches the mod even if S1API's routing is not in place.
        /// </summary>
        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                _harmony = new HarmonyLib.Harmony("wvc.snow.console");

                System.Reflection.MethodInfo awake = AccessTools.Method(
                    typeof(NativeConsole),
                    "Awake");

                if (awake != null)
                {
                    _harmony.Patch(
                        awake,
                        postfix: new HarmonyMethod(
                            typeof(WvcConsoleCommands),
                            nameof(ConsoleAwake_Postfix)));
                }
                else
                {

                }

                // The command is an S1API console command, so S1API's own scan finds it when the
                // console wakes up - no registration call is needed here (the registry S1API keeps is
                // internal to it). Nothing is patched onto SubmitCommand either: S1API already routes
                // words the game does not answer, and a second route of our own made every command
                // run twice.
                _applied = true;
                IsRegistered = true;
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The console has just built its command table: take the game's own <c>setweather</c> out
        /// of it, both the list and the table the console actually answers from.
        /// </summary>
        private static void ConsoleAwake_Postfix(NativeConsole __instance)
        {
            try
            {
                RemoveNativeSetWeatherCommand(__instance);

                if (!_logged)
                {
                    _logged = true;


                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Runs the pieces of a typed <c>setweather</c> line: the first word that is not the command
        /// itself is the argument, and everything after it is handed on as the rest. The console
        /// splits a line differently depending on how it was reached - the game's own command table,
        /// S1API's routing, or a hotkey standing in for either - so nothing is assumed about which
        /// piece is which.
        /// </summary>
        public static void RunSetWeather(List<string> pieces)
        {
            string arg = string.Empty;
            var rest = new List<string>();

            if (pieces != null)
            {
                bool haveArg = false;

                for (int i = 0; i < pieces.Count; i++)
                {
                    string piece = pieces[i];

                    if (string.IsNullOrWhiteSpace(piece) || IsSetWeatherWord(piece))
                        continue;

                    if (!haveArg)
                    {
                        arg = piece.Trim();
                        haveArg = true;
                    }
                    else
                    {
                        rest.Add(piece.Trim());
                    }
                }
            }



            HandleWeatherCommand(arg, rest);
        }

        /// <summary>Answers in the game's own console as well as the log, so the player sees it.</summary>
        private static void Report(string message)
        {


            try
            {
                NativeConsole.LogWarning("[WVC Snow] " + message);
            }
            catch (Exception)
            {

            }
        }

        public static void HandleWeatherCommand(string arg)
        {
            HandleWeatherCommand(arg, null);
        }

        /// <summary>
        /// Runs a typed command, with any words that followed it.
        ///
        /// The settings words (<c>schedule</c>, <c>blizzards</c>, <c>flakes</c>) take a value after
        /// them, which is why the words past the first one are handed over rather than dropped.
        /// </summary>
        public static void HandleWeatherCommand(string arg, List<string> rest)
        {
            if (string.IsNullOrEmpty(arg))
            {
                Report(
                    "setweather needs one of: snow, heavysnow, snowstorm, blizzard, clear, " +
                    "lightrain, heavyrain, schedule on|off, blizzards <0-7>, flakes <n>, " +
                    "stormflakes <n>, chatter on|off, settings, sky, diag.");

                return;
            }

            if (arg.Equals("diag", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("debug", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                WvcSnowWeather.ReportState("setweather " + arg.ToLowerInvariant());

                Report("Snow state written to MelonLoader\\Latest.log.");

                return;
            }

            string value =
                rest != null && rest.Count > 0
                    ? rest[0].Trim()
                    : string.Empty;

            // The settings words. None of them touches the weather, so they answer and stop here
            // rather than marking the day as hand-set weather.
            if (arg.Equals("schedule", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("schedulesnow", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOnOff(value, out bool enabled))
                {
                    Report(
                        "setweather schedule needs 'on' or 'off' (now " +
                        (WvcSnowSettings.ScheduleEnabled ? "on" : "off") + ").");

                    return;
                }

                WvcSnowSettings.ScheduleEnabled = enabled;

                Report(
                    "Snow schedule " + (enabled ? "on" : "off") + " - automatic snow and blizzards " +
                    (enabled ? "will run" : "will not run") + " (" +
                    WvcSnowSettings.Describe() + ").");

                return;
            }

            if (arg.Equals("blizzards", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("storms", StringComparison.OrdinalIgnoreCase))
            {
                int count;

                if (!TryReadCount(value, 0, 7, out count))
                {
                    Report(
                        "setweather blizzards needs a number from 0 to 7 (now " +
                        WvcSnowSettings.BlizzardsPerWeek + " a week).");

                    return;
                }

                WvcSnowSettings.BlizzardsPerWeek = count;

                Report(
                    "Blizzards each week: " + WvcSnowSettings.BlizzardsPerWeek +
                    " - the schedule picks the days when it rolls a new week.");

                return;
            }

            if (arg.Equals("flakes", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("snowflakes", StringComparison.OrdinalIgnoreCase))
            {
                int count;

                if (!TryReadCount(
                        value,
                        WvcSnowSettings.MinSnowFlakes,
                        WvcSnowSettings.MaxSnowFlakes,
                        out count))
                {
                    Report(
                        "setweather flakes needs a number from " +
                        WvcSnowSettings.MinSnowFlakes + " to " +
                        WvcSnowSettings.MaxSnowFlakes + " (now " +
                        WvcSnowSettings.SnowFlakes + ").");

                    return;
                }

                WvcSnowSettings.SnowFlakes = count;

                Report(
                    "Calm snowflakes: " + WvcSnowSettings.SnowFlakes +
                    ". Lower means more frames; it takes effect as the overlay is rebuilt.");

                return;
            }

            if (arg.Equals("stormflakes", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("blizzardflakes", StringComparison.OrdinalIgnoreCase))
            {
                int count;

                if (!TryReadCount(
                        value,
                        WvcSnowSettings.MinSnowFlakes,
                        WvcSnowSettings.MaxSnowFlakes,
                        out count))
                {
                    Report(
                        "setweather stormflakes needs a number from " +
                        WvcSnowSettings.MinSnowFlakes + " to " +
                        WvcSnowSettings.MaxSnowFlakes + " (now " +
                        WvcSnowSettings.StormFlakes + ").");

                    return;
                }

                WvcSnowSettings.StormFlakes = count;

                Report(
                    "Blizzard flakes: " + WvcSnowSettings.StormFlakes +
                    ". Lower means more frames; it takes effect as the overlay is rebuilt.");

                return;
            }

            if (arg.Equals("settings", StringComparison.OrdinalIgnoreCase))
            {
                Report(
                    "Snow settings: " + WvcSnowSettings.Describe() +
                    ". The same values are rows in the game's settings screen, and in " +
                    "%USERPROFILE%\\AppData\\LocalLow\\TVGS\\Schedule I\\MelonPreferences.cfg.");

                return;
            }

            if (arg.Equals("chatter", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("talk", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOnOff(value, out bool talking))
                {
                    Report(
                        "setweather chatter needs 'on' or 'off' (now " +
                        (WvcSnowSettings.WeatherDialogue ? "on" : "off") + ").");

                    return;
                }

                WvcSnowSettings.WeatherDialogue = talking;

                Report(
                    "Weather talk " + (talking ? "on" : "off") + " - people you walk up to " +
                    (talking ? "will" : "will not") + " say something about the weather.");

                return;
            }

            if (arg.Equals("sky", StringComparison.OrdinalIgnoreCase))
            {
                WvcSnowWeather.ReportSky();

                Report("Sky state written to MelonLoader\\Latest.log.");

                return;
            }

            // Everything below is the player setting the weather by hand, so the automatic snow
            // schedule has to be told to leave it alone for the rest of the in-game day. Without
            // that the schedule turns the weather straight back the way it wanted it a frame later,
            // which looks exactly like the command having done nothing at all.
            WvcSnowWeather.MarkManualWeather();

            if (arg.Equals(
                    "snowstorm",
                    StringComparison.OrdinalIgnoreCase) ||
                arg.Equals(
                    "blizzard",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (WvcSnowWeather.EnableSnowstorm() &&
                    WvcSnowWeather.IsSnowstorm)
                {
                    Report("Snowstorm on.");
                }
                else
                {
                    Report(
                        "The snowstorm did not start - the log says why (profiles not ready, or " +
                        "the environment is missing).");
                }

                WvcSnowWeather.ReportState("after setweather snowstorm");

                return;
            }

            if (arg.Equals("snow", StringComparison.OrdinalIgnoreCase))
            {
                if (WvcSnowWeather.EnableSnow() &&
                    WvcSnowWeather.IsSnowActive)
                {
                    Report("Snow on.");
                }
                else
                {
                    Report(
                        "The snow did not start - the log says why (profiles not ready, or the " +
                        "environment is missing).");
                }

                WvcSnowWeather.ReportState("after setweather snow");

                return;
            }

            // A heavy fall: as much snow as a blizzard drops, under the calm sky. It is set after
            // plain snow for the reader's sake only - both words are matched whole.
            if (arg.Equals("heavysnow", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("heavysnowfall", StringComparison.OrdinalIgnoreCase))
            {
                if (WvcSnowWeather.EnableHeavySnow() &&
                    WvcSnowWeather.IsSnowActive)
                {
                    Report("Heavy snow on.");
                }
                else
                {
                    Report(
                        "The heavy snow did not start - the log says why (profiles not ready, or " +
                        "the environment is missing).");
                }

                WvcSnowWeather.ReportState("after setweather heavysnow");

                return;
            }

            if (arg.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("lightrain", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("heavyrain", StringComparison.OrdinalIgnoreCase))
            {
                if (WvcSnowWeather.SetNativeWeather(arg))
                {
                    Report("Weather set to " + arg.ToLowerInvariant() + ".");
                }
                else
                {
                    Report(
                        "The weather could not be set to " + arg.ToLowerInvariant() +
                        " - the log says why.");
                }

                WvcSnowWeather.ReportState("after setweather " + arg.ToLowerInvariant());

                return;
            }

            Report(
                "setweather does not know '" + arg +
                "'. Use one of: snow, heavysnow, snowstorm, blizzard, clear, lightrain, " +
                "heavyrain.");
        }

        /// <summary>
        /// Takes the game's own <c>setweather</c> out of the console: the list it shows and the table
        /// it answers words from are two different things, and the game's command is in both.
        /// </summary>
        private static void RemoveNativeSetWeatherCommand(NativeConsole console)
        {
            try
            {
                int removed = 0;

                var list = NativeConsole.Commands;

                if (list != null)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var command = list[i];

                        if (command == null)
                            continue;

                        if (IsSetWeatherWord(WordOf(command)))
                        {
                            list.RemoveAt(i);
                            removed++;
                        }
                    }
                }
                else
                {

                }

                var table = NativeConsole.commands;

                if (table != null)
                {
                    // The words are collected first and the table is written to afterwards: the
                    // console answers from this table, and changing it while walking it is not
                    // allowed.
                    var keys = new List<string>();

                    foreach (var pair in table)
                    {
                        if (pair.Value == null)
                            continue;

                        if (IsSetWeatherWord(WordOf(pair.Value)))
                            keys.Add(pair.Key);
                    }

                    for (int i = 0; i < keys.Count; i++)
                    {
                        table.Remove(keys[i]);
                        removed++;
                    }
                }
                else
                {

                }

                if (removed <= 0)
                {

                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
        }

        private static bool IsSetWeatherWord(string word)
        {
            return string.Equals(word, "setweather", StringComparison.OrdinalIgnoreCase);
        }

        private static string WordOf(NativeConsole.ConsoleCommand command)
        {
            try
            {
                return command.CommandWord;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Reads on/off the several ways a player might type it.</summary>
        private static bool TryReadOnOff(string text, out bool value)
        {
            value = false;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            switch (text.Trim().ToLowerInvariant())
            {
                case "on":
                case "true":
                case "yes":
                case "1":
                case "enable":
                case "enabled":
                    value = true;
                    return true;

                case "off":
                case "false":
                case "no":
                case "0":
                case "disable":
                case "disabled":
                    value = false;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Reads a whole number and keeps it only if it is inside the range asked for.</summary>
        private static bool TryReadCount(
            string text,
            int min,
            int max,
            out int value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (!int.TryParse(text.Trim(), out int parsed))
                return false;

            if (parsed < min || parsed > max)
                return false;

            value = parsed;
            return true;
        }
    }

    /// <summary>
    /// The mod's <c>setweather</c>.
    ///
    /// It is an S1API console command rather than one of the game's own command types: S1API looks
    /// for these when the console wakes up and hands them the words the game itself does not answer,
    /// which is the road its own commands - and the command that hands out powder - already travel.
    ///
    /// The typed line is handed on as it arrived; which piece of it is the argument is worked out
    /// there rather than assumed, so the command answers the same however the console split it.
    /// </summary>
    public class SetWeatherSnowCommand : BaseConsoleCommand
    {
        public override string CommandWord => "setweather";

        public override string CommandDescription => "Sets the weather, snow included.";

        public override string ExampleUsage =>
            "setweather snow | heavysnow | snowstorm | blizzard | clear | lightrain | heavyrain | " +
            "schedule on|off | blizzards 2 | flakes 850 | chatter on|off | sky | diag";

        public override void ExecuteCommand(List<string> args)
        {
            try
            {
                WvcConsoleCommands.RunSetWeather(args);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snow] setweather exception: " + ex);
            }
        }
    }
}
