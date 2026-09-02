using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Chemistry
{
    public static class ChemistryStationPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony(
                "westvilleconnection.chemistry"
            );

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        /*
         * Tracks stations that have detected a complete brownie recipe
         * and are counting down to auto-produce the mix.
         */
        private static readonly Dictionary<int, float> PendingCookTimes =
            new Dictionary<int, float>();

        private const float CookDelay = 4.0f;

        private static float _scanTimer;
        private static bool _applied;
        private static bool _updateConfirmed;

        // ============================================================
        // Setup
        // ============================================================

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                // Auto-cook only needs the slot filters so the custom
                // ingredients can be placed into the input slots.
                PatchSlotMethods();

                _applied = true;

                MelonLogger.Msg(
                    "[WVC Chemistry] Auto-cook patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Chemistry] Patch setup failed: " + ex
                );
            }
        }

        private static void PatchSlotMethods()
        {
            MethodInfo[] methods =
                typeof(ItemSlot).GetMethods(
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
            }
        }

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
                    typeof(ChemistryStationPatch),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        // ============================================================
        // Update loop (call from MainMod.OnUpdate)
        // ============================================================

        public static void Update()
        {
            if (!_applied)
                return;

            if (!_updateConfirmed)
            {
                _updateConfirmed = true;

                MelonLogger.Msg(
                    "[WVC Chemistry] Auto-cook Update is running."
                );
            }

            _scanTimer += Time.deltaTime;

            if (_scanTimer < 0.5f)
                return;

            _scanTimer = 0f;

            try
            {
                ChemistryStation[] stations =
                    UnityEngine.Object.FindObjectsOfType<ChemistryStation>();

                if (stations == null)
                    return;

                for (int i = 0; i < stations.Length; i++)
                {
                    TryAutoCook(stations[i]);
                }
            }
            catch
            {
            }
        }

        private static void TryAutoCook(
            ChemistryStation station)
        {
            if (station == null)
                return;

            int id = station.GetInstanceID();

            try
            {
                BrownieChemistryRecipe.State state =
                    BrownieChemistryRecipe.Read(station);

                if (!state.IsComplete)
                {
                    PendingCookTimes.Remove(id);
                    return;
                }

                if (station.OutputSlot == null ||
                    station.OutputSlot.ItemInstance != null)
                {
                    PendingCookTimes.Remove(id);
                    return;
                }

                if (!PendingCookTimes.TryGetValue(
                        id,
                        out float cookAt))
                {
                    PendingCookTimes[id] =
                        Time.time + CookDelay;

                    MelonLogger.Msg(
                        "[WVC Chemistry] Brownie ingredients detected. " +
                        "Auto-cooking in " + CookDelay + "s..."
                    );

                    return;
                }

                if (Time.time < cookAt)
                    return;

                Produce(station);

                PendingCookTimes.Remove(id);
            }
            catch (Exception ex)
            {
                PendingCookTimes.Remove(id);

                MelonLogger.Warning(
                    "[WVC Chemistry] Auto-cook failed: " +
                    ex.Message
                );
            }
        }

        private static void Produce(
            ChemistryStation station)
        {
            ItemInstance output =
                CreateBrownieMixInstance(
                    BrownieChemistryRecipe.OutputBatchSize
                );

            if (output == null)
            {
                MelonLogger.Warning(
                    "[WVC Chemistry] Could not create Unbaked Brownie Mix."
                );

                return;
            }

            BrownieChemistryRecipe.Consume(station);

            bool placed =
                SetOutputSlotItem(
                    station.OutputSlot,
                    output
                );

            if (!placed)
            {
                MelonLogger.Warning(
                    "[WVC Chemistry] Failed to place brownie mix in output slot."
                );

                return;
            }

            MelonLogger.Msg(
                "[WVC Chemistry] Produced Unbaked Brownie Mix x" +
                BrownieChemistryRecipe.OutputBatchSize
            );
        }

        private static bool SetOutputSlotItem(
            ItemSlot slot,
            ItemInstance item)
        {
            if (slot == null || item == null)
                return false;

            try
            {
                slot.SetStoredItem(item, false);
                return true;
            }
            catch
            {
            }

            try
            {
                MethodInfo[] methods =
                    slot.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "SetStoredItem")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 2)
                        continue;

                    method.Invoke(
                        slot,
                        new object[]
                        {
                            item,
                            false
                        }
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Chemistry] SetOutputSlotItem failed: " +
                    ex.Message
                );
            }

            return false;
        }

        private static ItemInstance CreateBrownieMixInstance(
            int quantity)
        {
            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(
                        UnbakedBrownieMix.ItemId
                    );

                if (wrapper == null)
                {
                    MelonLogger.Warning(
                        "[WVC Chemistry] Brownie mix definition missing."
                    );

                    return null;
                }

                object rawObject =
                    GetMember(
                        wrapper,
                        "S1ItemDefinition"
                    );

                ItemDefinition rawDefinition =
                    rawObject as ItemDefinition;

                if (rawDefinition == null)
                {
                    MelonLogger.Warning(
                        "[WVC Chemistry] Raw brownie mix definition missing."
                    );

                    return null;
                }

                try
                {
                    return rawDefinition.GetDefaultInstance(
                        quantity
                    );
                }
                catch
                {
                }

                MethodInfo[] methods =
                    rawDefinition.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "GetDefaultInstance")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1)
                        continue;

                    object result =
                        method.Invoke(
                            rawDefinition,
                            new object[]
                            {
                                quantity
                            }
                        );

                    ItemInstance item =
                        result as ItemInstance;

                    if (item != null)
                        return item;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Chemistry] Instance creation failed: " +
                    ex.Message
                );

                return null;
            }
        }

        // ============================================================
        // Slot filters
        // ============================================================

        public static bool ItemFilter_Prefix(
            ItemSlot __instance,
            ItemInstance item,
            ref bool __result)
        {
            try
            {
                if (__instance == null ||
                    item == null)
                {
                    return true;
                }

                if (!IsChemistryInputSlot(__instance))
                    return true;

                if (!BrownieChemistryRecipe.IsBrownieIngredient(item))
                    return true;

                __result = true;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool Capacity_Prefix(
            ItemSlot __instance,
            ItemInstance item,
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

                if (!IsChemistryInputSlot(__instance))
                    return true;

                if (!BrownieChemistryRecipe.IsBrownieIngredient(item))
                    return true;

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

        private static bool IsChemistryInputSlot(
            ItemSlot slot)
        {
            try
            {
                if (slot == null ||
                    slot.SlotOwner == null)
                {
                    return false;
                }

                Il2CppObjectBase ownerBase =
                    slot.SlotOwner as Il2CppObjectBase;

                ChemistryStation station =
                    ownerBase?.TryCast<ChemistryStation>();

                if (station == null)
                    return false;

                // Never touch the output slot.
                if (station.OutputSlot != null &&
                    station.OutputSlot.Pointer == slot.Pointer)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // Reflection
        // ============================================================

        private static object GetMember(
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
                    PropertyInfo p =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (p != null)
                        return p.GetValue(target);

                    FieldInfo f =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (f != null)
                        return f.GetValue(target);
                }
                catch
                {
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}