using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI.Phone;

namespace CustomNPCExample.QoL
{
    public static class FlashlightBoost
    {
        private const string Tag = "[Flashlight Boost]";
        private const float RefreshInterval = 0.25f;

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<float> _phoneIntensity;
        private static MelonPreferences_Entry<float> _handheldIntensity;
        private static MelonPreferences_Entry<float> _rangeMultiplier;
        private static MelonPreferences_Entry<float> _spotAngleMultiplier;
        private static MelonPreferences_Entry<bool> _logDiscoveries;

        private sealed class TrackedLight
        {
            public Light Light;
            public OptimizedLight Controller;
            public bool IsPhone;
            public string Source;

            public bool HasBaseline;
            public float BaseIntensity;
            public float BaseRange;
            public float BaseSpotAngle;

            public bool LoggedApplication;
        }

        private sealed class PendingDiscovery
        {
            public IntPtr Owner;
            public string Source;
            public Func<int> Discover;
            public int AttemptsLeft = 4;
            public int Found;
        }

        private static readonly List<TrackedLight> Lights =
            new List<TrackedLight>();

        private static readonly List<PendingDiscovery> Pending =
            new List<PendingDiscovery>();

        private static readonly HashSet<string> SeenHooks =
            new HashSet<string>();

        private static readonly HashSet<string> ReportedProblems =
            new HashSet<string>();

        private static bool _initialized;
        private static float _nextRefresh;

        public static void Initialize(HarmonyLib.Harmony harmony)
        {
            if (_initialized)
                return;

            if (harmony == null)
                throw new ArgumentNullException(nameof(harmony));

            _category = MelonPreferences.CreateCategory(
                "FlashlightBoost",
                "Flashlight Boost"
            );

            _enabled = _category.CreateEntry(
                "Enabled",
                true
            );

            _phoneIntensity = _category.CreateEntry(
                "PhoneIntensityMultiplier",
                2.5f
            );

            _handheldIntensity = _category.CreateEntry(
                "HandheldIntensityMultiplier",
                2.5f
            );

            _rangeMultiplier = _category.CreateEntry(
                "RangeMultiplier",
                1.5f
            );

            _spotAngleMultiplier = _category.CreateEntry(
                "SpotAngleMultiplier",
                1f
            );

            _logDiscoveries = _category.CreateEntry(
                "LogDiscoveredLights",
                false
            );

            _initialized = true;

            PatchDeclared(
                harmony,
                typeof(Phone),
                "ToggleFlashlight",
                nameof(PhoneTogglePostfix)
            );

            PatchDeclared(
                harmony,
                typeof(FlashlightAvatarEquippable),
                "Equip",
                nameof(AvatarEquipPostfix),
                new[] { typeof(Avatar) }
            );

            PatchDeclared(
                harmony,
                typeof(Equippable_Viewmodel),
                "Equip",
                nameof(ViewmodelEquipPostfix),
                new[] { typeof(ItemInstance) }
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"{Tag} Ready. Enabled={_enabled.Value}, " +
                $"phone x{_phoneIntensity.Value}, " +
                $"handheld x{_handheldIntensity.Value}, " +
                $"range x{_rangeMultiplier.Value}."
            );
        }

        public static bool IsBoostEnabled => _enabled != null && _enabled.Value;

        public static void OnUpdate()
        {
            if (!_initialized)
                return;

            if (Lights.Count == 0 && Pending.Count == 0)
                return;

            float now = Time.unscaledTime;

            if (now < _nextRefresh)
                return;

            _nextRefresh = now + RefreshInterval;

            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                PendingDiscovery request = Pending[i];

                try
                {
                    request.Found = Math.Max(
                        request.Found,
                        request.Discover()
                    );
                }
                catch (Exception ex)
                {
                    WarnOnce(
                        "discovery-" + request.Source,
                        $"{request.Source} discovery failed: {ex.Message}"
                    );

                    Pending.RemoveAt(i);
                    continue;
                }

                request.AttemptsLeft--;

                if (request.AttemptsLeft > 0)
                    continue;

                if (request.Found == 0)
                {
                    WarnOnce(
                        "empty-" + request.Source,
                        $"{request.Source} hook ran, but no Unity Light " +
                        "reference was found. No brightness was changed."
                    );
                }

                Pending.RemoveAt(i);
            }

