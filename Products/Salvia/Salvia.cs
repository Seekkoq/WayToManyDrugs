using System;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Products;
using UnityEngine;

using S1DrugType = S1API.Products.DrugType;
using S1LegalStatus = S1API.Items.LegalStatus;
using S1PackagingDefinition = S1API.Products.PackagingDefinition;
using S1ProductDefinition = S1API.Products.ProductDefinition;
using S1Quality = S1API.Products.Quality;

namespace CustomNPCExample.Products
{
    public static class Salvia
    {
        public const string ProductKindId =
            "westvilleconnection:salvia_v1";

        public const string ProductId =
            "westvilleconnection:products/salvia_v1";

        private const string OwnerId =
            "westvilleconnection";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static bool _packagingRegistered;
        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;

        private static ProductPresentationProfile _presentationProfile;
        private static GameObject _budVisualSource;
        private static bool _presentationRegistering;

        public static bool IsBuilt => _built;

        public static bool TryEnsureBuilt()
        {
            return _built || TryBuild();
        }

        public static bool TryRegister()
        {
            if (_discovered)
                return true;

            if (!_built && !TryBuild())
                return false;

            return TryDiscover();
        }

        private static bool TryBuild()
        {
            try
            {
                S1ProductDefinition gdpDonor =
                    FindGdpDonorProduct();

                if (gdpDonor == null)
                {


                    return false;
                }

                S1PackagingDefinition baggie =
                    ItemManager.GetDefinition("baggie")
                    as S1PackagingDefinition;

                S1PackagingDefinition jar =
                    ItemManager.GetDefinition("jar")
                    as S1PackagingDefinition;

                S1PackagingDefinition brick =
                    ItemManager.GetDefinition("brick")
                    as S1PackagingDefinition;

                if (baggie == null ||
                    jar == null ||
                    brick == null)
                {


                    return false;
                }

                if (_productKind == null)
                {
                    _productKind =
                        new ProductKindBuilder(ProductKindId)
                            .WithCompatibilityDrugType(
                                S1DrugType.Marijuana
                            )
                            .Build();
                }

                SalviaMixing.Register(_productKind);

                if (!SalviaMixing.IsRegistered)
                {


                    return false;
                }

                EnsurePackagingRegistered();

                _definition =
                    CustomProductItemCreator
                        .CreateBuilder(ProductId, _productKind)
                        .WithName("Salvia")
                        .WithDescription(
                            "Dried Salvia leaf with an intense, short-lived effect."
                        )
                        .WithProductPrice(110f)
                        .WithLegalStatus((S1LegalStatus)1)
                        .WithBaseAddictiveness(0.05f)
                        .WithDefaultQuality((S1Quality)2)

                        .WithRepresentationsFrom(gdpDonor)

                        .WithValidPackaging(
                            new S1PackagingDefinition[]
                            {
                                baggie,
                                jar,
                                brick
                            }
                        )
                        .WithEffectDurations(60, 120)

                        .WithNativeMixerMap(
                            ProductMixingMap.Marijuana
                        )
                        .Build();

                _built = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Salvia] Built using native Grand Daddy Purple representations " +
                    "and Marijuana baggie/jar/brick scaffolds."
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;

                    MelonLogger.Error(
                        "[WVC Salvia] Build failed: " + ex
                    );
                }

