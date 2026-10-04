using System;
using MelonLoader;
using S1API.Products;

namespace CustomNPCExample.Products
{
    public static class SalviaMixing
    {
        private static ProductMixingProfile _profile;
        private static bool _failureLogged;

        public static bool IsRegistered =>
            _profile != null;

        public static bool Register(ProductKind productKind)
        {
            if (_profile != null)
                return true;

            if (productKind == null)
                return false;

            try
            {
                _profile =
                    new ProductMixingProfileBuilder(productKind)
                        .WithMixerMap(
                            ProductMixingMap.Marijuana
                        )
                        .WithPropertyColorMixing()
                        .WithOutputFactoryCompatibility(
                            "westvilleconnection:mixing/salvia",
                            1
                        )
                        .WithOutputFactory(CreateMixedOutput)
                        .Build();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Salvia Mixing] Profile registered using Marijuana " +
                    "map with property-color mixing."
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;

                    MelonLogger.Error(
                        "[Salvia Mixing] Registration failed: " + ex
                    );
                }

                return false;
            }
        }

        private static ProductMixingOutputDefinition CreateMixedOutput(
            ProductMixingOutput input)
        {
            float price = input.SourcePrice + 10f;

            if (float.IsNaN(price) ||
                float.IsInfinity(price))
            {
                price = 70f;
            }

            price = Math.Max(
                1f,
                Math.Min(999f, price)
            );

            string name = input.MixName;

            if (string.IsNullOrWhiteSpace(name))
                name = "Salvia Mix";

            return new ProductMixingOutputDefinition(
                name,
                input.SourceKind,
                price
            );
        }
    }
}