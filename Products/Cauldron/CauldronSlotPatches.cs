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

                // The slot may only take the ingredient it already holds. Without this the game
                // believes a *different* ingredient fits, stores it over the stack that was there
                // and the old items are simply gone - which is what shift-clicking a second
                // ingredient into an occupied slot used to do.
                if (!SlotHoldsSameIngredient(__instance, item))
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

                if (!SlotHoldsSameIngredient(__instance, item))
                {
                    // Report no room for a foreign ingredient: that is what stops a shift-click
                    // from overwriting the stack already in the slot.
                    __result = 0;
                    return false;
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

        /// <summary>
        /// True when the slot is empty or already holds the very same definition.
        /// </summary>
        private static bool SlotHoldsSameIngredient(
            ItemSlot slot,
            ItemInstance item)
        {
            try
            {
                ItemInstance stored = slot?.ItemInstance;

                if (stored == null)
                    return true;

                if (item == null || item.Definition == null || stored.Definition == null)
                    return false;

                string storedId = stored.Definition.ID;
                string incomingId = item.Definition.ID;

                if (string.IsNullOrEmpty(storedId) || string.IsNullOrEmpty(incomingId))
                    return false;

                return string.Equals(storedId, incomingId, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
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
