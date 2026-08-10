using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Registers MDMA's mixing profile.
    /// WithPropertyColorMixing() is what makes mixed output take on
    /// the mixed property color, like weed and cocaine do.
    /// </summary>
    public static class MDMAMixing
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
                    "[MDMA Mixing] Product kind was null. Mixing not registered."
                );
                return;
            }

            try
            {
                _profile = new ProductMixingProfileBuilder(productKind)
                    .WithMixerMap((ProductMixingMap)2)
                    .WithPropertyColorMixing()
                    .WithOutputFactoryCompatibility(
                        "westvilleconnection:mixing/mdma",
                        1
                    )
                    .WithOutputFactory(CreateMixedOutput)
                    .Build();

                MelonLogger.Msg(
                    "[MDMA Mixing] MDMA mixing profile registered with color mixing."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[MDMA Mixing] Registration failed: " + ex
                );
            }
        }

        private static ProductMixingOutputDefinition CreateMixedOutput(
            ProductMixingOutput input
        )
        {
            // API rejects prices outside 1..999
            float price = input.SourcePrice + 12f;

            if (float.IsNaN(price) || float.IsInfinity(price))
                price = 95f;

            price = Math.Max(1f, Math.Min(999f, price));

            string mixName = input.MixName;

            if (string.IsNullOrWhiteSpace(mixName))
                mixName = "MDMA Mix";

            return new ProductMixingOutputDefinition(
                mixName,
                input.SourceKind,
                price
            );
        }
    }
}