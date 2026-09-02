namespace CustomNPCExample.Products.Cauldrons
{
    public static class CauldronRecipeAmounts
    {
        // MDMA: 2 Safrole + 3 PMK
        public const int SafroleRequired = 2;
        public const int PmkRequired = 3;

        public const int MdmaBatchSize = 10;
        public const int GummyBatchSize = 1;

        // Gummies: 3 THC Oil + 5 Gelatin + 3 Sugar
        public const int ThcOilRequired = 3;
        public const int GelatinRequired = 5;
        public const int SugarRequired = 3;

        // DMT: 3 Bark + 2 Caustic Base + 1 Lab Solvent
        public const int DmtBarkRequired = 3;
        public const int DmtCausticBaseRequired = 2;
        public const int DmtLabSolventRequired = 1;
        public const int DmtCrystalizerRequired = 1;
        public const int DmtBatchSize = 1;

        // Cookies: 2 Cannabis Flour + 3 Butterscotch Chips -> 1 Unbaked Cookie Dough
        public const int CannabisFlourRequired = 2;
        public const int ButterscotchChipsRequired = 3;
        public const int CookieDoughBatchSize = 1;

        public const string GasolineId = "gasoline";
        public const int GasolineRequired = 1;

        public static int GetOutputBatchSize(string outputItemId)
        {
            if (CauldronRecipes.IdEquals(
                    outputItemId,
                    CauldronRecipeIds.MdmaId))
            {
                return MdmaBatchSize;
            }

            if (CauldronRecipes.IdEquals(
                    outputItemId,
                    CauldronRecipeIds.GummyMixId))
            {
                return GummyBatchSize;
            }

            if (CauldronRecipes.IdEquals(
                    outputItemId,
                    CauldronRecipeIds.CookieDoughId))
            {
                return CookieDoughBatchSize;
            }

            if (DmtCauldronRecipe.IsDmtOutput(outputItemId))
                return DmtBatchSize;

            return 1;
        }
    }
}