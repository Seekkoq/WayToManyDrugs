using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using UnityEngine;

using NativeNpc = Il2CppScheduleOne.NPCs.NPC;
using NativeController = Il2CppScheduleOne.Dialogue.DialogueController;

namespace CustomNPCExample.Casino
{
    /// <summary>
    /// Attaches the casino laundry to the real, vanilla Philip Wentworth.
    ///
    /// He is an ordinary game NPC, so there is no OnCreated of ours to hang off: the wrapper comes
    /// from S1API's built-in resolution (<c>NPC.Get("philip_wentworth")</c>), retried on a short
    /// coroutine after the Main scene loads and again from the interact hook, so a slow spawn or a
    /// scene reload just means he is armed on the next attempt rather than never.
    ///
    /// Two Harmony prefixes on the game's own DialogueController methods refresh the conversation
    /// right before it opens - the container text (cooldown, shortfall) is rebuilt from live state
    /// each time - and hand the vanilla conversation back when the kill switch is off. The prefixes
    /// never throw: a failure leaves Philip exactly as the game has him.
    /// </summary>
    public static class PhilipWentworthBootstrap
    {
        /// <summary>S1API's own id for the vanilla Philip Wentworth.</summary>
        public static string PhilipNpcId =>
            S1API.Entities.NPCs.Downtown.PhilipWentworth.NPCId;

        private static HarmonyLib.Harmony _harmony;
        private static bool _patched;
        private static bool _armRoutineRunning;

        private static NPC _philip;
        private static NPCDialogue _dialogue;
        private static bool _armed;

        /// <summary>True once the casino container is armed on Philip's dialogue.</summary>
        public static bool IsArmed => _armed && _dialogue != null;

        /// <summary>The dialogue the casino container is armed on, when there is one.</summary>
        public static NPCDialogue ActiveDialogue => _dialogue;

        /// <summary>
        /// Called once during mod start-up: brings the preference category into existence and hooks
        /// the game's dialogue entry points. Idempotent.
        /// </summary>
        public static void Initialize()
        {
            CasinoLaunderingManager.Initialize();
            ApplyPatches();
        }

        private static void ApplyPatches()
        {
            if (_patched)
                return;

            try
            {
                _harmony = new HarmonyLib.Harmony("wvc.casino.philip");

                // Both entry points run before the conversation is shown, which is what makes the
                // pre-open refresh possible. Resolved by name with a null check - a method the game
                // no longer offers must not take mod start-up down with it.
                PatchSafe("Interacted");
                PatchSafe("StartGenericDialogue");

                _patched = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] Could not hook the dialogue entry points: " + ex);
            }
        }

