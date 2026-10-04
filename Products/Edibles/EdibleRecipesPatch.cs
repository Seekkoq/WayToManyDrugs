using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using S1API.Items;

using NativeItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

using NativeItemSlot =
    Il2CppScheduleOne.ItemFramework.ItemSlot;

using NativeStorableDefinition =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

using NativeProductDefinition =
    Il2CppScheduleOne.Product.ProductDefinition;

using NativeMixOperation =
    Il2CppScheduleOne.ObjectScripts.MixOperation;

namespace CustomNPCExample.Products.Edibles
{
    public static class EdibleRecipesPatch
    {
        private const string HarmonyId =
            "westvilleconnection.edibles.mixer";

        private static bool _applied;
        private static bool _errorLogged;

        private static readonly Dictionary<IntPtr, string>
            PendingOutputs =
                new Dictionary<IntPtr, string>();

        public static bool ApplyPatch()
        {
            if (_applied)
                return true;

            try
            {
                HarmonyLib.Harmony harmony =
                    new HarmonyLib.Harmony(HarmonyId);

                PatchHardFilter(harmony);
                PatchCanStartMix(harmony);
                PatchGetMixQuantity(harmony);
                PatchGetProduct(harmony);
                PatchSendMixingOperation(harmony);
                PatchMixingDone(harmony);
                PatchOnTimePass(harmony);

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Mixing Station patches applied."
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Edibles] Mixer patch failed: " + ex
                );

