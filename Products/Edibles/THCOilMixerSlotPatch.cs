using System;
using System.Reflection;
using CustomNPCExample.Products.Edibles;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Edibles
{
    public static class THCOilMixerSlotPatch
    {
        private const string HarmonyId =
            "westvilleconnection.edibles.thcoil-product-slot";

        private static bool _applied;
        private static bool _allowLogged;
        private static bool _errorLogged;

        public static bool ApplyPatch()
        {
            if (_applied)
                return true;

            try
            {
                MethodInfo original = AccessTools.Method(
                    typeof(ItemSlot),
                    nameof(ItemSlot.DoesItemMatchHardFilters),
                    new Type[]
                    {
                        typeof(ItemInstance)
                    }
                );

                MethodInfo postfix = AccessTools.Method(
                    typeof(THCOilMixerSlotPatch),
                    nameof(DoesItemMatchHardFilters_Postfix)
                );

                if (original == null || postfix == null)
                {


                    return false;
                }

                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(HarmonyId);

                harmony.Patch(
                    original,
                    postfix: new HarmonyMethod(postfix)
                );

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC THC Oil Slot] ProductSlot filter patch applied."
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC THC Oil Slot] Patch failed: " + ex
                );

                return false;
            }
        }

        private static void DoesItemMatchHardFilters_Postfix(
            ItemSlot __instance,
            ItemInstance item,
            ref bool __result)
        {
            if (__result)
                return;

            try
            {
                if (__instance == null || item == null)
                    return;

                if (!IsThcOil(item))
                    return;

                MixingStation station =
                    GetOwningMixingStation(__instance);

                if (station == null ||
                    station.ProductSlot == null)
                {
                    return;
                }

                if (!IsSameSlot(
                    __instance,
                    station.ProductSlot))
                {
                    return;
                }

                __result = true;

                if (!_allowLogged)
                {
                    _allowLogged = true;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC THC Oil Slot] Allowed THC Oil into " +
                        "MixingStation.ProductSlot."
                    );
                }
            }
            catch (Exception)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;


                }
            }
        }

        private static bool IsThcOil(
            ItemInstance item)
        {
            try
            {
                string itemId =
                    item.Definition?.ID;

                return string.Equals(
                    itemId,
                    GummyIngredients.ThcOilId,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return false;
            }
        }

        private static MixingStation GetOwningMixingStation(
            ItemSlot slot)
        {
            try
            {
                if (slot?.SlotOwner == null)
                    return null;

                return slot.SlotOwner.TryCast<MixingStation>();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSameSlot(
            ItemSlot first,
            ItemSlot second)
        {
            if (first == null || second == null)
                return false;

            try
            {
                IntPtr firstPointer =
                    IL2CPP.Il2CppObjectBaseToPtr(first);

                IntPtr secondPointer =
                    IL2CPP.Il2CppObjectBaseToPtr(second);

                return firstPointer != IntPtr.Zero &&
                       firstPointer == secondPointer;
            }
            catch
            {
                return ReferenceEquals(first, second);
            }
        }
    }
}
