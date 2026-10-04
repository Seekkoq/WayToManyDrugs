using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

using GameSkyState = Il2CppScheduleOne.Core.Weather.SkyState;
using Il2CppScheduleOne.Core;
using Il2CppScheduleOne.Core.Weather;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Weather;

namespace CustomNPCExample.Weather
{
    /// <summary>
    /// The mod's snow weather.
    ///
    /// The game has no snow of its own - there is no snow profile, no snow sky and no snow greeting
    /// anywhere in its data - so snow is built here: a weather profile made at runtime and added to
    /// the environment's own list, the game's own weather machinery pointed at it, and the flakes,
    /// the fog and the sky drawn by the mod on top.
    ///
    /// What it is *not* is a copy of the game's rain. That was the first way it was built, and it was
    /// wrong: the game decides what the weather is from the profile's conditions, so a snowstorm made
    /// out of a rain profile was read as rain by everything that looks - the greetings people gave
    /// ("Gotta get out of this rain..." in the middle of a blizzard), the rain sound, the wet look on
    /// every surface, the umbrellas. Snow is now built on the game's overcast profile with
    /// <c>Rainy = 0</c> and <c>Snowy = 1</c>, its rain settings switched off, and no thunder borrowed
    /// from the rain: cloudy, snowy and windy, and nothing else.
    /// </summary>
    public static class WvcSnowWeather
    {
        public const string SnowProfileId = "WVC_Snow";

        private const int SnowFlakeCount = 850;
        private const float SnowAreaRadius = 22f;
        private const float SnowSpawnHeight = 12f;
        private const float KillBelowPlayer = 5f;

        // The storm variant. More flakes, a wider and taller volume, and a hard wind that throws
        // them down a steep diagonal. StormFlakeCount is only a ceiling: the adaptive governor in
        // RunFlakeBudget walks the simulated and drawn count down whenever the frame time says the
        // machine cannot carry it, so the cost of the effect stays bounded either way.
        private const int StormFlakeCount = 2600;
        private const float StormAreaRadius = 30f;
        private const float StormSpawnHeight = 20f;
        private const float StormKillBelowPlayer = 6f;

        // Ground wind, in metres per second, and the bearing it blows towards in world space.
        private const float StormWindSpeed = 7.5f;
        private const float StormWindBearing = 37f;

        // A storm spends its frame budget on flakes instead of on collision: no shelter checks and
        // no per-flake ground raycast, because at three times the flake count that is what would
        // actually cost the frames.
        private const bool StormCollision = false;

        /// <summary>
        /// The haze a mode sits in. The snow sky is built from the same two colours, so the fog on
        /// the ground and the fog in the sky cannot drift apart.
        /// </summary>
        internal static Color HazeFor(bool storm)
        {
            return WvcSnowSky.Haze(storm);
        }

        // Storm flakes are pure white and individually fainter: with thousands of them stacked
        // against a whiteout, a strong per-flake alpha would read as static rather than as weather.
        private static readonly Color StormFlakeColor =
            new Color(1f, 1f, 1f, 1f);

        private static Vector2 StormWindDirection
        {
            get
            {
                float radians =
                    StormWindBearing * Mathf.Deg2Rad;

                return new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
            }
        }

        /// <summary>
        /// Two detuned slow waves added on top of the steady wind, so gusts swell and fade instead
        /// of pulsing on a metronome. Returns roughly -1..1.
        /// </summary>
        private static float StormGustWave(float time)
        {
            return Mathf.Sin(time * 0.19f) * 0.62f +
                   Mathf.Sin(time * 0.073f + 1.7f) * 0.38f;
        }

        private static readonly Color SnowFlakeColor =
            new Color(0.96f, 0.98f, 1f, 0.92f);

        private static readonly System.Random Random =
            new System.Random(0x534E4F57);

        private const int MaxRaycastsPerFrame = 80;
        private const int ShelterChecksPerFrame = 32;
        private const float ShelterCheckHeight = 35f;

        private static int _raycastBudget;
        private static int _shelterCursor;
        private static int _collisionMask = 0;

        /// <summary>
        /// Whether the mod puts weather on screen by itself.
        ///
        /// The setting owns the value - the config file and the row in the game's settings screen
        /// both write there - and this is the name the rest of this file asks it by.
        /// </summary>
        public static bool AutoScheduleEnabled
        {
            get { return WvcSnowSettings.ScheduleEnabled; }
            set { WvcSnowSettings.ScheduleEnabled = value; }
        }

        private const int SnowDaysPerWeek = 4;

        private static readonly string[] DayNames =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        private static readonly List<int> SnowDaysThisWeek =
            new List<int>();

        /// <summary>
        /// Which of the week's snow days the schedule runs as blizzards. Two a week by default:
        /// often enough to be part of a snowy week's shape, rare enough that a blizzard still
        /// reads as an event rather than as the normal weather.
        /// </summary>
        private static readonly List<int> StormDaysThisWeek =
            new List<int>();

        private static int _scheduledWeekAnchor = int.MinValue;
        private static int _lastElapsedDays = int.MinValue;
        private static int _manualOverrideDay = int.MinValue;

        private static bool _forceSnowNextDay;
        private static bool _todayIsSnowDay;
        private static bool _todayIsStormDay;
        private static int _snowStartMinute;
        private static int _snowEndMinute;

        private static bool _snowActive;
        private static bool _stormMode;

        /// <summary>
        /// Snow falling as thick as a blizzard without any of a blizzard's whiteout: the flake count
        /// only. Requested with <c>setweather heavysnow</c>.
        /// </summary>
        private static bool _heavyMode;
        private static bool _profileReady;
        private static string _previousWeatherId;

        private static WeatherProfile _snowProfile;
        private static SkySettings _snowSky;

        private static int _environmentInstanceId;
        private static EnvironmentManager _environmentCache;
        private static TimeManager _timeManagerCache;
        private static float _timeManagerResolveTimer = 1f;
        private static float _profileRetryTimer;
        private static float _rainSuppressionTimer;

        // How often the world-painting writes - the profile's values, the fog, the sky and the
        // game's own weather selection - are repeated while snow is up. Every one of them crosses
        // into native code and most of them dirty the weather and lighting systems, which the frame
        // rate pays for and the player cannot see: the values only differ when the mode changes.
        // Nothing between two paints needs repainting, so half a second is a free half second.
        private const float PaintInterval = 0.5f;

        // The rain renderers are cheap to hold off but expensive to search for (a
        // GetComponentsInChildren walk over every active weather volume), so the search runs when
        // the volume set changes and the holding-off runs on this timer.
        private const float RainSuppressionInterval = 2f;

        private static float _paintTimer;
        private static bool _paintNow;
        private static Camera _cameraCache;
        private static float _cameraResolveTimer;
        private static int _rainScanVolumeCount = -1;
        private static bool _settingsHooked;

        /// <summary>Guards the single "what is the snow system actually doing" report per session.</summary>
        private static bool _stateReported;
        private static float _stateReportTimer;

        private static bool _fogCaptured;
        private static bool _originalFog;
        private static FogMode _originalFogMode;
        private static Color _originalFogColor;
        private static float _originalFogDensity;
        private static float _originalFogStart;
        private static float _originalFogEnd;

        private static AmbientMode _originalAmbientMode;
        private static Color _originalAmbientLight;
        private static float _originalAmbientIntensity;
        private static float _originalReflectionIntensity;

        private sealed class SnowFlake
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Size;
            public float Phase;
            public float Alpha;

            public float KillY;
        }

        private static GameObject _snowObject;
        private static Mesh _snowMesh;
        private static MeshFilter _snowMeshFilter;
        private static MeshRenderer _snowRenderer;
        private static Material _snowMaterial;
        private static Texture2D _snowTexture;

        private static SnowFlake[] _flakes;
        private static Vector3[] _vertices;
        private static Vector2[] _uv;
        private static Color[] _colours;
        private static int[] _triangles;
        private static int _allocatedFlakes;
        private static bool _coloursDirty;

        // Adaptive quality: _activeFlakes is what the simulation and the mesh are allowed to use
        // this frame, somewhere between the governor floor and the mode's cap. _drawnFlakes is the
        // count the mesh index buffer is currently cut to.
        private static int _activeFlakes;
        private static int _drawnFlakes;
        private static float _frameAverage = 1f / 60f;
        private static float _governorTimer;
        private static int _governorDrops;

        private sealed class NativeRendererState
        {
            public Renderer Renderer;
            public bool WasEnabled;
        }

        private static readonly List<NativeRendererState>
            NativeRainRenderers =
                new List<NativeRendererState>();

        private static readonly HashSet<int>
            NativeRainRendererIds =
                new HashSet<int>();

        private static object _rainRendererFeature;
        private static object _rendererData;

        private static bool _rainFeatureStateCaptured;
        private static bool _rainFeatureWasActive;
        private static bool _rainFeatureSuppressed;
        private static bool _rainSuppressionLogged;
        private static bool _rainSuppressionFailedLogged;
        private static int _rainRenderersDisabled;

        public static void Update()
        {
            EnvironmentManager environment =
                GetEnvironment();

            CheckForNewEnvironment(environment);
            HandleKeys();
            HookSettings();

            if (environment == null)
                return;

            _profileRetryTimer += Time.deltaTime;

            if (!_profileReady &&
                _profileRetryTimer >= 2f)
            {
                _profileRetryTimer = 0f;
                TryEnsureProfiles();
            }

            UpdateSchedule();

            // One report per session on its own, so a run where no snow ever appears leaves behind
            // enough to say why without anyone having to type anything.
            _stateReportTimer += Time.deltaTime;

            if (!_stateReported &&
                (_profileReady || _stateReportTimer >= 30f))
            {
                _stateReported = true;

                ReportState(
                    _profileReady
                        ? "world ready"
                        : "profiles still missing after 30s"
                );
            }

            if (!_snowActive)
                return;

            if (_snowProfile == null)
            {
                DisableWeather();
                return;
            }

            // Everything that paints the world - the profile's values, the fog, the sky and the
            // game's own weather selection - is written on a timer rather than every frame. Each of
            // those writes crosses into native code and most of them dirty the weather and lighting
            // systems, which the frame rate pays for and the player cannot see: the values only
            // differ when the mode changes. Whatever changes the mode sets _paintNow, so a switch is
            // still instant.
            _paintTimer += Time.deltaTime;

            if (_paintNow || _paintTimer >= PaintInterval)
            {
                _paintNow = false;
                _paintTimer = 0f;

                EnsureWeatherStillSelected(environment, _snowProfile);
                KeepProfileConfigured(_snowProfile);
                ApplyFogForSnow();
            }

            if (EnsureSnowOverlay())
                UpdateSnowOverlay();

            _rainSuppressionTimer += Time.deltaTime;

            if (_rainSuppressionTimer >= RainSuppressionInterval)
            {
                _rainSuppressionTimer = 0f;
                SuppressNativeRainVisuals();
            }
        }

