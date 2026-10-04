using CustomNPCExample.Products;

namespace CustomNPCExample.SupplierPricing
{
    public static class RoscoePricing
    {
        public static void Register()
        {
            SupplierPricingManager.RegisterItem(
                MollyIngredients.SafroleId,
                basePrice: 40f,
                minMultiplier: 0.50f,
                maxMultiplier: 1.75f
            );

            SupplierPricingManager.RegisterItem(
                MollyIngredients.PmkId,
                basePrice: 35f,
                minMultiplier: 0.50f,
                maxMultiplier: 1.75f
            );

            SupplierPricingManager.RegisterItem(
                MollyIngredients.PmkRefinedId,
                basePrice: 75f,
                minMultiplier: 0.50f,
                maxMultiplier: 1.75f
            );

            SupplierPricingManager.RegisterItem(
                MollyIngredients.PmkLabGradeId,
                basePrice: 140f,
                minMultiplier: 0.50f,
                maxMultiplier: 1.75f
            );
        }
    }
}