                return false;
            }
        }

        private static void EnsurePackagingRegistered()
        {
            if (_packagingRegistered)
                return;

            _baggieProfile =
                new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Marijuana,
                        TintSalviaVisual
                    )
                    .Build();

            _jarProfile =
                new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Marijuana,
                        TintSalviaVisual
                    )
                    .Build();

            _brickProfile =
                new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Marijuana,
                        TintSalviaVisual
                    )
                    .Build();

            RegisterPackaging(
                "baggie",
                _baggieProfile
            );

            RegisterPackaging(
                "jar",
                _jarProfile
            );

            RegisterPackaging(
                "brick",
                _brickProfile
            );

            _packagingRegistered = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Salvia] Native Marijuana baggie, jar, and brick " +
                "packaging profiles registered (purple-tinted)."
            );
        }

        private static void TintSalviaVisual(GameObject visual)
        {
            if (visual == null)
                return;

            try
            {
                foreach (Renderer renderer in
                    visual.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    Material[] materials = renderer.materials;
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];

                        if (material == null)
                            continue;

                        if (material.HasProperty("_BaseColor"))
                        {
                            material.SetColor(
                                "_BaseColor",
                                ShiftToPurple(material.GetColor("_BaseColor"))
                            );

                            changed = true;
                        }
                        else if (material.HasProperty("_Color"))
                        {
                            material.SetColor(
                                "_Color",
                                ShiftToPurple(material.GetColor("_Color"))
                            );

                            changed = true;
                        }
                    }

                    if (changed)
                        renderer.materials = materials;
                }
            }
            catch (Exception)
            {
                if (!_tintFailureLogged)
                {
                    _tintFailureLogged = true;


                }
            }
        }

        private static bool _tintFailureLogged;

        private static Color ShiftToPurple(Color color)
        {
            float hue;
            float saturation;
            float value;

            Color.RGBToHSV(color, out hue, out saturation, out value);

            return Color.HSVToRGB(
                0.76f,
                Mathf.Clamp(saturation, 0.35f, 0.75f),
                value
            );
        }

        private static void RegisterPackaging(
            string packagingId,
            ProductPackagingContentProfile profile)
        {
            ProductPackagingContentProfileRegistry.Register(
                OwnerId,
                ProductId,
                packagingId,
                profile
            );

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                OwnerId,
                ProductKindId,
                packagingId,
                profile
            );
        }

        private static S1ProductDefinition FindGdpDonorProduct()
        {
            string[] ids =
            {
                "granddaddypurple",
                "grand_daddypurple",
                "granddaddy_purple",
                "granddaddypurple_bud"
            };

            foreach (string id in ids)
            {
                try
                {
                    S1ProductDefinition product =
                        ItemManager.GetDefinition(id)
                        as S1ProductDefinition;

                    if (product != null)
                    {


                        return product;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool TryDiscover()
        {
            try
            {
                if (_definition == null)
                    return false;

                _definition.Discover(false);

                ConsoleItemAliases.Register(
                    "salvia",
                    ProductId
                );

                ConsoleItemAliases.Register(
                    "sage",
                    ProductId
                );

                ConsoleItemAliases.Register(
                    "divinorum",
                    ProductId
                );

                _discovered = true;

                EnsurePresentationRegistered();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Salvia] Product registered and discovered."
                );

                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;

                    MelonLogger.Error(
                        "[WVC Salvia] Discovery failed: " + ex
                    );
                }

                return false;
            }
        }

        public static bool TryRegisterMetadata()
        {
            if (_metadata != null)
                return true;

            if (_definition == null ||
                _productKind == null)
            {
                return false;
            }

            try
            {
                Sprite icon = _definition.Icon;

                if (icon == null)
                    return false;

                _metadata =
                    new ProductKindMetadataBuilder(_productKind)
                        .WithDisplayName("Salvia")

                        .WithColor(
                            new Color(0.38f, 0.60f, 0.28f)
                        )
                        .WithIcon(icon)
                        .WithSortOrder(11)
                        .WithSearchAliases(
                            new string[]
                            {
                                "salvia",
                                "sage",
                                "divinorum"
                            }
                        )
                        .WithProductManagerVisibility(true)
                        .Build();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Salvia] Product metadata registered."
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;

                    MelonLogger.Error(
                        "[WVC Salvia] Metadata failed: " + ex
                    );
                }

                return false;
            }
        }

        public static Sprite DeliveryIcon
        {
            get
            {
                try
                {
                    return _definition != null
                        ? _definition.Icon
                        : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null)
                return;

            if (_presentationRegistering)
                return;

            _presentationRegistering = true;

            try
            {
                if (_definition == null || _productKind == null)
                    return;

                if (_budVisualSource == null)
                    _budVisualSource =
                        SalviaGrowing.GetDonorBudVisualSource();

                if (_budVisualSource == null)
                {


                    return;
                }

                ProductPresentationTransform loose =
                    new ProductPresentationTransform(
                        Vector3.zero,
                        new Vector3(0f, 12f, 0f),
                        Vector3.one * 1.15f
                    );

                ProductPresentationTransform held =
                    new ProductPresentationTransform(
                        new Vector3(0f, 0.002f, 0f),
                        new Vector3(-70f, -1f, 0f),
                        Vector3.one * 1.65f
                    );

                _presentationProfile =
                    new ProductPresentationProfileBuilder()
                        .WithLooseVisual(
                            () => _budVisualSource,
                            loose
                        )
                        .WithHeldVisual(
                            () => _budVisualSource,
                            held
                        )
                        .Build();

                ProductPresentationProfileRegistry.RegisterForProduct(
                    OwnerId,
                    ProductId,
                    _presentationProfile
                );

                ProductPresentationProfileRegistry.RegisterForProductKind(
                    OwnerId,
                    _productKind,
                    _presentationProfile
                );


            }
            catch (Exception ex)
            {
                _presentationProfile = null;

                MelonLogger.Error(
                    "[WVC Salvia] Presentation registration failed: " + ex
                );
            }
            finally
            {
                _presentationRegistering = false;
            }
        }
    }
}
