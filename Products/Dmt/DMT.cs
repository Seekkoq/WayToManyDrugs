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
    public static class DMT
    {
        public const string ProductKindId =
            "westvilleconnection:dmt_v1";

        public const string ProductId =
            "westvilleconnection:products/dmt";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile
            _presentationProfile;

        private static ProductPackagingContentProfile
            _baggieProfile;

        private static ProductPackagingContentProfile
            _jarProfile;

        private static ProductPackagingContentProfile
    _brickProfile;

        private static Material _brickMaterial;   

        private static GameObject _visualSource;
        private static Material _powderMaterial;

        private static Sprite _fallbackIcon;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;
        private static bool _presentationRegistering;

        // ============================================================
        // Powder model settings
        // ============================================================

        /*
         * The mesh is constructed horizontally:
         *
         * X/Z = surface plane
         * Y   = height
         *
         * Because Y is already up, loose and held presentation must
         * not use the 90-degree rotations required by XY-plane models.
         */
        private const int PowderSegments = 32;
        private const int PowderRings = 8;

        private const float PowderRadiusX = 0.031f;
        private const float PowderRadiusZ = 0.025f;

        private const float PowderHeight = 0.011f;
        private const float PowderBottomDepth = 0.0018f;

        // ============================================================
        // Registration
        // ============================================================

        public static bool TryRegister()
        {
            if (_discovered)
                return true;

            if (!_built &&
                !TryBuild())
            {
                return false;
            }

            return TryDiscover();
        }

        private static bool TryBuild()
        {
            if (_built && _definition != null)
                return true;

            try
            {
                S1ProductDefinition weedTemplate =
                    ItemManager.GetDefinition("ogkush") as S1ProductDefinition
                    ?? ItemManager.GetDefinition("sourdiesel") as S1ProductDefinition
                    ?? ItemManager.GetDefinition("greencrack") as S1ProductDefinition
                    ?? ItemManager.GetDefinition("granddaddypurple") as S1ProductDefinition;

                if (weedTemplate == null)
                {
                    MelonLogger.Warning(
                        "[WVC DMT] No weed donor template found."
                    );

                    return false;
                }

                _fallbackIcon = weedTemplate.Icon;

                S1PackagingDefinition baggie =
                    ItemManager.GetDefinition("baggie") as S1PackagingDefinition;

                S1PackagingDefinition jar =
                    ItemManager.GetDefinition("jar") as S1PackagingDefinition;

                S1PackagingDefinition brick =
                    ItemManager.GetDefinition("brick") as S1PackagingDefinition;

                if (baggie == null || jar == null || brick == null)
                    return false;

                if (_productKind == null)
                {
                    _productKind =
                        new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(S1DrugType.Marijuana)
                        .Build();
                }

                DMTMixing.Register(_productKind);

                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                _definition =
                    CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("DMT")
                    .WithDescription(
                        "A small amount of pale, waxy powder. " +
                        "Smoked for a short, intense trip."
                    )
                    .WithProductPrice(180f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.25f)
                    .WithDefaultQuality((S1Quality)2)
                    .WithRepresentationsFrom(weedTemplate)
                    .WithValidPackaging(
                        new S1PackagingDefinition[]
                        {
                    baggie,
                    jar,
                    brick
                        }
                    )
                    .WithEffectDurations(90, 180)
                    .WithNativeMixerMap((ProductMixingMap)0)
                    .Build();

                _built = true;

                MelonLogger.Msg(
                    "[WVC DMT] Built with custom powder visual, " +
                    "weed smoking representation, and brick packaging."
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;

                    MelonLogger.Error(
                        "[WVC DMT] Build failed: " + ex
                    );
                }

                return false;
            }
        }   

        private static bool TryDiscover()
        {
            if (_definition == null)
                return false;

            try
            {
                _definition.Discover(
                    false
                );

                ConsoleItemAliases.Register(
                    "dmt",
                    ProductId
                );

                ConsoleItemAliases.Register(
                    "dim",
                    ProductId
                );

                ConsoleItemAliases.Register(
                    "dimethyl",
                    ProductId
                );

                _discovered = true;

                MelonLogger.Msg(
                    "[WVC DMT] Registered and discovered."
                );

                return true;
            }
            catch (InvalidOperationException)
            {
                // Product registry is not ready yet.
                return false;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;

                    MelonLogger.Error(
                        "[WVC DMT] Discovery failed: " +
                        ex
                    );
                }

                return false;
            }
        }

        // ============================================================
        // Product Manager metadata
        // ============================================================

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
                Sprite icon =
                    _definition.Icon ??
                    _fallbackIcon;

                if (icon == null)
                    return false;

                _metadata =
                    new ProductKindMetadataBuilder(
                        _productKind
                    )
                    .WithDisplayName(
                        "DMT"
                    )
                    .WithColor(
                        new Color(
                            0.92f,
                            0.86f,
                            0.66f
                        )
                    )
                    .WithIcon(
                        icon
                    )
                    .WithSortOrder(
                        12
                    )
                    .WithSearchAliases(
                        new string[]
                        {
                            "dmt",
                            "dim",
                            "dimethyl"
                        }
                    )
                    .WithProductManagerVisibility(
                        true
                    )
                    .Build();

                MelonLogger.Msg(
                    "[WVC DMT] Metadata complete."
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;

                    MelonLogger.Error(
                        "[WVC DMT] Metadata failed: " +
                        ex
                    );
                }

                return false;
            }
        }

        // ============================================================
        // Powder visual
        // ============================================================

        private static GameObject GetVisual()
        {
            GameObject visual =
                GetOrCreatePowderVisual();

            if (visual == null)
            {
                MelonLogger.Error(
                    "[WVC DMT] Powder visual could not be created."
                );
            }

            return visual;
        }

        private static GameObject GetOrCreatePowderVisual()
        {
            if (_visualSource != null)
                return _visualSource;

            EnsurePowderMaterial();

            GameObject root =
                new GameObject(
                    "WVC_DMT_Powder"
                );

            MeshFilter meshFilter =
                root.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer =
                root.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh =
                CreatePowderMoundMesh();

            meshRenderer.sharedMaterial =
                _powderMaterial;

            root.transform.position =
                new Vector3(
                    0f,
                    -20000f,
                    0f
                );

            root.transform.rotation =
                Quaternion.identity;

            root.transform.localScale =
                Vector3.one;

            UnityEngine.Object.DontDestroyOnLoad(
                root
            );

            _visualSource =
                root;

            MelonLogger.Msg(
                "[WVC DMT] Created powder-only product visual."
            );

            return _visualSource;
        }

        private static void EnsurePowderMaterial()
        {
            if (_powderMaterial != null)
                return;

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
                ??
                Shader.Find(
                    "Standard"
                )
                ??
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No shader available for DMT powder."
                );
            }

            _powderMaterial =
                new Material(
                    shader
                );

            _powderMaterial.name =
                "WVC_DMT_Powder_Material";

            /*
             * Slightly warm off-white. Pure white tends to resemble
             * cocaine and can become overexposed under bright lighting.
             */
            Color powderColor =
                new Color(
                    0.94f,
                    0.91f,
                    0.79f,
                    1f
                );

            if (_powderMaterial.HasProperty(
                    "_BaseColor"))
            {
                _powderMaterial.SetColor(
                    "_BaseColor",
                    powderColor
                );
            }

            if (_powderMaterial.HasProperty(
                    "_Color"))
            {
                _powderMaterial.SetColor(
                    "_Color",
                    powderColor
                );
            }

            // Powder should be matte rather than shiny.
            if (_powderMaterial.HasProperty(
                    "_Smoothness"))
            {
                _powderMaterial.SetFloat(
                    "_Smoothness",
                    0.10f
                );
            }

            if (_powderMaterial.HasProperty(
                    "_Glossiness"))
            {
                _powderMaterial.SetFloat(
                    "_Glossiness",
                    0.10f
                );
            }

            if (_powderMaterial.HasProperty(
                    "_Metallic"))
            {
                _powderMaterial.SetFloat(
                    "_Metallic",
                    0f
                );
            }
        }

        // ============================================================
        // Powder mesh
        // ============================================================

        /*
         * Creates one closed, continuous powder mound.
         *
         * It has:
         * - an oval irregular footprint
         * - a broad rounded top
         * - shallow fine surface variation
         * - a flat sealed underside
         *
         * No separate chunks, crystals, spheres, or containers.
         */
        private static Mesh CreatePowderMoundMesh()
        {
            List<Vector3> vertices =
                new List<Vector3>();

            List<int> triangles =
                new List<int>();

            /*
             * Top-center vertex.
             *
             * The center is offset slightly so the mound does not
             * look perfectly manufactured or symmetrical.
             */
            vertices.Add(
                new Vector3(
                    0.0012f,
                    PowderHeight,
                    -0.0007f
                )
            );

            // --------------------------------------------------------
            // Top rings
            // --------------------------------------------------------

            for (int ring = 1;
                 ring <= PowderRings;
                 ring++)
            {
                float radialT =
                    (float)ring /
                    PowderRings;

                for (int segment = 0;
                     segment < PowderSegments;
                     segment++)
                {
                    float angle =
                        ((float)segment /
                         PowderSegments) *
                        Mathf.PI *
                        2f;

                    float edgeVariation =
                        GetEdgeVariation(
                            angle
                        );

                    float innerVariation =
                        1f +
                        (
                            Mathf.Sin(
                                angle * 5f +
                                ring * 0.73f
                            ) * 0.025f
                        ) *
                        Mathf.Sin(
                            Mathf.PI *
                            radialT
                        );

                    float xRadius =
                        PowderRadiusX *
                        radialT *
                        edgeVariation *
                        innerVariation;

                    float zRadius =
                        PowderRadiusZ *
                        radialT *
                        edgeVariation *
                        innerVariation;

                    float x =
                        Mathf.Cos(
                            angle
                        ) *
                        xRadius;

                    float z =
                        Mathf.Sin(
                            angle
                        ) *
                        zRadius;

                    /*
                     * Wide rounded powder profile.
                     *
                     * Power > 1 keeps the top broad and lets the edge
                     * fall away more strongly near the outside.
                     */
                    float profile =
                        1f -
                        Mathf.Pow(
                            radialT,
                            1.72f
                        );

                    float y =
                        PowderHeight *
                        profile;

                    /*
                     * Fine surface variation. This remains subtle so
                     * the mound does not become faceted or rocky.
                     */
                    float surfaceVariation =
                        Mathf.Sin(
                            angle * 4f +
                            ring * 1.13f
                        ) *
                        0.00045f;

                    surfaceVariation +=
                        Mathf.Cos(
                            angle * 9f -
                            ring * 0.61f
                        ) *
                        0.00022f;

                    surfaceVariation *=
                        Mathf.Sin(
                            Mathf.PI *
                            radialT
                        );

                    y +=
                        surfaceVariation;

                    /*
                     * Outer edge touches the ground plane exactly.
                     */
                    if (ring == PowderRings)
                        y = 0f;

                    vertices.Add(
                        new Vector3(
                            x,
                            y,
                            z
                        )
                    );
                }
            }

            // --------------------------------------------------------
            // Center fan
            // --------------------------------------------------------

            int firstRingStart =
                GetRingStart(
                    1
                );

            for (int segment = 0;
                 segment < PowderSegments;
                 segment++)
            {
                int next =
                    (segment + 1) %
                    PowderSegments;

                triangles.Add(
                    0
                );

                triangles.Add(
                    firstRingStart +
                    next
                );

                triangles.Add(
                    firstRingStart +
                    segment
                );
            }

            // --------------------------------------------------------
            // Connect the top rings
            // --------------------------------------------------------

            for (int ring = 1;
                 ring < PowderRings;
                 ring++)
            {
                int innerStart =
                    GetRingStart(
                        ring
                    );

                int outerStart =
                    GetRingStart(
                        ring + 1
                    );

                for (int segment = 0;
                     segment < PowderSegments;
                     segment++)
                {
                    int next =
                        (segment + 1) %
                        PowderSegments;

                    int innerCurrent =
                        innerStart +
                        segment;

                    int innerNext =
                        innerStart +
                        next;

                    int outerCurrent =
                        outerStart +
                        segment;

                    int outerNext =
                        outerStart +
                        next;

                    triangles.Add(
                        innerCurrent
                    );

                    triangles.Add(
                        outerNext
                    );

                    triangles.Add(
                        outerCurrent
                    );

                    triangles.Add(
                        innerCurrent
                    );

                    triangles.Add(
                        innerNext
                    );

                    triangles.Add(
                        outerNext
                    );
                }
            }

            // --------------------------------------------------------
            // Closed bottom
            // --------------------------------------------------------

            int outerTopStart =
                GetRingStart(
                    PowderRings
                );

            int bottomRingStart =
                vertices.Count;

            for (int segment = 0;
                 segment < PowderSegments;
                 segment++)
            {
                Vector3 outerVertex =
                    vertices[
                        outerTopStart +
                        segment
                    ];

                vertices.Add(
                    new Vector3(
                        outerVertex.x,
                        -PowderBottomDepth,
                        outerVertex.z
                    )
                );
            }

            int bottomCenter =
                vertices.Count;

            vertices.Add(
                new Vector3(
                    0f,
                    -PowderBottomDepth,
                    0f
                )
            );

            for (int segment = 0;
                 segment < PowderSegments;
                 segment++)
            {
                int next =
                    (segment + 1) %
                    PowderSegments;

                int topCurrent =
                    outerTopStart +
                    segment;

                int topNext =
                    outerTopStart +
                    next;

                int bottomCurrent =
                    bottomRingStart +
                    segment;

                int bottomNext =
                    bottomRingStart +
                    next;

                // Outer edge wall
                triangles.Add(
                    topCurrent
                );

                triangles.Add(
                    topNext
                );

                triangles.Add(
                    bottomCurrent
                );

                triangles.Add(
                    bottomCurrent
                );

                triangles.Add(
                    topNext
                );

                triangles.Add(
                    bottomNext
                );

                // Flat underside
                triangles.Add(
                    bottomCenter
                );

                triangles.Add(
                    bottomCurrent
                );

                triangles.Add(
                    bottomNext
                );
            }

            Mesh mesh =
                new Mesh();

            mesh.name =
                "WVC_DMT_PowderMound";

            mesh.vertices =
                vertices.ToArray();

            mesh.triangles =
                triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static int GetRingStart(
            int ring
        )
        {
            return 1 +
                (
                    (ring - 1) *
                    PowderSegments
                );
        }

        private static float GetEdgeVariation(
            float angle
        )
        {
            /*
             * Small overlapping waves create an irregular powder edge.
             * The values are deliberately restrained so the footprint
             * stays natural and never becomes star-shaped.
             */
            float broad =
                Mathf.Sin(
                    angle * 2f +
                    0.41f
                ) *
                0.055f;

            float medium =
                Mathf.Sin(
                    angle * 5f +
                    1.38f
                ) *
                0.028f;

            float fine =
                Mathf.Cos(
                    angle * 9f -
                    0.72f
                ) *
                0.012f;

            return 1f +
                broad +
                medium +
                fine;
        }

        // ============================================================
        // Presentation
        // ============================================================

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null)
                return;

            if (_presentationRegistering)
            {
                MelonLogger.Warning(
                    "[WVC DMT] Presentation re-entry blocked. " +
                    "Profile will not apply. Find the recursive call."
                );

                return;
            }

            _presentationRegistering = true;

            try
            {
                // Powder mesh is horizontal already.
                ProductPresentationTransform loose =
                    new ProductPresentationTransform(
                        Vector3.zero,
                        new Vector3(
                            0f,
                            12f,
                            0f
                        ),
                        Vector3.one * 1.15f
                    );

                ProductPresentationTransform held =
                    new ProductPresentationTransform(
                        new Vector3(
                            0f,
                            0.002f,
                            0f
                        ),
                        new Vector3(
                            -70f,
                            -1f,
                            0f
                        ),
                        Vector3.one * 1.65f
                    );

                _presentationProfile =
                    new ProductPresentationProfileBuilder()
                    .WithLooseVisual(
                        () => GetVisual(),
                        loose
                    )
                    .WithHeldVisual(
                        () => GetVisual(),
                        held
                    )
                    .WithFunctionalProductConvexMeshColliders()
                    .WithGeneratedIconFromLooseVisual(
                        512,
                        true,
                        0.80f
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

                MelonLogger.Msg(
                    "[WVC DMT] Presentation profile registered successfully."
                );
            }
            catch (Exception ex)
            {
                _presentationProfile = null;

                MelonLogger.Error(
                    "[WVC DMT] Presentation registration failed: " + ex
                );
            }
            finally
            {
                _presentationRegistering = false;
            }
        }

        private static void ApplyBrickMaterial(GameObject clone)
        {
            if (clone == null)
                return;

            /*
             * Pale waxy off-white, matching the powder color.
             * Slightly darker so it reads as a pressed block.
             */
            Color dmtBrick =
                new Color(
                    0.86f,
                    0.82f,
                    0.68f,
                    1f
                );

            Renderer[] renderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] mats = renderer.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                        continue;

                    /*
                     * CRITICAL:
                     * Copy the existing native material instead of
                     * new Material(shader).
                     *
                     * Creating a fresh material discards the plastic wrap
                     * and duct tape textures, producing a blank white block.
                     */
                    Material copy = new Material(mats[i]);
                    copy.name = "WVC_DbMT_Brick_Material";

                    if (copy.HasProperty("_BaseColor"))
                        copy.SetColor("_BaseColor", dmtBrick);

                    if (copy.HasProperty("_Color"))
                        copy.SetColor("_Color", dmtBrick);

                    mats[i] = copy;
                }

                renderer.sharedMaterials = mats;
            }
        }

        // ============================================================
        // Packaging
        // ============================================================

        private static void EnsurePackagingRegistered()
        {
            if (_baggieProfile == null)
            {
                /*
                 * The baggie is viewed primarily from the side/front.
                 * Rotate the powder 90 degrees inside only this packaging
                 * context so it remains visible through the plastic.
                 *
                 * This does not affect loose or held rotation.
                 */
                _baggieProfile =
                    new ProductPackagingContentProfileBuilder()
                    .WithContent(
                        () => GetVisual()
                    )
                    .AddPlacement(
                        new ProductPresentationTransform(
                            new Vector3(
                                0f,
                                -0.005f,
                                0f
                            ),
                            new Vector3(
                                90f,
                                0f,
                                0f
                            ),
                            Vector3.one *
                            0.95f
                        )
                    )
                    .Build();
            }

            if (_jarProfile == null)
            {
                /*
                 * Larger, wider-spread powder piles.
                 *
                 * Bottom layer sits near the jar floor and spreads outward.
                 * Two smaller piles rest on top so the jar reads as filled
                 * rather than having a few small clumps in the center.
                 */
                _jarProfile =
                    new ProductPackagingContentProfileBuilder()
                    .WithContent(
                        () => GetVisual()
                    )
                    .AddPlacements(
                        new ProductPresentationTransform[]
                        {
                // Bottom layer
                CreateJarPowder(
                    0.000f,
                    0.004f,
                    0.000f,
                    10f,
                    0.95f
                ),

                CreateJarPowder(
                    -0.019f,
                    0.004f,
                    -0.012f,
                    -58f,
                    0.86f
                ),

                CreateJarPowder(
                    0.019f,
                    0.004f,
                    0.011f,
                    92f,
                    0.86f
                ),

                CreateJarPowder(
                    0.014f,
                    0.004f,
                    -0.018f,
                    150f,
                    0.80f
                ),

                CreateJarPowder(
                    -0.015f,
                    0.004f,
                    0.017f,
                    -120f,
                    0.80f
                ),

                // Upper layer
                CreateJarPowder(
                    -0.007f,
                    0.015f,
                    0.005f,
                    -30f,
                    0.72f
                ),

                CreateJarPowder(
                    0.008f,
                    0.015f,
                    -0.006f,
                    64f,
                    0.72f
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

            ProductPackagingContentProfileRegistry.Register(
    "westvilleconnection",
    ProductId,
    "brick",
    _brickProfile
);

            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection",
                ProductKindId,
                "brick",
                _brickProfile
            );
        }

        private static ProductPresentationTransform CreateJarPowder(
            float x,
            float y,
            float z,
            float yRotation,
            float scale
        )
        {
            return new ProductPresentationTransform(
                new Vector3(
                    x,
                    y,
                    z
                ),
                new Vector3(
                    0f,
                    yRotation,
                    0f
                ),
                Vector3.one *
                scale
            );
        }
    }
}