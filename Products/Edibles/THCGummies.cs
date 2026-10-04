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

namespace CustomNPCExample.Products.Edibles
{
    public static class THCGummies
    {
        public const string ProductKindId = "westvilleconnection:thc_gummies";
        public const string ProductId = "westvilleconnection:products/thc_gummies";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile _presentationProfile;
        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static Material _brickMaterial;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;
        private static bool _presentationRegistering;

        public static bool TryRegister()
        {
            if (_discovered) return true;
            if (!_built && !TryBuild()) return false;
            return TryDiscover();
        }

        private static bool TryBuild()
        {
            if (_built) return true;

            try
            {
                S1ProductDefinition cocaineTemplate =
                    ItemManager.GetDefinition("cocaine") as S1ProductDefinition;

                S1ProductDefinition shroomTemplate =
                    ItemManager.GetDefinition("shroom") as S1ProductDefinition;

                S1ProductDefinition template = shroomTemplate ?? cocaineTemplate;

                if (shroomTemplate == null)
                {

                }

                S1PackagingDefinition baggie =
                    ItemManager.GetDefinition("baggie") as S1PackagingDefinition;
                S1PackagingDefinition jar =
                    ItemManager.GetDefinition("jar") as S1PackagingDefinition;
                S1PackagingDefinition brick =
                    ItemManager.GetDefinition("brick") as S1PackagingDefinition;

                if (template == null || baggie == null || jar == null || brick == null)
                    return false;

                if (_productKind == null)
                {
                    _productKind = new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(S1DrugType.Marijuana)
                        .Build();
                }

                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                _definition = CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("THC Gummies")
                    .WithDescription("Chewy cannabis-infused gummy bears.")
                    .WithProductPrice(45f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.20f)
                    .WithDefaultQuality((S1Quality)2)
                    .WithRepresentationsFrom(template)
                    .WithValidPackaging(new S1PackagingDefinition[] { baggie, jar, brick })
                    .WithEffectDurations(180, 360)
                    .WithNativeMixerMap((ProductMixingMap)2)
                    .Build();

                _built = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Gummies] THC Gummies built (donor: " +
                    (shroomTemplate != null ? "shroom" : "cocaine") + ").");

                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Gummies] Build failed: " + ex);
                }
                return false;
            }
        }

        private static bool TryDiscover()
        {
            if (_definition == null) return false;

            try
            {
                _definition.Discover(false);

                ConsoleItemAliases.Register("gummies", ProductId);
                ConsoleItemAliases.Register("gummy", ProductId);
                ConsoleItemAliases.Register("thcgummies", ProductId);

                _discovered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Gummies] THC Gummies registered and discovered.");
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Gummies] Discovery failed: " + ex);
                }
                return false;
            }
        }

        public static bool TryRegisterMetadata()
        {
            if (_metadata != null) return true;
            if (_definition == null || _productKind == null) return false;

            try
            {
                Sprite icon = _definition.Icon;
                if (icon == null) return false;

                _metadata = new ProductKindMetadataBuilder(_productKind)
                    .WithDisplayName("THC Gummies")
                    .WithColor(new Color(0.90f, 0.04f, 0.025f))
                    .WithIcon(icon)
                    .WithSortOrder(11)
                    .WithSearchAliases(new string[] { "gummies", "gummy", "thc gummies" })
                    .WithProductManagerVisibility(true)
                    .Build();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Gummies] Product Manager metadata registered.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;
                    MelonLogger.Error("[WVC Gummies] Metadata failed: " + ex);
                }
                return false;
            }
        }

        private static GameObject GetVisual()
        {
            return GummyVisual.GetOrCreate();
        }

        private static bool _iconRepairDone;
        private static float _iconRepairTimer;

        public static void UpdateIconRepair()
        {
            if (_iconRepairDone || _definition == null)
                return;

            GameObject visual = GetVisual();
            if (visual == null)
                return;

            _iconRepairTimer += Time.deltaTime;

            if (_iconRepairTimer < 90f)
                return;

            _iconRepairDone = true;

            try
            {
                Sprite clean =
                    DMTIngredients.RenderModelIcon(
                        visual,
                        "WVC_Gummies_Product_Icon_Clean"
                    );

                if (clean != null)
                {
                    bool applied =
                        global::CustomNPCExample.Utils.WvcIcon.Apply(ProductId, clean);

                    if (applied)
                    {
                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Gummies] Clean product icon applied over generated icon."
                        );
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null) return;

            if (_presentationRegistering)
            {

                return;
            }

            _presentationRegistering = true;

            try
            {
                ProductPresentationTransform loose = new ProductPresentationTransform(
                    Vector3.zero, new Vector3(90f, 0f, 180f), Vector3.one * 0.85f);

                ProductPresentationTransform held = new ProductPresentationTransform(
                    Vector3.zero, new Vector3(90f, 0f, 180f), Vector3.one * 1.9f);

                _presentationProfile = new ProductPresentationProfileBuilder()
                    .WithLooseVisual(() => GetVisual(), loose)
                    .WithHeldVisual(() => GetVisual(), held)
                    .WithFunctionalProductConvexMeshColliders()
                    .WithGeneratedIconFromLooseVisual(512, true, 0.78f)
                    .Build();

                ProductPresentationProfileRegistry.RegisterForProduct(
                    "westvilleconnection", ProductId, _presentationProfile);
                ProductPresentationProfileRegistry.RegisterForProductKind(
                    "westvilleconnection", _productKind, _presentationProfile);

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Gummies] Presentation profile registered successfully.");
            }
            catch (Exception ex)
            {
                _presentationProfile = null;
                MelonLogger.Error("[WVC Gummies] Presentation registration failed: " + ex);
            }
            finally
            {
                _presentationRegistering = false;
            }
        }

        private static void EnsurePackagingRegistered()
        {
            if (_baggieProfile == null)
            {
                _baggieProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetVisual())
                    .AddPlacement(new ProductPresentationTransform(
                        Vector3.zero, new Vector3(90f, 0f, 180f), Vector3.one * 0.75f))
                    .Build();
            }

            if (_jarProfile == null)
            {
                _jarProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetVisual())
                    .AddPlacements(new ProductPresentationTransform[]
                    {
                        new ProductPresentationTransform(
                            new Vector3(0f, 0.007f, 0f),
                            new Vector3(90f, 0f, 180f),
                            Vector3.one * 0.80f),

                        new ProductPresentationTransform(
                            new Vector3(0f, 0.007f, 0.012f),
                            new Vector3(90f, 20f, 180f),
                            Vector3.one * 0.78f),

                        new ProductPresentationTransform(
                            new Vector3(-0.012f, 0.007f, 0f),
                            new Vector3(90f, -75f, 180f),
                            Vector3.one * 0.78f),

                        new ProductPresentationTransform(
                            new Vector3(0.012f, 0.007f, 0f),
                            new Vector3(90f, 75f, 180f),
                            Vector3.one * 0.78f),

                        new ProductPresentationTransform(
                            new Vector3(0f, 0.007f, -0.012f),
                            new Vector3(90f, -160f, 180f),
                            Vector3.one * 0.78f),

                        new ProductPresentationTransform(
                            new Vector3(-0.006f, 0.017f, 0.006f),
                            new Vector3(90f, 45f, 180f),
                            Vector3.one * 0.70f),

                        new ProductPresentationTransform(
                            new Vector3(0.007f, 0.017f, -0.005f),
                            new Vector3(90f, -110f, 180f),
                            Vector3.one * 0.70f),

                        new ProductPresentationTransform(
                            new Vector3(0.001f, 0.017f, 0.001f),
                            new Vector3(90f, 160f, 180f),
                            Vector3.one * 0.68f)
                    })
                    .Build();
            }

            if (_brickProfile == null)
            {
                _brickProfile = new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Cocaine,
                        clone => ApplyGummyBrickMaterial(clone),
                        null)
                    .Build();
            }

            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "brick", _brickProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "brick", _brickProfile);
        }

        private static void ApplyGummyBrickMaterial(GameObject clone)
        {
            if (clone == null) return;

            if (_brickMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                if (shader == null) return;

                _brickMaterial = new Material(shader);
                _brickMaterial.name = "WVC_Gummy_Brick_Material";

                Color red = new Color(0.80f, 0.06f, 0.05f, 1f);
                if (_brickMaterial.HasProperty("_BaseColor")) _brickMaterial.SetColor("_BaseColor", red);
                if (_brickMaterial.HasProperty("_Color")) _brickMaterial.SetColor("_Color", red);
                if (_brickMaterial.HasProperty("_Smoothness")) _brickMaterial.SetFloat("_Smoothness", 0.7f);
                if (_brickMaterial.HasProperty("_Glossiness")) _brickMaterial.SetFloat("_Glossiness", 0.7f);
                if (_brickMaterial.HasProperty("_Metallic")) _brickMaterial.SetFloat("_Metallic", 0f);
            }

            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = _brickMaterial;
                r.sharedMaterials = mats;
            }
        }
    }
}
