using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products.Edibles
{
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
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
            global::CustomNPCExample.Utils.WvcLog.Msg(
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