                return false;
            }
        }

        private static void PatchHardFilter(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(
                typeof(NativeItemSlot),
                nameof(NativeItemSlot.DoesItemMatchHardFilters)
            );

            MethodInfo postfix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(DoesItemMatchHardFilters_Postfix)
            );

            if (original == null || postfix == null)
            {

                return;
            }

            harmony.Patch(
                original,
                postfix: new HarmonyMethod(postfix)
            );
        }

        private static void PatchCanStartMix(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(
                typeof(MixingStation),
                nameof(MixingStation.CanStartMix)
            );

            MethodInfo prefix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(CanStartMix_Prefix)
            );

            if (original == null || prefix == null)
            {

                return;
            }

            harmony.Patch(
                original,
                prefix: new HarmonyMethod(prefix)
            );
        }

        private static void PatchGetMixQuantity(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(
                typeof(MixingStation),
                nameof(MixingStation.GetMixQuantity)
            );

            MethodInfo postfix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(GetMixQuantity_Postfix)
            );

            if (original == null || postfix == null)
            {

                return;
            }

            harmony.Patch(
                original,
                postfix: new HarmonyMethod(postfix)
            );
        }

        private static void PatchGetProduct(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(
                typeof(MixingStation),
                nameof(MixingStation.GetProduct)
            );

            MethodInfo postfix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(GetProduct_Postfix)
            );

            if (original == null || postfix == null)
            {

                return;
            }

            harmony.Patch(
                original,
                postfix: new HarmonyMethod(postfix)
            );
        }

        private static void PatchSendMixingOperation(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo original = AccessTools.Method(
                typeof(MixingStation),
                nameof(MixingStation.SetMixOperation)
            );

            MethodInfo prefix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(SetMixOperation_Prefix)
            );

            if (original == null || prefix == null)
            {

                return;
            }

            harmony.Patch(
                original,
                prefix: new HarmonyMethod(prefix)
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Edibles] Patched MixingStation.SetMixOperation."
            );
        }

        private static void PatchMixingDone(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo prefix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(MixingDone_Prefix)
            );

            if (prefix == null)
            {

                return;
            }

            MethodInfo baseDone = AccessTools.Method(
                typeof(MixingStation),
                nameof(MixingStation.MixingDone)
            );

            if (baseDone != null)
            {
                harmony.Patch(
                    baseDone,
                    prefix: new HarmonyMethod(prefix)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Patched MixingStation.MixingDone."
                );
            }

            MethodInfo mk2Done = AccessTools.Method(
                typeof(MixingStationMk2),
                nameof(MixingStationMk2.MixingDone)
            );

            if (mk2Done != null)
            {
                harmony.Patch(
                    mk2Done,
                    prefix: new HarmonyMethod(prefix)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Patched MixingStationMk2.MixingDone."
                );
            }
        }

        private static void PatchOnTimePass(
            HarmonyLib.Harmony harmony)
        {
            MethodInfo postfix = AccessTools.Method(
                typeof(EdibleRecipesPatch),
                nameof(OnTimePass_Postfix)
            );

            if (postfix == null)
                return;

            MethodInfo baseTick = AccessTools.Method(
                typeof(MixingStation),
                "OnTimePass"
            );

            if (baseTick != null)
            {
                harmony.Patch(
                    baseTick,
                    postfix: new HarmonyMethod(postfix)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Patched MixingStation.OnTimePass."
                );
            }

            MethodInfo mk2Tick = AccessTools.Method(
                typeof(MixingStationMk2),
                "OnTimePass"
            );

            if (mk2Tick != null)
            {
                harmony.Patch(
                    mk2Tick,
                    postfix: new HarmonyMethod(postfix)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Patched Mk2.OnTimePass."
                );
            }
        }

        private static void DoesItemMatchHardFilters_Postfix(
            NativeItemSlot __instance,
            NativeItemInstance item,
            ref bool __result)
        {
            if (__result)
                return;

            try
            {
                if (__instance == null ||
                    item == null ||
                    item.Definition == null)
                {
                    return;
                }

                string itemId = item.Definition.ID;

                bool allowedBase =
                    string.Equals(
                        itemId,
                        GummyIngredients.ThcOilId,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    string.Equals(
                        itemId,
                        GummyIntermediates.GelatinBaseId,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    string.Equals(
                        itemId,
                        THCGummies.ProductId,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (!allowedBase)
                    return;

                MixingStation station =
                    __instance.SlotOwner?.TryCast<MixingStation>();

                if (station == null ||
                    station.ProductSlot == null)
                {
                    return;
                }

                IntPtr currentPointer =
                    IL2CPP.Il2CppObjectBaseToPtr(__instance);

                IntPtr productPointer =
                    IL2CPP.Il2CppObjectBaseToPtr(
                        station.ProductSlot
                    );

                if (currentPointer != IntPtr.Zero &&
                    currentPointer == productPointer)
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                LogOnce(
                    "Hard-filter postfix failed: " + ex.Message
                );
            }
        }

        private static bool CanStartMix_Prefix(
            MixingStation __instance,
            ref bool __result)
        {
            try
            {
                string outputId;
                int quantity;

                if (!TryGetCustomMix(
                        __instance,
                        out outputId,
                        out quantity))
                {
                    return true;
                }

                __result =
                    quantity > 0 &&
                    CanOutputAccept(
                        __instance.OutputSlot,
                        outputId
                    );

                return false;
            }
            catch (Exception ex)
            {
                LogOnce(
                    "CanStartMix failed: " + ex.Message
                );

                return true;
            }
        }

        private static void GetMixQuantity_Postfix(
            MixingStation __instance,
            ref int __result)
        {
            try
            {
                if (__result > 0)
                    return;

                string outputId;
                int quantity;

                if (TryGetCustomMix(
                        __instance,
                        out outputId,
                        out quantity))
                {
                    __result = quantity;
                }
            }
            catch (Exception ex)
            {
                LogOnce(
                    "GetMixQuantity failed: " + ex.Message
                );
            }
        }

        private static void GetProduct_Postfix(
            MixingStation __instance,
            ref NativeProductDefinition __result)
        {
            try
            {
                string outputId;
                int quantity;

                if (!TryGetCustomMix(
                        __instance,
                        out outputId,
                        out quantity))
                {
                    return;
                }

                NativeProductDefinition proxy =
                    GetVanillaProxyProduct();

                if (proxy == null)
                {

                    return;
                }

                __result = proxy;

                RememberPendingOutput(
                    __instance,
                    outputId
                );
            }
            catch (Exception ex)
            {
                LogOnce(
                    "GetProduct proxy failed: " + ex.Message
                );
            }
        }

        private static NativeProductDefinition
            GetVanillaProxyProduct()
        {
            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition("cocaine");

                if (wrapper == null)
                    return null;

                object raw =
                    GetMemberValue(
                        wrapper,
                        "S1ProductDefinition"
                    );

                NativeProductDefinition product =
                    raw as NativeProductDefinition;

                if (product != null)
                    return product;

                raw =
                    GetMemberValue(
                        wrapper,
                        "S1ItemDefinition"
                    );

                return raw as NativeProductDefinition;
            }
            catch
            {
                return null;
            }
        }

        private static bool SetMixOperation_Prefix(
    MixingStation __instance,
    Il2CppFishNet.Connection.NetworkConnection conn,
    NativeMixOperation operation,
    int mixTime)
        {
            try
            {
                if (__instance == null)
                    return true;

                IntPtr key =
                    IL2CPP.Il2CppObjectBaseToPtr(__instance);

                if (IsRecentlyHandled(key))
                    return false;

                string outputId;
                int quantity;

                if (!TryGetCustomMix(
                        __instance,
                        out outputId,
                        out quantity))
                {
                    return true;
                }

                if (quantity < 1)
                    return false;

                NativeItemSlot outputSlot =
                    __instance.OutputSlot;

                if (!CanOutputAccept(outputSlot, outputId))
                {

                    return false;
                }

                NativeItemInstance output;

                if (!TryCreateInstance(
                        outputId,
                        quantity,
                        out output))
                {

                    return false;
                }

                if (!PlaceOutput(
                        outputSlot,
                        output,
                        outputId,
                        quantity))
                {

                    return false;
                }

                ConsumeSlot(__instance.ProductSlot, quantity);
                ConsumeSlot(__instance.MixerSlot, quantity);

                MarkHandled(key);
                PendingOutputs.Remove(key);

                try { __instance.CurrentMixOperation = null; } catch { }
                try { __instance.SetMixerToLowered(); } catch { }
                try { __instance.onMixDone?.Invoke(); } catch { }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Mixed " + outputId +
                    " x" + quantity
                );

                return false;
            }
            catch (Exception ex)
            {
                LogOnce(
                    "SetMixOperation instant mix failed: " + ex.Message
                );
                return true;
            }
        }

        private static void RememberPendingOutput(
            MixingStation station,
            string outputId)
        {
            if (station == null ||
                string.IsNullOrEmpty(outputId))
            {
                return;
            }

            IntPtr key =
                IL2CPP.Il2CppObjectBaseToPtr(station);

            if (key != IntPtr.Zero)
            {
                PendingOutputs[key] = outputId;
            }
        }

        private static readonly Dictionary<IntPtr, DateTime>
    RecentlyHandled =
        new Dictionary<IntPtr, DateTime>();

        private static bool IsRecentlyHandled(IntPtr key)
        {
            if (key == IntPtr.Zero)
                return false;

            DateTime until;

            if (!RecentlyHandled.TryGetValue(key, out until))
                return false;

            if (DateTime.UtcNow <= until)
                return true;

            RecentlyHandled.Remove(key);
            return false;
        }

        private static void MarkHandled(IntPtr key)
        {
            if (key == IntPtr.Zero)
                return;

            RecentlyHandled[key] =
                DateTime.UtcNow.AddSeconds(2.0);
        }

        private static void ConsumeSlot(
            NativeItemSlot slot,
            int quantity)
        {
            if (slot == null || quantity <= 0)
                return;

            int remaining = slot.Quantity - quantity;

            if (remaining <= 0)
                slot.ClearStoredInstance(false);
            else
                slot.SetQuantity(remaining, false);
        }

        private static bool MixingDone_Prefix(
            MixingStation __instance)
        {
            try
            {
                if (__instance == null)
                    return true;

                IntPtr key =
                    IL2CPP.Il2CppObjectBaseToPtr(__instance);

                string outputId = null;

                if (key != IntPtr.Zero)
                {
                    PendingOutputs.TryGetValue(
                        key,
                        out outputId
                    );

                    PendingOutputs.Remove(key);
                }

                var operation =
                    __instance.CurrentMixOperation;

                if (operation == null)
                {

                    return true;
                }

                string productId = operation.ProductID;
                string ingredientId = operation.IngredientID;
                int quantity = operation.Quantity;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Finished mix operation: " +
                    productId + " + " + ingredientId + " x" + quantity +
                    " (Pending Output: " + (outputId ?? "none") + ")"
                );

                if (string.IsNullOrEmpty(outputId))
                {
                    outputId = ResolveOutput(productId, ingredientId);
                }

                if (string.IsNullOrEmpty(outputId))
                    return true;

                if (quantity < 1)
                    quantity = 1;

                NativeItemInstance output;

                if (!TryCreateInstance(
                        outputId,
                        quantity,
                        out output))
                {

                    return true;
                }

                NativeItemSlot outputSlot =
                    __instance.OutputSlot;

                if (outputSlot == null)
                {

                    return true;
                }

                bool placed = PlaceOutput(
                    outputSlot,
                    output,
                    outputId,
                    quantity
                );

                if (!placed)
                {

                    return true;
                }

                __instance.CurrentMixOperation = null;

                try { __instance.SetMixerToLowered(); } catch { }
                try { __instance.onMixDone?.Invoke(); } catch { }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Produced " +
                    outputId + " x" + quantity
                );

                return false;
            }
            catch (Exception ex)
            {
                LogOnce(
                    "MixingDone failed: " + ex
                );

                return true;
            }
        }

        private static void OnTimePass_Postfix(
            MixingStation __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                IntPtr key =
                    IL2CPP.Il2CppObjectBaseToPtr(__instance);

                if (key == IntPtr.Zero)
                    return;

                string outputId;
                if (!PendingOutputs.TryGetValue(key, out outputId))
                    return;

                var operation = __instance.CurrentMixOperation;

                if (operation == null)
                {
                    PendingOutputs.Remove(key);
                    return;
                }

                if (!__instance.IsMixingDone)
                    return;

                int quantity = operation.Quantity;
                if (quantity < 1)
                    quantity = 1;

                NativeItemInstance output;
                if (!TryCreateInstance(outputId, quantity, out output))
                {

                    PendingOutputs.Remove(key);
                    return;
                }

                if (!PlaceOutput(
                        __instance.OutputSlot,
                        output,
                        outputId,
                        quantity))
                {
                    return;
                }

                PendingOutputs.Remove(key);

                try { __instance.CurrentMixOperation = null; } catch { }
                try { __instance.SetMixerToLowered(); } catch { }
                try { __instance.onMixDone?.Invoke(); } catch { }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Edibles] Timer complete. Produced " +
                    outputId + " x" + quantity
                );
            }
            catch (Exception ex)
            {
                LogOnce("OnTimePass completion failed: " + ex.Message);
            }
        }

        private static string ResolveOutput(
            string productId,
            string ingredientId)
        {
            if (string.Equals(
                    productId,
                    GummyIngredients.ThcOilId,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    ingredientId,
                    GummyIngredients.GelatinId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return GummyIntermediates.GelatinBaseId;
            }

            if (string.Equals(
                    productId,
                    GummyIntermediates.GelatinBaseId,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    ingredientId,
                    Sugar.SugarId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return GummyIntermediates.UnbakedMixId;
            }

            return null;
        }

        private static bool TryGetCustomMix(
            MixingStation station,
            out string outputId,
            out int quantity)
        {
            outputId = null;
            quantity = 0;

            if (station == null)
                return false;

            string productId =
                station.ProductSlot
                    ?.ItemInstance
                    ?.Definition
                    ?.ID;

            string ingredientId =
                station.MixerSlot
                    ?.ItemInstance
                    ?.Definition
                    ?.ID;

            outputId = ResolveOutput(
                productId,
                ingredientId
            );

            if (outputId == null)
                return false;

            int productQuantity =
                station.ProductSlot?.Quantity ?? 0;

            int ingredientQuantity =
                station.MixerSlot?.Quantity ?? 0;

            quantity =
                Math.Min(
                    productQuantity,
                    ingredientQuantity
                );

            return quantity > 0;
        }

        private static bool CanOutputAccept(
            NativeItemSlot outputSlot,
            string targetId)
        {
            if (outputSlot == null)
                return false;

            if (outputSlot.ItemInstance == null)
                return true;

            string existingId =
                outputSlot.ItemInstance
                    ?.Definition
                    ?.ID;

            return string.Equals(
                       existingId,
                       targetId,
                       StringComparison.OrdinalIgnoreCase
                   ) &&
                   !outputSlot.IsAtCapacity;
        }

        private static bool PlaceOutput(
            NativeItemSlot outputSlot,
            NativeItemInstance output,
            string outputId,
            int quantity)
        {
            if (outputSlot == null)
                return false;

            try
            {
                if (outputSlot.ItemInstance == null)
                {
                    outputSlot.SetStoredItem(
                        output,
                        false
                    );

                    string placedId =
                        outputSlot.ItemInstance
                            ?.Definition
                            ?.ID;

                    return string.Equals(
                        placedId,
                        outputId,
                        StringComparison.OrdinalIgnoreCase
                    );
                }

                string existingId =
                    outputSlot.ItemInstance
                        ?.Definition
                        ?.ID;

                if (!string.Equals(
                        existingId,
                        outputId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                outputSlot.ChangeQuantity(
                    quantity,
                    false
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCreateInstance(
            string itemId,
            int quantity,
            out NativeItemInstance instance)
        {
            instance = null;

            try
            {
                S1API.Items.ItemDefinition wrapper =
                    ItemManager.GetDefinition(itemId);

                if (wrapper == null)
                    return false;

                object raw =
                    GetMemberValue(
                        wrapper,
                        "S1ItemDefinition"
                    );

                NativeStorableDefinition definition =
                    raw as NativeStorableDefinition;

                if (definition == null)
                    return false;

                instance =
                    definition.GetDefaultInstance(
                        quantity
                    );

                return instance != null;
            }
            catch
            {
                return false;
            }
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null)
                return null;

            Type type =
                target.GetType();

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
                {
                    try
                    {
                        return property.GetValue(target);
                    }
                    catch
                    {
                    }
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
                {
                    try
                    {
                        return field.GetValue(target);
                    }
                    catch
                    {
                    }
                }

                type =
                    type.BaseType;
            }

            return null;
        }

        private static void LogOnce(
            string message)
        {
            if (_errorLogged)
                return;

            _errorLogged = true;


        }
    }
}
