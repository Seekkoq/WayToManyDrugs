using System;
using CustomNPCExample.Products.Edibles;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;

namespace CustomNPCExample.Products.Ovens
{
    public static class LabOvenRecipes
    {
        public static string UnbakedGummyMixId =>
            UnbakedGummyMix.ItemId;

        public static string ThcGummiesId =>
            THCGummies.ProductId;

        public const int MixRequired = 1;
        public const int GummiesProduced = 10;

        public const float BakeSeconds = 15f;

        public static bool IdEquals(string first, string second)
        {
            return string.Equals(
                first,
                second,
                StringComparison.OrdinalIgnoreCase
            );
        }

        public static string GetItemId(ItemSlot slot)
        {
            try
            {
                return slot?.ItemInstance?.Definition?.ID;
            }
            catch
            {
                return null;
            }
        }

        public static bool SlotHasGummyMix(LabOven oven)
        {
            if (oven?.IngredientSlot?.ItemInstance == null)
                return false;

            return IdEquals(
                GetItemId(oven.IngredientSlot),
                UnbakedGummyMixId
            ) &&
            oven.IngredientSlot.Quantity >= MixRequired;
        }

        public static bool OutputIsEmpty(LabOven oven)
        {
            return oven?.OutputSlot == null ||
                   oven.OutputSlot.ItemInstance == null ||
                   oven.OutputSlot.Quantity <= 0;
        }

        public static bool OutputIsGummies(LabOven oven)
        {
            return IdEquals(
                GetItemId(oven.OutputSlot),
                ThcGummiesId
            );
        }

        public static int FindSlotIndex(
            LabOven oven,
            ItemSlot target)
        {
            if (oven?.ItemSlots == null ||
                target == null)
            {
                return -1;
            }

            try
            {
                for (int i = 0; i < oven.ItemSlots.Count; i++)
                {
                    ItemSlot slot = oven.ItemSlots[i];

                    if (slot != null &&
                        slot.Pointer == target.Pointer)
                    {
                        return i;
                    }
                }
            }
            catch
            {
            }

            return -1;
        }
    }
}
