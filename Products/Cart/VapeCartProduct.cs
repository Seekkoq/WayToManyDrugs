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
    public static class VapeCartProduct
    {
        public const string ProductKindId = "westvilleconnection:vape_cart";

        public const string ProductId = "westvilleconnection:products/vape_cart";
        public const string ProductId_Premium = "westvilleconnection:products/vape_cart_premium";
        public const string ProductId_Heavenly = "westvilleconnection:products/vape_cart_heavenly";

        private static ProductKind _productKind;

        private static CustomProductDefinition _defStandard;
        private static CustomProductDefinition _defPremium;
        private static CustomProductDefinition _defHeavenly;

        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile _presentationProfile;
        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static GameObject _looseSource;
        private static Material _brickMaterial;
        private static Sprite _cartIcon;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;
        private static bool _waitingForManifestLogged;
        private static bool _heldApplied;
        private static bool _iconRefreshStarted;

        public static bool TryRegister()
        {
            if (_discovered) return true;
            if (!_built && !TryBuild()) return false;
            return TryDiscover();
        }

        private static S1Quality ResolveApiQuality(string name)
        {
            return (S1Quality)Enum.Parse(typeof(S1Quality), name, true);
        }

        private static bool TryBuild()
        {
            try
            {
                var weedTemplate = ItemManager.GetDefinition("ogkush") as S1ProductDefinition;
                var fallbackTemplate = ItemManager.GetDefinition("shroom") as S1ProductDefinition;
                S1ProductDefinition template = weedTemplate ?? fallbackTemplate;
                if (template == null) return false;

                var baggie = ItemManager.GetDefinition("baggie") as S1PackagingDefinition;
                var jar = ItemManager.GetDefinition("jar") as S1PackagingDefinition;
                var brick = ItemManager.GetDefinition("brick") as S1PackagingDefinition;

                if (baggie == null || jar == null || brick == null) return false;

                if (CartObjVisual.GetOrCreate() == null) return false;

                if (_productKind == null)
                {
                    _productKind = new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(ResolveWeedDrugType())
                        .Build();
                }

                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                var validPackaging = new S1PackagingDefinition[] { baggie, jar, brick };

                _defStandard = CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("Vape Cart")
                    .WithDescription("A refillable ceramic THC vape cartridge.")
                    .WithProductPrice(50f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.25f)

                    .WithRepresentationsFrom(template)

                    .WithDefaultQuality(ResolveApiQuality("Standard"))

                    .WithValidPackaging(validPackaging)
                    .WithEffectDurations(180, 300)
                    .WithNativeMixerMap((ProductMixingMap)0)
                    .Build();

                _defPremium = CustomProductItemCreator
                    .CreateBuilder(ProductId_Premium, _productKind)
                    .WithName("Premium Vape Cart")
                    .WithDescription("A high-potency distillate ceramic THC cartridge.")
                    .WithProductPrice(95f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.35f)

                    .WithRepresentationsFrom(template)
                    .WithDefaultQuality(ResolveApiQuality("Premium"))

                    .WithValidPackaging(validPackaging)
                    .WithEffectDurations(240, 420)
                    .WithNativeMixerMap((ProductMixingMap)0)
                    .Build();

                _defHeavenly = CustomProductItemCreator
                    .CreateBuilder(ProductId_Heavenly, _productKind)
                    .WithName("Heavenly Vape Cart")
                    .WithDescription("Pure live resin distillate cartridge. Top-tier quality.")
                    .WithProductPrice(160f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.50f)

                    .WithRepresentationsFrom(template)
                    .WithDefaultQuality(ResolveApiQuality("Heavenly"))

                    .WithValidPackaging(validPackaging)
                    .WithEffectDurations(360, 600)
                    .WithNativeMixerMap((ProductMixingMap)0)
                    .Build();

                _built = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] All 3 cart quality definitions built.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Cart] Product build failed: " + ex);
                }
                return false;
            }
        }

        private static bool TryDiscover()
        {
            if (_discovered)
                return true;

            if (_defStandard == null ||
                _defPremium == null ||
                _defHeavenly == null)
            {
                return false;
            }

            if (ItemManager.GetDefinition(ProductId) == null ||
                ItemManager.GetDefinition(ProductId_Premium) == null ||
                ItemManager.GetDefinition(ProductId_Heavenly) == null)
            {
                if (!_waitingForManifestLogged)
                {
                    _waitingForManifestLogged = true;
                }

                return false;
            }

            try
            {
                _defStandard.Discover(false);
                _defPremium.Discover(false);
                _defHeavenly.Discover(false);
            }
            catch (Exception)
            {

                return false;
            }

            try
            {
                if (!_heldApplied)
                {
                    bool standardHeld =
                        CartItem.ApplyOriginalHeldRepresentationToProduct(
                            ProductId);

                    bool premiumHeld =
                        CartItem.ApplyOriginalHeldRepresentationToProduct(
                            ProductId_Premium);

                    bool heavenlyHeld =
                        CartItem.ApplyOriginalHeldRepresentationToProduct(
                            ProductId_Heavenly);

                    _heldApplied =
                        standardHeld &&
                        premiumHeld &&
                        heavenlyHeld;
                }

                CartItem.ApplyCartIconToProduct(
                    ProductId);

                CartItem.ApplyCartIconToProduct(
                    ProductId_Premium);

                CartItem.ApplyCartIconToProduct(
                    ProductId_Heavenly);

                if (!_iconRefreshStarted)
                {
                    _iconRefreshStarted = true;

                    MelonCoroutines.Start(
                        RefreshProductIconAfterUiLoad());
                }

                RegisterCartAlias(
                    "cart",
                    ProductId);

                RegisterCartAlias(
                    "vape",
                    ProductId);

                RegisterCartAlias(
                    "vapecart",
                    ProductId);

                RegisterCartAlias(
                    "cartridge",
                    ProductId);

                RegisterCartAlias(
                    "cartprem",
                    ProductId_Premium);

                RegisterCartAlias(
                    "premiumcart",
                    ProductId_Premium);

                RegisterCartAlias(
                    "cartheav",
                    ProductId_Heavenly);

                RegisterCartAlias(
                    "heavenlycart",
                    ProductId_Heavenly);
            }
            catch (Exception)
            {
            }

            return true;
        }

        private static void RegisterCartAlias(
            string alias,
            string productId)
        {
            try
            {
                ConsoleItemAliases.Register(
                    alias,
                    productId);
            }
            catch (Exception)
            {

            }
        }

        private static System.Collections.IEnumerator RefreshProductIconAfterUiLoad()
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(1.5f);
                CartItem.ApplyCartIconToProduct(ProductId);
                CartItem.ApplyCartIconToProduct(ProductId_Premium);
                CartItem.ApplyCartIconToProduct(ProductId_Heavenly);
            }
        }

        public static bool TryRegisterMetadata()
        {
            if (_metadata != null) return true;
            if (!_discovered || _productKind == null) return false;

            try
            {
                if (_cartIcon == null)
                    _cartIcon = CartItem.GetOrCreateCartIcon();

                Sprite icon = _cartIcon;
                if (icon == null) return false;

                _metadata = new ProductKindMetadataBuilder(_productKind)
                    .WithDisplayName("Vape Carts")
                    .WithColor(new Color(0.95f, 0.62f, 0.15f))
                    .WithIcon(icon)
                    .WithSortOrder(20)
                    .WithSearchAliases(new string[] { "cart", "vape", "cartridge", "pen" })
                    .WithProductManagerVisibility(true)
                    .Build();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] Products app category registered.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;
                    MelonLogger.Error("[WVC Cart] Metadata failed: " + ex);
                }
                return false;
            }
        }

        private static S1DrugType ResolveWeedDrugType()
        {
            string[] candidates = { "Marijuana", "Weed", "Cannabis" };
            foreach (string name in candidates)
            {
                try
                {
                    if (Enum.IsDefined(typeof(S1DrugType), name))
                        return (S1DrugType)Enum.Parse(typeof(S1DrugType), name);
                }
                catch { }
            }
            return (S1DrugType)0;
        }

        private static GameObject GetOrCreateLooseSource()
        {
            if (_looseSource != null) return _looseSource;
            _looseSource = CartObjVisual.GetOrCreate();
            return _looseSource;
        }

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null) return;

            ProductPresentationTransform loose =
                new ProductPresentationTransform(
                    Vector3.zero,
                    new Vector3(0f, 0f, 90f),
                    Vector3.one * 0.014f
                );

            _presentationProfile = new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => GetOrCreateLooseSource(), loose)
                .WithFunctionalProductConvexMeshColliders()
                .Build();

            ProductPresentationProfileRegistry.RegisterForProductKind(
                "westvilleconnection", _productKind, _presentationProfile);
        }

        private static void EnsurePackagingRegistered()
        {
            if (_baggieProfile == null)
            {
                _baggieProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateLooseSource())
                    .AddPlacement(new ProductPresentationTransform(
                        new Vector3(0f, -0.001f, 0f),
                        new Vector3(0f, 0f, 90f),
                        Vector3.one * 0.009f
                    ))
                    .Build();
            }

            if (_jarProfile == null)
            {
                _jarProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateLooseSource())
                    .AddPlacements(new ProductPresentationTransform[]
                    {
                        JarCart(-0.006f, -0.006f, -0.003f,  15f, 0.0075f),
                        JarCart( 0.006f, -0.006f,  0.003f, -30f, 0.0075f),
                        JarCart( 0.000f, -0.006f,  0.000f,  70f, 0.0075f)
                    })
                    .Build();
            }

            if (_brickProfile == null)
            {
                _brickProfile = new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Cocaine,
                        clone => ApplyBrickMaterial(clone),
                        null)
                    .Build();
            }

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "baggie", _baggieProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "jar", _jarProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "brick", _brickProfile);
        }

        private static ProductPresentationTransform JarCart(float x, float y, float z, float yRotation, float scale)
        {
            return new ProductPresentationTransform(
                new Vector3(x, y, z),
                new Vector3(0f, yRotation, 0f),
                Vector3.one * scale
            );
        }

        private static void ApplyBrickMaterial(GameObject clone)
        {
            if (clone == null) return;

            if (_brickMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Sprites/Default");
                if (shader == null) return;

                _brickMaterial = new Material(shader) { name = "WVC_VapeCart_Brick_Material" };
                Color amber = new Color(0.85f, 0.55f, 0.12f, 1f);

                if (_brickMaterial.HasProperty("_BaseColor")) _brickMaterial.SetColor("_BaseColor", amber);
                if (_brickMaterial.HasProperty("_Color")) _brickMaterial.SetColor("_Color", amber);
                if (_brickMaterial.HasProperty("_Smoothness")) _brickMaterial.SetFloat("_Smoothness", 0.35f);
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
