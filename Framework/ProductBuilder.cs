using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Products;
using S1MAPI.Gltf;
using S1MAPI.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using S1 = Il2CppScheduleOne;
using S1Product = Il2CppScheduleOne.Product;

namespace CustomNPCExample.Framework
{
    internal static class ModInfo
    {
        internal const string id = "westvilleconnection";
        internal const string rootNamespace = "CustomNPCExample";
    }

    internal class ProductBuilder
    {
        private string id = string.Empty;
        private string name = string.Empty;
        private string description = string.Empty;
        private string resource = string.Empty;
        private string consumptionTemplate = String.Empty;

        private string scaffoldId = "cocaine";
        private DrugType scaffoldDrugType = DrugType.Cocaine;
        private bool copyScaffoldEffects;

        private readonly List<S1API.Properties.Interfaces.PropertyBase> properties =
            new List<S1API.Properties.Interfaces.PropertyBase>();

        private int? playerEffectSeconds;
        private int? npcEffectSeconds;

        private string productId => $"{ModInfo.id}:products/{id}";
        private string kindId => $"{ModInfo.id}:kinds/{id}";
        public string mixingId => $"{ModInfo.id}:mixing/{id}";
        private string assetName => $"{ModInfo.rootNamespace}_{char.ToUpper(id[0])}{id.Substring(1)}";
        private string consoleAlias => id;

        private bool discover;
        private bool mixingEnabled;
        private bool brickSet;

        private int sortOrder;

        private float price;
        private float addictiveness;
        private float cameraFill;
        private bool iconPose;
        private float modelTargetSize = 0.045f;
        private float mixingMaximumPrice;
        private float mixingPriceIncrease;

        private LegalStatus legalStatus;
        private ProductPackagingVisualTemplate brickTemplate;

        private Color metadataColor = Color.white;

        private ProductKind? kind;
        private CustomProductDefinition? definition;
        private ProductPresentationProfile? presentationProfile;
        private ProductPackagingContentProfile? baggieProfile;
        private ProductPackagingContentProfile? jarProfile;
        private ProductPackagingContentProfile? brickProfile;
        private ProductMixingProfile? mixingProfile;
        private ProductKindMetadata? metadata;
        private ProductPresentationTransform loosePose = new ProductPresentationTransform(Vector3.zero, Vector3.zero, Vector3.one);
        private ProductPresentationTransform heldPose = new ProductPresentationTransform(Vector3.zero, Vector3.zero, Vector3.one);
        private ProductPresentationTransform functionalPose = new ProductPresentationTransform(Vector3.zero, Vector3.zero, Vector3.one);
        private ProductPresentationTransform baggiePlacement = new ProductPresentationTransform(Vector3.zero, Vector3.zero, Vector3.one);
        private ProductPresentationTransform[] jarPlacements = Array.Empty<ProductPresentationTransform>();

        private GameObject? consumptionSource;
        private GameObject? asset;
        private GameObject? fitNode;
        private GameObject? assetRoot;
        private Vector3 modelEulerAngles = Vector3.zero;

        /// <summary>
        /// The plain wrapper that gets handed to S1API and to the packaging providers. Every pose
        /// rewrites the transform of the object it clones, so the unit fit and the orientation fix
        /// live on a child node instead of on the wrapper itself.
        /// </summary>
        internal GameObject? VisualAsset => asset;


        internal ProductBuilder ID(string Id)
        {
            id = Id;
            return this;
        }

        internal ProductBuilder Name(string Name)
        {
            name = Name;
            return this;
        }

        internal ProductBuilder Description(string Description)
        {
            description = Description;
            return this;
        }

        internal ProductBuilder Price(float Price)
        {
            price = Price;
            return this;
        }

        internal ProductBuilder LegalStatus(LegalStatus LegalStatus)
        {
            legalStatus = LegalStatus;
            return this;
        }

        internal ProductBuilder Addictiveness(float Addictiveness)
        {
            addictiveness = Addictiveness;
            return this;
        }

        internal ProductBuilder Model(string ResourcePath)
        {
            resource = ResourcePath;
            return this;
        }

        /// <summary>Longest axis, in world units, the source model is scaled down to.</summary>
        internal ProductBuilder ModelFit(float LongestDimension)
        {
            modelTargetSize = LongestDimension;
            return this;
        }

