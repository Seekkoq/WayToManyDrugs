using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Cauldrons
{
    /// <summary>
    /// 3 THC Oil + 5 Gelatin + 3 Infused Sugar
    /// -> Unbaked Gummy Mix
    /// </summary>
    public static class GummyCauldronRecipe
    {
        public struct State
        {
            public int OilCount;
            public int GelatinCount;
            public int SugarCount;

            public ItemInstance OilItem;
            public ItemInstance GelatinItem;

            public bool HasAny =>
                OilCount > 0 ||
                GelatinCount > 0 ||
                SugarCount > 0;

            public bool IsComplete =>
                OilCount >=
                    CauldronRecipeAmounts.ThcOilRequired &&
                GelatinCount >=
                    CauldronRecipeAmounts.GelatinRequired &&
                SugarCount >=
                    CauldronRecipeAmounts.SugarRequired;
        }

        public static State Read(
            Cauldron cauldron)
        {
            State state = new State();

            if (cauldron?.ItemSlots == null)
                return state;

            try
            {
                for (int i = 0;
                     i < cauldron.ItemSlots.Count;
                     i++)
                {
                    ItemSlot slot =
                        cauldron.ItemSlots[i];

                    if (slot?.ItemInstance == null)
                        continue;

                    string id =
                        slot.ItemInstance.Definition?.ID;

                    if (CauldronRecipes.IdEquals(
                            id,
                            CauldronRecipeIds.ThcOilId))
                    {
                        state.OilCount += slot.Quantity;

                        if (state.OilItem == null)
                        {
                            state.OilItem =
                                slot.ItemInstance;
                        }
                    }
                    else if (CauldronRecipes.IdEquals(
                            id,
                            CauldronRecipeIds.GelatinId))
                    {
                        state.GelatinCount += slot.Quantity;

                        if (state.GelatinItem == null)
                        {
                            state.GelatinItem =
                                slot.ItemInstance;
                        }
                    }
                    else if (CauldronRecipes.IdEquals(
                            id,
                            CauldronRecipeIds.SugarId))
                    {
                        state.SugarCount += slot.Quantity;
                    }
                }
            }
            catch { }

            return state;
        }

        /*
         * Currently unused. CauldronPatch.StartCustomCook
         * handles consumption. No CauldronFinishedOutput here.
         */
        public static void Consume(
            Cauldron cauldron,
            State state)
        {
            MelonLogger.Msg(
                "[WVC Cauldron] Consuming " +
                CauldronRecipeAmounts.ThcOilRequired +
                " THC Oil + " +
                CauldronRecipeAmounts.GelatinRequired +
                " Gelatin + " +
                CauldronRecipeAmounts.SugarRequired +
                " Sugar."
            );

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CauldronRecipeIds.ThcOilId,
                CauldronRecipeAmounts.ThcOilRequired
            );

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CauldronRecipeIds.GelatinId,
                CauldronRecipeAmounts.GelatinRequired
            );

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CauldronRecipeIds.SugarId,
                CauldronRecipeAmounts.SugarRequired
            );
        }
    }
}