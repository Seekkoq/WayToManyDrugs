using System;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Properties;

namespace CustomNPCExample.Products.Edibles
{
    public static class UnbakedGummyMix
    {
        public const string ItemId =
            "westvilleconnection:ingredients/unbaked_gummy_mix";

        private static bool _registered;
        private static bool _failed;

        public static bool TryRegister()
        {
            if (_registered) return true;
            if (_failed) return false;

            try
            {
                if (ItemManager.GetDefinition("iodine") == null)
                    return false;

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        ItemId,
                        "Unbaked Gummy Mix",
                        "Sweet cannabis slurry. Bake it in the Lab Oven.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(Property.Energizing)
                    .Build();

                try { ConsoleItemAliases.Register("gummymix", ItemId); }
                catch { }

                _registered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Unbaked Gummy Mix registered.");
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error("[WVC] Gummy Mix registration failed: " + ex);
                return false;
            }
        }
    }
}