            for (int i = Lights.Count - 1; i >= 0; i--)
            {
                TrackedLight entry = Lights[i];

                try
                {
                    if (entry.Light == null)
                    {
                        Lights.RemoveAt(i);
                        continue;
                    }

                    ApplyTrackedLight(entry);
                }
                catch (Exception ex)
                {
                    WarnOnce(
                        "apply-" + entry.Source,
                        $"{entry.Source} light update failed: {ex.Message}"
                    );

                    Lights.RemoveAt(i);
                }
            }
        }

        private static void PhoneTogglePostfix(Phone __instance)
        {
            if (!_initialized || __instance == null)
                return;

            QueueDiscovery(
                __instance.Pointer,
                "Phone",
                () => __instance != null
                    ? DiscoverRoot(
                        __instance.PhoneFlashlight,
                        true,
                        "Phone")
                    : 0
            );
        }

        private static void AvatarEquipPostfix(
            FlashlightAvatarEquippable __instance)
        {
            if (!_initialized || __instance == null)
                return;

            QueueDiscovery(
                __instance.Pointer,
                "Avatar",
                () => DiscoverAvatar(__instance)
            );
        }

        private static void ViewmodelEquipPostfix(
            Equippable_Viewmodel __instance)
        {
            if (!_initialized || __instance == null)
                return;

            try
            {
                Flashlight flashlight =
                    __instance.TryCast<Flashlight>();

                if (flashlight == null)
                    return;

                QueueDiscovery(
                    flashlight.Pointer,
                    "Handheld",
                    () => flashlight != null
                        ? DiscoverRoot(
                            flashlight.gameObject,
                            false,
                            "Handheld")
                        : 0
                );
            }
            catch (Exception ex)
            {
                WarnOnce(
                    "viewmodel-cast",
                    "Could not inspect equipped item: " + ex.Message
                );
            }
        }

