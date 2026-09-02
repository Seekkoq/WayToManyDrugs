using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

using NativeLabOven = Il2CppScheduleOne.ObjectScripts.LabOven;
using NativeOvenOperation = Il2CppScheduleOne.ObjectScripts.OvenCookOperation;
using NativeItemSlot = Il2CppScheduleOne.ItemFramework.ItemSlot;
using NativeItemInstance = Il2CppScheduleOne.ItemFramework.ItemInstance;
using NativeStorableDefinition =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;
using NativeQualityItemInstance =
    Il2CppScheduleOne.ItemFramework.QualityItemInstance;
using NativeEQuality = Il2CppScheduleOne.ItemFramework.EQuality;

namespace CustomNPCExample.Products
{
    public static class DMTLabOvenPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony("westvilleconnection.dmt.laboven");

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> PendingAutoStarts =
            new HashSet<int>();

        private static readonly Dictionary<int, NativeEQuality>
            PendingQualities =
                new Dictionary<int, NativeEQuality>();

        private static bool _applied;

        private const string OperationIngredientId = "cocainebase";

        // Inventory yield
        private const int DmtOutputQuantity = 15;

        // Visual tray shards only. 15 crashes LabOven.Shatter.
        private const int DmtVisualShardQuantity = 1;

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
                    "[WVC DMT Oven] Patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC DMT Oven] Patch setup failed: " + ex
                );
            }
        }

        private static void PatchLabOvenMethods()
        {
            MethodInfo[] methods =
                typeof(NativeLabOven).GetMethods(
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

                int args = method.GetParameters().Length;

                if (method.Name == "IsIngredientCookable" && args == 0)
                    AddPrefix(method, nameof(IsIngredientCookable_Prefix));
                else if (method.Name == "CreateStationItems" && args == 1)
                    AddPrefix(method, nameof(CreateStationItems_Prefix));
                else if (method.Name == "IsReadyToStart" && args == 0)
                    AddPrefix(method, nameof(IsReadyToStart_Prefix));
                else if (method.Name == "CanOutputSpaceFitCurrentOperation" &&
                         args == 0)
                    AddPrefix(method, nameof(CanOutputSpaceFit_Prefix));
                else if (method.Name == "Use" && args == 0)
                    AddPostfix(method, nameof(Use_Postfix));
                else if (method.Name == "SendCookOperation" && args == 1)
                    AddPrefix(method, nameof(SendCookOperation_Prefix));
                else if (method.Name.StartsWith(
                             "RpcLogic___SendCookOperation",
                             StringComparison.Ordinal) &&
                         args == 1)
                    AddPrefix(method, nameof(SendCookOperation_Prefix));
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
                    Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(
                            typeof(DMTLabOvenPatch),
                            nameof(SetStoredItem_Prefix)
                        )
                    );
                }
            }
        }

        private static bool SlotHasExtract(NativeLabOven oven)
        {
            try
            {
                return DMTIntermediates.IsCrudeExtractId(
                    oven?.IngredientSlot
                        ?.ItemInstance
                        ?.Definition
                        ?.ID
                );
            }
            catch
            {
                return false;
            }
        }

        private static bool OutputIsEmpty(NativeLabOven oven)
        {
            try
            {
                return oven?.OutputSlot == null ||
                       oven.OutputSlot.ItemInstance == null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDmtOven(NativeLabOven oven)
        {
            try
            {
                if (oven == null)
                    return false;

                if (PendingQualities.ContainsKey(oven.GetInstanceID()))
                    return true;

                if (SlotHasExtract(oven))
                    return true;

                NativeOvenOperation operation = oven.CurrentOperation;

                return operation != null &&
                       string.Equals(
                           operation.ProductID,
                           DMT.ProductId,
                           StringComparison.OrdinalIgnoreCase
                       );
            }
            catch
            {
                return false;
            }
        }

        private static NativeEQuality ReadExtractQuality(
            NativeLabOven oven)
        {
            try
            {
                string itemId =
                    oven?.IngredientSlot
                        ?.ItemInstance
                        ?.Definition
                        ?.ID;

                if (string.Equals(
                        itemId,
                        DMTIntermediates.PremiumCrudeExtractId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return NativeEQuality.Premium;
                }
            }
            catch
            {
            }

            return NativeEQuality.Standard;
        }

        private static void RememberQuality(
            NativeLabOven oven)
        {
            if (oven == null || !SlotHasExtract(oven))
                return;

            NativeEQuality quality = ReadExtractQuality(oven);

            PendingQualities[oven.GetInstanceID()] = quality;

            MelonLogger.Msg(
                "[WVC DMT Oven] Pending quality=" + quality
            );
        }

        public static bool IsIngredientCookable_Prefix(
            NativeLabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !SlotHasExtract(__instance))
                return true;

            __result = true;
            return false;
        }

        public static bool IsReadyToStart_Prefix(
            NativeLabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !SlotHasExtract(__instance))
                return true;

            __result =
                OutputIsEmpty(__instance) &&
                __instance.CurrentOperation == null;

            return false;
        }

        public static bool CanOutputSpaceFit_Prefix(
            NativeLabOven __instance,
            ref bool __result)
        {
            if (__instance == null || !SlotHasExtract(__instance))
                return true;

            __result = OutputIsEmpty(__instance);
            return false;
        }

        public static void CreateStationItems_Prefix(
            NativeLabOven __instance,
            ref int __0)
        {
            if (__instance == null || !IsDmtOven(__instance))
                return;

            __0 = DmtVisualShardQuantity;
        }

        public static void SendCookOperation_Prefix(
            NativeLabOven __instance)
        {
            RememberQuality(__instance);
        }

        public static void Use_Postfix(NativeLabOven __instance)
        {
            try
            {
                if (__instance == null ||
                    !SlotHasExtract(__instance) ||
                    __instance.CurrentOperation != null ||
                    !OutputIsEmpty(__instance))
                {
                    return;
                }

                int ovenId = __instance.GetInstanceID();

                if (!PendingAutoStarts.Add(ovenId))
                    return;

                MelonCoroutines.Start(
                    AutoStartCrystallization(__instance, ovenId)
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Oven] Use postfix failed: " + ex.Message
                );
            }
        }

        private static IEnumerator AutoStartCrystallization(
            NativeLabOven oven,
            int ovenId)
        {
            yield return null;
            yield return null;

            bool started = false;

            try
            {
                if (oven == null ||
                    !SlotHasExtract(oven) ||
                    oven.CurrentOperation != null ||
                    !OutputIsEmpty(oven))
                {
                    yield break;
                }

                if (S1API.Items.ItemManager.GetDefinition(
                        OperationIngredientId) == null)
                {
                    yield break;
                }

                NativeEQuality quality = ReadExtractQuality(oven);

                PendingQualities[ovenId] = quality;

                NativeOvenOperation operation =
                    new NativeOvenOperation(
                        OperationIngredientId,
                        quality,
                        DmtOutputQuantity,
                        DMT.ProductId
                    );

                MelonLogger.Msg(
                    "[WVC DMT Oven] Starting crystallization. Quality=" +
                    quality
                );

                oven.SendCookOperation(operation);
                ConsumeExtract(oven);

                started = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Oven] Auto-start failed: " + ex.Message
                );
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

        private static void ConsumeExtract(NativeLabOven oven)
        {
            try
            {
                if (oven?.IngredientSlot?.ItemInstance == null)
                    return;

                if (!DMTIntermediates.IsCrudeExtractId(
                        oven.IngredientSlot.ItemInstance.Definition?.ID))
                {
                    return;
                }

                int slotIndex = FindIngredientSlotIndex(oven);
                int remaining =
                    Math.Max(0, oven.IngredientSlot.Quantity - 1);

                if (slotIndex >= 0)
                {
                    oven.SetItemSlotQuantity(slotIndex, remaining);
                    return;
                }

                oven.IngredientSlot.SetStoredItem(null, true);

                try { oven.IngredientSlot.ReplicateStoredInstance(); }
                catch { }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Oven] Consume failed: " + ex.Message
                );
            }
        }

        private static int FindIngredientSlotIndex(NativeLabOven oven)
        {
            try
            {
                if (oven?.ItemSlots == null ||
                    oven.IngredientSlot == null)
                {
                    return 0;
                }

                for (int i = 0; i < oven.ItemSlots.Count; i++)
                {
                    NativeItemSlot slot = oven.ItemSlots[i];

                    if (slot != null &&
                        slot.Pointer == oven.IngredientSlot.Pointer)
                    {
                        return i;
                    }
                }
            }
            catch { }

            return 0;
        }

        private static void SendEscapeKey()
        {
            try
            {
                Keyboard keyboard = Keyboard.current;

                if (keyboard == null)
                    return;

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
            }
            catch { }
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

                NativeLabOven oven = GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!DMTIntermediates.IsCrudeExtractId(item.Definition?.ID))
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
            NativeItemSlot __instance,
            NativeItemInstance item,
            bool checkPlayerFilters,
            ref int __result)
        {
            try
            {
                if (__instance == null || item == null)
                    return true;

                NativeLabOven oven = GetOwningOven(__instance);

                if (oven?.IngredientSlot == null ||
                    oven.IngredientSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                if (!DMTIntermediates.IsCrudeExtractId(item.Definition?.ID))
                    return true;

                __result = Math.Max(0, 20 - __instance.Quantity);
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
                if (__instance == null || __0 == null)
                    return true;

                NativeLabOven oven = GetOwningOven(__instance);

                if (oven?.OutputSlot == null ||
                    oven.OutputSlot.Pointer != __instance.Pointer)
                {
                    return true;
                }

                int ovenId = oven.GetInstanceID();

                NativeEQuality quality;

                if (!PendingQualities.TryGetValue(ovenId, out quality))
                    return true;

                NativeItemInstance dmt;

                if (!TryCreateDmt(DmtOutputQuantity, quality, out dmt) ||
                    dmt == null)
                {
                    MelonLogger.Warning(
                        "[WVC DMT Oven] Could not create DMT output."
                    );

                    return true;
                }

                __0 = dmt;
                PendingQualities.Remove(ovenId);

                MelonLogger.Msg(
                    "[WVC DMT Oven] Output DMT x" +
                    DmtOutputQuantity +
                    " quality=" + quality
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Oven] Output swap failed: " + ex.Message
                );

                return true;
            }
        }

        private static bool TryCreateDmt(
            int quantity,
            NativeEQuality quality,
            out NativeItemInstance instance)
        {
            instance = null;

            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(DMT.ProductId);

                if (wrapper == null)
                    return false;

                object raw =
                    GetMember(wrapper, "S1ItemDefinition")
                    ?? GetMember(wrapper, "S1ProductDefinition");

                NativeStorableDefinition definition =
                    raw as NativeStorableDefinition;

                if (definition == null)
                {
                    Il2CppObjectBase native = raw as Il2CppObjectBase;
                    definition = native?.TryCast<NativeStorableDefinition>();
                }

                if (definition == null)
                    return false;

                instance = definition.GetDefaultInstance(quantity);

                if (instance == null)
                    return false;

                if (!ApplyQuality(instance, quality))
                {
                    MelonLogger.Warning(
                        "[WVC DMT Oven] Created DMT but quality apply failed. " +
                        "Instance type=" + instance.GetType().FullName
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Oven] TryCreateDmt failed: " + ex.Message
                );

                return false;
            }
        }

        private static bool ApplyQuality(
            NativeItemInstance instance,
            NativeEQuality quality)
        {
            if (instance == null)
                return false;

            try
            {
                NativeQualityItemInstance qualityItem =
                    instance.TryCast<NativeQualityItemInstance>();

                if (qualityItem == null)
                    return false;

                /*
                 * Native QualityItemInstance.SetQuality(EQuality, bool)
                 * is the same path the game uses for product stacks.
                 */
                try
                {
                    qualityItem.SetQuality(quality);
                    return true;
                }
                catch
                {
                }

                try
                {
                    qualityItem.Quality = quality;
                    return true;
                }
                catch
                {
                }
            }
            catch
            {
            }

            return false;
        }

        private static NativeLabOven GetOwningOven(NativeItemSlot slot)
        {
            try
            {
                if (slot?.SlotOwner == null)
                    return null;

                Il2CppObjectBase owner =
                    slot.SlotOwner as Il2CppObjectBase;

                return owner?.TryCast<NativeLabOven>();
            }
            catch
            {
                return null;
            }
        }

        private static void AddPrefix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(DMTLabOvenPatch), handler)
            );

            PatchedMethods.Add(method);
        }

        private static void AddPostfix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(DMTLabOvenPatch), handler)
            );

            PatchedMethods.Add(method);
        }

        private static object GetMember(object target, string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null)
                        return property.GetValue(target);

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

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }
    }
}