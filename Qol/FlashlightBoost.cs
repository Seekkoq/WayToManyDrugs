using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI.Phone;

namespace CustomNPCExample.QoL
{
    public static class FlashlightBoost
    {
        private const string Tag = "[Flashlight Boost]";

        // ---- Preferences ----------------------------------------------------
        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<float> _phoneIntensity;
        private static MelonPreferences_Entry<float> _handheldIntensity;
        private static MelonPreferences_Entry<float> _rangeMult;
        private static MelonPreferences_Entry<float> _spotAngleMult;
        private static MelonPreferences_Entry<float> _rescanInterval;
        private static MelonPreferences_Entry<bool> _logDiscoveries;

        // ---- State ----------------------------------------------------------
        private struct Vanilla
        {
            public float Intensity;
            public float Range;
            public float SpotAngle;
        }

        private static readonly Dictionary<int, Vanilla> _vanilla = new Dictionary<int, Vanilla>();
        private static float _nextScan;
        private static bool _initialized;
        private static bool _scanErrorLogged;

        // Only scan as a fallback if we're missing something
        private static int _missingScans;

        // =====================================================================
        //  Entry points
        // =====================================================================

        public static void Initialize(HarmonyLib.Harmony harmony)
        {
            if (_initialized) return;
            _initialized = true;

            _cat = MelonPreferences.CreateCategory("FlashlightBoost", "Flashlight Boost");
            _enabled = _cat.CreateEntry("Enabled", true);
            _phoneIntensity = _cat.CreateEntry("PhoneIntensityMultiplier", 2.5f, description: "Phone flashlight brightness. 1 = vanilla.");
            _handheldIntensity = _cat.CreateEntry("HandheldIntensityMultiplier", 2.5f, description: "Handheld flashlight item (1st + 3rd person). 1 = vanilla.");
            _rangeMult = _cat.CreateEntry("RangeMultiplier", 1.5f, description: "How far the beam reaches. 1 = vanilla.");
            _spotAngleMult = _cat.CreateEntry("SpotAngleMultiplier", 1.0f, description: "Beam width. 1 = vanilla.");
            // Increased from 0.5 to 3.0 to fix stuttering
            _rescanInterval = _cat.CreateEntry("RescanIntervalSeconds", 3.0f, description: "Fallback scan interval (higher = less stutter).");
            _logDiscoveries = _cat.CreateEntry("LogDiscoveredLights", false, description: "Log each flashlight Light the first time it's seen.");

            // Hooks = instant boost when flashlights are equipped/toggled
            TryPatch(harmony, typeof(Phone), "ToggleFlashlight", nameof(Phone_ToggleFlashlight_Postfix));
            TryPatch(harmony, typeof(FlashlightAvatarEquippable), "Equip", nameof(AvatarFlashlight_Equip_Postfix));
            TryPatch(harmony, typeof(Flashlight), "Equip", nameof(Viewmodel_Equip_Postfix));

            MelonLogger.Msg($"{Tag} Ready. Phone x{_phoneIntensity.Value}, handheld x{_handheldIntensity.Value}, range x{_rangeMult.Value}.");
        }

        /// <summary>Call from MelonMod.OnUpdate. Does nothing most frames.</summary>
        public static void OnUpdate()
        {
            if (!_initialized || !_enabled.Value) return;

            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + Mathf.Max(1.0f, _rescanInterval.Value);

            // Only do the expensive scene scan if we haven't found many lights yet,
            // or as a very infrequent fallback.
            _missingScans++;

            // After we've successfully found lights a few times, be much more conservative with scans
            // to avoid stuttering. 6 = ~18 seconds between "search everything" passes at 3s interval.
            if (_missingScans > 6)
            {
                _missingScans = 0;
                return;
            }

            ScanAll();
        }

        // =====================================================================
        //  Harmony postfixes
        // =====================================================================

        private static void Phone_ToggleFlashlight_Postfix(Phone __instance)
        {
            if (!_enabled.Value || __instance == null) return;
            Boost(__instance.PhoneFlashlight, _phoneIntensity.Value, "Phone");
        }

        private static void AvatarFlashlight_Equip_Postfix(FlashlightAvatarEquippable __instance)
        {
            if (!_enabled.Value || __instance == null) return;
            BoostAvatarFlashlight(__instance);
        }