        private static void UpdateSchedule()
        {
            if (!AutoScheduleEnabled)
                return;

            TimeManager time =
                GetTimeManager();

            if (time == null)
                return;

            int elapsedDays;
            int dayOfWeek;
            int minuteOfDay;

            if (!TryReadClock(
                    time,
                    out elapsedDays,
                    out dayOfWeek,
                    out minuteOfDay))
            {
                return;
            }

            if (elapsedDays != _lastElapsedDays)
            {
                _lastElapsedDays = elapsedDays;

                OnDayChanged(
                    elapsedDays,
                    dayOfWeek
                );
            }

            if (!AutoScheduleEnabled)
            {
                // The schedule is off, so whatever it started has to go. Snow the player asked for
                // by hand stays: that is the difference between turning the schedule off and turning
                // the weather off, and the manual override is exactly the mark that tells them apart.
                if (_snowActive && _manualOverrideDay != elapsedDays)
                {

                    DisableWeather();
                }

                return;
            }

            if (_manualOverrideDay == elapsedDays)
                return;

            bool wantSnow =
                _todayIsSnowDay &&
                minuteOfDay >= _snowStartMinute &&
                minuteOfDay < _snowEndMinute;

            if (wantSnow && !_snowActive)
            {
                bool storm = _todayIsStormDay;

                // Logged at Always: this fires once when a snow window opens, so it is throttled
                // by definition and it is the line you actually want in a default log.

                EnableSnow(storm, false);
                return;
            }

            if (!wantSnow && _snowActive)
            {

                DisableWeather();
            }
        }

        private static void OnDayChanged(
            int elapsedDays,
            int dayOfWeek)
        {
            int weekAnchor =
                elapsedDays - dayOfWeek;

            if (weekAnchor != _scheduledWeekAnchor)
                RollWeekSchedule(weekAnchor);

            bool forced = _forceSnowNextDay;
            _forceSnowNextDay = false;

            _manualOverrideDay = int.MinValue;

            _todayIsSnowDay =
                forced ||
                SnowDaysThisWeek.Contains(dayOfWeek);

            // A forced day is snow asked for by hand, so it counts as a storm day only when the
            // schedule itself picked that day for one.
            _todayIsStormDay =
                _todayIsSnowDay &&
                StormDaysThisWeek.Contains(dayOfWeek);

            if (!_todayIsSnowDay)
            {

                return;
            }

            RollTodayWindow(
                elapsedDays,
                forced
            );

        }

        private static void RollWeekSchedule(int weekAnchor)
        {
            _scheduledWeekAnchor = weekAnchor;
            SnowDaysThisWeek.Clear();

            System.Random rng =
                new System.Random(
                    unchecked(weekAnchor * 7919 + 104729)
                );

            // A snow week is a single block of days, not a scatter. Four scattered days read as
            // random noise and never let the ground stay white; a run of days in a row feels like
            // a cold snap moving through, and the snow profile was tuned against a cold snap.
            int wanted =
                Mathf.Clamp(SnowDaysPerWeek, 0, 7);

            int start =
                wanted >= 7
                    ? 0
                    : rng.Next(7);

            for (int i = 0; i < wanted; i++)
                SnowDaysThisWeek.Add((start + i) % 7);

            SnowDaysThisWeek.Sort();


            RollStormDays(rng);
        }

        /// <summary>
        /// Picks which of the week's snow days run as blizzards.
        ///
        /// Evenly spread rather than scattered: the snow days are already a block, so a storm every
        /// other one reads as the week having a shape. The count is a setting (two by default) and a
        /// week with fewer snow days than storms simply gets a storm on every snow day.
        /// </summary>
        private static void RollStormDays(System.Random rng)
        {
            StormDaysThisWeek.Clear();

            int snowDays = SnowDaysThisWeek.Count;

            if (snowDays == 0)
                return;

            int wanted =
                Mathf.Clamp(
                    WvcSnowSettings.BlizzardsPerWeek,
                    0,
                    snowDays);

            if (wanted <= 0)
            {

                return;
            }

            if (wanted >= snowDays)
            {
                for (int i = 0; i < snowDays; i++)
                    StormDaysThisWeek.Add(SnowDaysThisWeek[i]);
            }
            else
            {
                // The stride steps through the block, and the offset - from the same week seed the
                // days came from - keeps successive weeks from always storming on the same weekday.
                int offset = rng.Next(snowDays);

                for (int i = 0; i < wanted; i++)
                {
                    int day =
                        SnowDaysThisWeek[
                            (offset + i * snowDays / wanted) % snowDays];

                    if (!StormDaysThisWeek.Contains(day))
                        StormDaysThisWeek.Add(day);
                }
            }

            StormDaysThisWeek.Sort();

        }

        private static void RollTodayWindow(
            int elapsedDays,
            bool forced)
        {
            if (forced)
            {
                _snowStartMinute = 6 * 60;
                _snowEndMinute = 21 * 60;
                return;
            }

            System.Random rng =
                new System.Random(
                    unchecked(elapsedDays * 6151 + 55711)
                );

            _snowStartMinute =
                rng.Next(5 * 60, 18 * 60);

            int duration =
                rng.Next(120, 361);

            _snowEndMinute =
                Mathf.Min(
                    _snowStartMinute + duration,
                    23 * 60 + 30
                );
        }