        /// <summary>
        /// Local rotation applied to the source model inside the fitted wrapper. Source meshes are
        /// authored in arbitrary orientations while the native icon camera and the product poses all
        /// assume the object faces the viewer, so a source model can be straightened up here.
        /// </summary>
        internal ProductBuilder ModelRotation(Vector3 EulerAngles)
        {
            modelEulerAngles = EulerAngles;
            return this;
        }

        internal ProductBuilder Scaffold(string TemplateId, DrugType CompatibilityDrugType)
        {
            scaffoldId = TemplateId;
            scaffoldDrugType = CompatibilityDrugType;
            return this;
        }

        internal ProductBuilder CopyScaffoldEffects(bool Enabled)
        {
            copyScaffoldEffects = Enabled;
            return this;
        }

        /// <summary>
        /// Effects shown on the product card. Every property must resolve to a distinct native
        /// property and a product can carry at most eight. Native tokens live on
        /// <see cref="S1API.Properties.Property"/>, for example
        /// <c>Property.Calming</c>; see <see cref="CustomNPCExample.Products.Xanax"/>.
        /// </summary>
        internal ProductBuilder Properties(
            params S1API.Properties.Interfaces.PropertyBase[] Properties)
        {
            if (Properties == null)
                return this;

            foreach (S1API.Properties.Interfaces.PropertyBase property in Properties)
            {
                if (property != null && !properties.Contains(property))
                    properties.Add(property);
            }

            return this;
        }

        /// <summary>
        /// How long a consumed unit keeps its effects, in seconds, for the player and for NPCs.
        /// Native products default this per product, so it is only set when a product asks for it.
        /// </summary>
        internal ProductBuilder EffectDurations(int PlayerSeconds, int NpcSeconds)
        {
            playerEffectSeconds = PlayerSeconds;
            npcEffectSeconds = NpcSeconds;
            return this;
        }

        internal ProductBuilder Mixing(bool Enabled, float MixingMaximumPrice, float MixingPriceIncrease)
        {
            mixingEnabled = Enabled;
            mixingMaximumPrice = MixingMaximumPrice;
            mixingPriceIncrease = MixingPriceIncrease;
            return this;
        }

        public ProductBuilder LoosePose(Vector3 Position, Vector3 Rotation, Vector3 Scale)
        {
            loosePose = new ProductPresentationTransform(Position, Rotation, Scale);
            return this;
        }

        public ProductBuilder HeldPose(Vector3 Position, Vector3 Rotation, Vector3 Scale)
        {
            heldPose = new ProductPresentationTransform(Position, Rotation, Scale);
            return this;
        }

        public ProductBuilder FunctionalPose(Vector3 Position, Vector3 Rotation, Vector3 Scale)
        {
            functionalPose = new ProductPresentationTransform(Position, Rotation, Scale);
            return this;
        }

        public ProductBuilder Baggie(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            baggiePlacement = new ProductPresentationTransform(position, rotation, scale);
            return this;
        }

        public ProductBuilder Jar(params ProductPresentationTransform[] placements)
        {
            jarPlacements = placements ?? Array.Empty<ProductPresentationTransform>();
            return this;
        }

        public static ProductPresentationTransform JarPlacement(float x, float y, float zRotation, float scale)
        {
            return new ProductPresentationTransform(new Vector3(x, y, 0f), new Vector3(78f, 0f, zRotation), Vector3.one * scale);
        }

        /// <summary>
        /// Same as the (x, y) overload but with a full offset, for jars that hold several units
        /// laid out side by side instead of one.
        /// </summary>
        public static ProductPresentationTransform JarPlacement(Vector3 Position, float zRotation, float scale)
        {
            return new ProductPresentationTransform(Position, new Vector3(78f, 0f, zRotation), Vector3.one * scale);
        }

        public ProductBuilder Brick(ProductPackagingVisualTemplate Template)
        {
            brickTemplate = Template;
            brickSet = true;
            return this;
        }

        internal ProductBuilder Icon(float CameraFill)
        {
            cameraFill = CameraFill;
            return this;
        }

