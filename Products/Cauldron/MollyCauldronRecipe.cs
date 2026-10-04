using System;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class MollyCauldronRecipe
    {
        public enum PmkGradeKind
        {
            None = 0,
            Standard = 1,
            Refined = 2,
            LabGrade = 3
        }

        public struct State
        {
            public int SafroleCount;
            public int PmkCount;

            public ItemInstance SafroleItem;
            public ItemInstance PmkItem;

            public PmkGradeKind PmkGrade;
            public string PmkItemId;

            public bool HasAny =>
                SafroleCount > 0 || PmkCount > 0;

            public bool IsComplete =>
                SafroleCount >= CauldronRecipeAmounts.SafroleRequired &&
                PmkCount >= CauldronRecipeAmounts.PmkRequired;
        }

        public static State Read(Cauldron cauldron)
        {
            State state = new State
            {
                PmkGrade = PmkGradeKind.None
            };

            if (cauldron?.ItemSlots == null)
                return state;

            try
            {
                for (int i = 0; i < cauldron.ItemSlots.Count; i++)
                {
                    ItemSlot slot = cauldron.ItemSlots[i];

                    if (slot?.ItemInstance == null || slot.Quantity <= 0)
                        continue;

                    string id = slot.ItemInstance.Definition?.ID;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (CauldronRecipes.IdEquals(id, CauldronRecipes.SafroleId))
                    {
                        state.SafroleCount += slot.Quantity;
                        if (state.SafroleItem == null)
                            state.SafroleItem = slot.ItemInstance;
                        continue;
                    }

                    PmkGradeKind grade = GetPmkGrade(id);
                    if (grade == PmkGradeKind.None)
                        continue;

                    if (state.PmkGrade == PmkGradeKind.None)
                    {
                        state.PmkGrade = grade;
                        state.PmkItemId = id;
                    }

                    if (grade != state.PmkGrade)
                        continue;

                    state.PmkCount += slot.Quantity;
                    if (state.PmkItem == null)
                        state.PmkItem = slot.ItemInstance;
                }
            }
            catch (Exception)
            {

            }

            return state;
        }

        public static void Consume(Cauldron cauldron, State state)
        {
            if (cauldron == null)
                return;

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                CauldronRecipes.SafroleId,
                CauldronRecipeAmounts.SafroleRequired
            );

            string pmkId =
                string.IsNullOrEmpty(state.PmkItemId)
                    ? CauldronRecipes.PmkId
                    : state.PmkItemId;

            CauldronRecipes.ConsumeByItemId(
                cauldron,
                pmkId,
                CauldronRecipeAmounts.PmkRequired
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cauldron] Consumed MDMA ingredients. PMK grade: " +
                state.PmkGrade
            );
        }

        public static PmkGradeKind GetPmkGrade(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return PmkGradeKind.None;

            if (CauldronRecipes.IdEquals(itemId, MollyIngredients.PmkLabGradeId))
                return PmkGradeKind.LabGrade;

            if (CauldronRecipes.IdEquals(itemId, MollyIngredients.PmkRefinedId))
                return PmkGradeKind.Refined;

            if (CauldronRecipes.IdEquals(itemId, CauldronRecipes.PmkId))
                return PmkGradeKind.Standard;

            return PmkGradeKind.None;
        }

        public static EQuality GetQuality(PmkGradeKind grade)
        {
            switch (grade)
            {
                case PmkGradeKind.LabGrade:
                    return (EQuality)4;
                case PmkGradeKind.Refined:
                    return (EQuality)3;
                default:
                    return (EQuality)2;
            }
        }
    }
}