        private static void QueueDiscovery(
            IntPtr owner,
            string source,
            Func<int> discover)
        {
            if (SeenHooks.Add(source))
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    $"{Tag} {source} hook ran; light discovery queued."
                );
            }

            for (int i = 0; i < Pending.Count; i++)
            {
                if (Pending[i].Owner != owner)
                    continue;

                Pending[i].Discover = discover;
                Pending[i].AttemptsLeft = 4;
                return;
            }

            Pending.Add(new PendingDiscovery
            {
                Owner = owner,
                Source = source,
                Discover = discover
            });
        }

        private static int DiscoverAvatar(
            FlashlightAvatarEquippable avatar)
        {
            if (avatar == null)
                return 0;

            int found = 0;

            OptimizedLight controller = avatar.Light;

            if (controller != null && controller._Light != null)
            {
                TrackLight(
                    controller._Light,
                    controller,
                    false,
                    "Avatar"
                );

                found++;
            }

            return found + DiscoverRoot(
                avatar.gameObject,
                false,
                "Avatar"
            );
        }

        private static int DiscoverRoot(
            GameObject root,
            bool isPhone,
            string source)
        {
            if (root == null)
                return 0;

            int found = 0;

            var controllers =
                root.GetComponentsInChildren<OptimizedLight>(true);

            for (int i = 0; i < controllers.Length; i++)
            {
                OptimizedLight controller = controllers[i];

                if (controller == null || controller._Light == null)
                    continue;

                TrackLight(
                    controller._Light,
                    controller,
                    isPhone,
                    source
                );

                found++;
            }

            var ordinaryLights =
                root.GetComponentsInChildren<Light>(true);

            for (int i = 0; i < ordinaryLights.Length; i++)
            {
                Light light = ordinaryLights[i];

                if (light == null)
                    continue;

                TrackLight(light, null, isPhone, source);
                found++;
            }

            return found;
        }

        private static void TrackLight(
            Light light,
            OptimizedLight controller,
            bool isPhone,
            string source)
        {
            if (light == null)
                return;

            for (int i = 0; i < Lights.Count; i++)
            {
                TrackedLight existing = Lights[i];

                if (existing.Light == null ||
                    existing.Light.Pointer != light.Pointer)
                {
                    continue;
                }

                if (controller != null)
                    existing.Controller = controller;

                existing.IsPhone |= isPhone;
                return;
            }

            Lights.Add(new TrackedLight
            {
                Light = light,
                Controller = controller,
                IsPhone = isPhone,
                Source = source
            });

            if (_logDiscoveries.Value)
            {
                string controllerState = controller != null
                    ? $"controllerEnabled={controller.Enabled}, " +
                      $"disabledForOptimization={controller.DisabledForOptimization}, " +
                      $"culled={controller.culled}"
                    : "no OptimizedLight controller";

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    $"{Tag} Found {source} light '{light.gameObject.name}'. " +
                    $"Intensity={light.intensity:0.###}, " +
                    $"range={light.range:0.###}, " +
                    $"lightEnabled={light.enabled}, " +
                    $"active={light.gameObject.activeInHierarchy}; " +
                    controllerState
                );
            }
        }

        private static void ApplyTrackedLight(TrackedLight entry)
        {
            Light light = entry.Light;
            OptimizedLight controller = entry.Controller;

            if (controller != null &&
                (!controller.Enabled ||
                 controller.DisabledForOptimization ||
                 controller.culled))
            {
                return;
            }

            if (!light.enabled || !light.gameObject.activeInHierarchy)
                return;

            float currentIntensity = light.intensity;

            if (float.IsNaN(currentIntensity) ||
                float.IsInfinity(currentIntensity) ||
                currentIntensity <= 0f ||
                light.range <= 0f)
            {
                return;
            }

            if (!entry.HasBaseline)
            {
                entry.BaseIntensity = currentIntensity;
                entry.BaseRange = light.range;
                entry.BaseSpotAngle = light.spotAngle;
                entry.HasBaseline = true;
            }

            bool enabled = _enabled.Value;

            float intensityMultiplier = enabled
                ? ReadMultiplier(entry.IsPhone
                    ? _phoneIntensity.Value
                    : _handheldIntensity.Value)
                : 1f;

            float rangeMultiplier = enabled
                ? ReadMultiplier(_rangeMultiplier.Value)
                : 1f;

            float angleMultiplier = enabled
                ? ReadMultiplier(_spotAngleMultiplier.Value)
                : 1f;

            float targetIntensity =
                entry.BaseIntensity * intensityMultiplier;

            float targetRange =
                entry.BaseRange * rangeMultiplier;

            float targetAngle = Mathf.Clamp(
                entry.BaseSpotAngle * angleMultiplier,
                1f,
                179f
            );

            if (Mathf.Abs(light.intensity - targetIntensity) > 0.001f)
                light.intensity = targetIntensity;

            if (Mathf.Abs(light.range - targetRange) > 0.001f)
                light.range = targetRange;

            if (light.type == LightType.Spot &&
                Mathf.Abs(light.spotAngle - targetAngle) > 0.001f)
            {
                light.spotAngle = targetAngle;
            }

            if (_logDiscoveries.Value && !entry.LoggedApplication)
            {
                entry.LoggedApplication = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    $"{Tag} Applied to {entry.Source} " +
                    $"'{light.gameObject.name}': " +
                    $"intensity {entry.BaseIntensity:0.###} -> " +
                    $"{light.intensity:0.###}; " +
                    $"range {entry.BaseRange:0.###} -> {light.range:0.###}."
                );
            }
        }

        private static float ReadMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1f;

            return Mathf.Clamp(value, 0.1f, 20f);
        }

        private static void PatchDeclared(
            HarmonyLib.Harmony harmony,
            Type declaringType,
            string methodName,
            string postfixName,
            Type[] parameters = null)
        {
            try
            {
                var original = AccessTools.DeclaredMethod(
                    declaringType,
                    methodName,
                    parameters
                );

                if (original == null)
                {


                    return;
                }

                harmony.Patch(
                    original,
                    postfix: new HarmonyMethod(
                        typeof(FlashlightBoost),
                        postfixName
                    )
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    $"{Tag} Hooked {declaringType.Name}.{methodName}."
                );
            }
            catch (Exception)
            {

            }
        }

        private static void WarnOnce(string key, string message)
        {
            if (ReportedProblems.Add(key))
                { }
        }
    }
}
