using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Weather
{
    /// <summary>
    /// What the people on the street say about the weather.
    ///
    /// The game's weather greeting is a line out of the NPC's greeting container, one category per
    /// kind of weather: <c>rainy_greeting</c> ("Rainy one, isn't it?", "Gotta get out of this rain",
    /// "Hopefully it stops raining soon", ...), <c>morning_greeting</c>, <c>afternoon_greeting</c>,
    /// <c>night_greeting</c> - and no snow category anywhere in the game. The mod's snow profile is
    /// a copy of the game's heavy rain, which it has to be or the game would not treat it as weather,
    /// so on the day it snows the game reads rain and says rain: the player gets told to get out of
    /// the rain in the middle of a blizzard, and nothing about snow is ever said.
    ///
    /// Rain is the one sky the game speaks for itself - it has a greeting category of its own for it -
    /// so rain lines are left where they are. Every other sky is answered here: snow and blizzards
    /// every time, a grey sky and fog every time they are up, and a plain day only now and then, so
    /// that a fine afternoon is still mostly the game's own small talk rather than a remark about the
    /// weather from everybody in town.
    ///
    /// So the mod answers for the weather instead of asking the game to, at three places, ranked so
    /// that they do not talk over each other:
    ///
    /// 1. <c>WorldspaceDialogueRenderer.ShowText</c> is where a line ends up on screen, whatever
    ///    asked for it - a greeting, an NPC behaviour, a message. This is the one that decides what
    ///    is actually drawn, so it is the one that decides what the player reads.
    /// 2. <c>DialogueController.GetActiveGreeting</c> hands back the line the game settled on. It is
    ///    rewritten too, so the greeting the game carries around - and repeats, out of the cache it
    ///    keeps - is the weather's line rather than the rain's.
    /// 3. <c>DialogueHandler.ShowWorldspaceDialogue</c> and <c>DialogueController.Hovered</c> are the
    ///    fallbacks, patched only when the two above could not be: one line too many is easy to live
    ///    with, none at all is not.
    ///
    /// Two things keep a line from being repeated at the player, and one of them has to be about the
    /// person, not the town. A line already on screen is never reworded (the renderer is asked whether
    /// it is still showing it), and the answer given to a line the game asks for again is remembered
    /// for as long as that line is up - but that memory is kept per line, because the body text under
    /// an NPC's name is drawn on every frame it is up and the whole town would otherwise be given
    /// whichever answer came first. The greeting is what hands each person a line of their own, one
    /// per conversation; the drawn line keeps what it was handed. Picks come out of the pool rather
    /// than off the top of it: the lines just said are held back, which is what makes all of them get
    /// used.
    ///
    /// The last line a person says is left on their body for a while after the conversation ends, and
    /// that is the game's own doing: it is where a snow line is most often seen, and why the line has
    /// to be picked per person rather than per town.
    /// </summary>
    public static class WvcWeatherChatter
    {
        private const string LogPrefix = "[WVC Weather Talk]";

        /// <summary>
        /// How long the words of a greeting stay the same while the game keeps asking. Short, because
        /// the game's own cache is what used to make a line last forever: this only has to stop the
        /// line changing while it is being read, or between what is said on the way in and what is
        /// said in the dialogue box.
        /// </summary>
        private const float GreetingHold = 4f;

        /// <summary>
        /// How long the same drawn line keeps the same words. Keyed on the line the game asked for,
        /// not on the speaker: the line under one person's name is drawn again every frame it is up,
        /// and must not be reworded while it is being read - but the next person's line is a
        /// different line, and gets its own.
        /// </summary>
        private const float ShownHold = 6f;

        /// <summary>How long the fallback path leaves a line on screen.</summary>
        private const float FallbackDuration = 3.6f;

        /// <summary>The most characters a line may have and still be treated as a weather line.</summary>
        private const int WeatherLineLength = 90;

        /// <summary>How many of the lines just said are held back from the next pick.</summary>
        private const int RecentLines = 5;

        /// <summary>How many drawn lines are written to the log, so a stray one can be identified.</summary>
        private const int Diagnostics = 20;

        /// <summary>
        /// The game's own rainy greetings, kept word for word, plus the mod's own. Rain lines are here
        /// so that the rain the game does bring is still talked about, and so that a rain line left in
        /// the pool can be recognised as one of ours when the weather turns.
        /// </summary>
        private static readonly string[] RainLines =
        {
            "Gotta get out of this rain...",
            "Rainy one, isn't it?",
            "This damn rain...",
            "So much rain...",
            "Hopefully it stops raining soon...",
            "Don't like this rain...",
            "Another day of rain...",
            "Soaked through...",
            "Never stops, does it...",
            "My shoes are ruined...",
            "Whole week of this...",
            "Off to wring my coat out...",
            "Should've brought an umbrella...",
            "Listen to it out there...",
            "Damp through and through...",
            "Rain, rain, rain..."
        };

        /// <summary>What is said in snow that is not yet a blizzard.</summary>
        private static readonly string[] SnowLines =
        {
            "Cold as hell out here...",
            "This snow's not letting up...",
            "Can't feel my hands...",
            "Hate this weather...",
            "Should've worn a thicker coat...",
            "Snow again... great.",
            "Everything's white already...",
            "Freezing my arse off...",
            "My feet are soaked...",
            "Wasn't dressed for this...",
            "Snowing again, of course...",
            "Any more of this and I'm moving...",
            "Should've stayed in bed...",
            "Is it snowing? Of course it is...",
            "Been coming down all day...",
            "Can't stand the cold...",
            "Chilly one, isn't it...",
            "Everything's frozen over..."
        };

        /// <summary>What is said when the snow is coming in sideways.</summary>
        private static readonly string[] BlizzardLines =
        {
            "Can't see a thing out here!",
            "This blizzard's insane!",
            "I need to get inside, now!",
            "Gotta find shelter before this gets worse!",
            "Can barely see my own hands!",
            "This wind's going to take me off my feet!",
            "Blizzard's getting worse...",
            "I can't even see the street!",
            "No weather to be out in!",
            "Snow's coming down sideways!",
            "Where's the nearest door?!",
            "Getting buried out here!",
            "I'm going home, this is mad!",
            "Wind nearly knocked me over!",
            "Couldn't find my way in this!",
            "Shouldn't be out in this!"
        };

        /// <summary>What is said when there is nothing in the sky to complain about.</summary>
        private static readonly string[] SunnyLines =
        {
            "Nice one, isn't it...",
            "Not a cloud up there...",
            "Lovely day for it...",
            "Sun's out for once...",
            "Can't complain about this...",
            "Good day to be out and about...",
            "Sky's clear for once...",
            "About time we got some sun...",
            "Warm one today...",
            "Nothing like a day like this...",
            "Weather's finally behaving...",
            "Should make the most of this..."
        };

        /// <summary>What is said under a flat grey sky.</summary>
        private static readonly string[] OvercastLines =
        {
            "Grey one today...",
            "Looks like rain...",
            "Sun's gone in...",
            "Gloomy out, isn't it...",
            "Cloud's coming in...",
            "Feels like it's about to pour...",
            "Dull one today...",
            "Sky's gone flat...",
            "No sun today, then...",
            "Could do with some light...",
            "Looks like it might turn...",
            "That sky's not promising..."
        };

        /// <summary>What is said when the street has gone grey.</summary>
        private static readonly string[] FogLines =
        {
            "Can barely see the road...",
            "Where'd everything go...",
            "Fog's thick today...",
            "Can't see past the corner...",
            "Keep your eyes open in this...",
            "Whole street's gone grey...",
            "It's like soup out here...",
            "Can't see the end of the street...",
            "Fogged right in...",
            "You can hardly see your hand..."
        };

        /// <summary>Every line the mod can say, for recognising one of its own on the way back in.</summary>
        private static readonly HashSet<string> OwnLines =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Handed out when a mood has no lines of its own, so nothing has to null-check.</summary>
        private static readonly string[] EmptyLines = new string[0];

        /// <summary>The line each speaker was last given, and when.</summary>
        private static readonly Dictionary<int, Spoken> SpokenLines =
            new Dictionary<int, Spoken>();

        /// <summary>
        /// What was drawn in answer to a line the game asked for, keyed on the game's own words.
        ///
        /// This is what keeps a line from being reworded while it is on screen: the renderer is asked
        /// to draw "Cold as hell out here..." on every frame that it is up, so the same words have to
        /// give the same answer. Keyed by the game's words rather than by the speaker, because one
        /// answer for the whole town is what used to put the same line under everybody's name.
        /// </summary>
        private static readonly Dictionary<string, Spoken> Drawn =
            new Dictionary<string, Spoken>(StringComparer.Ordinal);

        /// <summary>The lines just said, oldest first, so the pool is worked through rather than picked from.</summary>
        private static readonly List<string> Recent =
            new List<string>();

        /// <summary>Lines already written to the log, so a per-frame draw call cannot fill it up.</summary>
        private static readonly HashSet<string> Diagnosed =
            new HashSet<string>(StringComparer.Ordinal);

        private static readonly System.Random Random = new System.Random(0x57435431);

        private sealed class Spoken
        {
            public string Line;
            public int Mood;
            public float Time;
        }

        private static bool _failureLogged;
        private static int _linesSpoken;
        private static int _lastMood;
        private static int _diagnostics = Diagnostics;

        /// <summary>The NPC whose greeting was most recently decided, for emptying its cache later.</summary>
        private static DialogueController _lastGreeted;

        static WvcWeatherChatter()
        {
            foreach (string line in RainLines)
                OwnLines.Add(line);

            foreach (string line in SnowLines)
                OwnLines.Add(line);

            foreach (string line in BlizzardLines)
                OwnLines.Add(line);

            foreach (string line in SunnyLines)
                OwnLines.Add(line);

            foreach (string line in OvercastLines)
                OwnLines.Add(line);

            foreach (string line in FogLines)
                OwnLines.Add(line);
        }

        /// <summary>How many lines the mod has put in an NPC's mouth this session.</summary>
        public static int LinesSpoken
        {
            get { return _linesSpoken; }
        }

        public static string Describe()
        {
            return "on=" + WvcSnowSettings.WeatherDialogue +
                   " spoken=" + _linesSpoken +
                   " mood=" + WvcSnowWeather.MoodName(WvcSnowWeather.WeatherMood()) +
                   " speakers=" + SpokenLines.Count;
        }

        /// <summary>Forgets who said what, for a new world.</summary>
        public static void Reset()
        {
            SpokenLines.Clear();
            Drawn.Clear();
            Recent.Clear();
            Diagnosed.Clear();
            _linesSpoken = 0;
            _lastMood = WvcSnowWeather.MoodClear;
            _diagnostics = Diagnostics;
        }

        /// <summary>
        /// Replaces the greeting the game decided on with the one the weather calls for. Called with
        /// the line the game was going to say; whatever is left in <paramref name="greeting"/> is what
        /// the player reads.
        ///
        /// A line of the mod's own coming back in is not a reason to do nothing: the game keeps the
        /// line it decided on per NPC and hands it back until its own timer runs out, so a line
        /// written once would otherwise be the line that person says every time.
        /// It is rewritten like any other, and the hold is what keeps it from changing while it is
        /// being read.
        /// </summary>
        public static void RewriteGreeting(DialogueController controller, ref string greeting)
        {
            try
            {
                if (!WvcSnowSettings.WeatherDialogue)
                    return;

                if (string.IsNullOrEmpty(greeting))
                    return;

                int mood = WvcSnowWeather.WeatherMood();

                bool ours = OwnLines.Contains(greeting);

                if (mood == WvcSnowWeather.MoodClear ||
                    mood == WvcSnowWeather.MoodRain)
                {
                    if (ours)
                    {
                        // Snow words in weather that is not snow: whoever just read them was the last
                        // of them. The cache is emptied so the game decides for itself next time,
                        // rather than keeping a snowstorm's line in the sun.
                        ForgetGreetingCache(controller, "the weather moved on");
                    }

                    return;
                }

                // A plain day is not worth a remark every time somebody says hello, so the game's own
                // greeting keeps most of them: it is only now and then that the weather is mentioned.
                // Every other sky is spoken for - the rain by the game, and the rest by this file.
                if (mood == WvcSnowWeather.MoodSunny && !ours && Random.Next(3) != 0)
                    return;

                _lastGreeted = controller;

                int speaker = controller != null ? controller.GetInstanceID() : 0;

                if (ReuseHeld(speaker, mood, GreetingHold, ref greeting))
                    return;

                string line = Pick(mood, speaker);

                if (string.IsNullOrEmpty(line))
                    return;

                Remember(speaker, line, mood);

                if (_diagnostics > 0)
                {
                    _diagnostics--;


                }

                greeting = line;

                _linesSpoken++;
                Announce(line, mood, "greeting");
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>
        /// The line that is actually going on screen. Whatever asked for it, what the player reads is
        /// decided here:
        ///
        /// - a line of the mod's own is left exactly as it is when it belongs to the weather that is
        ///   running: it is the line this person was given, and the game putting it under their name
        ///   again is not a reason to change their mind for them. The greeting hook is what gives
        ///   everybody a line of their own, one per conversation;
        /// - a line of the game's that talks about rain, shown while it is snowing, becomes a snow
        ///   line - this is the one that catches a greeting the game had cached from the rain, and a
        ///   line of the mod's own left over from other weather;
        /// - anything else - the game's time-of-day greetings, quest lines, messages - is left alone.
        ///
        /// What keeps a line steady while it is being read is the answer given to the game's own words:
        /// the renderer asks for the same line on every frame it is up, and gets the same answer back
        /// (<see cref="ShownHold"/>). A different line is a different question and gets its own answer,
        /// which is what lets the whole town be saying something different.
        /// </summary>
        public static void RewriteWorldspace(
            ref string text,
            bool stillOnScreen,
            string source)
        {
            try
            {
                if (!WvcSnowSettings.WeatherDialogue)
                    return;

                if (string.IsNullOrEmpty(text))
                    return;

                string seen = text;
                int mood = WvcSnowWeather.WeatherMood();

                bool ours = OwnLines.Contains(text);
                bool aboutRain = MentionsRain(text);

                // What is going to happen is worked out first and written once, so the log entry says
                // what was seen and what was done about it together.
                string line = null;
                string why;

                if (mood == WvcSnowWeather.MoodClear)
                {
                    why = "no weather to talk about";
                }
                else if (ours && InPool(mood, text))
                {
                    why = "the line they were given, for this weather";
                }
                else if (!ours && !aboutRain)
                {
                    why = "not a weather line";
                }
                else if (aboutRain &&
                         !ours &&
                         mood == WvcSnowWeather.MoodRain)
                {
                    why = "the game's own rain line, in rain";
                }
                else if (stillOnScreen)
                {
                    why = "already on screen";
                }
                else if (ReuseDrawn(seen, mood, ref line))
                {
                    why = "kept: the same line is still being drawn";
                }
                else
                {
                    line = Pick(mood, 0);

                    if (string.IsNullOrEmpty(line))
                    {
                        line = null;
                        why = "no line to say";
                    }
                    else
                    {
                        Remember(0, line, mood);
                        RememberDrawn(seen, line, mood);
                        _linesSpoken++;
                        why = "picked";

                        Announce(line, mood, source);
                    }
                }

                if (mood != WvcSnowWeather.MoodClear && _diagnostics > 0 && Diagnosed.Add(seen))
                {
                    // Every distinct line drawn while the weather is up, and what was done about it, so
                    // a line that got through can be named out of the log rather than guessed at. Every
                    // *distinct* line, because a line sits on screen for seconds and may be drawn on
                    // every frame of them. Bounded: this is here to work the feature out, not to run
                    // forever.
                    _diagnostics--;


                }

                if (line == null)
                    return;

                text = line;
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>True while the given text is a line short enough to be a greeting and mentions rain.</summary>
        private static bool MentionsRain(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > WeatherLineLength)
                return false;

            return text.IndexOf("rain", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Empties the greeting the game is holding for one NPC.
        ///
        /// <c>DialogueController.cachedGreeting</c> and <c>lastGreetingTime</c> are per NPC, and the
        /// game hands the same line back until its own timer runs out - which is why a person you
        /// spoke to keeps saying the same thing every time, however many lines the pool has in it.
        /// Emptying the cache is how the next conversation gets a line of its own.
        /// </summary>
        private static void ForgetGreetingCache(DialogueController controller, string why)
        {
            if (controller == null)
                return;

            try
            {
                string held = controller.cachedGreeting;

                if (string.IsNullOrEmpty(held))
                    return;

                controller.cachedGreeting = null;
                controller.lastGreetingTime = 0f;

                if (_diagnostics > 0)
                {
                    _diagnostics--;


                }
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.0");
        }

        /// <summary>
        /// A conversation has ended. The cache is emptied here rather than when the next one starts,
        /// so the line said on the way in and the line in the dialogue box stay the same line, and
        /// only the conversation after that gets a new one.
        /// </summary>
        public static void OnConversationEnded()
        {
            int mood = WvcSnowWeather.WeatherMood();

            if (mood != WvcSnowWeather.MoodSnow &&
                mood != WvcSnowWeather.MoodBlizzard)
            {
                return;
            }

            DialogueController controller = _lastGreeted;

            if (controller == null)
                return;

            // The line they were holding goes too, so the next greeting is picked fresh instead of
            // coming back with the same words a moment later.
            SpokenLines.Remove(controller.GetInstanceID());

            ForgetGreetingCache(controller, "the conversation ended");
        }

        /// <summary>
        /// What a dialogue is being started with: the container, and the word the game opens it at.
        /// If that word is a sentence, the line in the dialogue box is the greeting this file has
        /// already rewritten; if it is a label, the box is showing a node out of the NPC's own
        /// container, which no amount of rewriting the greeting can reach. Written to the log so the
        /// question can be answered from a session rather than guessed at.
        /// </summary>
        public static void StartDialogue_Prefix(string __0, bool __1, string __2)
        {
            try
            {
                if (_diagnostics <= 0)
                    return;

                _diagnostics--;


            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>
        /// The line this speaker was given recently, if the weather has not changed since: a line
        /// asked for twice in quick succession has to give the same answer, or it rewords itself
        /// while it is being read.
        /// </summary>
        private static bool ReuseHeld(
            int speaker,
            int mood,
            float hold,
            ref string text)
        {
            if (!SpokenLines.TryGetValue(speaker, out Spoken spoken))
                return false;

            if (spoken.Mood != mood || Time.unscaledTime - spoken.Time >= hold)
                return false;

            text = spoken.Line;
            return true;
        }

        /// <summary>
        /// The answer already given to the same line, while that line is still worth keeping steady.
        /// This is what holds a drawn line's words still: the renderer asks for it again on every frame
        /// it is up, and the same words have to come back. A different line was asked about under
        /// different circumstances and is answered on its own terms.
        /// </summary>
        private static bool ReuseDrawn(string seen, int mood, ref string text)
        {
            if (!Drawn.TryGetValue(seen, out Spoken spoken))
                return false;

            if (spoken.Mood != mood || Time.unscaledTime - spoken.Time >= ShownHold)
                return false;

            text = spoken.Line;
            return true;
        }

        private static void RememberDrawn(string seen, string line, int mood)
        {
            if (Drawn.Count > 256)
                Drawn.Clear();

            Drawn[seen] = new Spoken
            {
                Line = line,
                Mood = mood,
                Time = Time.unscaledTime
            };
        }

        /// <summary>True when the line is one of the mood's own, which is what says the weather it was
        /// written for.</summary>
        private static bool InPool(int mood, string line)
        {
            string[] pool = Pool(mood);

            for (int i = 0; i < pool.Length; i++)
            {
                if (string.Equals(pool[i], line, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void Remember(int speaker, string line, int mood)
        {
            if (SpokenLines.Count > 512)
                SpokenLines.Clear();

            SpokenLines[speaker] = new Spoken
            {
                Line = line,
                Mood = mood,
                Time = Time.unscaledTime
            };

            if (mood != _lastMood)
            {
                // New weather: nothing said under the old one is worth avoiding any more.
                Recent.Clear();
                _lastMood = mood;
            }

            Recent.Add(line);

            while (Recent.Count > RecentLines)
                Recent.RemoveAt(0);
        }

        /// <summary>
        /// One of the mood's lines, holding back the ones just said and the speaker's own last line.
        /// Without that the same line comes up over and over - the pool is small and the dice are
        /// independent - and the rest of the lines are never heard.
        /// </summary>
        private static string Pick(int mood, int speaker)
        {
            string[] pool = Pool(mood);

            if (pool.Length == 0)
                return null;

            int index = Random.Next(pool.Length);

            if (pool.Length > 1)
            {
                for (int attempt = 0; attempt < 6 && IsStale(pool[index], speaker); attempt++)
                    index = Random.Next(pool.Length);

                if (IsStale(pool[index], speaker))
                    index = (index + 1) % pool.Length;
            }

            return pool[index];
        }

        private static bool IsStale(string line, int speaker)
        {
            if (Recent.Contains(line))
                return true;

            return SpokenLines.TryGetValue(speaker, out Spoken spoken) &&
                   string.Equals(spoken.Line, line, StringComparison.Ordinal);
        }

        private static string[] Pool(int mood)
        {
            switch (mood)
            {
                case WvcSnowWeather.MoodBlizzard:
                    return BlizzardLines;

                case WvcSnowWeather.MoodSnow:
                    return SnowLines;

                case WvcSnowWeather.MoodRain:
                    return RainLines;

                case WvcSnowWeather.MoodOvercast:
                    return OvercastLines;

                case WvcSnowWeather.MoodFog:
                    return FogLines;

                case WvcSnowWeather.MoodSunny:
                    return SunnyLines;

                default:
                    return EmptyLines;
            }
        }

        private static void Announce(string line, int mood, string source)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg(
                LogPrefix + " '" + line + "' (" + source + ", " +
                WvcSnowWeather.MoodName(mood) + ").");

            if (_linesSpoken == 1)
            {
                // The first line of a session is worth a plain log entry: it is the only proof in the
                // log that the weather talk is reaching anybody.

            }
        }

        private static void Warn(Exception ex)
        {
            if (_failureLogged)
                return;

            _failureLogged = true;


        }

        // ---- The fallback path ---------------------------------------------------------------
        // Only used when nothing that draws a line could be hooked: the line is then said on top of
        // whatever the game decided to say, which is why it is not the first choice.

        public static void OnNpcHovered(DialogueController controller)
        {
            try
            {
                if (controller == null || !WvcSnowSettings.WeatherDialogue)
                    return;

                int mood = WvcSnowWeather.WeatherMood();

                if (mood == WvcSnowWeather.MoodClear)
                    return;

                DialogueHandler handler = controller.handler;

                if (handler == null || handler.IsDialogueInProgress)
                    return;

                int speaker = controller.GetInstanceID();

                if (SpokenLines.TryGetValue(speaker, out Spoken spoken) &&
                    spoken.Mood == mood &&
                    Time.unscaledTime - spoken.Time < GreetingHold)
                {
                    return;
                }

                string line = Pick(mood, speaker);

                if (string.IsNullOrEmpty(line))
                    return;

                Remember(speaker, line, mood);
                _linesSpoken++;

                handler.ShowWorldspaceDialogue(line, FallbackDuration);

                Announce(line, mood, "hover");
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        // ---- Hooks --------------------------------------------------------------------------

        /// <summary>The line the game settled on, about to be used. Rewritten in place.</summary>
        public static void Greeting_Postfix(DialogueController __instance, ref string __result)
        {
            RewriteGreeting(__instance, ref __result);
        }

        /// <summary>
        /// A line about to be drawn under an NPC, from wherever it came. This is the one that decides
        /// what the player reads, so it is the first hook the patcher asks for.
        /// </summary>
        public static void Renderer_Prefix(WorldspaceDialogueRenderer __instance, ref string __0)
        {
            bool onScreen =
                __instance != null &&
                __instance.IsVisible &&
                string.Equals(__instance.ShownText, __0, StringComparison.Ordinal);

            RewriteWorldspace(ref __0, onScreen, "drawn");
        }

        /// <summary>The same, one step earlier on the road a greeting travels. A fallback.</summary>
        public static void Worldspace_Prefix(ref string __0)
        {
            RewriteWorldspace(ref __0, false, "handler");
        }

        /// <summary>
        /// A line of dialogue on its way to the dialogue box. The same narrow rule as the drawn line,
        /// for the opposite reason: if the box is showing a node out of the NPC's own container rather
        /// than the greeting, the greeting hook cannot reach it, and a rain line in the box during a
        /// blizzard is exactly what this is here to stop.
        /// </summary>
        public static void DialogueText_Postfix(string __0, ref string __result)
        {
            RewriteWorldspace(ref __result, false, "dialogue box");
        }

        /// <summary>The player has walked up to somebody, if nothing else could be hooked.</summary>
        public static void Hovered_Postfix(DialogueController __instance)
        {
            OnNpcHovered(__instance);
        }

        /// <summary>A conversation has ended: the town's cached greeting is dropped, so the next
        /// conversation gets a line of its own. No instance is needed - the cache is static.</summary>
        public static void EndDialogue_Postfix()
        {
            OnConversationEnded();
        }
    }

    /// <summary>
    /// The weather talk's hooks, patched by hand rather than by attribute.
    ///
    /// Attribute patches are applied by the mod's one <c>PatchAll</c> call at start-up, and anything
    /// it cannot patch throws out of that call and takes the rest of the start-up with it: a hook on
    /// a method the game renamed would cost the whole mod. Here each method is looked up on its own
    /// and each patch is allowed to fail on its own.
    ///
    /// The ranking matters. The line the player actually reads is drawn by
    /// <c>WorldspaceDialogueRenderer.ShowText</c>, so that is the one always wanted; the greeting the
    /// game decided on comes next; and the two older hooks - the handler's draw call and the hover
    /// event - are asked for only when nothing above them could be taken, because a hook that says a
    /// second line on top of the first is worse than no hook at all.
    /// </summary>
    public static class WvcWeatherChatterPatches
    {
        private static bool _applied;

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            if (_applied || harmony == null)
                return;

            _applied = true;

            bool drawn = Patch(
                harmony,
                typeof(WorldspaceDialogueRenderer),
                "ShowText",
                new[] { typeof(string), typeof(float) },
                nameof(WvcWeatherChatter.Renderer_Prefix),
                true);

            Patch(
                harmony,
                typeof(DialogueController),
                "GetActiveGreeting",
                null,
                nameof(WvcWeatherChatter.Greeting_Postfix),
                false);

            if (!drawn)
            {
                Patch(
                    harmony,
                    typeof(DialogueHandler),
                    "ShowWorldspaceDialogue",
                    new[] { typeof(string), typeof(float) },
                    nameof(WvcWeatherChatter.Worldspace_Prefix),
                    true);
            }

            Patch(
                harmony,
                typeof(DialogueController),
                "Hovered",
                null,
                nameof(WvcWeatherChatter.Hovered_Postfix),
                false);

            // The game keeps one greeting for the whole town (its cache and timer are static), so a
            // conversation is what empties it: without this, the second conversation repeats the
            // first one's line, and so does everybody else's.
            Patch(
                harmony,
                typeof(DialogueHandler),
                "EndDialogue",
                null,
                nameof(WvcWeatherChatter.EndDialogue_Postfix),
                false);

            // Only a diagnostic: it says what the dialogue was started with, which is what decides
            // whether the line in the dialogue box can be reached at all.
            Patch(
                harmony,
                typeof(DialogueHandler),
                "StartDialogue",
                new[] { typeof(string), typeof(bool), typeof(string) },
                nameof(WvcWeatherChatter.StartDialogue_Prefix),
                true);

            // The dialogue box itself, for a box that is showing a node of the NPC's own container
            // rather than the greeting: the same rain rule, applied to the text on its way there.
            Patch(
                harmony,
                typeof(DialogueHandler),
                "ModifyDialogueText",
                new[] { typeof(string), typeof(string) },
                nameof(WvcWeatherChatter.DialogueText_Postfix),
                false);
        }

        private static bool Patch(
            HarmonyLib.Harmony harmony,
            Type owner,
            string methodName,
            Type[] parameters,
            string hookName,
            bool asPrefix)
        {
            try
            {
                MethodInfo original = parameters == null
                    ? AccessTools.Method(owner, methodName)
                    : AccessTools.Method(owner, methodName, parameters);

                if (original == null)
                {


                    return false;
                }

                var hook = new HarmonyMethod(typeof(WvcWeatherChatter), hookName);

                harmony.Patch(
                    original,
                    asPrefix ? hook : null,
                    asPrefix ? null : hook);



                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }
    }
}
