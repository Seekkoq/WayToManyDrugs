using MelonLoader;

namespace CustomNPCExample.Products
{
    public static class WvcDebugItems
    {
        public static void PrintMollyIngredientCommands(
            int amount = 10
        )
        {
            MollyIngredients.TryRegister();

            MelonLogger.Msg("");
            MelonLogger.Msg(
                "[WVC Testing] Use these commands in the Schedule I console:"
            );
            MelonLogger.Msg("");
            MelonLogger.Msg(
                $"give safrole {amount}"
            );
            MelonLogger.Msg(
                $"give pmk {amount}"
            );
            MelonLogger.Msg("");
            MelonLogger.Msg(
                "Full item IDs:"
            );
            MelonLogger.Msg(
                $"give {MollyIngredients.SafroleId} {amount}"
            );
            MelonLogger.Msg(
                $"give {MollyIngredients.PmkId} {amount}"
            );
            MelonLogger.Msg("");
        }
    }
}