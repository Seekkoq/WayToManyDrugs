using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    public static class THCGummyEffectManager
    {
        public static void Update()
        {
            // Do not create a volume. Vanilla weed handles the high.
        }

        public static void TriggerEffect()
        {
            MelonLogger.Msg("[Gummy Effect] Trigger ignored. Using native weed FX.");
        }

        public static void StopEffect()
        {
            THCGummyScreenEffect.ShouldStart = false;
            THCGummyScreenEffect.ShouldStop = true;
        }
    }
}