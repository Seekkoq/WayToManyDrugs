using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.InputSystem.LowLevel;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

using NativeEQuality =
    Il2CppScheduleOne.ItemFramework.EQuality;

using NativeItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

using NativeItemSlot =
    Il2CppScheduleOne.ItemFramework.ItemSlot;

namespace CustomNPCExample.Products.Ovens
{
    /*
     * 1x Unbaked Brownie Mix
     * -> 10x Brownie
     *
     * Uses the native Lab Oven's cooking timer, lights, sounds,
     * tray, hammer animation, and output handling.
     */
    public static class BrownieOvenPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony(
                "westvilleconnection.brownieoven"
            );

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> PendingAutoStarts =
            new HashSet<int>();

        private static bool _applied;

        /*
         * This is only used to construct the native OvenCookOperation.
         * The resulting output is intercepted and replaced with Brownies.
         */
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

                MelonLogger.Msg(
                    "[WVC Brownie Oven] Patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Brownie Oven] Patch setup failed: " +
                    ex
                );
            }
        }

        // ============================================================
        // Native Lab Oven patches
        // ============================================================

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
                else if (method.Name == "IsReadyToStart" &&
                         parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(IsReadyToStart_Prefix)
                    );
                }
                else if (method.Name ==
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
                else if (method.Name == "CreateStationItems" &&
                         parameterCount == 1)
                {
                    AddPrefix(
                        method,
                        nameof(CreateStationItems_Prefix)
                    );
                }
            }
        }

        // ============================================================
        // Oven readiness
        // ============================================================

        public static bool IsIngredientCookable_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null ||
                !BrownieOvenRecipes.SlotHasBrownieMix(__instance))
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
                !BrownieOvenRecipes.SlotHasBrownieMix(__instance))
            {
                return true;
            }

            __result =
                BrownieOvenRecipes.OutputIsEmpty(__instance) &&
                __instance.CurrentOperation == null;

            return false;
        }

        public static bool CanOutputSpaceFit_Prefix(
            LabOven __instance,
            ref bool __result)
        {
            if (__instance == null ||
                !BrownieOvenRecipes.SlotHasBrownieMix(__instance))
            {
                return true;
            }

            __result =
                BrownieOvenRecipes.OutputIsEmpty(__instance);

            return false;
        }

        // ============================================================
        // Native visual tray count
        // ============================================================

        /*
         * The native operation may think it produces one item.
         * Make the tray / broken pieces visually represent ten brownies.
         */
        public static void CreateStationItems_Prefix(
            LabOven __instance,
            ref int __0)
        {
            try
            {
                if (__instance == null)
                    return;

                bool isBrownieBake =
                    BrownieOvenRecipes.SlotHasBrownieMix(__instance) ||
                    LabOvenFinishedOutput.TryGetPendingOutput(
                        __instance,
                        out string pendingId
                    ) &&
                    BrownieOvenRecipes.IdEquals(
                        pendingId,
                        BrownieOvenRecipes.BrownieId
                    );

                if (!isBrownieBake)
                    return;

                if (__0 >= BrownieOvenRecipes.BrowniesProduced)
                    return;

                __0 =
                    BrownieOvenRecipes.BrowniesProduced;

                MelonLogger.Msg(
                    "[WVC Brownie Oven] Tray count forced to " +
                    BrownieOvenRecipes.BrowniesProduced
                );
            }
            catch
            {
            }
        }

        // ============================================================
        // Start / auto-start
        // ============================================================

        public static void SendCookOperation_Prefix(
            LabOven __instance)
        {
            if (__instance == null)
                return;

            if (BrownieOvenRecipes.SlotHasBrownieMix(__instance) ||
                LabOvenFinishedOutput.TryGetPendingOutput(
                    __instance,
                    out string existing
                ) &&
                BrownieOvenRecipes.IdEquals(
                    existing,
                    BrownieOvenRecipes.BrownieId
                ))
            {
                LabOvenFinishedOutput.MarkPending(
                    __instance,
                    BrownieOvenRecipes.BrownieId
                );
            }
        }

        public static void Use_Postfix(
            LabOven __instance)
        {
            try
            {
                if (__instance == null ||
                    !BrownieOvenRecipes.SlotHasBrownieMix(__instance) ||
                    __instance.CurrentOperation != null ||
                    !BrownieOvenRecipes.OutputIsEmpty(__instance))
                {
                    return;
                }

                int ovenId =
                    __instance.GetInstanceID();

                if (!PendingAutoStarts.Add(ovenId))
                    return;

                MelonCoroutines.Start(
                    AutoStartBrownieBake(
                        __instance,
                        ovenId
                    )
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Brownie Oven] Use postfix failed: " +
                    ex.Message
                );
            }
        }

        private static IEnumerator AutoStartBrownieBake(
            LabOven oven,
            int ovenId)
        {
            yield return null;
            yield return null;

            bool started = false;

            try
            {
                if (oven == null ||
                    !BrownieOvenRecipes.SlotHasBrownieMix(oven) ||
                    oven.CurrentOperation != null ||
                    !BrownieOvenRecipes.OutputIsEmpty(oven))
                {
                    yield break;
                }

                if (S1API.Items.ItemManager.GetDefinition(
                        OperationIngredientId
                    ) == null)
                {
                    MelonLogger.Warning(
                        "[WVC Brownie Oven] Missing operation ingredient: " +
                        OperationIngredientId
                    );

                    yield break;
                }

                /*
                 * Native oven operation:
                 * ingredient ID is only a donor/proxy.
                 * Product ID is Brownie, so visuals and output replacement
                 * know this is a brownie bake.
                 */
                OvenCookOperation operation =
                    new OvenCookOperation(
                        OperationIngredientId,
                        NativeEQuality.Standard,
                        BrownieOvenRecipes.BrowniesProduced,
                        BrownieOvenRecipes.BrownieId
                    );

                LabOvenFinishedOutput.MarkPending(
                    oven,
                    BrownieOvenRecipes.BrownieId
                );

                MelonLogger.Msg(
                    "[WVC Brownie Oven] Auto-starting brownie bake."
                );

                oven.SendCookOperation(operation);

                ConsumeBrownieMix(oven);

                started = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Brownie Oven] Auto-start failed: " +
                    ex.Message
                );
            }
            finally
            {
                PendingAutoStarts.Remove(ovenId);
            }

            /*
             * Same behavior as your gummy oven patch:
             * releases/escapes from the interactive oven interface.
             */
            if (started)
            {
                yield return null;
                yield return new WaitForSeconds(0.10f);

                SendEscapeKey();
            }
        }

        // ============================================================
        // Ingredient consumption
        // ============================================================

        private static bool ConsumeBrownieMix(
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

                if (!BrownieOvenRecipes.IdEquals(
                        oven.IngredientSlot.ItemInstance.Definition?.ID,
                        BrownieOvenRecipes.UnbakedBrownieMixId
                    ))
                {
                    return false;
                }

                int slotIndex =
                    BrownieOvenRecipes.FindSlotIndex(
                        oven,
                        oven.IngredientSlot
                    );

                int remaining =
                    Math.Max(
                        0,
                        oven.IngredientSlot.Quantity -
                        BrownieOvenRecipes.MixRequired
                    );

                if (slotIndex >= 0)
                {
                    oven.SetItemSlotQuantity(
                        slotIndex,
                        remaining
                    );

                    MelonLogger.Msg(
                        "[WVC Brownie Oven] Consumed 1 Unbaked Brownie Mix. " +
                        "Remaining=" + remaining
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

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Brownie Oven] Failed to consume mix: " +
                    ex.Message
                );

                return false;
            }
        }

        // ============================================================
        // Input / output ItemSlot interception
        // ============================================================

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
                        nameof(ItemFilter_Prefix)
                    );
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    AddPrefix(
                        method,
                        nameof(Capacity_Prefix)
                    );
                }
                else if (method.Name == "SetStoredItem")
                {
                    AddPrefix(
                        method,
                        nameof(SetStoredItem_Prefix)
                    );
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
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!BrownieOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        BrownieOvenRecipes.UnbakedBrownieMixId
                    ))
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
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!BrownieOvenRecipes.IdEquals(
                        item.Definition?.ID,
                        BrownieOvenRecipes.UnbakedBrownieMixId
                    ))
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

        /*
         * Replaces the native/proxy output with 10 real Brownie items.
         */
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
                    oven.OutputSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!LabOvenFinishedOutput.TryGetPendingOutput(
                        oven,
                        out string outputItemId
                    ))
                {
                    return true;
                }

                if (!BrownieOvenRecipes.IdEquals(
                        outputItemId,
                        BrownieOvenRecipes.BrownieId
                    ))
                {
                    return true;
                }

                if (!LabOvenFinishedOutput.TryCreateOutputInstance(
                        outputItemId,
                        BrownieOvenRecipes.BrowniesProduced,
                        out NativeItemInstance brownies
                    ) ||
                    brownies == null)
                {
                    MelonLogger.Warning(
                        "[WVC Brownie Oven] Could not create Brownie output."
                    );

                    return true;
                }

                __0 = brownies;

                LabOvenFinishedOutput.ClearPending(
                    oven
                );

                MelonLogger.Msg(
                    "[WVC Brownie Oven] Output redirected to Brownie x" +
                    BrownieOvenRecipes.BrowniesProduced
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Brownie Oven] Output replacement failed: " +
                    ex.Message
                );

                return true;
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static void AddPrefix(
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
                prefix: new HarmonyLib.HarmonyMethod(
                    typeof(BrownieOvenPatch),
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
                    typeof(BrownieOvenPatch),
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

        private static void SendEscapeKey()
        {
            try
            {
                Keyboard keyboard =
                    Keyboard.current;

                if (keyboard == null)
                    return;

                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState(
                        Key.Escape
                    )
                );

                InputSystem.Update();

                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState()
                );

                InputSystem.Update();
            }
            catch
            {
            }
        }
    }
}