        /// <summary>
        /// Turns the model while its icon is rendered so the face carrying the artwork looks at
        /// the native icon camera. The loose pose is left alone, so this changes the inventory
        /// icon only. See <see cref="CustomNPCExample.Utils.WvcIconPose"/>.
        /// </summary>
        internal ProductBuilder IconPose()
        {
            iconPose = true;
            return this;
        }

        internal ProductBuilder ConsumptionTemplate(string Template)
        {
            consumptionTemplate = Template;
            return this;
        }

        internal ProductBuilder ProductKindColor(Color Color)
        {
            metadataColor = Color;
            return this;
        }

        internal ProductBuilder SortOrder(int Order)
        {
            sortOrder = Order;
            return this;
        }

        internal bool IsBuilt => definition != null;

        internal bool IsFullyLoaded => metadata != null;

        internal string ProductId => productId;

        internal void RegisterContent()
        {
            if (definition != null) return;

            ProductDefinition templateDefinition = ItemManager.GetDefinition(scaffoldId) as ProductDefinition ?? throw new InvalidOperationException($"The native '{scaffoldId}' product scaffold is unavailable.");

            kind ??= new ProductKindBuilder(kindId).WithCompatibilityDrugType(scaffoldDrugType).Build();

            GetOrLoad();

            EnsurePresentationRegistered();
            EnsurePackagingRegistered();
            RegisterMixing(kind);

            definition = CreateDefinition(templateDefinition).Build();

            if (!string.IsNullOrWhiteSpace(consoleAlias)) ConsoleItemAliases.Register(consoleAlias, productId);

            //Create recipe exactly here


        }

        internal void CompleteLoad()
        {
            if (definition == null || kind == null) return;

            if (metadata == null)
            {
                Sprite icon = definition.Icon;

                if (icon == null) { }
                else metadata = new ProductKindMetadataBuilder(kind)
                        .WithDisplayName(name)
                        .WithColor(metadataColor)
                        .WithIcon(icon)
                        .WithSortOrder(sortOrder)
                        .WithSearchAliases(id)
                        .WithProductManagerVisibility(true)
                        .Build();
            }

            definition.Discover(discover);
        }

        internal CustomProductDefinitionBuilder Restore()
        {

            ProductDefinition templateDefinition = ItemManager.GetDefinition(scaffoldId) as ProductDefinition ?? throw new InvalidOperationException($"Cannot restore {name} without the native {scaffoldId} product scaffold.");

            kind ??= new ProductKindBuilder(kindId)
                .WithCompatibilityDrugType(scaffoldDrugType)
                .Build();

            GetOrLoad();

            EnsurePresentationRegistered();
            EnsurePackagingRegistered();
            RegisterMixing(kind);

            return CreateDefinition(templateDefinition);
        }

        private CustomProductDefinitionBuilder CreateDefinition(ProductDefinition template)
        {
            ProductKind productKind = kind ?? throw new InvalidOperationException($"Product kind '{kindId}' is not registered.");

            CustomProductDefinitionBuilder builder = CustomProductItemCreator.CreateBuilder(productId, productKind)
                .WithRepresentationsFrom(template)
                .WithName(name)
                .WithDescription(description)
                .WithProductPrice(price)
                .WithLegalStatus(legalStatus)
                .WithBaseAddictiveness(addictiveness)
                .WithDefaultQuality(Quality.Standard)
                .WithSaveProvider($"{ModInfo.id}:products", 2, id);

            if (copyScaffoldEffects)
            {
                try
                {
                    var properties = template.Properties;

                    if (properties != null)
                    {
                        List<S1API.Properties.Interfaces.PropertyBase> copied = new List<S1API.Properties.Interfaces.PropertyBase>();

                        foreach (var property in properties)
                        {
                            S1API.Properties.Interfaces.PropertyBase baseProperty = property as S1API.Properties.Interfaces.PropertyBase;

                            if (baseProperty != null) copied.Add(baseProperty);
                        }

                        if (copied.Count > 0) builder.WithProperties(copied.ToArray());
                    }
                }
                catch (Exception)
                {

                }
            }

            if (properties.Count > 0)
            {
                try
                {
                    builder.WithProperties(properties.ToArray());
                }
                catch (Exception)
                {

                }
            }

            if (playerEffectSeconds.HasValue && npcEffectSeconds.HasValue)
            {
                try
                {
                    builder.WithEffectDurations(
                        playerEffectSeconds.Value,
                        npcEffectSeconds.Value
                    );
                }
                catch (Exception)
                {

                }
            }

            return builder;
        }

