using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

using NativeEQuality    = Il2CppScheduleOne.ItemFramework.EQuality;
using NativeItemInstance = Il2CppScheduleOne.ItemFramework.ItemInstance;
using NativeItemSlot    = Il2CppScheduleOne.ItemFramework.ItemSlot;

namespace CustomNPCExample.Products.Ovens
{
    public static class CookieOvenPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony("westvilleconnection.cookieoven");

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> PendingAutoStarts =
            new HashSet<int>();

        private static bool _applied;

        private const string OperationIngredientId = "cocainebase";

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                PatchLabOvenMethods();
                PatchItemSlotMethods();

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cookie Oven] Patches applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Cookie Oven] Patch setup failed: " + ex);
            }
        }

        private static void PatchLabOvenMethods()
        {
            MethodInfo[] methods = typeof(LabOven).GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public   |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName || method.Name.Contains("b__"))
                    continue;

                int pc = method.GetParameters().Length;

                if (method.Name == "IsIngredientCookable" && pc == 0)
                    AddPrefix(method, nameof(IsIngredientCookable_Prefix));

                else if (method.Name == "IsReadyToStart" && pc == 0)
                    AddPrefix(method, nameof(IsReadyToStart_Prefix));

                else if (method.Name == "CanOutputSpaceFitCurrentOperation" && pc == 0)
                    AddPrefix(method, nameof(CanOutputSpaceFit_Prefix));

                else if (method.Name == "Use" && pc == 0)
                    AddPostfix(method, nameof(Use_Postfix));

                else if (method.Name == "SendCookOperation" && pc == 1)
                    AddPrefix(method, nameof(SendCookOperation_Prefix));

                else if (method.Name.StartsWith("RpcLogic___SendCookOperation", StringComparison.Ordinal) && pc == 1)
                    AddPrefix(method, nameof(SendCookOperation_Prefix));

                else if (method.Name == "CreateStationItems" && pc == 1)
                    AddPrefix(method, nameof(CreateStationItems_Prefix));
            }
        }

        public static bool IsIngredientCookable_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !CookieOvenRecipes.SlotHasCookieDough(__instance))
                return true;

            __result = true;
            return false;
        }

        public static bool IsReadyToStart_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !CookieOvenRecipes.SlotHasCookieDough(__instance))
                return true;

            __result = CookieOvenRecipes.OutputIsEmpty(__instance) &&
                       __instance.CurrentOperation == null;
            return false;
        }

        public static bool CanOutputSpaceFit_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !CookieOvenRecipes.SlotHasCookieDough(__instance))
                return true;

            __result = CookieOvenRecipes.OutputIsEmpty(__instance);
            return false;
        }

        public static void CreateStationItems_Prefix(
            LabOven __instance,
            ref int __0)
        {
            try
            {
                if (__instance == null)
                    return;

                bool isCookieBake =
                    CookieOvenRecipes.SlotHasCookieDough(__instance) ||
                    (LabOvenFinishedOutput.TryGetPendingOutput(__instance, out string pendingId) &&
                     CookieOvenRecipes.IdEquals(pendingId, CookieOvenRecipes.CookieId));

                if (!isCookieBake || __0 >= CookieOvenRecipes.CookiesProduced)
                    return;

                __0 = CookieOvenRecipes.CookiesProduced;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Oven] Tray count forced to " +
                    CookieOvenRecipes.CookiesProduced
                );
            }
            catch { }
        }

        public static void SendCookOperation_Prefix(LabOven __instance)
        {
            if (__instance == null)
                return;

            if (CookieOvenRecipes.SlotHasCookieDough(__instance) ||
                (LabOvenFinishedOutput.TryGetPendingOutput(__instance, out string existing) &&
                 CookieOvenRecipes.IdEquals(existing, CookieOvenRecipes.CookieId)))
            {
                LabOvenFinishedOutput.MarkPending(__instance, CookieOvenRecipes.CookieId);
            }
        }

        public static void Use_Postfix(LabOven __instance)
        {
            try
            {
                if (__instance == null                                   ||
                    !CookieOvenRecipes.SlotHasCookieDough(__instance)   ||
                    __instance.CurrentOperation != null                  ||
                    !CookieOvenRecipes.OutputIsEmpty(__instance))
                {
                    return;
                }

                int ovenId = __instance.GetInstanceID();
                if (!PendingAutoStarts.Add(ovenId))
                    return;

                MelonCoroutines.Start(AutoStartCookieBake(__instance, ovenId));
            }
            catch (Exception)
            {

            }
        }

        private static IEnumerator AutoStartCookieBake(LabOven oven, int ovenId)
        {
            yield return null;
            yield return null;

            bool started = false;

            try
            {
                if (oven == null                                    ||
                    !CookieOvenRecipes.SlotHasCookieDough(oven)    ||
                    oven.CurrentOperation != null                   ||
                    !CookieOvenRecipes.OutputIsEmpty(oven))
                {
                    yield break;
                }

                if (S1API.Items.ItemManager.GetDefinition(OperationIngredientId) == null)
                {

                    yield break;
                }

                OvenCookOperation operation = new OvenCookOperation(
                    OperationIngredientId,
                    NativeEQuality.Standard,
                    CookieOvenRecipes.CookiesProduced,
                    CookieOvenRecipes.CookieId
                );

                LabOvenFinishedOutput.MarkPending(oven, CookieOvenRecipes.CookieId);

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cookie Oven] Auto-starting cookie bake.");

                oven.SendCookOperation(operation);
                ConsumeCookieDough(oven);

                started = true;
            }
            catch (Exception)
            {

            }
            finally
            {
                PendingAutoStarts.Remove(ovenId);
            }

            if (started)
            {
                yield return null;
                yield return new WaitForSeconds(0.10f);
                SendEscapeKey();
            }
        }

        private static bool ConsumeCookieDough(LabOven oven)
        {
            try
            {
                if (oven?.IngredientSlot?.ItemInstance == null)
                    return false;

                if (!CookieOvenRecipes.IdEquals(
                        oven.IngredientSlot.ItemInstance.Definition?.ID,
                        CookieOvenRecipes.UnbakedCookieDoughId))
                {
                    return false;
                }

                int slotIndex = CookieOvenRecipes.FindSlotIndex(oven, oven.IngredientSlot);
                int remaining = Math.Max(0, oven.IngredientSlot.Quantity - CookieOvenRecipes.MixRequired);

                if (slotIndex >= 0)
                {
                    oven.SetItemSlotQuantity(slotIndex, remaining);
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Cookie Oven] Consumed 1 Unbaked Cookie Dough. Remaining=" + remaining
                    );
                    return true;
                }

                oven.IngredientSlot.SetStoredItem(null, true);
                try { oven.IngredientSlot.ReplicateStoredInstance(); } catch { }

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        private static void PatchItemSlotMethods()
        {
            MethodInfo[] methods = typeof(NativeItemSlot).GetMethods(
                BindingFlags.Instance  |
                BindingFlags.Public    |
                BindingFlags.NonPublic
            );

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName)
                    continue;

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    AddPrefix(method, nameof(ItemFilter_Prefix));
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    AddPrefix(method, nameof(Capacity_Prefix));
                }
                else if (method.Name == "SetStoredItem")
                {
                    AddPrefix(method, nameof(SetStoredItem_Prefix));
                }
            }
        }

        public static bool ItemFilter_Prefix(
            NativeItemSlot __instance,
            NativeItemInstance item,
            ref bool __result)
        {
            try
            {
                if (__instance == null || item == null)
                    return true;

                LabOven oven = GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!CookieOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        CookieOvenRecipes.UnbakedCookieDoughId))
                {
                    return true;
                }

                __result = true;
                return false;
            }
            catch { return true; }
        }

        public static bool Capacity_Prefix(
            NativeItemSlot __instance,
            NativeItemInstance item,
            bool checkPlayerFilters,
            ref int __result)
        {
            try
            {
                if (__instance == null || item == null)
                    return true;

                LabOven oven = GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!CookieOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        CookieOvenRecipes.UnbakedCookieDoughId))
                {
                    return true;
                }

                __result = Math.Max(0, 20 - __instance.Quantity);
                return false;
            }
            catch { return true; }
        }

        public static bool SetStoredItem_Prefix(
            NativeItemSlot __instance,
            ref NativeItemInstance __0)
        {
            try
            {
                if (__instance == null || __0 == null)
                    return true;

                LabOven oven = GetOwningOven(__instance);

                if (oven?.OutputSlot == null ||
                    oven.OutputSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!LabOvenFinishedOutput.TryGetPendingOutput(oven, out string outputItemId))
                    return true;

                if (!CookieOvenRecipes.IdEquals(outputItemId, CookieOvenRecipes.CookieId))
                    return true;

                if (!LabOvenFinishedOutput.TryCreateOutputInstance(
                        outputItemId,
                        CookieOvenRecipes.CookiesProduced,
                        out NativeItemInstance cookies) ||
                    cookies == null)
                {

                    return true;
                }

                __0 = cookies;

                LabOvenFinishedOutput.ClearPending(oven);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Oven] Output redirected to THC Cookie x" +
                    CookieOvenRecipes.CookiesProduced
                );

                return true;
            }
            catch (Exception)
            {

                return true;
            }
        }

        private static void AddPrefix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(typeof(CookieOvenPatch), handler)
            );

            PatchedMethods.Add(method);
        }

        private static void AddPostfix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                postfix: new HarmonyLib.HarmonyMethod(typeof(CookieOvenPatch), handler)
            );

            PatchedMethods.Add(method);
        }

        private static LabOven GetOwningOven(NativeItemSlot slot)
        {
            try
            {
                if (slot?.SlotOwner == null)
                    return null;

                Il2CppObjectBase owner = slot.SlotOwner as Il2CppObjectBase;
                return owner?.TryCast<LabOven>();
            }
            catch { return null; }
        }

        private static void SendEscapeKey()
        {
            try
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null)
                    return;

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            catch { }
        }
    }
}
