using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Registers DMT's mixing profile.
    /// Map 0 keeps DMT separate from MDMA (map 2) and
    /// Gummies (map 1) so the mixing station cannot confuse them.
    /// </summary>
    public static class DMTMixing
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
                    "[DMT Mixing] Product kind was null. Mixing not registered."
                );
                return;
            }

            try
            {
                _profile = new ProductMixingProfileBuilder(productKind)
                    .WithMixerMap((ProductMixingMap)0)
                    .WithPropertyColorMixing()
                    .WithOutputFactoryCompatibility(
                        "westvilleconnection:mixing/dmt",
                        1
                    )
                    .WithOutputFactory(CreateMixedOutput)
                    .Build();

                MelonLogger.Msg(
                    "[DMT Mixing] DMT mixing profile registered with color mixing."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[DMT Mixing] Registration failed: " + ex
                );
            }
        }

        private static ProductMixingOutputDefinition CreateMixedOutput(
            ProductMixingOutput input
        )
        {
            MelonLogger.Msg(
                "[DMT Mixing] Output factory called. Name=" +
                input.MixName +
                ", SourcePrice=" +
                input.SourcePrice
            );

            float price = input.SourcePrice + 20f;

            if (float.IsNaN(price) || float.IsInfinity(price))
                price = 180f;

            price = Math.Max(1f, Math.Min(999f, price));

            string mixName = input.MixName;

            if (string.IsNullOrWhiteSpace(mixName))
                mixName = "DMT Mix";

            return new ProductMixingOutputDefinition(
                mixName,
                input.SourceKind,
                price
            );
        }
    }
}