using System;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class DmtCauldronRecipe
    {
        public struct State
        {
            public int BarkCount;
            public int BaseCount;
            public int SolventCount;
            public int CrystalizerCount;

            public ItemInstance BarkItem;
            public ItemInstance BaseItem;

            public bool HasAny =>
                BarkCount > 0 ||
                BaseCount > 0 ||
                SolventCount > 0 ||
                CrystalizerCount > 0;

            public bool IsComplete =>
                BarkCount >=
                    CauldronRecipeAmounts.DmtBarkRequired &&
                BaseCount >=
                    CauldronRecipeAmounts.DmtCausticBaseRequired &&
                SolventCount >=
                    CauldronRecipeAmounts.DmtLabSolventRequired;

            public bool IsPremium =>
                CrystalizerCount >=
                CauldronRecipeAmounts.DmtCrystalizerRequired;
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

                    if (slot?.ItemInstance == null ||
                        slot.Quantity <= 0)
                    {
                        continue;
                    }

                    string id =
                        slot.ItemInstance.Definition?.ID;

                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (CauldronRecipes.IdEquals(
                            id,
                            DMTIngredients.DreamrootId))
                    {
                        state.BarkCount += slot.Quantity;

                        if (state.BarkItem == null)
                            state.BarkItem = slot.ItemInstance;
                    }
                    else if (CauldronRecipes.IdEquals(
                                 id,
                                 DMTIngredients.CausticBaseId))
                    {
                        state.BaseCount += slot.Quantity;

                        if (state.BaseItem == null)
                            state.BaseItem = slot.ItemInstance;
                    }
                    else if (CauldronRecipes.IdEquals(
                                 id,
                                 DMTIngredients.LabSolventId))
                    {
                        state.SolventCount += slot.Quantity;
                    }
                    else if (CauldronRecipes.IdEquals(
                                 id,
                                 DMTIngredients.CrystalizerId))
                    {
                        state.CrystalizerCount += slot.Quantity;
                    }
                }
            }
            catch (Exception)
            {

            }

            return state;
        }

        public static bool Consume(
            Cauldron cauldron,
            State state,
            out string outputItemId,
            out EQuality quality)
        {
            outputItemId =
                DMTIntermediates.CrudeExtractId;

            quality = EQuality.Standard;

            if (cauldron == null || !state.IsComplete)
                return false;

            bool barkOk =
                CauldronRecipes.ConsumeByItemId(
                    cauldron,
                    DMTIngredients.DreamrootId,
                    CauldronRecipeAmounts.DmtBarkRequired
                );

            bool baseOk =
                CauldronRecipes.ConsumeByItemId(
                    cauldron,
                    DMTIngredients.CausticBaseId,
                    CauldronRecipeAmounts.DmtCausticBaseRequired
                );

            bool solventOk =
                CauldronRecipes.ConsumeByItemId(
                    cauldron,
                    DMTIngredients.LabSolventId,
                    CauldronRecipeAmounts.DmtLabSolventRequired
                );

            if (!barkOk || !baseOk || !solventOk)
            {


                return false;
            }

            bool premium = state.IsPremium;

            if (premium)
            {
                bool crystalOk =
                    CauldronRecipes.ConsumeByItemId(
                        cauldron,
                        DMTIngredients.CrystalizerId,
                        CauldronRecipeAmounts.DmtCrystalizerRequired
                    );

                if (!crystalOk)
                {
                    premium = false;


                }
            }

            if (premium)
            {
                outputItemId =
                    DMTIntermediates.PremiumCrudeExtractId;

                quality = EQuality.Premium;
            }
            else
            {
                outputItemId =
                    DMTIntermediates.CrudeExtractId;

                quality = EQuality.Standard;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cauldron] Consumed DMT inputs. " +
                "bark=" + CauldronRecipeAmounts.DmtBarkRequired +
                ", base=" + CauldronRecipeAmounts.DmtCausticBaseRequired +
                ", solvent=" + CauldronRecipeAmounts.DmtLabSolventRequired +
                ", output=" + outputItemId +
                ", quality=" + quality
            );

            return true;
        }

        public static bool IsDmtOutput(string itemId)
        {
            return CauldronRecipes.IdEquals(
                       itemId,
                       DMTIntermediates.CrudeExtractId
                   ) ||
                   CauldronRecipes.IdEquals(
                       itemId,
                       DMTIntermediates.PremiumCrudeExtractId
                   );
        }
    }
}
