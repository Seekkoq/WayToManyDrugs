using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products
{
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
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
            global::CustomNPCExample.Utils.WvcLog.Msg(
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
