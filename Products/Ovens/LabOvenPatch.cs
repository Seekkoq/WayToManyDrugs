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

using NativeItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

using NativeItemSlot =
    Il2CppScheduleOne.ItemFramework.ItemSlot;

using NativeEQuality =
    Il2CppScheduleOne.ItemFramework.EQuality;

namespace CustomNPCExample.Products.Ovens
{
    public static class LabOvenPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony(
                "westvilleconnection.laboven"
            );

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> PendingAutoStarts =
            new HashSet<int>();

        private static bool _applied;

        private const string OperationIngredientId =
            "cocainebase";

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                PatchLabOvenMethods();
                PatchItemSlotMethods();

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Oven] Patch setup failed: " +
                    ex
                );
            }
        }

        private static void PatchLabOvenMethods()
        {
            MethodInfo[] methods =
                typeof(LabOven).GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

            foreach (MethodInfo method in methods)
            {
                if (method == null ||
                    method.IsSpecialName ||
                    method.Name.Contains("b__"))
                {
                    continue;
                }

                int parameterCount =
                    method.GetParameters().Length;

                if (method.Name == "IsIngredientCookable" &&
                    parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(IsIngredientCookable_Prefix)
                    );
                }

                else if (method.Name == "CreateStationItems" &&
         parameterCount == 1)
                {
                    AddPrefix(
                        method,
                        nameof(CreateStationItems_Prefix)
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Oven] Patched CreateStationItems."
                    );
                }
                else if (method.Name == "IsReadyToStart" &&
                         parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(IsReadyToStart_Prefix)
                    );
                }
                else if (
                    method.Name ==
                        "CanOutputSpaceFitCurrentOperation" &&
                    parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(CanOutputSpaceFit_Prefix)
                    );
                }
                else if (method.Name == "Use" &&
                         parameterCount == 0)
                {
                    AddPostfix(
                        method,
                        nameof(Use_Postfix)
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Oven] Hooked auto-start on Use."
                    );
                }
                else if (method.Name == "SendCookOperation" &&
                         parameterCount == 1)
                {
                    AddPrefix(
                        method,
                        nameof(SendCookOperation_Prefix)
                    );
                }
                else if (
                    method.Name.StartsWith(
                        "RpcLogic___SendCookOperation",
                        StringComparison.Ordinal
                    ) &&
                    parameterCount == 1)
                {
                    AddPrefix(
                        method,
                        nameof(SendCookOperation_Prefix)
                    );
                }
            }
        }

        private static bool IsGummyOven(
    LabOven oven)
        {
            try
            {
                if (oven == null)
                    return false;

                if (LabOvenFinishedOutput.IsPending(oven))
                    return true;

                if (LabOvenRecipes.SlotHasGummyMix(oven))
                    return true;

                OvenCookOperation operation =
                    oven.CurrentOperation;

                return operation != null &&
                       LabOvenRecipes.IdEquals(
                           operation.ProductID,
                           LabOvenRecipes.ThcGummiesId
                       );
            }
            catch
            {
                return false;
            }
        }

        public static void CreateStationItems_Prefix(
            LabOven __instance,
            ref int __0)
        {
            try
            {
                if (__instance == null)
                    return;

                if (!IsGummyOven(__instance))
                    return;

                if (__0 >= LabOvenRecipes.GummiesProduced)
                    return;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Tray item count " +
                    __0 +
                    " -> " +
                    LabOvenRecipes.GummiesProduced
                );

                __0 = LabOvenRecipes.GummiesProduced;
            }
            catch (Exception)
            {

            }
        }

        public static bool IsIngredientCookable_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null ||
                !LabOvenRecipes.SlotHasGummyMix(__instance))
            {
                return true;
            }

            __result = true;
            return false;
        }

        public static bool IsReadyToStart_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null ||
                !LabOvenRecipes.SlotHasGummyMix(__instance))
            {
                return true;
            }

            __result =
                LabOvenRecipes.OutputIsEmpty(__instance) &&
                __instance.CurrentOperation == null;

            return false;
        }

        public static bool CanOutputSpaceFit_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null ||
                !LabOvenRecipes.SlotHasGummyMix(__instance))
            {
                return true;
            }

            __result =
                LabOvenRecipes.OutputIsEmpty(__instance);

            return false;
        }

        public static void SendCookOperation_Prefix(
            LabOven __instance)
        {
            if (__instance == null)
                return;

            if (LabOvenRecipes.SlotHasGummyMix(__instance) ||
                LabOvenFinishedOutput.TryGetPendingOutput(
                    __instance,
                    out _))
            {
                LabOvenFinishedOutput.MarkPending(
                    __instance,
                    LabOvenRecipes.ThcGummiesId
                );
            }
        }

        public static void Use_Postfix(
            LabOven __instance)
        {
            try
            {
                if (__instance == null ||
                    !LabOvenRecipes.SlotHasGummyMix(__instance) ||
                    __instance.CurrentOperation != null ||
                    !LabOvenRecipes.OutputIsEmpty(__instance))
                {
                    return;
                }

                int ovenId =
                    __instance.GetInstanceID();

                if (!PendingAutoStarts.Add(ovenId))
                    return;

                MelonCoroutines.Start(
                    AutoStartGummyBake(
                        __instance,
                        ovenId
                    )
                );
            }
            catch (Exception)
            {

            }
        }

        private static IEnumerator AutoStartGummyBake(
            LabOven oven,
            int ovenId)
        {
            yield return null;
            yield return null;

            bool started = false;

            try
            {
                if (oven == null ||
                    !LabOvenRecipes.SlotHasGummyMix(oven) ||
                    oven.CurrentOperation != null ||
                    !LabOvenRecipes.OutputIsEmpty(oven))
                {
                    yield break;
                }

                if (S1API.Items.ItemManager.GetDefinition(
                        OperationIngredientId
                    ) == null)
                {


                    yield break;
                }

                OvenCookOperation operation =
                    new OvenCookOperation(
                        OperationIngredientId,
                        NativeEQuality.Standard,
                        LabOvenRecipes.GummiesProduced,
                        LabOvenRecipes.ThcGummiesId
                    );

                LabOvenFinishedOutput.MarkPending(
                    oven,
                    LabOvenRecipes.ThcGummiesId
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Auto-starting gummy bake."
                );

                oven.SendCookOperation(operation);

                ConsumeGummyMix(oven);



                started = true;

                started = true;
            }
            catch (Exception)
            {

            }
            finally
            {
                PendingAutoStarts.Remove(ovenId);
            }

            if (!started)
                yield break;

            yield return null;
            yield return new WaitForSeconds(0.1f);

            SendEscapeKey();
        }

        private static bool ConsumeGummyMix(
    LabOven oven)
        {
            try
            {
                if (oven == null ||
                    oven.IngredientSlot == null ||
                    oven.IngredientSlot.ItemInstance == null)
                {
                    return false;
                }

                if (!LabOvenRecipes.IdEquals(
                        oven.IngredientSlot.ItemInstance.Definition?.ID,
                        LabOvenRecipes.UnbakedGummyMixId))
                {
                    return false;
                }

                int slotIndex =
                    FindIngredientSlotIndex(oven);

                int remaining =
                    Math.Max(
                        0,
                        oven.IngredientSlot.Quantity -
                        LabOvenRecipes.MixRequired
                    );

                if (slotIndex >= 0)
                {
                    oven.SetItemSlotQuantity(
                        slotIndex,
                        remaining
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Oven] Consumed 1 Unbaked Gummy Mix. Remaining=" +
                        remaining
                    );

                    return true;
                }

                oven.IngredientSlot.SetStoredItem(
                    null,
                    true
                );

                try
                {
                    oven.IngredientSlot.ReplicateStoredInstance();
                }
                catch
                {
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Consumed Unbaked Gummy Mix by clearing slot."
                );

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static int FindIngredientSlotIndex(
            LabOven oven)
        {
            try
            {
                if (oven?.ItemSlots != null &&
                    oven.IngredientSlot != null)
                {
                    for (int i = 0; i < oven.ItemSlots.Count; i++)
                    {
                        NativeItemSlot slot =
                            oven.ItemSlots[i];

                        if (slot != null &&
                            slot.Pointer == oven.IngredientSlot.Pointer)
                        {
                            return i;
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private static void SendEscapeKey()
        {
            try
            {
                Keyboard keyboard =
                    Keyboard.current;

                if (keyboard == null)
                {


                    return;
                }

                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState(Key.Escape)
                );

                InputSystem.Update();

                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState()
                );

                InputSystem.Update();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Sent simulated Escape to close oven UI."
                );
            }
            catch (Exception)
            {

            }
        }

        private static void PatchItemSlotMethods()
        {
            MethodInfo[] methods =
                typeof(NativeItemSlot).GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (MethodInfo method in methods)
            {
                if (method == null ||
                    method.IsSpecialName)
                {
                    continue;
                }

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    AddPrefix(
                        method,
                        typeof(LabOvenPatch),
                        nameof(ItemFilter_Prefix)
                    );
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    AddPrefix(
                        method,
                        typeof(LabOvenPatch),
                        nameof(Capacity_Prefix)
                    );
                }
                else if (method.Name == "SetStoredItem")
                {
                    try
                    {
                        Harmony.Patch(
                            method,
                            prefix: new HarmonyLib.HarmonyMethod(
                                typeof(LabOvenPatch),
                                nameof(SetStoredItem_Prefix)
                            )
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Oven] Patched ItemSlot.SetStoredItem."
                        );
                    }
                    catch (Exception)
                    {

                    }
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
                if (__instance == null ||
                    item == null)
                {
                    return true;
                }

                LabOven oven =
                    GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer !=
                        __instance.Pointer)
                {
                    return true;
                }

                if (!LabOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        LabOvenRecipes.UnbakedGummyMixId))
                {
                    return true;
                }

                __result = true;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool Capacity_Prefix(
            NativeItemSlot __instance,
            NativeItemInstance item,
            bool checkPlayerFilters,
            ref int __result)
        {
            try
            {
                if (__instance == null ||
                    item == null)
                {
                    return true;
                }

                LabOven oven =
                    GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer !=
                        __instance.Pointer)
                {
                    return true;
                }

                if (!LabOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        LabOvenRecipes.UnbakedGummyMixId))
                {
                    return true;
                }

                __result =
                    Math.Max(
                        0,
                        20 - __instance.Quantity
                    );

                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool SetStoredItem_Prefix(
    NativeItemSlot __instance,
    ref NativeItemInstance __0)
        {
            try
            {
                if (__instance == null ||
                    __0 == null)
                {
                    return true;
                }

                LabOven oven =
                    GetOwningOven(__instance);

                if (oven?.OutputSlot == null ||
                    oven.OutputSlot.Pointer !=
                        __instance.Pointer)
                {
                    return true;
                }

                if (!LabOvenFinishedOutput.TryGetPendingOutput(
                        oven,
                        out string outputItemId))
                {
                    return true;
                }

                string incomingId =
                    __0.Definition?.ID ?? "null";

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Native output write: " +
                    incomingId
                );

                if (!LabOvenFinishedOutput.TryCreateOutputInstance(
                        outputItemId,
                        LabOvenRecipes.GummiesProduced,
                        out NativeItemInstance gummies) ||
                    gummies == null)
                {


                    return true;
                }

                __0 = gummies;

                LabOvenFinishedOutput.ClearPending(
                    oven
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven] Redirected output to: " +
                    outputItemId +
                    " x" +
                    LabOvenRecipes.GummiesProduced
                );

                return true;
            }
            catch (Exception)
            {


                return true;
            }
        }

        private static void AddPrefix(
            MethodInfo method,
            string handler)
        {
            AddPrefix(
                method,
                typeof(LabOvenPatch),
                handler
            );
        }

        private static void AddPrefix(
            MethodInfo method,
            Type handlerType,
            string handler)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(
                    handlerType,
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        private static void AddPostfix(
            MethodInfo method,
            string handler)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                postfix: new HarmonyLib.HarmonyMethod(
                    typeof(LabOvenPatch),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        private static LabOven GetOwningOven(
            NativeItemSlot slot)
        {
            try
            {
                if (slot?.SlotOwner == null)
                    return null;

                Il2CppObjectBase owner =
                    slot.SlotOwner as Il2CppObjectBase;

                return owner?.TryCast<LabOven>();
            }
            catch
            {
                return null;
            }
        }
    }
}