        public static void ForceSnowNextDay()
        {
            _forceSnowNextDay = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snow] Snow forced for the next in-game day."
            );
        }

        /// <summary>
        /// Starts listening to the settings, once.
        ///
        /// The settings are the config file and the row in the game's settings screen, and both can
        /// change while the game is running, so the effect has to be told rather than only read at
        /// startup: a row clicked in the settings screen lands here on the next frame.
        /// </summary>
        private static void HookSettings()
        {
            if (_settingsHooked)
                return;

            _settingsHooked = true;

            try
            {
                WvcSnowSettings.Changed += OnSettingsChanged;
            }
            catch (Exception)
            {

            }
        }

        private static void OnSettingsChanged()
        {
            // Repaint at once: the switch may have been the schedule being turned off, which the
            // schedule path acts on this frame, and the paint is what makes the haze match.
            _paintNow = true;

            // A flake count only matters when the geometry is built, and the geometry is built once
            // per mode, so a change while snow is up rebuilds the overlay instead of waiting for the
            // next time snow is turned on.
            if (_snowActive && _allocatedFlakes != WantedFlakeCount())
            {
                RebuildSnowOverlay();
                ResetFlakeGovernor();
            }


        }

        private static int WantedFlakeCount()
        {
            // A heavy fall is the blizzard's fall rate without the blizzard: the one thing it shares
            // with a storm is how much snow is in the air.
            if (_stormMode || _heavyMode)
                return WvcSnowSettings.StormFlakes;

            return WvcSnowSettings.SnowFlakes;
        }

        private static bool TryReadClock(
            TimeManager time,
            out int elapsedDays,
            out int dayOfWeek,
            out int minuteOfDay)
        {
            elapsedDays = 0;
            dayOfWeek = 0;
            minuteOfDay = 0;

            try
            {
                elapsedDays = time.ElapsedDays;

                int raw = time.CurrentTime;

                int hours = raw / 100;
                int minutes = raw % 100;

                minuteOfDay =
                    Mathf.Clamp(
                        hours * 60 + minutes,
                        0,
                        1439
                    );
            }
            catch
            {
                return false;
            }

            try
            {
                dayOfWeek = ((int)time.CurrentDay) % 7;
            }
            catch
            {
                dayOfWeek = elapsedDays % 7;
            }

            if (dayOfWeek < 0)
                dayOfWeek += 7;

            return true;
        }

        private static TimeManager GetTimeManager()
        {
            // The resolution is a generic NetworkSingleton lookup, which showed up in the frame
            // profiler as a recurring winter hit even when snow was off - so it is cached and
            // refreshed a couple of times a second rather than every frame.
            _timeManagerResolveTimer -= Time.deltaTime;

            if (_timeManagerResolveTimer > 0f && _timeManagerCache != null)
                return _timeManagerCache;

            _timeManagerResolveTimer = 0.5f;

            try
            {
                _timeManagerCache = NetworkSingleton<
                    TimeManager
                >.Instance;
            }
            catch
            {
                _timeManagerCache = null;
            }

            return _timeManagerCache;
        }

        private static string DayName(int index)
        {
            if (index < 0 || index >= DayNames.Length)
                return "Day " + index;

            return DayNames[index];
        }

        private static string DescribeStormDays()
        {
            if (StormDaysThisWeek.Count == 0)
                return "none";

            string text = "";

            for (int i = 0; i < StormDaysThisWeek.Count; i++)
            {
                if (i > 0)
                    text += ", ";

                text += DayName(StormDaysThisWeek[i]);
            }

            return text;
        }

        private static string DescribeSnowDays()
        {
            if (SnowDaysThisWeek.Count == 0)
                return "none";

            string text = "";

            for (int i = 0; i < SnowDaysThisWeek.Count; i++)
            {
                if (i > 0)
                    text += ", ";

                text += DayName(SnowDaysThisWeek[i]);
            }

            return text;
        }

        private static string FormatMinutes(int minutes)
        {
            int hour = minutes / 60;
            int minute = minutes % 60;

            return hour.ToString("00") +
                   ":" +
                   minute.ToString("00");
        }

        public static bool EnableSnow()
        {
            return EnableSnow(false, false);
        }

        /// <summary>
        /// The blizzard variant: the same weather profile and the same world-space overlay, driven
        /// by the storm branch of the flake simulation and a much thicker haze. See the Storm*
        /// constants for what a storm changes.
        /// </summary>
        public static bool EnableSnowstorm()
        {
            return EnableSnow(true, false);
        }

        /// <summary>
        /// Snow coming down as thickly as it does in a blizzard, under the calm sky: a heavy fall
        /// that is still snow to anybody standing in it, so the people on the street go on talking
        /// about snow rather than sheltering from a whiteout. The flake count is the only thing a
        /// heavy fall changes - see <see cref="WantedFlakeCount"/>.
        /// </summary>
        public static bool EnableHeavySnow()
        {
            return EnableSnow(false, true);
        }

        /// <summary>
        /// True while the storm variant is the thing on screen.
        /// </summary>
        public static bool IsSnowstorm
        {
            get
            {
                return _snowActive && _stormMode;
            }
        }

        /// <summary>True while the mod's own snow is the weather on screen.</summary>
        public static bool IsSnowActive
        {
            get
            {
                return _snowActive;
            }
        }

        /// <summary>True while the heavy fall is the snow that is running, rather than a blizzard.</summary>
        public static bool IsHeavySnow
        {
            get
            {
                return _snowActive && _heavyMode && !_stormMode;
            }
        }

        /// <summary>What the snow is doing, for the log and for the console.</summary>
        public static string SnowModeWord()
        {
            if (_stormMode)
                return "blizzard";

            return _heavyMode ? "heavy snow" : "snow";
        }

        /// <summary>Nothing worth remarking on - or nothing known about the weather at all.</summary>
        public const int MoodClear = 0;

        /// <summary>The game's own rain, and anything the profile calls rainy.</summary>
        public const int MoodRain = 1;

        public const int MoodSnow = 2;
        public const int MoodBlizzard = 3;

        /// <summary>A flat grey sky with the sun somewhere behind it.</summary>
        public const int MoodOvercast = 4;

        /// <summary>Fog thick enough to be worth a word.</summary>
        public const int MoodFog = 5;

        /// <summary>Sun out and nothing in the sky to speak of.</summary>
        public const int MoodSunny = 6;

        /// <summary>
        /// What the weather is doing, as the rest of the mod talks about it: the mod's own snow and
        /// blizzards first, because those are known rather than measured, then the game's own
        /// conditions - so a snowstorm the mod did not start is still snow to the people standing in
        /// it, and rain is rain whether it came from the console or the game's own weather.
        /// </summary>
        public static int WeatherMood()
        {
            if (_snowActive)
                return _stormMode ? MoodBlizzard : MoodSnow;

            WeatherConditions conditions = GetConditions();

            if (conditions != null)
            {
                if (conditions.Snowy >= 0.5f)
                    return MoodBlizzard;

                if (conditions.Snowy >= 0.25f)
                    return MoodSnow;

                if (conditions.Rainy >= 0.35f)
                    return MoodRain;

                if (conditions.Foggy >= 0.45f)
                    return MoodFog;

                if (conditions.Cloudy >= 0.45f)
                    return MoodOvercast;
            }

            // The conditions are not always filled in - a profile that has only just been selected has
            // nothing blended into them yet - so the weather the game is running is asked as well, by
            // name. Without this a grey afternoon reads as a clear one, and the people on the street
            // talk about the sunshine.
            string profile = CurrentProfileWord();

            if (profile.Length > 0)
            {
                if (profile.Contains("fog"))
                    return MoodFog;

                if (profile.Contains("overcast") || profile.Contains("cloud"))
                    return MoodOvercast;

                if (profile.Contains("rain") || profile.Contains("storm"))
                    return MoodRain;
            }

            if (conditions == null && profile.Length == 0)
                return MoodClear;

            return MoodSunny;
        }

        /// <summary>
        /// The weather the game is running, lower case, for telling one sky from another when the
        /// blended conditions cannot.
        /// </summary>
        private static string CurrentProfileWord()
        {
            try
            {
                WeatherVolume target =
                    GetEnvironment()?._targetWeatherVolume;

                WeatherProfile profile =
                    target?.WeatherProfile;

                if (profile == null)
                    return string.Empty;

                return ((profile.Id ?? string.Empty) + " " + (profile.name ?? string.Empty))
                    .ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string MoodName(int mood)
        {
            switch (mood)
            {
                case MoodRain:
                    return "rain";

                case MoodSnow:
                    return "snow";

                case MoodBlizzard:
                    return "blizzard";

                case MoodOvercast:
                    return "overcast";

                case MoodFog:
                    return "fog";

                case MoodSunny:
                    return "sunny";

                default:
                    return "clear";
            }
        }

        /// <summary>
        /// The conditions the weather is running under, blended, with the profile the volume is
        /// wearing as the fallback for the first frame after a change.
        /// </summary>
        private static WeatherConditions GetConditions()
        {
            try
            {
                EnvironmentManager environment = GetEnvironment();

                if (environment == null)
                    return null;

                WeatherConditions conditions =
                    environment._currentWeatherConditions;

                if (conditions != null)
                    return conditions;

                return environment._targetWeatherVolume != null &&
                       environment._targetWeatherVolume.WeatherProfile != null
                    ? environment._targetWeatherVolume.WeatherProfile.Conditions
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Marks the weather as set by hand for the rest of the in-game day.
        ///
        /// The snow schedule runs from the clock and switches the snow off again the moment the day's
        /// window says it should not be snowing. That is right for its own snow, but it also means
        /// anything that sets the weather by hand has to say so, or its work is undone a frame later.
        /// The hotkeys do this; the console command has to as well.
        /// </summary>
        public static void MarkManualWeather()
        {
            MarkManualOverride();
        }

        /// <summary>
        /// Writes the whole state of the snow system to the log.
        ///
        /// Almost everything this system can get wrong stays silent: a profile never added to the
        /// environment, an overlay never built, the schedule quietly switching the snow back off.
        /// This is the one place that says what is true right now - <c>setweather diag</c> calls it,
        /// and the world reports it once by itself.
        /// </summary>
        public static void ReportState(string reason)
        {
            try
            {
                EnvironmentManager environment =
                    GetEnvironment();

















                ReportSky(environment);


            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The sky, read back from the game rather than from what was asked for: the values the live
        /// sky state is holding, the conditions the weather is running under, and whether the profile
        /// the volume is wearing is carrying the cold sky. This is the report that says whether the
        /// snow's sky actually reached the game.
        /// </summary>
        public static void ReportSky()
        {
            ReportSky(GetEnvironment());
        }

        private static void ReportSky(EnvironmentManager environment)
        {
            try
            {


                if (environment == null)
                {

                    return;
                }

                GameSkyState state = null;

                try
                {
                    state = environment._currentSkyState;
                }
                catch { }

                if (state == null)
                {

                    return;
                }



                WeatherConditions conditions =
                    GetConditions();


            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// What the snow profile's gradients evaluate to now. Snow's own palette is recognisable in
        /// the numbers - a flat fog density, a grey sky - so this says whether the profile is
        /// carrying it, whatever the live state happens to hold.
        /// </summary>
        private static string DescribeSkyGradients(WeatherProfile profile)
        {
            try
            {
                SkySettings sky =
                    profile != null ? profile.SkySettings : null;

                if (sky == null)
                    return "no profile sky";

                DynamicGradient fog = sky.FogDensityGradient;
                DynamicGradient upper = sky.SkyUpperGradient;
                DynamicGradient clouds = sky.CloudDensityGradient;

                return "fogDensity@" +
                       (fog != null ? ColourOf(fog.Evaluate(0.5f)) : "?") +
                       " upper@" + (upper != null ? ColourOf(upper.Evaluate(0.5f)) : "?") +
                       " clouds@" + (clouds != null ? ColourOf(clouds.Evaluate(0.5f)) : "?") +
                       " (" + WvcSnowSky.Describe() + ")";
            }
            catch (Exception ex)
            {
                return "unreadable: " + ex.Message;
            }
        }

        private static string ColourOf(Color colour)
        {
            return "(" +
                   colour.r.ToString("0.00") + "," +
                   colour.g.ToString("0.00") + "," +
                   colour.b.ToString("0.00") + ")";
        }

        private static string DescribeProfile(WeatherProfile profile)
        {
            try
            {
                if (profile == null)
                    return "null";

                return "'" + profile.Id + "'/" + profile.name;
            }
            catch
            {
                return "?";
            }
        }

        private static string DescribeCurrentWeather(EnvironmentManager environment)
        {
            try
            {
                WeatherVolume target =
                    environment?._targetWeatherVolume;

                return DescribeProfile(target?.WeatherProfile);
            }
            catch
            {
                return "?";
            }
        }

        private static string DescribeClock()
        {
            try
            {
                TimeManager time =
                    GetTimeManager();

                if (time == null)
                    return "no clock";

                int elapsedDays;
                int dayOfWeek;
                int minuteOfDay;

                if (!TryReadClock(
                        time,
                        out elapsedDays,
                        out dayOfWeek,
                        out minuteOfDay))
                {
                    return "unreadable";
                }

                return DayName(dayOfWeek) +
                       " day " + elapsedDays +
                       " " + FormatMinutes(minuteOfDay);
            }
            catch
            {
                return "?";
            }
        }

        private static bool EnableSnow(bool storm, bool heavy)
        {
            if (_snowActive)
            {
                // Already snowing: a storm or a heavy fall upgrades the effect that is already
                // running rather than starting it again.
                if (storm)
                    SetStorm(true);

                if (heavy)
                    SetHeavy(true);



                return true;
            }



            if (!TryEnsureProfiles())
            {


                return false;
            }

            EnvironmentManager environment =
                GetEnvironment();

            if (environment == null ||
                _snowProfile == null)
            {


                return false;
            }

            try
            {
                RememberCurrentWeather(environment);

                _snowActive = true;
                _stormMode = storm;
                _heavyMode = heavy;
                _rainSuppressionTimer = 1f;
                _paintNow = true;
                _rainScanVolumeCount = -1;

                ResetFlakeGovernor();

                // The profile carries the sky, so it is told which mode is running before the game is
                // told to use it: starting straight in a blizzard would otherwise open on the calm
                // palette and only turn white on the next mode change.
                ConfigureProfile(environment, _snowProfile);

                environment.SetWeather(_snowProfile.Id);

                if (environment._targetWeatherVolume != null)
                {
                    environment._targetWeatherVolume
                        ._weatherProfile = _snowProfile;
                }

                RebuildSnowOverlay();
                KeepProfileConfigured(_snowProfile);
                ApplyFogForSnow();
                SuppressNativeRainVisuals();



                return true;
            }
            catch (Exception ex)
            {
                _snowActive = false;
                _stormMode = false;
                _heavyMode = false;

                RestoreFog();
                RestoreNativeRainVisuals();
                DestroySnowOverlay();

                MelonLogger.Error(
                    "[WVC Snow] Enable failed: " +
                    ex.Message
                );

                return false;
            }
        }

        public static void ToggleSnow()
        {
            MarkManualOverride();

            if (_snowActive)
                DisableWeather();
            else
                EnableSnow();
        }

        public static void ToggleSnowstorm()
        {
            MarkManualOverride();

            if (_snowActive && _stormMode)
                DisableWeather();
            else
                EnableSnowstorm();
        }

        /// <summary>
        /// Switches a running effect between calm snow and a storm. Only the overlay and the haze
        /// move; the weather profile the game was put into stays where it was put.
        /// </summary>
        public static void SetStorm(bool storm)
        {
            if (_stormMode == storm)
                return;

            _stormMode = storm;

            if (!_snowActive)
                return;

            _paintNow = true;

            // The sky is part of the profile, so a mode change means rebuilding it: the profile is
            // handed the blizzard's cold, closed-in sky instead of the snow one. The weather is then
            // re-selected, because a volume that has already been initialised keeps the settings it
            // was initialised with until the game is told about the new ones.
            if (_snowProfile != null)
            {
                ConfigureProfile(GetEnvironment(), _snowProfile);
                ReselectWeather();
            }

            RebuildSnowOverlay();
            ResetFlakeGovernor();
            ApplyFogForSnow();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                storm
                    ? "[WVC Snow] Switched to snowstorm."
                    : "[WVC Snow] Switched back to snow."
            );
        }

        /// <summary>
        /// Switches a running effect between the ordinary fall and a heavy one. A heavy fall is the
        /// flake count of a blizzard under the calm sky, so only the overlay is rebuilt: the profile
        /// the game was put into, the haze and the palette all stay where they were.
        /// </summary>
        public static void SetHeavy(bool heavy)
        {
            if (_heavyMode == heavy)
                return;

            _heavyMode = heavy;

            if (!_snowActive)
                return;

            _paintNow = true;

            RebuildSnowOverlay();
            ResetFlakeGovernor();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                heavy
                    ? "[WVC Snow] Switched to a heavy fall."
                    : "[WVC Snow] Switched back to an ordinary fall."
            );
        }

        // ---- Adaptive flake budget ---------------------------------------------------------
        //
        // The frame time is averaged every frame and the budget is nudged twice a second: a storm
        // starts at its cap and hands flakes back first if the machine cannot carry them, so the
        // effect loses density instead of losing frames.

        private const float BudgetInterval = 0.5f;
        private const float SlowFrame = 0.0205f;
        private const float FastFrame = 0.0135f;
        private const int BudgetFloor = 220;

        private static void ResetFlakeGovernor()
        {
            _frameAverage = 1f / 60f;
            _governorTimer = 0f;
            _activeFlakes = _allocatedFlakes;
            _drawnFlakes = -1;
        }

        private static void RunFlakeGovernor()
        {
            _frameAverage +=
                (Time.unscaledDeltaTime - _frameAverage) * 0.08f;

            _governorTimer += Time.unscaledDeltaTime;

            if (_governorTimer < BudgetInterval)
                return;

            _governorTimer = 0f;

            int cap = _allocatedFlakes;

            if (cap <= 0)
                return;

            int floor = Mathf.Max(BudgetFloor, cap / 3);
            int step = Mathf.Max(96, cap / 6);

            if (_frameAverage > SlowFrame)
            {
                if (_activeFlakes <= floor)
                    return;

                _activeFlakes = Mathf.Max(floor, _activeFlakes - step);

                if (_governorDrops++ % 6 == 0)
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snow] Frame budget down to " +
                        _activeFlakes +
                        " of " + cap +
                        " flakes (" +
                        Mathf.RoundToInt(
                            1f / Mathf.Max(0.0001f, _frameAverage)) +
                        " fps).");
            }
            else if (_frameAverage < FastFrame &&
                     _activeFlakes < cap)
            {
                _activeFlakes = Mathf.Min(cap, _activeFlakes + step);
            }
        }

        /// <summary>
        /// Cuts the overlay's index buffer down to the number of flakes the budget allows. Flakes
        /// that come back when the budget grows are re-seeded rather than left wherever the last
        /// gust put them, and the mesh stops at the budget, so everything past it is neither drawn
        /// nor simulated.
        /// </summary>
        private static void ApplyFlakeBudget(Vector3 origin)
        {
            if (_snowMesh == null ||
                _triangles == null ||
                _allocatedFlakes <= 0)
            {
                return;
            }

            int wanted =
                Mathf.Clamp(
                    _activeFlakes,
                    0,
                    _allocatedFlakes);

            if (wanted == _drawnFlakes)
                return;

            if (wanted > _drawnFlakes &&
                _flakes != null)
            {
                int from =
                    Mathf.Max(0, _drawnFlakes);

                for (int i = from; i < wanted; i++)
                {
                    SnowFlake flake = _flakes[i];

                    if (flake != null)
                        ResetFlake(i, flake, origin, false);
                }
            }

            _drawnFlakes = wanted;

            try
            {
                if (wanted >= _allocatedFlakes)
                {
                    _snowMesh.triangles = _triangles;
                }
                else
                {
                    int[] trimmed = new int[wanted * 6];

                    Array.Copy(
                        _triangles,
                        trimmed,
                        trimmed.Length);

                    _snowMesh.triangles = trimmed;
                }
            }
            catch { }
        }

        public static bool DisableWeather()
        {
            EnvironmentManager environment =
                GetEnvironment();

            try
            {
                bool wasActive = _snowActive;
                _snowActive = false;
                _stormMode = false;
                _heavyMode = false;

                RestoreNativeRainVisuals();
                DestroySnowOverlay();
                RestoreFog();

                if (environment != null && wasActive)
                {
                    string restoreId =
                        _previousWeatherId;

                    if (string.IsNullOrEmpty(restoreId))
                        restoreId =
                            FindClearProfileId(environment);

                    if (string.IsNullOrEmpty(restoreId))
                        restoreId = "Clear";

                    WeatherProfile restoreProfile =
                        FindProfile(
                            environment,
                            restoreId,
                            true
                        );

                    environment.SetWeather(restoreId);

                    if (restoreProfile != null &&
                        environment._targetWeatherVolume != null)
                    {
                        environment._targetWeatherVolume
                            ._weatherProfile = restoreProfile;
                    }


                }

                _previousWeatherId = null;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snow] Disable failed: " +
                    ex.Message
                );

                return false;
            }
        }

        public static bool SetNativeWeather(string weatherId)
        {
            try
            {
                if (string.IsNullOrEmpty(weatherId))
                    return false;

                EnvironmentManager environment = GetEnvironment();
                if (environment == null)
                    return false;

                if (_snowActive)
                    DisableWeather();

                environment.SetWeather(weatherId);



                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snow] SetNativeWeather failed: " + ex.Message);
                return false;
            }
        }

        public static void Reset()
        {
            _snowActive = false;

            RestoreNativeRainVisuals();
            DestroySnowOverlay();
            RestoreFog();

            _profileReady = false;
            _snowProfile = null;
            _previousWeatherId = null;

            _environmentInstanceId = 0;
            _environmentCache = null;
            _timeManagerCache = null;
            _timeManagerResolveTimer = 1f;
            _profileRetryTimer = 0f;
            _rainSuppressionTimer = 0f;

            SnowDaysThisWeek.Clear();
            _scheduledWeekAnchor = int.MinValue;
            _lastElapsedDays = int.MinValue;
            _manualOverrideDay = int.MinValue;
            _forceSnowNextDay = false;
            _todayIsSnowDay = false;
            _collisionMask = 0;

            _snowSky = null;
            WvcSnowSky.Reset();
            WvcWeatherChatter.Reset();
        }

        private static void MarkManualOverride()
        {
            TimeManager time =
                GetTimeManager();

            if (time == null)
                return;

            try
            {
                _manualOverrideDay = time.ElapsedDays;
            }
            catch { }
        }

        private static bool TryEnsureProfiles()
        {
            EnvironmentManager environment =
                GetEnvironment();

            if (environment == null ||
                environment._weatherProfiles == null)
            {
                return false;
            }

            WeatherProfile snow;

            _profileReady =
                EnsureOneProfile(
                    environment,
                    SnowProfileId,
                    out snow
                );

            if (_profileReady)
                _snowProfile = snow;

            return _profileReady;
        }

        private static bool EnsureOneProfile(
            EnvironmentManager environment,
            string id,
            out WeatherProfile result)
        {
            result =
                FindProfile(
                    environment,
                    id,
                    true
                );

            if (result != null)
            {
                ConfigureProfile(
                    environment,
                    result
                );

                return true;
            }

            // Snow is built from a profile that has no rain in it, not from the game's rain: the
            // game decides what the weather *is* from these settings, and a snowstorm made out of a
            // rain profile is read as rain by everything that looks - the greetings, the sound, the
            // wet shaders, the people reaching for umbrellas. Overcast is the closest thing the game
            // has to weather that is simply grey, and it carries the sky snow is drawn under.
            WeatherProfile donor =
                FindProfile(
                    environment,
                    "Overcast",
                    true
                ) ??
                FindProfile(
                    environment,
                    "Foggy",
                    true
                ) ??
                FindProfile(
                    environment,
                    "Clear",
                    true
                );

            WeatherProfile rainDonor = null;

            if (donor == null)
            {
                // Nothing dry to build on: a rain profile is better than no snow at all, and the
                // rain in it is switched off below.
                rainDonor =
                    FindProfile(
                        environment,
                        "HeavyRain",
                        true
                    ) ??
                    FindProfile(
                        environment,
                        "LightRain",
                        true
                    );

                donor = rainDonor;
            }

            if (donor == null)
            {


                return false;
            }

            WeatherProfile skyDonor =
                FindProfile(
                    environment,
                    "Overcast",
                    true
                ) ??
                FindProfile(
                    environment,
                    "Foggy",
                    true
                ) ??
                FindProfile(
                    environment,
                    "Clear",
                    true
                );

            WeatherProfile clone =
                UnityEngine.Object.Instantiate(
                    donor
                );

            if (clone == null)
                return false;

            clone.name = id;
            clone._id = id;

            if (skyDonor != null)
            {
                clone._skySettings =
                    skyDonor._skySettings;

                clone._cloudSettings =
                    skyDonor._cloudSettings;
            }

            ConfigureProfile(
                environment,
                clone
            );

            environment._weatherProfiles.Add(clone);

            result = clone;



            return true;
        }

        private static void ConfigureProfile(
            EnvironmentManager environment,
            WeatherProfile profile)
        {
            if (profile == null)
                return;

            WeatherProfile overcast =
                FindProfile(
                    environment,
                    "Overcast",
                    true
                );

            WeatherProfile foggy =
                FindProfile(
                    environment,
                    "Foggy",
                    true
                );

            WeatherProfile skySource =
                overcast ?? foggy;

            // The profile's own sky settings are the copy the game reads back every frame, so this
            // is where snow's sky has to be decided: a cold, foggy one built from the overcast
            // profile's shape. Sharing the donor's object, which is what this did first, cannot make
            // snow look like anything but the donor's weather.
            SkySettings donorSky =
                skySource != null ? skySource._skySettings : null;

            SkySettings snowSky =
                WvcSnowSky.Build(
                    donorSky,
                    _stormMode,
                    skySource == null
                        ? "none"
                        : (skySource.Id ?? skySource.name));

            if (snowSky != null)
            {
                profile._skySettings = snowSky;
                _snowSky = snowSky;
            }
            else if (donorSky != null)
            {
                profile._skySettings = donorSky;
            }

            if (skySource != null)
            {
                profile._cloudSettings =
                    skySource._cloudSettings;
            }

            // The thunder settings are deliberately not borrowed from the rain: a snowstorm with
            // lightning strikes in it is a different kind of weather, and the dry profile snow is
            // built on carries none.

            KeepProfileConfigured(profile);
        }

        /// <summary>
        /// The values that make the profile snow rather than anything else. Written again on every
        /// paint, because the game blends profiles and writes the same fields itself.
        ///
        /// The one that matters most is <c>Rainy = 0</c>. Snow used to be a copy of the game's heavy
        /// rain with the rain left switched on, which is why the game still treated it as rain - it
        /// picked rain greetings, played rain sound and put the umbrellas up. Nothing about this
        /// weather is rain now: it is cloudy, snowy and windy, and the rain settings it may have
        /// inherited from a donor are switched off.
        /// </summary>
        private static void KeepProfileConfigured(
            WeatherProfile profile)
        {
            if (profile == null)
                return;

            bool storm = _stormMode;

            WeatherConditions conditions =
                profile._conditions;

            if (conditions != null)
            {
                conditions.Sunny = 0f;
                conditions.Cloudy = storm ? 1f : 0.9f;

                // Not rain, in any amount: this is what tells the game what the weather is.
                conditions.Rainy = 0f;

                conditions.Stormy = storm ? 0.35f : 0.1f;
                conditions.Snowy = 1f;
                conditions.Foggy = storm ? 0.6f : 0.25f;
                conditions.Windy = storm ? 0.9f : 0.35f;

                conditions.Hail = 0f;
                conditions.Sleet = 0f;
            }

            RainSettings rain =
                profile._rainSettings;

            if (rain != null)
            {
                // A donor that was a rain profile brings its rain with it: particles, sound and the
                // wet look on everything. Snow draws its own flakes and wants none of it.
                rain.IsActive = false;
            }
        }

        /// <summary>
        /// Hands the game the snow profile again, so the weather volume picks up settings that have
        /// changed since it was put there - which is what the storm's sky is. The profile reference is
        /// written straight onto the volume as well, because <c>SetWeather</c> alone leaves the volume
        /// that is already showing the weather where it was.
        /// </summary>
        private static void ReselectWeather()
        {
            try
            {
                EnvironmentManager environment =
                    GetEnvironment();

                if (environment == null || _snowProfile == null)
                    return;

                environment.SetWeather(_snowProfile.Id);

                if (environment._targetWeatherVolume != null)
                {
                    environment._targetWeatherVolume._weatherProfile =
                        _snowProfile;
                }
            }
            catch (Exception)
            {

            }
        }

        private static void EnsureWeatherStillSelected(
            EnvironmentManager environment,
            WeatherProfile profile)
        {
            try
            {
                WeatherVolume target =
                    environment._targetWeatherVolume;

                if (target == null ||
                    target.WeatherProfile == null ||
                    profile == null)
                {
                    return;
                }

                string currentId =
                    target.WeatherProfile.Id;

                if (!string.Equals(
                        currentId,
                        profile.Id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    environment.SetWeather(profile.Id);
                }
            }
            catch
            {
            }
        }

        private static WeatherProfile FindProfile(
            EnvironmentManager environment,
            string wantedId,
            bool exact)
        {
            if (environment == null ||
                environment._weatherProfiles == null ||
                string.IsNullOrEmpty(wantedId))
            {
                return null;
            }

            var profiles =
                environment._weatherProfiles;

            for (int i = 0; i < profiles.Count; i++)
            {
                WeatherProfile profile =
                    profiles[i];

                if (profile == null)
                    continue;

                string id = profile.Id;

                if (string.IsNullOrEmpty(id))
                    id = profile.name;

                if (string.IsNullOrEmpty(id))
                    continue;

                if (id.IndexOf(
                        "BloodMoon",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    continue;
                }

                bool matches =
                    exact
                        ? string.Equals(
                            id,
                            wantedId,
                            StringComparison.OrdinalIgnoreCase
                        )
                        : id.IndexOf(
                            wantedId,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                if (matches)
                    return profile;
            }

            return null;
        }

        private static string FindClearProfileId(
            EnvironmentManager environment)
        {
            WeatherProfile clear =
                FindProfile(
                    environment,
                    "Clear",
                    true
                );

            if (clear != null)
                return clear.Id;

            var profiles =
                environment._weatherProfiles;

            if (profiles == null)
                return null;

            for (int i = 0; i < profiles.Count; i++)
            {
                WeatherProfile profile =
                    profiles[i];

                if (profile == null)
                    continue;

                string id = profile.Id;

                if (string.IsNullOrEmpty(id) ||
                    string.Equals(
                        id,
                        SnowProfileId,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    id.IndexOf(
                        "BloodMoon",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    continue;
                }

                WeatherConditions conditions =
                    profile.Conditions;

                if (conditions != null &&
                    conditions.Sunny >= 0.5f)
                {
                    return id;
                }
            }

            return null;
        }

        private static void RememberCurrentWeather(
            EnvironmentManager environment)
        {
            try
            {
                WeatherVolume target =
                    environment._targetWeatherVolume;

                if (target == null ||
                    target.WeatherProfile == null)
                {
                    return;
                }

                string id =
                    target.WeatherProfile.Id;

                if (string.IsNullOrEmpty(id) ||
                    string.Equals(
                        id,
                        SnowProfileId,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    id.IndexOf(
                        "BloodMoon",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return;
                }

                _previousWeatherId = id;
            }
            catch { }
        }

        private static void ApplyFogForSnow()
        {
            CaptureFogIfNeeded();

            Color haze =
                HazeFor(_stormMode);

            try
            {
                RenderSettings.fog = true;

                RenderSettings.fogMode =
                    FogMode.ExponentialSquared;

                RenderSettings.fogColor = haze;

                // A storm is a whiteout rather than a haze: the same exponential-squared falloff,
                // an order of magnitude thicker, which is also what lets the flake count stop being
                // visible - nothing past forty metres needs to be drawn to read as a blizzard.
                if (_stormMode)
                {
                    RenderSettings.fogDensity = 0.032f;
                    RenderSettings.fogStartDistance = 3f;
                    RenderSettings.fogEndDistance = 45f;
                }
                else
                {
                    RenderSettings.fogDensity = 0.011f;
                    RenderSettings.fogStartDistance = 18f;
                    RenderSettings.fogEndDistance = 100f;
                }

                RenderSettings.ambientMode =
                    AmbientMode.Flat;

                RenderSettings.ambientLight = haze;
                RenderSettings.ambientIntensity =
                    _stormMode ? 0.62f : 0.85f;
                RenderSettings.reflectionIntensity = 0.35f;
            }
            catch { }

            ApplyGameSkyState();
        }

        private static void ApplyGameSkyState()
        {
            EnvironmentManager environment =
                GetEnvironment();

            if (environment == null)
                return;

            GameSkyState sky = null;

            try
            {
                sky = environment._currentSkyState;
            }
            catch { }

            if (sky == null)
                return;

            // The same palette the profile was given, so the frame the mode changes is already the
            // new sky rather than the last one.
            WvcSnowSky.PaintLive(sky, _stormMode);
        }

        private static void CaptureFogIfNeeded()
        {
            if (_fogCaptured)
                return;

            try
            {
                _originalFog = RenderSettings.fog;
                _originalFogMode = RenderSettings.fogMode;
                _originalFogColor = RenderSettings.fogColor;
                _originalFogDensity = RenderSettings.fogDensity;
                _originalFogStart = RenderSettings.fogStartDistance;
                _originalFogEnd = RenderSettings.fogEndDistance;

                _originalAmbientMode = RenderSettings.ambientMode;
                _originalAmbientLight = RenderSettings.ambientLight;
                _originalAmbientIntensity = RenderSettings.ambientIntensity;
                _originalReflectionIntensity = RenderSettings.reflectionIntensity;

                _fogCaptured = true;
            }
            catch { }
        }

        private static void RestoreFog()
        {
            if (!_fogCaptured)
                return;

            try
            {
                RenderSettings.fog = _originalFog;
                RenderSettings.fogMode = _originalFogMode;
                RenderSettings.fogColor = _originalFogColor;
                RenderSettings.fogDensity = _originalFogDensity;
                RenderSettings.fogStartDistance = _originalFogStart;
                RenderSettings.fogEndDistance = _originalFogEnd;

                RenderSettings.ambientMode = _originalAmbientMode;
                RenderSettings.ambientLight = _originalAmbientLight;
                RenderSettings.ambientIntensity = _originalAmbientIntensity;
                RenderSettings.reflectionIntensity = _originalReflectionIntensity;
            }
            catch { }

            _fogCaptured = false;
        }

        private static void RebuildSnowOverlay()
        {
            DestroySnowOverlay();
            EnsureSnowOverlay();
        }

        private static bool EnsureSnowOverlay()
        {
            if (_snowObject != null &&
                _snowMesh != null &&
                _snowRenderer != null)
            {
                return true;
            }

            try
            {
                Shader shader =
                    Shader.Find("Sprites/Default");

                if (shader == null)
                {
                    shader =
                        Shader.Find(
                            "Universal Render Pipeline/Unlit"
                        );
                }

                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");

                if (shader == null)
                {
                    MelonLogger.Error(
                        "[WVC Snow] Transparent shader not found."
                    );

                    return false;
                }

                _snowTexture = CreateSnowTexture();

                _snowMaterial = new Material(shader);
                _snowMaterial.name = "WVC_Snow_Material";
                _snowMaterial.color = Color.white;

                if (_snowTexture != null)
                {
                    _snowMaterial.SetTexture("_MainTex", _snowTexture);
                    _snowMaterial.SetTexture("_BaseMap", _snowTexture);
                }

                _snowMaterial.SetFloat("_Surface", 1f);
                _snowMaterial.SetFloat("_Blend", 0f);
                _snowMaterial.SetFloat("_ZWrite", 0f);
                _snowMaterial.SetFloat("_Cull", 0f);
                _snowMaterial.SetInt("_SrcBlend", 5);
                _snowMaterial.SetInt("_DstBlend", 10);

                _snowMaterial.EnableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT"
                );

                _snowMaterial.renderQueue = 3000;

                _snowObject =
                    new GameObject("WVC_Snow_World");

                UnityEngine.Object.DontDestroyOnLoad(
                    _snowObject
                );

                _snowMeshFilter =
                    _snowObject.AddComponent<MeshFilter>();

                _snowRenderer =
                    _snowObject.AddComponent<MeshRenderer>();

                _snowMesh = new Mesh();
                _snowMesh.name = "WVC_Snow_Mesh";
                _snowMesh.MarkDynamic();

                CreateSnowGeometry();

                _snowMeshFilter.sharedMesh = _snowMesh;
                _snowRenderer.sharedMaterial = _snowMaterial;

                _snowRenderer.receiveShadows = false;

                _snowRenderer.shadowCastingMode =
                    ShadowCastingMode.Off;

                _snowRenderer.sortingOrder = 100;
                _snowRenderer.enabled = true;



                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snow] Overlay creation failed: " +
                    ex.Message
                );

                DestroySnowOverlay();
                return false;
            }
        }

        private static void CreateSnowGeometry()
        {
            Vector3 origin = GetAnchorPosition();

            int count = WantedFlakeCount();

            _allocatedFlakes = count;
            _activeFlakes = count;
            _drawnFlakes = -1;

            _flakes = new SnowFlake[count];
            _vertices = new Vector3[count * 4];
            _uv = new Vector2[count * 4];
            _colours = new Color[count * 4];
            _triangles = new int[count * 6];

            _raycastBudget = count + 64;

            for (int i = 0; i < count; i++)
            {
                SnowFlake flake = new SnowFlake();

                _flakes[i] = flake;

                int vertex = i * 4;
                int triangle = i * 6;

                _uv[vertex] = new Vector2(0f, 0f);
                _uv[vertex + 1] = new Vector2(1f, 0f);
                _uv[vertex + 2] = new Vector2(1f, 1f);
                _uv[vertex + 3] = new Vector2(0f, 1f);

                _triangles[triangle] = vertex;
                _triangles[triangle + 1] = vertex + 2;
                _triangles[triangle + 2] = vertex + 1;
                _triangles[triangle + 3] = vertex;
                _triangles[triangle + 4] = vertex + 3;
                _triangles[triangle + 5] = vertex + 2;

                ResetFlake(
                    i,
                    flake,
                    origin,
                    true
                );
            }

            _snowMesh.Clear();
            _snowMesh.vertices = _vertices;
            _snowMesh.uv = _uv;
            _snowMesh.colors = _colours;
            _snowMesh.triangles = _triangles;

            _coloursDirty = false;

            _snowMesh.bounds =
                new Bounds(
                    Vector3.zero,
                    new Vector3(140f, 140f, 140f)
                );
        }

        private static void ResetFlake(
            int index,
            SnowFlake flake,
            Vector3 origin,
            bool fromTop)
        {
            bool storm = _stormMode;

            float areaRadius =
                storm ? StormAreaRadius : SnowAreaRadius;

            float spawnHeight =
                storm ? StormSpawnHeight : SnowSpawnHeight;

            float killBelow =
                storm
                    ? StormKillBelowPlayer
                    : KillBelowPlayer;

            Vector2 wind = StormWindDirection;

            float angle =
                NextRange(0f, Mathf.PI * 2f);

            float distance =
                Mathf.Sqrt(NextRange(0f, 1f)) *
                areaRadius;

            // A storm seeds its volume upwind of the player, so the wind spends the flake's whole
            // fall crossing the view instead of dropping straight past it.
            float drift =
                storm ? areaRadius * 0.45f : 0f;

            float x =
                origin.x +
                Mathf.Cos(angle) * distance -
                wind.x * drift;

            float z =
                origin.z +
                Mathf.Sin(angle) * distance -
                wind.y * drift;

            float topY =
                origin.y +
                spawnHeight +
                NextRange(0f, 4f);

            float killY =
                origin.y - killBelow;

            float surfaceY;

            // The ground snap is what makes snow pile up on porches; it costs a raycast per flake,
            // so a storm trades it for three times the flakes.
            if (storm
                    ? StormCollision
                    : true)
            {
                if (TryRaycastDown(
                        new Vector3(x, topY, z),
                        spawnHeight + killBelow + 8f,
                        out surfaceY))
                {
                    killY = surfaceY + 0.03f;
                }
            }

            flake.KillY = killY;

            float spread =
                storm ? 10f : 16f;

            float y =
                fromTop
                    ? topY + NextRange(0f, spread)
                    : Mathf.Lerp(
                        killY + 0.3f,
                        topY,
                        NextRange(0f, 1f)
                      );

            flake.Position = new Vector3(x, y, z);

            if (storm)
            {
                // Streaks come down a steep diagonal: the wind sets the horizontal run, gravity the
                // fall, and a per-flake gust so the sheet never moves as one solid block.
                float gust =
                    NextRange(0.62f, 1.38f);

                flake.Velocity =
                    new Vector3(
                        wind.x * StormWindSpeed * gust +
                        NextRange(-1.1f, 1.1f),
                        NextRange(-9.5f, -5.0f),
                        wind.y * StormWindSpeed * gust +
                        NextRange(-1.1f, 1.1f)
                    );

                flake.Size = NextRange(0.035f, 0.17f);
                flake.Alpha = NextRange(0.30f, 0.80f);
            }
            else
            {
                flake.Velocity =
                    new Vector3(
                        NextRange(-0.55f, 0.55f),
                        NextRange(-2.4f, -0.7f),
                        NextRange(-0.55f, 0.55f)
                    );

                flake.Size = NextRange(0.04f, 0.14f);
                flake.Alpha = NextRange(0.55f, 0.95f);
            }

            flake.Phase = NextRange(0f, Mathf.PI * 2f);

            if (_colours != null && index >= 0)
            {
                int vertex = index * 4;

                Color tint =
                    storm
                        ? StormFlakeColor
                        : SnowFlakeColor;

                Color color =
                    new Color(
                        tint.r,
                        tint.g,
                        tint.b,
                        flake.Alpha
                    );

                _colours[vertex] = color;
                _colours[vertex + 1] = color;
                _colours[vertex + 2] = color;
                _colours[vertex + 3] = color;

                _coloursDirty = true;
            }
        }

        private static void UpdateSnowOverlay()
        {
            if (!_snowActive ||
                _snowMesh == null ||
                _flakes == null ||
                _snowObject == null)
            {
                return;
            }

            bool storm = _stormMode;

            _raycastBudget =
                storm
                    ? MaxRaycastsPerFrame / 2
                    : MaxRaycastsPerFrame;

            Vector3 origin = GetAnchorPosition();

            RunFlakeGovernor();
            ApplyFlakeBudget(origin);

            int count =
                Mathf.Clamp(
                    _drawnFlakes,
                    0,
                    _allocatedFlakes);

            if (count <= 0)
                return;

            Camera camera = GetMainCamera();

            _snowObject.transform.position = origin;
            _snowObject.transform.rotation = Quaternion.identity;

            if (_snowRenderer != null)
                _snowRenderer.enabled = true;

            Vector3 right =
                camera != null
                    ? camera.transform.right
                    : Vector3.right;

            Vector3 up = Vector3.up;

            float deltaTime =
                Mathf.Min(Time.deltaTime, 0.05f);

            float time = Time.time;

            Vector2 wind = StormWindDirection;

            // The steady wind lives inside each flake's velocity; this is the slow surge riding on
            // top of it, so the whole sheet leans, eases off and leans again.
            Vector3 surge =
                storm
                    ? new Vector3(
                        wind.x,
                        0f,
                        wind.y) *
                      (StormGustWave(time) * 5.5f)
                    : Vector3.zero;

            float recycleDistance =
                (storm
                    ? StormAreaRadius
                    : SnowAreaRadius) + 8f;

            float recycleDistanceSqr =
                recycleDistance * recycleDistance;

            if (!storm)
                RunShelterChecks(origin);

            for (int i = 0; i < count; i++)
            {
                SnowFlake flake = _flakes[i];

                flake.Position +=
                    (flake.Velocity + surge) * deltaTime;

                float sway =
                    storm
                        ? Mathf.Sin(
                            time * 2.3f + flake.Phase) * 1.35f +
                          Mathf.Sin(
                            time * 5.1f + flake.Phase * 0.5f) * 0.55f
                        : Mathf.Sin(
                            time * 0.7f + flake.Phase) * 0.12f;

                flake.Position.x += sway * deltaTime;

                if (storm)
                    flake.Position.z += sway * 0.6f * deltaTime;

                Vector3 difference =
                    flake.Position - origin;

                float horizontalDistanceSqr =
                    difference.x * difference.x +
                    difference.z * difference.z;

                if (flake.Position.y <= flake.KillY)
                {
                    ResetFlake(
                        i,
                        flake,
                        origin,
                        true
                    );
                }
                else if (horizontalDistanceSqr > recycleDistanceSqr)
                {
                    ResetFlake(
                        i,
                        flake,
                        origin,
                        false
                    );
                }

                Vector3 localPosition =
                    flake.Position - origin;

                Vector3 horizontal = right * flake.Size;
                Vector3 vertical = up * flake.Size;

                int vertex = i * 4;

                _vertices[vertex] =
                    localPosition - horizontal - vertical;

                _vertices[vertex + 1] =
                    localPosition + horizontal - vertical;

                _vertices[vertex + 2] =
                    localPosition + horizontal + vertical;

                _vertices[vertex + 3] =
                    localPosition - horizontal + vertical;
            }

            _snowMesh.vertices = _vertices;

            if (_coloursDirty)
            {
                _snowMesh.colors = _colours;
                _coloursDirty = false;
            }

            // The bounds are set once when the geometry is built and never change: they are the
            // fixed 140 metre box the whole effect lives inside, and writing them again each frame
            // was only ever costing a call.
        }

        private static void RunShelterChecks(Vector3 origin)
        {
            // Only the flakes inside the frame budget are checked; anything past it is neither
            // drawn nor simulated, so asking whether it is indoors would be wasted work.
            int live =
                _drawnFlakes > 0 && _drawnFlakes < _allocatedFlakes
                    ? _drawnFlakes
                    : _allocatedFlakes;

            if (live <= 0)
                return;

            int checks =
                Mathf.Min(
                    ShelterChecksPerFrame,
                    live
                );

            for (int i = 0; i < checks; i++)
            {
                _shelterCursor++;

                if (_shelterCursor >= live)
                    _shelterCursor = 0;

                SnowFlake flake =
                    _flakes[_shelterCursor];

                if (flake == null)
                    continue;

                if (!IsSheltered(flake.Position))
                    continue;

                ResetFlake(
                    _shelterCursor,
                    flake,
                    origin,
                    true
                );
            }
        }

        private static bool IsSheltered(Vector3 position)
        {
            if (_raycastBudget <= 0)
                return false;

            _raycastBudget--;

            try
            {
                RaycastHit hit;

                return Physics.Raycast(
                    position,
                    Vector3.up,
                    out hit,
                    ShelterCheckHeight,
                    GetCollisionMask(),
                    QueryTriggerInteraction.Ignore
                );
            }
            catch
            {
                return false;
            }
        }

        private static bool TryRaycastDown(
            Vector3 from,
            float distance,
            out float surfaceY)
        {
            surfaceY = 0f;

            if (_raycastBudget <= 0)
                return false;

            _raycastBudget--;

            try
            {
                RaycastHit hit;

                if (Physics.Raycast(
                        from,
                        Vector3.down,
                        out hit,
                        distance,
                        GetCollisionMask(),
                        QueryTriggerInteraction.Ignore))
                {
                    surfaceY = hit.point.y;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static int GetCollisionMask()
        {
            if (_collisionMask != 0)
                return _collisionMask;

            int mask = ~0;

            mask = ExcludeLayer(mask, "Ignore Raycast");
            mask = ExcludeLayer(mask, "Player");
            mask = ExcludeLayer(mask, "NPC");
            mask = ExcludeLayer(mask, "Trigger");
            mask = ExcludeLayer(mask, "Water");

            _collisionMask = mask;
            return _collisionMask;
        }

        private static int ExcludeLayer(
            int mask,
            string layerName)
        {
            int layer =
                LayerMask.NameToLayer(layerName);

            if (layer < 0)
                return mask;

            return mask & ~(1 << layer);
        }

        private static Vector3 GetAnchorPosition()
        {
            try
            {
                var player =
                    Il2CppScheduleOne.PlayerScripts
                        .Player.Local;

                if (player != null &&
                    player.transform != null)
                {
                    return player.transform.position;
                }
            }
            catch { }

            Camera camera = GetMainCamera();

            if (camera != null)
                return camera.transform.position;

            return Vector3.zero;
        }

        /// <summary>
        /// The camera, cached for a second.
        ///
        /// <c>Camera.main</c> looks the tagged object up in the scene rather than remembering it,
        /// and the snow frame asks for it more than once, so the answer is held briefly. A second is
        /// long enough to pay for itself and short enough that a camera swap is never noticed.
        /// </summary>
        private static Camera GetMainCamera()
        {
            _cameraResolveTimer -= Time.deltaTime;

            if (_cameraResolveTimer > 0f && _cameraCache != null)
                return _cameraCache;

            _cameraResolveTimer = 1f;

            try
            {
                _cameraCache = Camera.main;
            }
            catch
            {
                _cameraCache = null;
            }

            return _cameraCache;
        }

        private static Texture2D CreateSnowTexture()
        {
            const int size = 32;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name = "WVC_Snowflake_Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx =
                        ((x + 0.5f) / size) * 2f - 1f;

                    float ny =
                        ((y + 0.5f) / size) * 2f - 1f;

                    float distance =
                        Mathf.Sqrt(nx * nx + ny * ny);

                    float radial =
                        Mathf.Clamp01(1f - distance);

                    radial *= radial;

                    float horizontal =
                        Mathf.Exp(-Mathf.Abs(ny) * 14f) *
                        Mathf.Clamp01(1f - distance);

                    float vertical =
                        Mathf.Exp(-Mathf.Abs(nx) * 14f) *
                        Mathf.Clamp01(1f - distance);

                    float diagonalA =
                        Mathf.Exp(-Mathf.Abs(nx - ny) * 10f) *
                        Mathf.Clamp01(1f - distance);

                    float diagonalB =
                        Mathf.Exp(-Mathf.Abs(nx + ny) * 10f) *
                        Mathf.Clamp01(1f - distance);

                    float arms =
                        Mathf.Max(
                            Mathf.Max(horizontal, vertical),
                            Mathf.Max(diagonalA, diagonalB)
                        ) * 0.65f;

                    float alpha =
                        distance > 1f
                            ? 0f
                            : Mathf.Max(radial, arms);

                    texture.SetPixel(
                        x,
                        y,
                        new Color(1f, 1f, 1f, alpha)
                    );
                }
            }

            texture.Apply();
            return texture;
        }

        private static void DestroySnowOverlay()
        {
            try
            {
                if (_snowObject != null)
                    UnityEngine.Object.Destroy(_snowObject);

                if (_snowMesh != null)
                    UnityEngine.Object.Destroy(_snowMesh);

                if (_snowMaterial != null)
                    UnityEngine.Object.Destroy(_snowMaterial);

                if (_snowTexture != null)
                    UnityEngine.Object.Destroy(_snowTexture);
            }
            catch { }

            _snowObject = null;
            _snowMesh = null;
            _snowMeshFilter = null;
            _snowRenderer = null;
            _snowMaterial = null;
            _snowTexture = null;

            _flakes = null;
            _vertices = null;
            _uv = null;
            _colours = null;
            _triangles = null;
            _allocatedFlakes = 0;
            _activeFlakes = 0;
            _drawnFlakes = -1;
            _shelterCursor = 0;
            _coloursDirty = false;
        }

        private static void SuppressNativeRainVisuals()
        {
            EnvironmentManager environment =
                GetEnvironment();

            if (environment == null)
                return;

            bool featureSuppressed =
                TrySuppressRainRendererFeature(environment);

            int disabledRenderers =
                DisableRainControllerRenderers(environment);
            if (!_rainSuppressionLogged &&
                (featureSuppressed || disabledRenderers > 0))
            {
                _rainSuppressionLogged = true;


            }
            else if (!_rainSuppressionFailedLogged &&
                     !featureSuppressed &&
                     disabledRenderers <= 0)
            {
                // Said once, so a run that shows rain over the snow says so instead of leaving it a
                // mystery: nothing in the environment looked like rain to switch off.
                _rainSuppressionFailedLogged = true;


            }
        }

        private static bool TrySuppressRainRendererFeature(
            EnvironmentManager environment)
        {
            if (_rainRendererFeature == null)
            {
                _rendererData =
                    environment._rendererData;

                _rainRendererFeature =
                    FindRainRendererFeature(_rendererData);
            }

            if (_rainRendererFeature == null)
                return false;

            if (!_rainFeatureStateCaptured)
            {
                object active =
                    GetMemberValue(
                        _rainRendererFeature,
                        "isActive"
                    );

                _rainFeatureWasActive =
                    active is bool && (bool)active;

                _rainFeatureStateCaptured = true;
            }

            if (!InvokeSetActive(_rainRendererFeature, false))
                return false;

            _rainFeatureSuppressed = true;

            TryMarkRendererDataDirty(_rendererData);

            return true;
        }

        private static object FindRainRendererFeature(
            object rendererData)
        {
            if (rendererData == null)
                return null;

            object features =
                GetMemberValue(rendererData, "rendererFeatures") ??
                GetMemberValue(rendererData, "m_RendererFeatures") ??
                GetMemberValue(rendererData, "_rendererFeatures");

            if (features == null)
                return null;

            int count = GetCollectionCount(features);

            for (int i = 0; i < count; i++)
            {
                object feature =
                    GetCollectionItem(features, i);

                if (feature == null)
                    continue;

                string typeName =
                    feature.GetType().FullName ??
                    feature.GetType().Name;

                if (typeName.IndexOf(
                        "VFXRainFeature",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snow] Found native rain feature: " +
                        typeName
                    );

                    return feature;
                }
            }

            return null;
        }

        private static int DisableRainControllerRenderers(
            EnvironmentManager environment)
        {
            int newlyCached = 0;

            var volumes =
                environment._activeWeatherVolumes;

            // The search walks every active weather volume with GetComponentsInChildren, which is
            // the expensive half of this and only has to happen when the volumes change - that is,
            // when the game swaps weather. The renderers found are held off on every pass either
            // way, because the game turns its rain visuals back on whenever it applies a profile.
            if (volumes != null &&
                _rainScanVolumeCount != volumes.Count)
            {
                _rainScanVolumeCount = volumes.Count;

                for (int i = 0; i < volumes.Count; i++)
                {
                    WeatherVolume volume = volumes[i];

                    if (volume == null ||
                        volume._rainController == null)
                    {
                        continue;
                    }

                    try
                    {
                        Renderer[] renderers =
                            volume._rainController
                                .GetComponentsInChildren<Renderer>(true);

                        if (renderers == null)
                            continue;

                        for (int r = 0; r < renderers.Length; r++)
                        {
                            Renderer renderer = renderers[r];

                            if (renderer == null)
                                continue;

                            int id = renderer.GetInstanceID();

                            if (NativeRainRendererIds.Add(id))
                            {
                                NativeRainRenderers.Add(
                                    new NativeRendererState
                                    {
                                        Renderer = renderer,
                                        WasEnabled = renderer.enabled
                                    }
                                );

                                newlyCached++;
                            }
                        }
                    }
                    catch { }
                }
            }

            for (int i = 0; i < NativeRainRenderers.Count; i++)
            {
                try
                {
                    Renderer renderer = NativeRainRenderers[i].Renderer;

                    if (renderer != null && renderer.enabled)
                        renderer.enabled = false;
                }
                catch { }
            }

            if (newlyCached > 0)
                _rainRenderersDisabled = NativeRainRenderers.Count;

            return newlyCached;
        }

        private static void RestoreNativeRainVisuals()
        {
            for (int i = 0; i < NativeRainRenderers.Count; i++)
            {
                NativeRendererState state =
                    NativeRainRenderers[i];

                try
                {
                    if (state.Renderer != null)
                        state.Renderer.enabled = state.WasEnabled;
                }
                catch { }
            }

            NativeRainRenderers.Clear();
            NativeRainRendererIds.Clear();

            // The next snow pass has to look for the rain renderers again: the game has been handed
            // its own renderers back, so what was cached is no longer what is on screen.
            _rainScanVolumeCount = -1;
            _rainRenderersDisabled = 0;

            if (_rainRendererFeature != null &&
                _rainFeatureSuppressed)
            {
                try
                {
                    InvokeSetActive(
                        _rainRendererFeature,
                        _rainFeatureWasActive
                    );

                    TryMarkRendererDataDirty(_rendererData);
                }
                catch { }
            }

            _rainRendererFeature = null;
            _rendererData = null;

            _rainFeatureStateCaptured = false;
            _rainFeatureWasActive = false;
            _rainFeatureSuppressed = false;
            _rainSuppressionLogged = false;
            _rainSuppressionFailedLogged = false;
        }

        private static bool InvokeSetActive(
            object feature,
            bool active)
        {
            if (feature == null)
                return false;

            try
            {
                MethodInfo[] methods =
                    feature.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "SetActive")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(bool))
                    {
                        continue;
                    }

                    method.Invoke(
                        feature,
                        new object[] { active }
                    );

                    return true;
                }
            }
            catch { }

            return false;
        }

        private static void TryMarkRendererDataDirty(
            object rendererData)
        {
            if (rendererData == null)
                return;

            try
            {
                MethodInfo method =
                    rendererData.GetType().GetMethod(
                        "SetDirty",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (method != null &&
                    method.GetParameters().Length == 0)
                {
                    method.Invoke(rendererData, null);
                }
            }
            catch { }
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(target);
                    }

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                        return field.GetValue(target);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }

        private static int GetCollectionCount(object collection)
        {
            if (collection == null)
                return 0;

            try
            {
                if (collection is Array)
                    return ((Array)collection).Length;

                if (collection is System.Collections.IList)
                {
                    return
                        ((System.Collections.IList)collection).Count;
                }

                object value =
                    GetMemberValue(collection, "Count") ??
                    GetMemberValue(collection, "Length");

                if (value != null)
                    return Convert.ToInt32(value);
            }
            catch { }

            return 0;
        }

        private static object GetCollectionItem(
            object collection,
            int index)
        {
            if (collection == null || index < 0)
                return null;

            try
            {
                if (collection is Array)
                {
                    Array array = (Array)collection;

                    return index < array.Length
                        ? array.GetValue(index)
                        : null;
                }

                if (collection is System.Collections.IList)
                {
                    System.Collections.IList list =
                        (System.Collections.IList)collection;

                    return index < list.Count
                        ? list[index]
                        : null;
                }
            }
            catch { }

            try
            {
                MethodInfo[] methods =
                    collection.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "get_Item")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(int))
                    {
                        continue;
                    }

                    return method.Invoke(
                        collection,
                        new object[] { index }
                    );
                }
            }
            catch { }

            return null;
        }

        public static void DumpProfiles()
        {
            EnvironmentManager environment =
                GetEnvironment();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "=========== WVC Snow Status ==========="
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Snow days this week: " +
                DescribeSnowDays()
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Today is a snow day: " +
                _todayIsSnowDay +
                (_todayIsSnowDay
                    ? " (" +
                      FormatMinutes(_snowStartMinute) +
                      " - " +
                      FormatMinutes(_snowEndMinute) +
                      ")"
                    : "") +
                " | forcedNextDay=" +
                _forceSnowNextDay
            );

            TimeManager time = GetTimeManager();

            if (time != null)
            {
                int elapsedDays;
                int dayOfWeek;
                int minuteOfDay;

                if (TryReadClock(
                        time,
                        out elapsedDays,
                        out dayOfWeek,
                        out minuteOfDay))
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "Clock: day " +
                        elapsedDays +
                        " (" +
                        DayName(dayOfWeek) +
                        ") " +
                        FormatMinutes(minuteOfDay)
                    );
                }
            }

            if (environment != null &&
                environment._weatherProfiles != null)
            {
                var profiles =
                    environment._weatherProfiles;

                for (int i = 0; i < profiles.Count; i++)
                {
                    WeatherProfile profile = profiles[i];

                    if (profile == null)
                        continue;

                    WeatherConditions conditions =
                        profile.Conditions;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[" + i + "] '" +
                        (profile.Id ?? "?") +
                        "' sunny=" +
                        Format(conditions != null ? conditions.Sunny : -1f) +
                        " rainy=" +
                        Format(conditions != null ? conditions.Rainy : -1f) +
                        " snowy=" +
                        Format(conditions != null ? conditions.Snowy : -1f) +
                        " foggy=" +
                        Format(conditions != null ? conditions.Foggy : -1f)
                    );
                }
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Active=" +
                _snowActive +
                " | profileReady=" +
                _profileReady +
                " | meshReady=" +
                (_snowMesh != null) +
                " | flakes=" +
                _allocatedFlakes +
                " | nativeRainRenderers=" +
                NativeRainRenderers.Count
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "======================================="
            );
        }

        private static string Format(float value)
        {
            return value < 0f
                ? "?"
                : value.ToString("0.00");
        }

        private static void CheckForNewEnvironment(
            EnvironmentManager environment)
        {
            if (environment == null)
                return;

            int id;

            try
            {
                id = environment.GetInstanceID();
            }
            catch
            {
                return;
            }

            if (_environmentInstanceId == 0)
            {
                _environmentInstanceId = id;
                return;
            }

            if (_environmentInstanceId == id)
                return;

            RestoreFog();
            RestoreNativeRainVisuals();
            DestroySnowOverlay();

            _environmentInstanceId = id;
            _environmentCache = null;
            _profileReady = false;
            _snowProfile = null;
            _previousWeatherId = null;
            _snowActive = false;

            _scheduledWeekAnchor = int.MinValue;
            _lastElapsedDays = int.MinValue;
            _manualOverrideDay = int.MinValue;
            _todayIsSnowDay = false;
            _collisionMask = 0;
        }

        private static float NextRange(
            float minimum,
            float maximum)
        {
            return minimum +
                   (float)Random.NextDouble() *
                   (maximum - minimum);
        }

        private static EnvironmentManager GetEnvironment()
        {
            // The generic NetworkSingleton resolution was a recurring frame-probe hit even while
            // no weather was active, so the reference is cached and validated with the cheap
            // instance-id probe (which CheckForNewEnvironment already performs every frame).
            try
            {
                if (_environmentCache != null)
                {
                    if (_environmentCache.GetInstanceID() == _environmentInstanceId)
                        return _environmentCache;

                    _environmentCache = null;
                }

                _environmentCache = NetworkSingleton<
                    EnvironmentManager
                >.Instance;

                if (_environmentCache != null)
                    _environmentInstanceId = _environmentCache.GetInstanceID();

                return _environmentCache;
            }
            catch
            {
                return _environmentCache;
            }
        }

        private static void HandleKeys()
        {
        }
    }
}
