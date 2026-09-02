using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products.Edibles
{
    /// <summary>
    /// Registers THC Gummies' mixing profile.
    /// Map 1 keeps Gummies separate from MDMA (map 2) and
    /// DMT (map 0) so the mixing station cannot confuse them.
    /// </summary>
    public static class GummyMixing
    {
        private static ProductMixingProfile _profile;

        public static bool IsRegistered => _profile != null;

        public static void Register(ProductKind productKind)
        {
            if (_profile != null)
                return;

            if (productKind == null)
            {
                MelonLogger.Warning(
                    "[Gummy Mixing] Product kind was null. Mixing not registered."
                );
                return;
            }

            try
            {
                _profile = new ProductMixingProfileBuilder(productKind)
                    .WithMixerMap((ProductMixingMap)1)
                    .WithPropertyColorMixing()
                    .WithOutputFactoryCompatibility(
                        "westvilleconnection:mixing/thc_gummies",
                        1
                    )
                    .WithOutputFactory(CreateMixedOutput)
                    .Build();

                MelonLogger.Msg(
                    "[Gummy Mixing] THC Gummies mixing profile registered with color mixing."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Gummy Mixing] Registration failed: " + ex
                );
            }
        }

        private static ProductMixingOutputDefinition CreateMixedOutput(
            ProductMixingOutput input
        )
        {
            MelonLogger.Msg(
                "[Gummy Mixing] Output factory called. Name=" +
                input.MixName +
                ", SourcePrice=" +
                input.SourcePrice
            );

            float price = input.SourcePrice + 8f;

            if (float.IsNaN(price) || float.IsInfinity(price))
                price = 45f;

            price = Math.Max(1f, Math.Min(999f, price));

            string mixName = input.MixName;

            if (string.IsNullOrWhiteSpace(mixName))
                mixName = "Gummy Mix";

            return new ProductMixingOutputDefinition(
                mixName,
                input.SourceKind,
                price
            );
        }
    }
}