using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

using GameSkyState = Il2CppScheduleOne.Core.Weather.SkyState;
using Il2CppScheduleOne.Core.Weather;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Weather;

namespace CustomNPCExample.Weather
{
    public static class WvcSnowWeather
    {
        public const string SnowProfileId = "WVC_Snow";

        // ------------------------------------------------------------
        // Keys
        // ------------------------------------------------------------

        public static KeyCode ToggleSnowKey = KeyCode.B;
        public static KeyCode ForceSnowNextDayKey = KeyCode.G;
        public static KeyCode ClearWeatherKey = KeyCode.N;
        public static KeyCode DumpProfilesKey = KeyCode.V;

        // ------------------------------------------------------------
        // Appearance
        // ------------------------------------------------------------

        private const int SnowFlakeCount = 850;
        private const float SnowAreaRadius = 22f;
        private const float SnowSpawnHeight = 12f;
        private const float KillBelowPlayer = 5f;

        private static readonly Color SnowHazeColor =
            new Color(0.73f, 0.77f, 0.84f, 1f);

        private static readonly Color SnowFlakeColor =
            new Color(0.96f, 0.98f, 1f, 0.92f);

        private static readonly System.Random Random =
            new System.Random(0x534E4F57);

        // ------------------------------------------------------------
        // Collision
        // ------------------------------------------------------------

        // Raycasts are budgeted so a full recycle burst never hitches.
        private const int MaxRaycastsPerFrame = 80;
        private const int ShelterChecksPerFrame = 32;
        private const float ShelterCheckHeight = 35f;

        private static int _raycastBudget;
        private static int _shelterCursor;
        private static int _collisionMask = 0;

        // ------------------------------------------------------------
        // Scheduling
        // ------------------------------------------------------------

        public static bool AutoScheduleEnabled = true;

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

        private static int _scheduledWeekAnchor = int.MinValue;
        private static int _lastElapsedDays = int.MinValue;
        private static int _manualOverrideDay = int.MinValue;

        private static bool _forceSnowNextDay;
        private static bool _todayIsSnowDay;
        private static int _snowStartMinute;
        private static int _snowEndMinute;

        // ------------------------------------------------------------
        // Weather state
        // ------------------------------------------------------------

        private static bool _snowActive;
        private static bool _profileReady;
        private static string _previousWeatherId;

        private static WeatherProfile _snowProfile;

        private static int _environmentInstanceId;
        private static float _profileRetryTimer;
        private static float _rainSuppressionTimer;

        // ------------------------------------------------------------
        // Fog backup
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // Snow mesh
        // ------------------------------------------------------------

        private sealed class SnowFlake
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Size;
            public float Phase;
            public float Alpha;

            // World height at which this flake hits something solid.
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

        // ------------------------------------------------------------
        // Native rain suppression
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // Main update
        // ------------------------------------------------------------

        public static void Update()
        {
            EnvironmentManager environment =
                GetEnvironment();

            CheckForNewEnvironment(environment);
            HandleKeys();

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

            if (!_snowActive)
                return;

            if (_snowProfile == null)
            {
                DisableWeather();
                return;
            }

            EnsureWeatherStillSelected(
                environment,
                _snowProfile
            );

            KeepProfileConfigured(_snowProfile);

            // Applied after the game's weather system has had a chance
            // to update its own SkyState.
            ApplyFogForSnow();

            if (EnsureSnowOverlay())
                UpdateSnowOverlay();

            _rainSuppressionTimer += Time.deltaTime;

            if (_rainSuppressionTimer >= 1f)
            {
                _rainSuppressionTimer = 0f;
                SuppressNativeRainVisuals();
            }
        }

        // ------------------------------------------------------------
        // Weekly schedule
        // ------------------------------------------------------------

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

            // The player took manual control today.
            if (_manualOverrideDay == elapsedDays)
                return;

            bool wantSnow =
                _todayIsSnowDay &&
                minuteOfDay >= _snowStartMinute &&
                minuteOfDay < _snowEndMinute;

            if (wantSnow && !_snowActive)
            {
                EnableSnow();
                return;
            }

            if (!wantSnow && _snowActive)
                DisableWeather();
        }

