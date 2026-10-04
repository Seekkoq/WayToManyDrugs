using System;
using System.Collections.Generic;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Chemistry
{
    public static class BrownieChemistryRecipe
    {
        public const int CocoaRequired = 3;
        public const int ButterRequired = 1;
        public const int LeavenRequired = 1;

        public const int OutputBatchSize = 1;

        public struct State
        {
            public int CocoaCount;
            public int ButterCount;
            public int LeavenCount;

            public bool HasAny =>
                CocoaCount > 0 ||
                ButterCount > 0 ||
                LeavenCount > 0;

            public bool IsComplete =>
                CocoaCount >= CocoaRequired &&
                ButterCount >= ButterRequired &&
                LeavenCount >= LeavenRequired;
        }

        public static List<ItemSlot> GetInputSlots(
            ChemistryStation station)
        {
            List<ItemSlot> result =
                new List<ItemSlot>();

            if (station == null)
                return result;

            try
            {
                var ingredientSlots =
                    station.IngredientSlots;

                if (ingredientSlots != null)
                {
                    for (int i = 0; i < ingredientSlots.Length; i++)
                    {
                        ItemSlot slot = ingredientSlots[i];

                        if (slot != null)
                            result.Add(slot);
                    }
                }

                if (result.Count > 0)
                    return result;

                var inputSlots =
                    station.InputSlots;

                if (inputSlots != null)
                {
                    for (int i = 0; i < inputSlots.Count; i++)
                    {
                        ItemSlot slot = inputSlots[i];

                        if (slot != null)
                            result.Add(slot);
                    }
                }
            }
            catch (Exception)
            {

            }

            return result;
        }

        public static State Read(ChemistryStation station)
        {
            State state = new State();

            if (station == null)
                return state;

            try
            {
                List<ItemSlot> slots =
                    GetInputSlots(station);

                for (int i = 0; i < slots.Count; i++)
                {
                    ItemSlot slot = slots[i];

                    if (slot == null ||
                        slot.ItemInstance == null ||
                        slot.Quantity <= 0)
                    {
                        continue;
                    }

                    string id =
                        slot.ItemInstance.Definition?.ID;

                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (IdEquals(id, BrownieIngredients.CocoaProductId))
                        state.CocoaCount += slot.Quantity;
                    else if (IdEquals(id, BrownieIngredients.ButterProductId))
                        state.ButterCount += slot.Quantity;
                    else if (IdEquals(id, BrownieIngredients.LeavenProductId))
                        state.LeavenCount += slot.Quantity;
                }
            }
            catch (Exception)
            {

            }

            return state;
        }

        public static void Consume(ChemistryStation station)
        {
            if (station == null)
                return;

            ConsumeById(
                station,
                BrownieIngredients.CocoaProductId,
                CocoaRequired
            );

            ConsumeById(
                station,
                BrownieIngredients.ButterProductId,
                ButterRequired
            );

            ConsumeById(
                station,
                BrownieIngredients.LeavenProductId,
                LeavenRequired
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Chemistry] Consumed " +
                CocoaRequired + " Cocoa, " +
                ButterRequired + " Butter, " +
                LeavenRequired + " Leaven."
            );
        }

        private static void ConsumeById(
            ChemistryStation station,
            string itemId,
            int amount)
        {
            if (station == null || amount <= 0)
                return;

            try
            {
                List<ItemSlot> slots =
                    GetInputSlots(station);

                int remaining = amount;

                for (int i = 0; i < slots.Count && remaining > 0; i++)
                {
                    ItemSlot slot = slots[i];

                    if (slot == null ||
                        slot.ItemInstance == null ||
                        slot.Quantity <= 0)
                    {
                        continue;
                    }

                    string id =
                        slot.ItemInstance.Definition?.ID;

                    if (!IdEquals(id, itemId))
                        continue;

                    int take =
                        Math.Min(remaining, slot.Quantity);

                    slot.ChangeQuantity(-take, false);

                    remaining -= take;
                }
            }
            catch (Exception)
            {

            }
        }

        public static bool IsBrownieIngredient(ItemInstance item)
        {
            if (item == null)
                return false;

            string id = item.Definition?.ID;

            if (string.IsNullOrEmpty(id))
                return false;

            return IdEquals(id, BrownieIngredients.CocoaProductId)
                || IdEquals(id, BrownieIngredients.ButterProductId)
                || IdEquals(id, BrownieIngredients.LeavenProductId);
        }

        public static bool IdEquals(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            return string.Equals(
                a,
                b,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
