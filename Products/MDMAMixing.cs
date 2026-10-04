using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products
{
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
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
