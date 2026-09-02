using System;
using System.Collections.Generic;
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
    public static class MDMA
    {
        public const string ProductKindId = "westvilleconnection:mdma_v3";
        public const string ProductId = "westvilleconnection:products/mdma_v3";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile _presentationProfile;
        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static GameObject _visualSource;
        private static Material _visualMaterial;
        private static Material _brickMaterial;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;

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
                // Cocaine is only kept as a fallback donor.
                var cocaineTemplate =
                    ItemManager.GetDefinition("cocaine") as S1ProductDefinition;

                // Shroom is the donor we actually want, because its native
                // representation should carry the eating animation instead of snorting.
                var shroomTemplate =
                    ItemManager.GetDefinition("shroom") as S1ProductDefinition;

                S1ProductDefinition template = shroomTemplate ?? cocaineTemplate;

                if (template == null) return false;

                if (shroomTemplate == null)
                {
                }

                var baggie = ItemManager.GetDefinition("baggie") as S1PackagingDefinition;
                var jar = ItemManager.GetDefinition("jar") as S1PackagingDefinition;
                var brick = ItemManager.GetDefinition("brick") as S1PackagingDefinition;

                if (baggie == null || jar == null || brick == null) return false;

                if (_productKind == null)
                {
                    // DO NOT change this to a shroom drug type. It breaks the item.
                    _productKind = new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(S1DrugType.Cocaine)
                        .Build();
                }

                MDMAMixing.Register(_productKind);

                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                _definition = CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("MDMA")
                    .WithDescription("A small pink heart-shaped pressed tablet.")
                    .WithProductPrice(95f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.65f)
                    .WithDefaultQuality((S1Quality)2)
                    .WithRepresentationsFrom(template)   // <-- shroom, not cocaine
                    .WithValidPackaging(new S1PackagingDefinition[] { baggie, jar, brick })
                    .WithEffectDurations(240, 480)
                    .WithNativeMixerMap((ProductMixingMap)2)
                    .Build();

                _built = true;
                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[Westville Connection] MDMA build failed: " + ex);
                }
                return false;
            }
        }

        private static bool TryDiscover()
        {
            try
            {
                _definition.Discover(false);

                ConsoleItemAliases.Register("mdma", ProductId);
                ConsoleItemAliases.Register("molly", ProductId);
                ConsoleItemAliases.Register("ecstasy", ProductId);

                _discovered = true;
                MelonLogger.Msg("[Westville Connection] MDMA registered and discovered.");
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[Westville Connection] MDMA discovery failed: " + ex);
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
                    .WithDisplayName("MDMA")
                    .WithColor(new Color(1.0f, 0.15f, 0.62f))
                    .WithIcon(icon)
                    .WithSortOrder(10)
                    .WithSearchAliases(new string[] { "mdma", "molly", "ecstasy" })
                    .WithProductManagerVisibility(true)
                    .Build();

                MelonLogger.Msg("[Westville Connection] MDMA Products app category registered.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;
                    MelonLogger.Error("[Westville Connection] MDMA metadata failed: " + ex);
                }
                return true;
            }
        }

        // ============================================================
        // Upright Heart Mesh (Fixes Hotbar & Discovery Screen)
        // ============================================================

        private static GameObject GetOrCreateVisualSource()
        {
            GameObject custom =
                MdmaObjVisual.GetOrCreate();

            if (custom != null)
                return custom;

            return GetOrCreateProceduralHeartFallback();
        }

        private static GameObject GetOrCreateProceduralHeartFallback()
        {
            if (_visualSource != null)
                return _visualSource;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
                throw new InvalidOperationException("No shader for MDMA heart.");

            _visualMaterial = new Material(shader);
            _visualMaterial.name = "WVC_MDMA_PinkHeart_Material";

            Color pink = new Color(1.0f, 0.16f, 0.62f, 1.0f);

            if (_visualMaterial.HasProperty("_BaseColor"))
                _visualMaterial.SetColor("_BaseColor", pink);

            if (_visualMaterial.HasProperty("_Color"))
                _visualMaterial.SetColor("_Color", pink);

            if (_visualMaterial.HasProperty("_Smoothness"))
                _visualMaterial.SetFloat("_Smoothness", 0.65f);

            if (_visualMaterial.HasProperty("_Glossiness"))
                _visualMaterial.SetFloat("_Glossiness", 0.65f);

            if (_visualMaterial.HasProperty("_Metallic"))
                _visualMaterial.SetFloat("_Metallic", 0.05f);

            GameObject go = new GameObject("WVC_MDMA_PinkHeart");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = CreateHeartTabletMesh();
            mr.sharedMaterial = _visualMaterial;

            go.transform.position = new Vector3(0, -20000f, 0);
            UnityEngine.Object.DontDestroyOnLoad(go);

            _visualSource = go;

            MelonLogger.Warning(
                "[Westville Connection] OBJ visual missing. Using procedural heart fallback."
            );

            return _visualSource;
        }

        private static Mesh CreateHeartTabletMesh()
        {
            const float thickness = 0.006f;

            // Upright heart outline in XY plane (point is at -Y, lobes at +Y)
            Vector2[] outline =
            {
                new Vector2( 0.0000f,  0.0075f),
                new Vector2(-0.0038f,  0.0120f),
                new Vector2(-0.0095f,  0.0130f),
                new Vector2(-0.0145f,  0.0095f),
                new Vector2(-0.0155f,  0.0030f),
                new Vector2(-0.0115f, -0.0045f),
                new Vector2(-0.0063f, -0.0110f),
                new Vector2( 0.0000f, -0.0188f),
                new Vector2( 0.0063f, -0.0110f),
                new Vector2( 0.0115f, -0.0045f),
                new Vector2( 0.0155f,  0.0030f),
                new Vector2( 0.0145f,  0.0095f),
                new Vector2( 0.0095f,  0.0130f),
                new Vector2( 0.0038f,  0.0120f)
            };

            if (SignedArea(outline) < 0f)
                Array.Reverse(outline);

            List<int> capTriangles = TriangulatePolygon(outline);
            int count = outline.Length;

            Vector3[] vertices = new Vector3[count * 2];

            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(
                    outline[i].x,
                    outline[i].y,
                    thickness
                );

                vertices[count + i] = new Vector3(
                    outline[i].x,
                    outline[i].y,
                    -thickness
                );
            }

            List<int> triangles = new List<int>();

            // Front
            for (int i = 0; i < capTriangles.Count; i += 3)
            {
                triangles.Add(capTriangles[i]);
                triangles.Add(capTriangles[i + 1]);
                triangles.Add(capTriangles[i + 2]);
            }

            // Back
            for (int i = 0; i < capTriangles.Count; i += 3)
            {
                triangles.Add(count + capTriangles[i]);
                triangles.Add(count + capTriangles[i + 2]);
                triangles.Add(count + capTriangles[i + 1]);
            }

            // Outer sides
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                triangles.Add(i);
                triangles.Add(count + i);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(count + i);
                triangles.Add(count + next);
            }

            Mesh mesh = new Mesh();
            mesh.name = "WVC_MDMA_UprightHeartTablet";
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static List<int> TriangulatePolygon(Vector2[] points)
        {
            List<int> result = new List<int>();
            List<int> remaining = new List<int>();
            for (int i = 0; i < points.Length; i++) remaining.Add(i);

            int safety = 0;
            while (remaining.Count > 3 && safety++ < 256)
            {
                bool foundEar = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int prevIdx = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int currIdx = remaining[i];
                    int nextIdx = remaining[(i + 1) % remaining.Count];

                    Vector2 a = points[prevIdx];
                    Vector2 b = points[currIdx];
                    Vector2 c = points[nextIdx];

                    if (Cross2D(a, b, c) <= 0.00001f) continue;

                    bool containsPoint = false;
                    foreach (int testIdx in remaining)
                    {
                        if (testIdx == prevIdx || testIdx == currIdx || testIdx == nextIdx) continue;
                        if (PointInTriangle(points[testIdx], a, b, c)) { containsPoint = true; break; }
                    }

                    if (containsPoint) continue;

                    result.Add(prevIdx);
                    result.Add(currIdx);
                    result.Add(nextIdx);
                    remaining.RemoveAt(i);
                    foundEar = true;
                    break;
                }

                if (!foundEar) break;
            }

            if (remaining.Count == 3)
            {
                result.Add(remaining[0]);
                result.Add(remaining[1]);
                result.Add(remaining[2]);
            }

            return result;
        }

        private static float SignedArea(Vector2[] pts)
        {
            float area = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                int j = (i + 1) % pts.Length;
                area += pts[i].x * pts[j].y - pts[j].x * pts[i].y;
            }
            return area * 0.5f;
        }

        private static float Cross2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2D(a, b, p);
            float d2 = Cross2D(b, c, p);
            float d3 = Cross2D(c, a, p);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        // ============================================================
        // Presentation + Packaging
        // ============================================================

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null)
                return;

            /*
             * The heart mesh is built in XY.
             *
             * Rotate X by 90 so the game's icon/mixer/loose-item cameras
             * see the full heart face instead of its thin edge.
             *
             * Z 180 flips the point downward.
             */
            ProductPresentationTransform loose =
                new ProductPresentationTransform(
                    Vector3.zero,
                    new Vector3(90f, 0f, 180f),
                    Vector3.one * 0.04f
                );

            ProductPresentationTransform held =
                new ProductPresentationTransform(
                    Vector3.zero,
                    new Vector3(90f, 0f, 180f),
                    Vector3.one * 0.06f
                );

            _presentationProfile = new ProductPresentationProfileBuilder()
                .WithLooseVisual(
                    () => GetOrCreateVisualSource(),
                    loose
                )
                .WithHeldVisual(
                    () => GetOrCreateVisualSource(),
                    held
                )
                .WithFunctionalProductConvexMeshColliders()
                .WithGeneratedIconFromLooseVisual(
                    512,
                    true,
                    0.78f
                )
                .Build();

            ProductPresentationProfileRegistry.RegisterForProduct(
                "westvilleconnection",
                ProductId,
                _presentationProfile
            );

            ProductPresentationProfileRegistry.RegisterForProductKind(
                "westvilleconnection",
                _productKind,
                _presentationProfile
            );
        }

        private static void EnsurePackagingRegistered()
{
            /*
             * BAGGIE:
             * Scale should match loose scale (~0.08f), not exceed it.
             */
            if (_baggieProfile == null)
            {
                _baggieProfile =
                    new ProductPackagingContentProfileBuilder()
                    .WithContent(
                        () => GetOrCreateVisualSource()
                    )
                    .AddPlacement(
                        new ProductPresentationTransform(
                            new Vector3(
                                0f,
                                -0.002f,
                                -0.001f
                            ),

                            /*
                             * Turn the tablet face toward the front of the bag.
                             * Without X=90, the OBJ appears edge-on/invisible.
                             */
                            new Vector3(
                                90f,
                                0f,
                                180f
                            ),

                            /*
                             * Keep your corrected packaging size.
                             */
                            Vector3.one * 0.028f
                        )
                    )
                    .Build();
            }

            /*
             * JAR:
             * Multiple pills lying flat. Keep scales small so they don't clip.
             */
            if (_jarProfile == null)
    {
        _jarProfile =
            new ProductPackagingContentProfileBuilder()
            .WithContent(
                () => GetOrCreateVisualSource()
            )
            .AddPlacements(
                new ProductPresentationTransform[]
                {
                    JarHeart(
                        -0.010f,
                         0.006f,
                        -0.008f,
                         20f,
                         0.019f
                    ),

                    JarHeart(
                         0.010f,
                         0.006f,
                        -0.006f,
                        -30f,
                         0.019f
                    ),

                    JarHeart(
                         0.000f,
                         0.006f,
                         0.010f,
                         55f,
                         0.019f
                    ),

                    JarHeart(
                        -0.007f,
                         0.016f,
                         0.003f,
                        -60f,
                         0.019f
                    ),

                    JarHeart(
                         0.007f,
                         0.016f,
                        -0.003f,
                         70f,
                         0.019f
                    )
                }
            )
            .Build();
    }

    if (_brickProfile == null)
    {
        _brickProfile =
            new ProductPackagingContentProfileBuilder()
            .WithNativeFilledVisualScaffold(
                ProductPackagingVisualTemplate.Cocaine,
                clone => ApplyBrickMaterial(clone),
                null
            )
            .Build();
    }

    ProductPackagingContentProfileRegistry.Register(
        "westvilleconnection",
        ProductId,
        "baggie",
        _baggieProfile
    );

    ProductPackagingContentProfileRegistry.Register(
        "westvilleconnection",
        ProductId,
        "jar",
        _jarProfile
    );

    ProductPackagingContentProfileRegistry.Register(
        "westvilleconnection",
        ProductId,
        "brick",
        _brickProfile
    );

    ProductPackagingContentProfileRegistry.RegisterForProductKind(
        "westvilleconnection",
        ProductKindId,
        "baggie",
        _baggieProfile
    );

    ProductPackagingContentProfileRegistry.RegisterForProductKind(
        "westvilleconnection",
        ProductKindId,
        "jar",
        _jarProfile
    );

    ProductPackagingContentProfileRegistry.RegisterForProductKind(
        "westvilleconnection",
        ProductKindId,
        "brick",
        _brickProfile
    );
}

        private static ProductPresentationTransform JarHeart(
    float x,
    float y,
    float z,
    float yRotation,
    float scale)
        {
            return new ProductPresentationTransform(
                new Vector3(x, y, z),

                // Flat on the jar floor.
                new Vector3(
                    0f,
                    yRotation,
                    0f
                ),

                Vector3.one * scale
            );
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

        private static void ApplyBrickMaterial(GameObject clone)
        {
            if (clone == null) return;

            if (_brickMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Sprites/Default");

                if (shader == null) return;

                _brickMaterial = new Material(shader);
                _brickMaterial.name = "WVC_MDMA_Brick_Material";

                Color brickPink = new Color(0.72f, 0.10f, 0.40f, 1f);

                if (_brickMaterial.HasProperty("_BaseColor"))
                    _brickMaterial.SetColor("_BaseColor", brickPink);

                if (_brickMaterial.HasProperty("_Color"))
                    _brickMaterial.SetColor("_Color", brickPink);

                if (_brickMaterial.HasProperty("_Smoothness"))
                    _brickMaterial.SetFloat("_Smoothness", 0.18f);

                if (_brickMaterial.HasProperty("_Glossiness"))
                    _brickMaterial.SetFloat("_Glossiness", 0.18f);

                if (_brickMaterial.HasProperty("_Metallic"))
                    _brickMaterial.SetFloat("_Metallic", 0.0f);
            }

            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                    materials[i] = _brickMaterial;

                renderer.sharedMaterials = materials;
            }
        }
    }
}