        private static void Viewmodel_Equip_Postfix(MonoBehaviour __instance)
        {
            if (!_enabled.Value || __instance == null) return;
            if (__instance.TryCast<Flashlight>() == null) return;
            Boost(__instance.gameObject, _handheldIntensity.Value, "Handheld");
        }

        // =====================================================================
        //  Discovery (expensive, now only runs rarely)
        // =====================================================================

        private static void ScanAll()
        {
            try
            {
                // 1. Phone (local player)
                Phone phone = UnityEngine.Object.FindObjectOfType<Phone>();
                if (phone != null)
                {
                    Boost(phone.PhoneFlashlight, _phoneIntensity.Value, "Phone");
                }

                // 2. Handheld viewmodel
                Flashlight[] viewmodels = UnityEngine.Object.FindObjectsOfType<Flashlight>();
                for (int i = 0; i < viewmodels.Length; i++)
                {
                    if (viewmodels[i] != null)
                        Boost(viewmodels[i].gameObject, _handheldIntensity.Value, "Handheld");
                }

                // 3. Avatar flashlights
                FlashlightAvatarEquippable[] avatars = UnityEngine.Object.FindObjectsOfType<FlashlightAvatarEquippable>();
                for (int i = 0; i < avatars.Length; i++)
                {
                    if (avatars[i] != null)
                        BoostAvatarFlashlight(avatars[i]);
                }
            }
            catch (Exception ex)
            {
                if (_scanErrorLogged) return;
                _scanErrorLogged = true;
                MelonLogger.Warning($"{Tag} Scan failed (will keep trying silently): {ex}");
            }
        }

        private static void BoostAvatarFlashlight(FlashlightAvatarEquippable eq)
        {
            float mult = _handheldIntensity.Value;
            Boost(eq.gameObject, mult, "Avatar");

            if (eq.Light != null)
                Boost(eq.Light.gameObject, mult, "Avatar");
        }

        // =====================================================================
        //  The actual boost
        // =====================================================================

        private static void Boost(GameObject root, float intensityMult, string label)
        {
            if (root == null) return;

            Light[] lights = root.GetComponentsInChildren<Light>(true);
            if (lights == null || lights.Length == 0) return;

            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null) continue;

                int id = light.GetInstanceID();
                if (!_vanilla.TryGetValue(id, out Vanilla v))
                {
                    // Don't capture vanilla while it's off (intensity 0)
                    if (light.intensity <= 0f) continue;

                    v = new Vanilla
                    {
                        Intensity = light.intensity,
                        Range = light.range,
                        SpotAngle = light.spotAngle
                    };
                    _vanilla[id] = v;

                    if (_logDiscoveries.Value)
                    {
                        MelonLogger.Msg($"{Tag} {label} light '{light.gameObject.name}': vanilla intensity {v.Intensity:0.##}");
                    }

                    // We just found a new light, reset the "missing" counter
                    _missingScans = 0;
                }

                Apply(light,
                      v.Intensity * intensityMult,
                      v.Range * _rangeMult.Value,
                      v.SpotAngle * _spotAngleMult.Value);
            }
        }

        private static void Apply(Light light, float intensity, float range, float spotAngle)
        {
            if (Math.Abs(light.intensity - intensity) > 0.001f)
                light.intensity = intensity;

            if (Math.Abs(light.range - range) > 0.001f)
                light.range = range;

            if (light.type == LightType.Spot)
            {
                spotAngle = Mathf.Clamp(spotAngle, 1f, 179f);
                if (Math.Abs(light.spotAngle - spotAngle) > 0.001f)
                    light.spotAngle = spotAngle;
            }
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static void TryPatch(HarmonyLib.Harmony harmony, Type type, string method, string postfix)
        {
            try
            {
                System.Reflection.MethodInfo original = AccessTools.Method(type, method);
                if (original == null)
                {
                    MelonLogger.Warning($"{Tag} {type.Name}.{method} not found – infrequent scan will cover it.");
                    return;
                }

                harmony.Patch(original, postfix: new HarmonyMethod(typeof(FlashlightBoost), postfix));
                MelonLogger.Msg($"{Tag} Hooked {original.DeclaringType?.Name}.{method}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"{Tag} Could not hook {type.Name}.{method} – infrequent scan will cover it. ({ex.Message})");
            }
        }
    }
}