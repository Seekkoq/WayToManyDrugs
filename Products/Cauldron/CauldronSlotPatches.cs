using System;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class CauldronSlotPatches
    {
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

                if (!IsCustomCauldronSlot(__instance))
                    return true;

                if (!CauldronRecipes.IsAllowedIngredient(item))
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

                if (!IsCustomCauldronSlot(__instance))
                    return true;

                if (!CauldronRecipes.IsAllowedIngredient(item))
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

        private static bool IsCustomCauldronSlot(
            ItemSlot slot)
        {
            try
            {
                if (slot == null ||
                    slot.SlotOwner == null)
                {
                    return false;
                }

                // FIX: Cast to Il2CppObjectBase first to allow TryCast to Cauldron
                Il2CppObjectBase ownerBase =
                    slot.SlotOwner as Il2CppObjectBase;

                Cauldron cauldron =
                    ownerBase?.TryCast<Cauldron>();

                if (cauldron == null)
                    return false;

                return CauldronPatch.IsCustomCauldron(
                    cauldron
                );
            }
            catch
            {
                return false;
            }
        }
    }
}