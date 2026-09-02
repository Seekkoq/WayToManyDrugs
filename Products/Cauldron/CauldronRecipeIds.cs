using System;
using System.Collections.Generic;
using CustomNPCExample.Products.Edibles;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class CauldronRecipeIds
    {
        public const string SafroleId =
            "westvilleconnection:ingredients/safrole_oil";

        public const string PmkId =
            "westvilleconnection:ingredients/pmk_powder";

        public const string ThcOilId =
            "westvilleconnection:ingredients/thc_oil";

        public const string GelatinId =
            "westvilleconnection:ingredients/gelatin";

        public const string SugarId =
            "westvilleconnection:ingredients/sugar";

        public const string ButterscotchChipsId =
            CookieIngredients.ButterscotchChipsId;

        public const string CannabisFlourId =
            CookieIngredients.CannabisFlourId;

        public static string MdmaId =>
            MDMA.ProductId;

        public static string GummyMixId =>
            UnbakedGummyMix.ItemId;

        public static string CookieDoughId =>
            UnbakedCookieDough.ItemId;

        public static readonly HashSet<string> AllowedIngredients =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
        SafroleId,
        PmkId,
        MollyIngredients.PmkRefinedId,
        MollyIngredients.PmkLabGradeId,
        ThcOilId,
        GelatinId,
        SugarId,
        ButterscotchChipsId,
        CannabisFlourId,

        DMTIngredients.DreamrootId,
        DMTIngredients.CausticBaseId,
        DMTIngredients.LabSolventId,
        DMTIngredients.CrystalizerId
            };

        public static bool IdEquals(
            string first,
            string second)
        {
            return string.Equals(
                first,
                second,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}