        private static void OnDayChanged(
            int elapsedDays,
            int dayOfWeek)
        {
            // The anchor is the elapsed-day index of this week's Monday.
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

            if (!_todayIsSnowDay)
            {
                MelonLogger.Msg(
                    "[WVC Snow] " +
                    DayName(dayOfWeek) +
                    ": no snow scheduled."
                );

                return;
            }

            RollTodayWindow(
                elapsedDays,
                forced
            );

            MelonLogger.Msg(
                "[WVC Snow] " +
                DayName(dayOfWeek) +
                ": snow from " +
                FormatMinutes(_snowStartMinute) +
                " to " +
                FormatMinutes(_snowEndMinute) +
                (forced ? " (forced)" : "") +
                "."
            );
        }

        private static void RollWeekSchedule(int weekAnchor)
        {
            _scheduledWeekAnchor = weekAnchor;
            SnowDaysThisWeek.Clear();

            // Seeded by the week so the same save always rolls the
            // same days, even after a reload.
            System.Random rng =
                new System.Random(
                    unchecked(weekAnchor * 7919 + 104729)
                );

            List<int> pool = new List<int>();

            for (int i = 0; i < 7; i++)
                pool.Add(i);

            int wanted =
                Mathf.Clamp(SnowDaysPerWeek, 0, 7);

            for (int i = 0; i < wanted; i++)
            {
                int index = rng.Next(pool.Count);

                SnowDaysThisWeek.Add(pool[index]);
                pool.RemoveAt(index);
            }

            SnowDaysThisWeek.Sort();

            MelonLogger.Msg(
                "[WVC Snow] Snow days this week: " +
                DescribeSnowDays()
            );
        }

