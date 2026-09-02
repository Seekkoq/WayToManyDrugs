using CustomNPCExample.Products.Edibles;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Cauldrons
{
    /// <summary>
    /// 2 Cannabis Flour + 3 Butterscotch Chips
    /// -> 1x Unbaked Cookie Dough
    /// </summary>
    public static class CookieCauldronRecipe
    {
        public struct State
        {
            public int FlourCount;
            public int ChipsCount;

            public ItemInstance FlourItem;
            public ItemInstance ChipsItem;

            public bool HasAny =>
                FlourCount > 0 ||
                ChipsCount > 0;

            public bool IsComplete =>
                FlourCount >= CauldronRecipeAmounts.CannabisFlourRequired &&
                ChipsCount >= CauldronRecipeAmounts.ButterscotchChipsRequired;
        }

        public static State Read(Cauldron cauldron)
        {
            State state = new State();

            if (cauldron?.ItemSlots == null)
                return state;

            try
            {
                for (int i = 0; i < cauldron.ItemSlots.Count; i++)
                {
                    ItemSlot slot = cauldron.ItemSlots[i];

                    if (slot?.ItemInstance == null)
                        continue;

                    string id = slot.ItemInstance.Definition?.ID;

                    if (CauldronRecipes.IdEquals(id, CookieIngredients.CannabisFlourId))
                    {
                        state.FlourCount += slot.Quantity;

                        if (state.FlourItem == null)
                        {
                            state.FlourItem = slot.ItemInstance;
                        }
                    }
                    else if (CauldronRecipes.IdEquals(id, CookieIngredients.ButterscotchChipsId))
                    {
                        state.ChipsCount += slot.Quantity;

                        if (state.ChipsItem == null)
                        {
                            state.ChipsItem = slot.ItemInstance;
                        }
                    }
                }
            }
            catch { }

            return state;
        }

        public static void Consume(Cauldron cauldron, State state)
        {
            MelonLogger.Msg(
                "[WVC Cauldron] Consuming " +
                CauldronRecipeAmounts.CannabisFlourRequired +
                " Cannabis Flour + " +
                CauldronRecipeAmounts.ButterscotchChipsRequired +
                " Butterscotch Chips."
            );

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CookieIngredients.CannabisFlourId,
                CauldronRecipeAmounts.CannabisFlourRequired
            );

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CookieIngredients.ButterscotchChipsId,
                CauldronRecipeAmounts.ButterscotchChipsRequired
            );
        }
    }
}
