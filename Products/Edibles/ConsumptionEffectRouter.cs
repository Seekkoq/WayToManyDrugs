using MelonLoader;
using CustomNPCExample.Products.Edibles;

namespace CustomNPCExample.Products
{
    public static class ConsumptionEffectRouter
    {
        public static void OnProductConsumed(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return;

            MelonLogger.Msg("[WVC Effects] Consumed product ID: " + productId);

            if (productId == MDMA.ProductId)
            {
                THCGummyEffectManager.StopEffect();
                MDMAEffectManager.TriggerEffect();
                return;
            }

            if (productId == THCGummies.ProductId)
            {
                MDMAEffectManager.StopEffect();
                THCGummyEffectManager.StopEffect();
                return;
            }
        }
    }
}