        private static void RollTodayWindow(
            int elapsedDays,
            bool forced)
        {
            if (forced)
            {
                // Long, obvious window for the debug key.
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

            MelonLogger.Msg(
                "[WVC Snow] Snow forced for the next in-game day."
            );
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
            try
            {
                return NetworkSingleton<
                    TimeManager
                >.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static string DayName(int index)
        {
            if (index < 0 || index >= DayNames.Length)
                return "Day " + index;

            return DayNames[index];
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

        // ------------------------------------------------------------
        // Public controls
        // ------------------------------------------------------------

        public static bool EnableSnow()
        {
            if (_snowActive)
                return true;

            MelonLogger.Msg(
                "[WVC Snow] Enabling snow..."
            );

            if (!TryEnsureProfiles())
            {
                MelonLogger.Warning(
                    "[WVC Snow] Weather profiles are not ready yet."
                );

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
                _rainSuppressionTimer = 1f;

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

                MelonLogger.Msg(
                    "[WVC Snow] Snow enabled."
                );

                return true;
            }
            catch (Exception ex)
            {
                _snowActive = false;

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

        public static bool DisableWeather()
        {
            EnvironmentManager environment =
                GetEnvironment();

            try
            {
                bool wasActive = _snowActive;
                _snowActive = false;

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

                    MelonLogger.Msg(
                        "[WVC Snow] Weather restored: " +
                        restoreId
                    );
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
            _profileRetryTimer = 0f;
            _rainSuppressionTimer = 0f;

            SnowDaysThisWeek.Clear();
            _scheduledWeekAnchor = int.MinValue;
            _lastElapsedDays = int.MinValue;
            _manualOverrideDay = int.MinValue;
            _forceSnowNextDay = false;
            _todayIsSnowDay = false;
            _collisionMask = 0;
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

        // ------------------------------------------------------------
        // Weather profiles
        // ------------------------------------------------------------

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

            WeatherProfile rainDonor =
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

            if (rainDonor == null)
            {
                MelonLogger.Warning(
                    "[WVC Snow] No rain donor profile found."
                );

                return false;
            }

            WeatherProfile clone =
                UnityEngine.Object.Instantiate(
                    rainDonor
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

            MelonLogger.Msg(
                "[WVC Snow] Injected '" +
                id +
                "' cloned from '" +
                rainDonor.Id +
                "'."
            );

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

            WeatherProfile heavyRain =
                FindProfile(
                    environment,
                    "HeavyRain",
                    true
                );

            WeatherProfile skySource =
                overcast ?? foggy;

            if (skySource != null)
            {
                // Do not use BloodMoon settings.
                profile._skySettings =
                    skySource._skySettings;

                profile._cloudSettings =
                    skySource._cloudSettings;
            }

            if (heavyRain != null &&
                heavyRain._thunderSettings != null)
            {
                profile._thunderSettings =
                    heavyRain._thunderSettings;
            }

            KeepProfileConfigured(profile);
        }

        private static void KeepProfileConfigured(
            WeatherProfile profile)
        {
            if (profile == null)
                return;

            WeatherConditions conditions =
                profile._conditions;

            if (conditions != null)
            {
                conditions.Sunny = 0f;
                conditions.Cloudy = 0.9f;

                // Keep rain logic active for umbrellas,
                // NPC dialogue, and wet-weather behavior.
                // Visible rain is suppressed separately.
                conditions.Rainy = 0.85f;

                conditions.Stormy = 0.35f;
                conditions.Snowy = 1f;
                conditions.Foggy = 0.25f;
                conditions.Windy = 0.35f;

                conditions.Hail = 0f;
                conditions.Sleet = 0f;
            }

            RainSettings rain =
                profile._rainSettings;

            if (rain != null)
            {
                rain.IsActive = true;
                rain.RainStrength = 0.85f;
                rain.RainSize = 0.5f;
                rain.RainColour = SnowFlakeColor;
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
                // The weather manager can be between volume updates.
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

        // ------------------------------------------------------------
        // Game sky and fog
        // ------------------------------------------------------------

        private static void ApplyFogForSnow()
        {
            CaptureFogIfNeeded();

            Color haze = SnowHazeColor;

            try
            {
                RenderSettings.fog = true;

                RenderSettings.fogMode =
                    FogMode.ExponentialSquared;

                RenderSettings.fogColor = haze;
                RenderSettings.fogDensity = 0.011f;
                RenderSettings.fogStartDistance = 18f;
                RenderSettings.fogEndDistance = 100f;

                RenderSettings.ambientMode =
                    AmbientMode.Flat;

                RenderSettings.ambientLight = haze;
                RenderSettings.ambientIntensity = 0.85f;
                RenderSettings.reflectionIntensity = 0.35f;
            }
            catch { }

            ApplyGameSkyState(haze);
        }

        private static void ApplyGameSkyState(Color haze)
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

            try
            {
                sky.SkyUpperColor = haze;
                sky.SkyMiddleColor = haze;
                sky.SkyLowerColor = haze;

                sky.SunLightColor = haze;
                sky.SunColor = haze;
                sky.SunSize = 0f;
                sky.SunIntensity = 0.30f;
                sky.SunShadowStrength = 0.30f;

                sky.MoonLightColor = haze;
                sky.MoonColor = haze;
                sky.MoonSize = 0f;
                sky.MoonIntensity = 0f;
                sky.MoonShadowStrength = 0f;

                sky.AmbientSkyColor = haze;
                sky.AmbientEquatorColor = haze;
                sky.AmbientGroundColor = haze;

                sky.FogColor = haze;
                sky.FogDensity = 0.28f;

                sky.WindIntensity = 0.30f;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snow] Could not update GameSkyState: " +
                    ex.Message
                );
            }
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

        // ------------------------------------------------------------
        // World-space snow mesh
        // ------------------------------------------------------------

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

                MelonLogger.Msg(
                    "[WVC Snow] Created world-space snow: " +
                    _allocatedFlakes +
                    " flakes."
                );

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

            int count = SnowFlakeCount;

            _allocatedFlakes = count;

            _flakes = new SnowFlake[count];
            _vertices = new Vector3[count * 4];
            _uv = new Vector2[count * 4];
            _colours = new Color[count * 4];
            _triangles = new int[count * 6];

            // One-off larger budget for the initial fill.
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
                    false
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
            float angle =
                NextRange(0f, Mathf.PI * 2f);

            float distance =
                Mathf.Sqrt(NextRange(0f, 1f)) *
                SnowAreaRadius;

            float x =
                origin.x + Mathf.Cos(angle) * distance;

            float z =
                origin.z + Mathf.Sin(angle) * distance;

            float topY =
                origin.y +
                SnowSpawnHeight +
                NextRange(0f, 4f);

            // Find the first solid surface underneath the spawn point.
            // The flake dies there instead of sinking through roofs,
            // awnings, floors, vehicles, etc.
            float killY =
                origin.y - KillBelowPlayer;

            float surfaceY;

            if (TryRaycastDown(
                    new Vector3(x, topY, z),
                    SnowSpawnHeight + KillBelowPlayer + 8f,
                    out surfaceY))
            {
                killY = surfaceY + 0.03f;
            }

            flake.KillY = killY;

            // Never seed a flake below whatever is covering that spot,
            // so nothing spawns inside a building.
            float y =
                fromTop
                    ? topY
                    : Mathf.Lerp(
                        killY + 0.3f,
                        topY,
                        NextRange(0f, 1f)
                      );

            flake.Position = new Vector3(x, y, z);

            flake.Velocity =
                new Vector3(
                    NextRange(-0.55f, 0.55f),
                    NextRange(-2.4f, -0.7f),
                    NextRange(-0.55f, 0.55f)
                );

            flake.Size = NextRange(0.04f, 0.14f);
            flake.Alpha = NextRange(0.55f, 0.95f);
            flake.Phase = NextRange(0f, Mathf.PI * 2f);

            if (_colours != null && index >= 0)
            {
                int vertex = index * 4;

                Color color =
                    new Color(
                        SnowFlakeColor.r,
                        SnowFlakeColor.g,
                        SnowFlakeColor.b,
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

            _raycastBudget = MaxRaycastsPerFrame;

            Vector3 origin = GetAnchorPosition();
            Camera camera = Camera.main;

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

            float recycleDistance =
                SnowAreaRadius + 8f;

            float recycleDistanceSqr =
                recycleDistance * recycleDistance;

            // Rolling shelter test: flakes that drifted under a roof,
            // balcony or overhang get recycled instead of falling
            // through the ceiling.
            RunShelterChecks(origin);

            for (int i = 0; i < _allocatedFlakes; i++)
            {
                SnowFlake flake = _flakes[i];

                flake.Position +=
                    flake.Velocity * deltaTime;

                float sway =
                    Mathf.Sin(time * 0.7f + flake.Phase) *
                    0.12f;

                flake.Position.x += sway * deltaTime;

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

            _snowMesh.bounds =
                new Bounds(
                    Vector3.zero,
                    new Vector3(140f, 140f, 140f)
                );
        }

        private static void RunShelterChecks(Vector3 origin)
        {
            if (_allocatedFlakes <= 0)
                return;

            int checks =
                Mathf.Min(
                    ShelterChecksPerFrame,
                    _allocatedFlakes
                );

            for (int i = 0; i < checks; i++)
            {
                _shelterCursor++;

                if (_shelterCursor >= _allocatedFlakes)
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

            Camera camera = Camera.main;

            if (camera != null)
                return camera.transform.position;

            return Vector3.zero;
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
            _shelterCursor = 0;
            _coloursDirty = false;
        }

        // ------------------------------------------------------------
        // Native rain suppression
        // ------------------------------------------------------------

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

                MelonLogger.Msg(
                    "[WVC Snow] Native rain suppressed. " +
                    "Renderer feature=" +
                    featureSuppressed +
                    ", cached renderers=" +
                    NativeRainRenderers.Count
                );
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
                    MelonLogger.Msg(
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

            if (volumes == null)
                return 0;

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

                        renderer.enabled = false;
                    }
                }
                catch { }
            }

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

        // ------------------------------------------------------------
        // Reflection helpers
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // Debug output
        // ------------------------------------------------------------

        public static void DumpProfiles()
        {
            EnvironmentManager environment =
                GetEnvironment();

            MelonLogger.Msg(
                "=========== WVC Snow Status ==========="
            );

            MelonLogger.Msg(
                "Snow days this week: " +
                DescribeSnowDays()
            );

            MelonLogger.Msg(
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
                    MelonLogger.Msg(
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

                    MelonLogger.Msg(
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

            MelonLogger.Msg(
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

            MelonLogger.Msg(
                "======================================="
            );
        }

        private static string Format(float value)
        {
            return value < 0f
                ? "?"
                : value.ToString("0.00");
        }

        // ------------------------------------------------------------
        // General helpers
        // ------------------------------------------------------------

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
            try
            {
                return NetworkSingleton<
                    EnvironmentManager
                >.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static void HandleKeys()
        {
            if (Input.GetKeyDown(ToggleSnowKey))
            {
                ToggleSnow();
                return;
            }

            if (Input.GetKeyDown(ForceSnowNextDayKey))
            {
                ForceSnowNextDay();
                return;
            }

            if (Input.GetKeyDown(ClearWeatherKey))
            {
                MarkManualOverride();
                DisableWeather();
                return;
            }

            if (Input.GetKeyDown(DumpProfilesKey))
            {
                DumpProfiles();
            }
        }
    }
}