        private static void PatchSafe(string methodName)
        {
            // 0.4.7 made DialogueController.Interacted private, so a public-only lookup misses
            // it. Search public AND non-public instance methods.
            var method = AccessTools.Method(typeof(NativeController), methodName)
                ?? typeof(NativeController).GetMethod(
                        methodName,
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
            if (method == null)
                return;

            try
            {
                _harmony.Patch(
                    method,
                    prefix: new HarmonyMethod(
                        typeof(PhilipWentworthBootstrap),
                        nameof(DialogueInteract_Prefix)));
            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Always(
                    "[Casino] Patch '" + methodName + "' threw: " + ex.Message);
            }
        }

        /// <summary>
        /// Runs before either dialogue entry point. For Philip it opens our laundering
        /// conversation and returns false to suppress the vanilla interact (so the vanilla menu
        /// cannot open over/after ours); for every other NPC it returns true (run vanilla as-is).
        /// </summary>
        public static bool DialogueInteract_Prefix(NativeController __instance)
        {
            try
            {
                return OnPhilipInteracted(__instance);
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool OnPhilipInteracted(NativeController controller)
        {
            if (controller == null)
                return true;

            NativeNpc npc = controller.npc;
            if (npc == null ||
                !string.Equals(npc.ID, PhilipNpcId, StringComparison.Ordinal))
                return true;

            if (!CasinoLaunderingManager.FeatureEnabled ||
                !CasinoLaunderingManager.DialogueOverrideEnabled)
            {
                // Kill switch: take our container away so the conversation that opens is the
                // game's own. Idempotent, and cheap enough to do on every interaction.
                try
                {
                    controller.ClearOverrideContainer();
                }
                catch (Exception)
                {
                }

                return true;
            }

            EnsureArmed();

            // On 0.4.7 the vanilla interact builds its own menu from the controller's Choices
            // list. Rather than suppressing it (which hid Philip's vanilla options), we inject
            // our laundering option into that list and let vanilla build the menu - so the player
            // sees Philip's vanilla buy/cage options AND our laundering option together.
            try
            {
                CasinoLaunderingDialogue.InjectMenu(controller);
            }
            catch (Exception)
            {
            }
            return true; // always let vanilla run so his own options stay visible
        }

        private static System.Type FindIl2CppType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static void SetMember(object target, string memberName, object value)
        {
            if (target == null) return;
            var t = target.GetType();
            var prop = t.GetProperty(memberName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.CanWrite) { prop.SetValue(target, value); return; }
            var field = t.GetField(memberName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        /// <summary>
        /// Resolves Philip through S1API's built-in wrapper and arms the casino container on his
        /// dialogue. Silent about absence - he simply is not there yet - and safe to call at any
        /// time, as often as needed.
        /// </summary>
        public static void EnsureArmed()
        {
            if (IsArmed)
                return;

            if (!CasinoLaunderingManager.FeatureEnabled ||
                !CasinoLaunderingManager.DialogueOverrideEnabled)
                return;

            try
            {
                NPC philip = NPC.Get(PhilipNpcId);
                if (philip == null)
                {
                    return;
                }

                NPCDialogue dialogue = philip.Dialogue;
                if (dialogue == null)
                {
                    return;
                }

                NativeNpc native = FindNativePhilip();
                if (native == null)
                {
                    return;
                }

                if (!CasinoLaunderingDialogue.TryRegisterAndArm(dialogue, native))
                {
                    return;
                }

                _philip = philip;
                _dialogue = dialogue;
                _armed = true;

            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Could not arm Philip yet: " + ex.Message);
            }
        }

        /// <summary>
        /// Scene loaded: whatever was armed belonged to the old world, so start over with the
        /// 60 x 0.25s retry routine the offshore captain uses to wait for his dialogue.
        /// </summary>
        public static void OnMainSceneLoaded()
        {
            _armed = false;
            _philip = null;
            _dialogue = null;
            CasinoLaunderingDialogue.Forget();

            if (_armRoutineRunning)
                return;

            MelonCoroutines.Start(ArmRoutine());
        }

        private static IEnumerator ArmRoutine()
        {
            _armRoutineRunning = true;

            for (int attempt = 0; attempt < 60; attempt++)
            {
                EnsureArmed();

                if (_armed)
                    break;

                yield return new WaitForSeconds(0.25f);
            }

            _armRoutineRunning = false;
        }

        /// <summary>
        /// Applies the dialogue kill switch immediately: on, (re)arm the casino container; off,
        /// drop it and give Philip his vanilla conversation back.
        /// </summary>
        public static void ApplyDialogueSwitch()
        {
            try
            {
                if (CasinoLaunderingManager.FeatureEnabled &&
                    CasinoLaunderingManager.DialogueOverrideEnabled)
                {
                    EnsureArmed();
                    return;
                }

                _armed = false;
                _dialogue = null;
                CasinoLaunderingDialogue.Forget();

                NativeNpc native = FindNativePhilip();
                if (native != null)
                {
                    CasinoLaunderingDialogue.StripFrom(native);
                    ClearOverride(native);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] Dialogue switch failed: " + ex);
            }
        }

        /// <summary>Finds the vanilla Philip in the game's NPC registry, or null.</summary>
        public static NativeNpc FindNativePhilip()
        {
            try
            {
                var registry = Il2CppScheduleOne.NPCs.NPCManager.NPCRegistry;
                if (registry == null)
                    return null;

                foreach (NativeNpc npc in registry)
                {
                    if (npc != null &&
                        string.Equals(npc.ID, PhilipNpcId, StringComparison.Ordinal))
                        return npc;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static void ClearOverride(NativeNpc native)
        {
            try
            {
                var controllers =
                    native.GetComponentsInChildren<NativeController>(true);

                if (controllers == null)
                    return;

                foreach (NativeController controller in controllers)
                {
                    if (controller != null)
                        controller.ClearOverrideContainer();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
