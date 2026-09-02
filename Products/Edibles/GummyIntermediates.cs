using System;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;

namespace CustomNPCExample.Products.Edibles
{
    public static class GummyIntermediates
    {
        public const string GelatinBaseId =
            "westvilleconnection:ingredients/thc_gelatin_base";

        public const string UnbakedMixId =
            "westvilleconnection:ingredients/unbaked_gummy_mix";

        private static bool _registered;
        private static bool _failed;

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                S1API.Items.ItemDefinition iodine =
                    ItemManager.GetDefinition("iodine");

                if (iodine == null)
                    return false;

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        GelatinBaseId,
                        "THC Gelatin Base",
                        "A warm gelatin mixture infused with THC oil.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        UnbakedMixId,
                        "Unbaked Gummy Mix",
                        "Sweet cannabis gummy slurry. Load into the Lab Oven to bake gummies.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                try { ConsoleItemAliases.Register("thcgelatinbase", GelatinBaseId); }
                catch { }

                try { ConsoleItemAliases.Register("unbakedgummy", UnbakedMixId); }
                catch { }

                _registered = true;
                MelonLogger.Msg(
                    "[WVC Edibles] Intermediate ingredients registered."
                );
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error(
                    "[WVC Edibles] Intermediate registration failed: " + ex
                );
                return false;
            }
        }
    }
}