        private void RegisterMixing(ProductKind productKind)
        {
            if (!mixingEnabled) return;

            mixingProfile ??= new ProductMixingProfileBuilder(productKind)
                .WithMixerMap(ProductMixingMap.Cocaine)
                .WithPropertyColorMixing()
                .WithOutputFactoryCompatibility(mixingId, 2)
                .WithOutputFactory(input => new ProductMixingOutputDefinition(input.MixName, input.SourceKind, Math.Min(mixingMaximumPrice, input.SourcePrice + mixingPriceIncrease)))
                .Build();
        }

        private void EnsurePresentationRegistered()
        {
            if (presentationProfile != null || asset == null) return;

            if (iconPose) CustomNPCExample.Utils.WvcIconPose.Watch(asset.name);

            consumptionSource ??= CreateConsumptionSource(loosePose);

            presentationProfile = new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => asset, loosePose)
                .WithHeldVisual(() => asset, heldPose)
                .WithFunctionalProductVisual(() => asset, functionalPose)
                .WithFunctionalProductConvexMeshColliders()
                .WithGeneratedIconFromLooseVisual(512, true, cameraFill)
                .WithConsumptionPrefab(() => consumptionSource)
                .Require(
                    ProductPresentationContext.Loose,
                    ProductPresentationContext.Stored,
                    ProductPresentationContext.Held,
                    ProductPresentationContext.Station,
                    ProductPresentationContext.FunctionalProduct,
                    ProductPresentationContext.Icon,
                    ProductPresentationContext.Consumption)
                .Build();

            if (presentationProfile == null) return;

            ProductPresentationProfileRegistry.RegisterForProduct(ModInfo.id, productId, presentationProfile);
            ProductPresentationProfileRegistry.RegisterForProductKind(ModInfo.id, kind ?? throw new InvalidOperationException($"Product kind '{kindId}' is unavailable."), presentationProfile);
        }

        private void EnsurePackagingRegistered()
        {
            baggieProfile ??= new ProductPackagingContentProfileBuilder()
                    .WithContent(() => asset)
                    .AddPlacement(baggiePlacement)
                    .Build();

            ProductPackagingContentProfileRegistry.Register(ModInfo.id, productId, "baggie", baggieProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(ModInfo.id, kindId, "baggie", baggieProfile);

            jarProfile ??= new ProductPackagingContentProfileBuilder()
                    .WithContent(() => asset)
                    .AddPlacements(jarPlacements)
                    .Build();

            ProductPackagingContentProfileRegistry.Register(ModInfo.id, productId, "jar", jarProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(ModInfo.id, kindId, "jar", jarProfile);

            if (!brickSet) return;

            Material material = null;

            foreach (Renderer renderer in asset.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material rendererMaterial in renderer.sharedMaterials)
                {
                    if (rendererMaterial != null)
                    {
                        material = rendererMaterial;
                        break;
                    }
                }

                if (material != null) break;

            }

            brickProfile ??= new ProductPackagingContentProfileBuilder()
                .WithNativeFilledVisualScaffold(brickTemplate, clone =>
                {
                    if (material != null)
                    {
                        bool customized = false;

                        foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
                        {
                            if (!string.IsNullOrWhiteSpace("Brick_LOD") && !renderer.name.StartsWith("Brick_LOD", StringComparison.OrdinalIgnoreCase)) continue;

                            Material[] materials = renderer.sharedMaterials;

                            for (int index = 0; index < materials.Length; index++) materials[index] = material;

                            renderer.sharedMaterials = materials;
                            customized = true;
                        }

                        if (!customized) throw new InvalidOperationException($"No matching renderers were found for packaging material prefix '{"Brick_LOD"}'.");
                    }
                })
                .Build();

            ProductPackagingContentProfileRegistry.Register(ModInfo.id, productId, "brick", brickProfile);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(ModInfo.id, kindId, "brick", brickProfile);
        }

        /// <summary>
        /// Resolves a native <see cref="S1Product.ProductDefinition"/> by id.
        /// The native registry result cannot be used directly: S1API hands out product data through
        /// Il2Cpp cross-type checks, and a raw CLR cast of the registry result can come back null even
        /// though the item exists. Scanning the loaded product definitions yields correctly typed
        /// native proxies instead.
        /// </summary>
        private static S1Product.ProductDefinition ResolveNativeProductDefinition(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId)) return null;

