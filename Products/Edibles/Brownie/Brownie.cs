using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public static class Brownie
    {
        public const string ProductKindId = "westvilleconnection:brownie_v1";
        public const string ProductId = "westvilleconnection:products/brownie_v1";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile _presentationProfile;
        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static GameObject _visualSource;
        private static GameObject _lowPolySource;
        private static Material _visualMaterial;
        private static Material _brickMaterial;
        private static Sprite _staticIcon;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;
        private static int _iconEnforceAttempts;
        private const int MaxIconEnforceAttempts = 180;

        public static bool TryRegister()
        {
            if (_discovered) return true;
            if (!_built && !TryBuild()) return false;
            return TryDiscover();
        }

        private static bool TryBuild()
        {
            try
            {
                var shroomTemplate = ItemManager.GetDefinition("shroom") as S1ProductDefinition;
                if (shroomTemplate == null) return false;

                var baggie = ItemManager.GetDefinition("baggie") as S1PackagingDefinition;
                var jar = ItemManager.GetDefinition("jar") as S1PackagingDefinition;
                var brick = ItemManager.GetDefinition("brick") as S1PackagingDefinition;
                if (baggie == null || jar == null || brick == null) return false;

                if (_productKind == null)
                {
                    _productKind = new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(S1DrugType.Cocaine)
                        .Build();
                }

                _staticIcon = GetOrCreateBrownieIcon();

                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                _definition = CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("Brownie")
                    .WithDescription("A dense, chocolatey brownie.")
                    .WithProductPrice(75f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.45f)
                    .WithDefaultQuality((S1Quality)2)
                    .WithRepresentationsFrom(shroomTemplate)
                    .WithValidPackaging(new S1PackagingDefinition[] { baggie, jar, brick })
                    .WithEffectDurations(300, 600)
                    .WithNativeMixerMap((ProductMixingMap)2)
                    .Build();

                EnforceIcon();
                MelonCoroutines.Start(FixIconRoutine());

                _built = true;
                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged) { _failureLogged = true; MelonLogger.Error("[WVC Brownie] Build failed: " + ex); }
                return false;
            }
        }

        public static void EnforceIcon()
        {
            if (_definition == null || _iconEnforceAttempts >= MaxIconEnforceAttempts) return;
            _iconEnforceAttempts++;

            Sprite icon = GetOrCreateBrownieIcon();
            if (icon == null) return;

            bool wrapperOk = TrySetIconMember(_definition, icon);
            bool rawOk = false;

            object rawObject = GetMember(_definition, "S1ItemDefinition")
                ?? GetMember(_definition, "NativeDefinition")
                ?? GetMember(_definition, "Definition");

            // FIX: fully qualified type name
            Il2CppScheduleOne.ItemFramework.ItemDefinition rawDef = rawObject as Il2CppScheduleOne.ItemFramework.ItemDefinition;

            if (rawDef != null)
            {
                try
                {
                    rawDef.Icon = icon;
                    rawOk = true;
                }
                catch
                {
                    rawOk = TrySetIconMember(rawObject, icon);
                }
            }
            else if (rawObject != null)
            {
                rawOk = TrySetIconMember(rawObject, icon);
            }

            if (_iconEnforceAttempts <= 3 || (_iconEnforceAttempts == 1))
            {
                MelonLogger.Msg(
                    "[WVC Brownie] Icon enforce " + _iconEnforceAttempts +
                    ": wrapper=" + wrapperOk +
                    ", raw=" + rawOk +
                    ", rawType=" + (rawObject?.GetType().Name ?? "null")
                );
            }
        }

        private static System.Collections.IEnumerator FixIconRoutine()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSeconds(0.5f);
                EnforceIcon();
            }
        }

        private static bool TrySetIconMember(object target, Sprite icon)
        {
            if (target == null) return false;
            bool changed = false;
            Type type = target.GetType();
            string[] names = { "Icon", "icon", "_icon", "ItemIcon" };

            while (type != null)
            {
                foreach (string name in names)
                {
                    try
                    {
                        PropertyInfo p = type.GetProperty(name,
                            BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (p != null && p.CanWrite &&
                            p.PropertyType.IsAssignableFrom(typeof(Sprite)))
                        {
                            p.SetValue(target, icon);
                            changed = true;
                        }

                        FieldInfo f = type.GetField(name,
                            BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (f != null &&
                            f.FieldType.IsAssignableFrom(typeof(Sprite)))
                        {
                            f.SetValue(target, icon);
                            changed = true;
                        }
                    }
                    catch { }
                }
                type = type.BaseType;
            }
            return changed;
        }

        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            while (type != null)
            {
                try
                {
                    PropertyInfo p = type.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (p != null) return p.GetValue(target);
                    FieldInfo f = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(target);
                }
                catch { }
                type = type.BaseType;
            }
            return null;
        }

        private static Sprite GetOrCreateBrownieIcon()
        {
            if (_staticIcon != null) return _staticIcon;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Color bg = new Color(0, 0, 0, 0);
            Color dark = new Color(0.22f, 0.11f, 0.05f, 1f);
            Color mid = new Color(0.38f, 0.20f, 0.09f, 1f);
            Color light = new Color(0.52f, 0.30f, 0.14f, 1f);
            Color chip = new Color(0.10f, 0.05f, 0.02f, 1f);
            Color border = new Color(0.15f, 0.07f, 0.03f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 28 || x >= 228 || y < 28 || y >= 228)
                    {
                        tex.SetPixel(x, y, bg);
                        continue;
                    }
                    float v = (y - 28f) / 200f;
                    Color c = Color.Lerp(dark, mid, v);
                    if (v > 0.6f) c = Color.Lerp(c, light, (v - 0.6f) * 1.5f);
                    if (x < 32 || x >= 224 || y < 32 || y >= 224) c = border;
                    if ((x - 90) * (x - 90) + (y - 130) * (y - 130) < 36) c = chip;
                    if ((x - 150) * (x - 150) + (y - 110) * (y - 110) < 25) c = chip;
                    if ((x - 130) * (x - 130) + (y - 170) * (y - 170) < 25) c = chip;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, true);
            _staticIcon = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _staticIcon.name = "WVC_Brownie_Static";
            MelonLogger.Msg("[WVC Brownie] Static icon created.");
            return _staticIcon;
        }

        private static bool TryDiscover()
        {
            try
            {
                _definition.Discover(false);
                ConsoleItemAliases.Register("brownie", ProductId);
                ConsoleItemAliases.Register("edible", ProductId);
                _discovered = true;
                MelonLogger.Msg("[Westville Connection] Brownie registered and discovered.");
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (Exception ex)
            {
                if (!_failureLogged) { _failureLogged = true; MelonLogger.Error("[WVC Brownie] Discovery failed: " + ex); }
                return false;
            }
        }

        public static bool TryRegisterMetadata()
        {
            if (_metadata != null) return true;
            if (_definition == null || _productKind == null) return false;
            try
            {
                EnforceIcon(); // apply before reading back
                Sprite icon = GetOrCreateBrownieIcon();
                _metadata = new ProductKindMetadataBuilder(_productKind)
                    .WithDisplayName("Brownie")
                    .WithColor(new Color(0.36f, 0.20f, 0.09f))
                    .WithIcon(icon)
                    .WithSortOrder(11)
                    .WithSearchAliases(new string[] { "brownie", "edible" })
                    .WithProductManagerVisibility(true)
                    .Build();
                MelonLogger.Msg("[Westville Connection] Brownie Products app category registered.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged) { _metadataFailureLogged = true; MelonLogger.Error("[WVC Brownie] Metadata failed: " + ex); }
                return false;
            }
        }

        private static GameObject GetOrCreateVisualSource()
        {
            if (_visualSource != null) return _visualSource;
            var custom = BrownieObjVisual.GetOrCreate();
            if (custom != null) { _visualSource = custom; return _visualSource; }
            return GetOrCreateLowPolySource();
        }

        private static GameObject GetOrCreateLowPolySource()
        {
            if (_lowPolySource != null) return _lowPolySource;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.36f, 0.20f, 0.09f, 1f));
            mat.SetColor("_Color", new Color(0.36f, 0.20f, 0.09f, 1f));
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "WVC_Brownie_LowPoly";
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
            go.transform.position = new Vector3(0, -20000f, 0);
            go.transform.localScale = new Vector3(0.025f, 0.012f, 0.025f);
            UnityEngine.Object.DontDestroyOnLoad(go);
            _lowPolySource = go;
            return _lowPolySource;
        }

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null) return;

            var loose = new ProductPresentationTransform(Vector3.zero, new Vector3(0f, 0f, 0f), Vector3.one * 0.013f);
            var held = new ProductPresentationTransform(Vector3.zero, new Vector3(0f, 0f, 0f), Vector3.one * 0.06f);

            // NO .WithGeneratedIconFromLooseVisual here
            _presentationProfile = new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => GetOrCreateVisualSource(), loose)
                .WithHeldVisual(() => GetOrCreateVisualSource(), held)
                .WithFunctionalProductConvexMeshColliders()
                .Build();

            ProductPresentationProfileRegistry.RegisterForProduct("westvilleconnection", ProductId, _presentationProfile);
            ProductPresentationProfileRegistry.RegisterForProductKind("westvilleconnection", _productKind, _presentationProfile);
        }

        private static void EnsurePackagingRegistered()
        {
            if (_baggieProfile == null)
            {
                _baggieProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateLowPolySource())
                    .AddPlacement(new ProductPresentationTransform(new Vector3(0f, -0.002f, 0f), Vector3.zero, Vector3.one * 0.027f))
                    .Build();
            }
            if (_jarProfile == null)
            {
                _jarProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateLowPolySource())
                    .AddPlacements(new[]
                    {
                        new ProductPresentationTransform(new Vector3(-0.0045f, 0.0050f, -0.0025f), new Vector3(0f, 18f, 0f), Vector3.one * 0.011f),
                        new ProductPresentationTransform(new Vector3(0.0045f, 0.0055f, 0.0025f), new Vector3(0f, -28f, 0f), Vector3.one * 0.011f),
                        new ProductPresentationTransform(new Vector3(0f, 0.0115f, 0f), new Vector3(4f, 48f, 2f), Vector3.one * 0.0105f),
                    })
                    .Build();
            }
            if (_brickProfile == null)
            {
                _brickProfile = new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(ProductPackagingVisualTemplate.Cocaine, clone => ApplyBrickMaterial(clone), null)
                    .Build();
            }
            ProductPackagingContentProfileRegistry.Register("westvilleconnection", ProductId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.Register("westvilleconnection", ProductId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.Register("westvilleconnection", ProductId, "brick", _brickProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind("westvilleconnection", ProductKindId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind("westvilleconnection", ProductKindId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind("westvilleconnection", ProductKindId, "brick", _brickProfile);
        }

        private static void ApplyBrickMaterial(GameObject clone)
        {
            if (clone == null) return;
            if (_brickMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) return;
                _brickMaterial = new Material(shader);
                _brickMaterial.SetColor("_BaseColor", new Color(0.30f, 0.15f, 0.06f, 1f));
                _brickMaterial.SetColor("_Color", new Color(0.30f, 0.15f, 0.06f, 1f));
                _brickMaterial.SetFloat("_Smoothness", 0.12f);
                _brickMaterial.SetFloat("_Glossiness", 0.12f);
                _brickMaterial.SetFloat("_Metallic", 0f);
            }
            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = _brickMaterial;
                r.sharedMaterials = mats;
            }
        }
    }
}