            try
            {
                S1Product.ProductDefinition matchWithoutAnimation = null;

                foreach (S1Product.ProductDefinition definition in
                    UnityEngine.Resources.FindObjectsOfTypeAll<S1Product.ProductDefinition>())
                {
                    if (definition == null || string.IsNullOrEmpty(definition.ID)) continue;

                    if (!string.Equals(definition.ID, templateId, StringComparison.OrdinalIgnoreCase)) continue;

                    if (definition.ConsumeAnimation != null) return definition;

                    if (matchWithoutAnimation == null) matchWithoutAnimation = definition;
                }

                if (matchWithoutAnimation != null) return matchWithoutAnimation;
            }
            catch (Exception)
            {

            }

            return null;
        }

        private static S1Product.ProductDefinition FindConsumptionAnimationOwner()
        {
            try
            {
                foreach (S1Product.ProductDefinition definition in
                    UnityEngine.Resources.FindObjectsOfTypeAll<S1Product.ProductDefinition>())
                {
                    if (definition != null && definition.ConsumeAnimation != null) return definition;
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        private GameObject CreateConsumptionSource(ProductPresentationTransform pose)
        {
            S1Product.ProductDefinition nativeTemplate = ResolveNativeProductDefinition(consumptionTemplate) ?? throw new InvalidOperationException($"Cannot create the product consumption prefab without the native {consumptionTemplate} scaffold.");

            if (nativeTemplate.ConsumeAnimation == null)
            {
                S1Product.ProductDefinition substitute = FindConsumptionAnimationOwner();

                if (substitute == null)
                {
                    throw new InvalidOperationException($"The native '{consumptionTemplate}' scaffold has no consume animation and no substitute was found.");
                }


                nativeTemplate = substitute;
            }

            GameObject consumptionSource = UnityEngine.Object.Instantiate(nativeTemplate.ConsumeAnimation.gameObject);
            consumptionSource.name = "ExtraDrugs_Consumption";
            UnityEngine.Object.DontDestroyOnLoad(consumptionSource);
            consumptionSource.transform.position = new Vector3(0f, -20000f, 0f);

            GameObject visual = new GameObject("ExtraDrugs_Consumption_Visual");
            visual.transform.SetParent(consumptionSource.transform, false);
            visual.transform.localPosition = pose.LocalPosition;
            visual.transform.localEulerAngles = pose.LocalEulerAngles;
            visual.transform.localScale = pose.LocalScale;

            if (asset == null) throw new InvalidOperationException("Cannot create consumption source because the product asset is null.");

            GameObject model = UnityEngine.Object.Instantiate(asset);
            model.name = "ProductVisual";
            model.transform.SetParent(visual.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localEulerAngles = Vector3.zero;
            model.transform.localScale = Vector3.one;
            model.SetActive(false);

            consumptionSource.SetActive(true);

            return consumptionSource;
        }

        internal void GetOrLoad()
        {
            if (asset != null) return;

            byte[] bytes = EmbeddedResourceLoader.LoadBytes(resource, typeof(ProductBuilder).Assembly);

            if (bytes == null)
            {
                foreach (string name in typeof(ProductBuilder).Assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith(resource, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var stream = typeof(ProductBuilder).Assembly.GetManifestResourceStream(name))
                        {
                            if (stream != null)
                            {
                                bytes = new byte[stream.Length];
                                stream.Read(bytes, 0, bytes.Length);
                            }
                        }

                        break;
                    }
                }
            }

            if (bytes == null) throw new InvalidOperationException($"Embedded GLB resource '{resource}' was not found.");

            assetRoot = new GameObject($"{char.ToUpper(id[0])}{id.Substring(1)}_Sources");
            assetRoot.transform.position = new Vector3(0f, -20000f, 0f);

            UnityEngine.Object.DontDestroyOnLoad(assetRoot);

            GameObject model = GltfLoader.LoadGlb(bytes, ResolveModelShader()) ?? throw new InvalidOperationException($"S1MAPI could not load embedded GLB resource '{resource}'.");

            model.name = $"{char.ToUpper(id[0])}{id.Substring(1)}_Model";
            model.SetActive(true);

            // The object handed to the presentation providers stays a plain wrapper: S1API writes the
            // pose transform onto the object it clones, so the fitted model has to sit underneath it.
            asset = new GameObject($"{char.ToUpper(id[0])}{id.Substring(1)}");
            asset.transform.SetParent(assetRoot.transform, false);
            asset.SetActive(true);

            fitNode = new GameObject($"{char.ToUpper(id[0])}{id.Substring(1)}_Fit");
            fitNode.transform.SetParent(asset.transform, false);
            fitNode.SetActive(true);

            model.transform.SetParent(fitNode.transform, false);

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh? mesh = filter.sharedMesh;

                if (mesh != null && (mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount)) mesh.RecalculateTangents();
            }

            ApplyModelSurfaceColor();

            NormalizeModel();

            return;
        }

        /// <summary>
        /// S1MAPI builds the model materials from the shader handed to the loader. Passing null
        /// leaves the renderers without a usable material, which Unity draws with its magenta
        /// error shader, so the pill gets one the same way the rest of the mod does it.
        /// </summary>
        private static Shader ResolveModelShader()
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null) throw new InvalidOperationException("No usable shader was found for the product model.");

            return shader;
        }

        private void ApplyModelSurfaceColor()
        {
            int repaired = 0;

            foreach (Renderer renderer in asset.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;

                if (material == null)
                {
                    material = new Material(ResolveModelShader());
                    material.name = $"WVC_{char.ToUpper(id[0])}{id.Substring(1)}_Material";

                    renderer.sharedMaterial = material;

                    repaired++;
                }
                else if (material.shader == null)
                {
                    material.shader = ResolveModelShader();

                    repaired++;
                }

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", metadataColor);

                if (material.HasProperty("_Color")) material.SetColor("_Color", metadataColor);

                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);

                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.4f);

                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.4f);
            }

            if (repaired > 0) { }
        }

        /// <summary>
        /// Source meshes are authored in arbitrary units and often sit away from the origin.
        /// Re-centres the model on its pivot and scales its longest axis to
        /// <see cref="modelTargetSize"/> so the product poses behave like the native products.
        /// </summary>
        private void NormalizeModel()
        {
            Bounds bounds = ComputeLocalBounds(asset);

            if (bounds.size == Vector3.zero) return;

            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

            if (longest <= 0.0001f) return;

            float scale = modelTargetSize / longest;

            Transform fit = fitNode != null ? fitNode.transform : asset.transform;

            // The fit (and the orientation fix) lives on the child node: every pose rewrites the
            // wrapper's own transform, which used to throw the fit away and leave the model at its
            // raw source size once S1API applied a pose to the clone.
            fit.localRotation = Quaternion.Euler(modelEulerAngles);
            fit.localScale = Vector3.one * scale;
            fit.localPosition = fit.localRotation * (bounds.center * -scale);


        }

        private static Bounds ComputeLocalBounds(GameObject root)
        {
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            bool started = false;

            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh? mesh = filter.sharedMesh;

                if (mesh == null) continue;

                Bounds local = mesh.bounds;
                Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;
                Vector3 center = local.center;
                Vector3 extents = local.extents;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = center + new Vector3(
                        (corner & 1) == 0 ? -extents.x : extents.x,
                        (corner & 2) == 0 ? -extents.y : extents.y,
                        (corner & 4) == 0 ? -extents.z : extents.z);

                    point = matrix.MultiplyPoint3x4(point);

                    if (!started)
                    {
                        result = new Bounds(point, Vector3.zero);
                        started = true;
                    }
                    else
                    {
                        result.Encapsulate(point);
                    }
                }
            }

            return result;
        }

        internal void Dispose()
        {
            if (assetRoot != null)
            {
                UnityEngine.Object.Destroy(assetRoot);
            }

            asset = null;

            assetRoot = null;
        }
    }
}