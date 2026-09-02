using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Properties;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class MollyIngredients
    {
        public const string SafroleId =
    "westvilleconnection:ingredients/safrole_oil";

        public const string PmkId =
            "westvilleconnection:ingredients/pmk_powder";

        public const string PmkRefinedId =
            "westvilleconnection:ingredients/pmk_refined";

        public const string PmkLabGradeId =
            "westvilleconnection:ingredients/pmk_lab_grade";

        // Balanced against MDMA's $95 base value.
        private const float SafrolePrice = 40f;
        private const float PmkPrice = 35f;
        private const float PmkRefinedPrice = 75f;
        private const float PmkLabGradePrice = 140f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _safroleVisual;
        private static GameObject _pmkVisual;
        private static GameObject _pmkRefinedVisual;
        private static GameObject _pmkLabGradeVisual;

        private static Sprite _safroleIcon;
        private static Sprite _pmkIcon;
        private static Sprite _pmkRefinedIcon;
        private static Sprite _pmkLabGradeIcon;

        private static readonly Dictionary<string, PropertyInfo> PropertyCache =
    new Dictionary<string, PropertyInfo>();

        private static readonly Dictionary<string, FieldInfo> FieldCache =
            new Dictionary<string, FieldInfo>();

        public static GameObject GetSafroleVisual()
        {
            return _safroleVisual;
        }

        public static GameObject GetPmkVisual()
        {
            return _pmkVisual;
        }

        public static GameObject GetPmkRefinedVisual()
        {
            return _pmkRefinedVisual;
        }

        public static GameObject GetPmkLabGradeVisual()
        {
            return _pmkLabGradeVisual;
        }

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                ItemDefinition motorOil =
                    ItemManager.GetDefinition("motoroil");

                ItemDefinition iodine =
                    ItemManager.GetDefinition("iodine");

                if (motorOil == null || iodine == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] Base templates are not ready."
                    );

                    return false;
                }

                // ============================================================
                // Safrole Oil
                // ============================================================

                MixIngredientItemCreator
                    .CloneFrom("motoroil")
                    .WithBasicInfo(
                        SafroleId,
                        "Safrole Oil",
                        "A heavy amber precursor oil used in the production of Molly.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(
                        Property.Energizing
                    )
                    .Build();

                // ============================================================
                // Standard PMK
                // ============================================================

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        PmkId,
                        "PMK Powder",
                        "Standard-grade PMK used with Safrole Oil.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(
                        Property.AntiGravity,
                        Property.Energizing
                    )
                    .Build();

                // ============================================================
                // Refined PMK
                // ============================================================

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        PmkRefinedId,
                        "Refined PMK Powder",
                        "Higher-purity PMK that produces premium-quality MDMA.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(
                        Property.AntiGravity,
                        Property.Energizing
                    )
                    .Build();

                // ============================================================
                // Lab-Grade PMK
                // ============================================================

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        PmkLabGradeId,
                        "Lab-Grade PMK Powder",
                        "Near-pure PMK used to produce top-quality MDMA.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(
                        Property.AntiGravity,
                        Property.Energizing
                    )
                    .Build();

                // ============================================================
                // Console aliases
                // ============================================================

                // --- Console Aliases ---
                RegisterAliasSafely("safrole", SafroleId);

                // Standard PMK Aliases
                RegisterAliasSafely("pmk", PmkId);
                RegisterAliasSafely("pmkpowder", PmkId);

                // Refined PMK Aliases
                RegisterAliasSafely("refinedpmk", PmkRefinedId);
                RegisterAliasSafely("refinedpmkpowder", PmkRefinedId);
                RegisterAliasSafely("rpp", PmkRefinedId);
                RegisterAliasSafely("pmkrefined", PmkRefinedId);

                // Lab-Grade PMK Aliases
                RegisterAliasSafely("labpmk", PmkLabGradeId);
                RegisterAliasSafely("labgradepmk", PmkLabGradeId);
                RegisterAliasSafely("labgradepmkpowder", PmkLabGradeId);
                RegisterAliasSafely("lgpp", PmkLabGradeId);
                RegisterAliasSafely("pmklab", PmkLabGradeId);

                // ============================================================
                // Models
                // ============================================================

                _safroleVisual =
    CreateSafroleBottle();

                _pmkVisual =
                    CreatePmkJar();

                _pmkRefinedVisual =
                    CreatePmkJarStyled(
                        "WVC_Custom_RefinedPmk_Jar",
                        new Color(0.62f, 0.85f, 0.96f, 1f),
                        new Color(0.05f, 0.30f, 0.42f, 1f),
                        new Color(0.95f, 0.99f, 1f, 1f),
                        new Color(0.07f, 0.42f, 0.55f, 1f),
                        Color.white,
                        "PMK",
                        "II"
                    );

                _pmkLabGradeVisual =
                    CreatePmkJarStyled(
                        "WVC_Custom_LabGradePmk_Jar",
                        new Color(0.11f, 0.11f, 0.13f, 1f),
                        new Color(0.86f, 0.66f, 0.14f, 1f),
                        new Color(1f, 0.98f, 0.90f, 1f),
                        new Color(0.90f, 0.72f, 0.16f, 1f),
                        Color.black,
                        "PMK",
                        "III"
                    );

                // ============================================================
                // Native representations
                // ============================================================

                ApplyCustomRepresentations(
                    SafroleId,
                    _safroleVisual
                );

                ApplyCustomRepresentations(
                    PmkId,
                    _pmkVisual
                );

                ApplyCustomRepresentations(
                    PmkRefinedId,
                    _pmkRefinedVisual
                );

                ApplyCustomRepresentations(
                    PmkLabGradeId,
                    _pmkLabGradeVisual
                );

                // ============================================================
                // Icons
                // ============================================================

                _safroleIcon =
                    RenderModelIcon(
                        _safroleVisual,
                        "WVC_SafroleOil_Icon"
                    );

                _pmkIcon =
                    RenderModelIcon(
                        _pmkVisual,
                        "WVC_PmkPowder_Icon"
                    );

                _pmkRefinedIcon =
                    RenderModelIcon(
                        _pmkRefinedVisual,
                        "WVC_RefinedPmk_Icon"
                    );

                _pmkLabGradeIcon =
                    RenderModelIcon(
                        _pmkLabGradeVisual,
                        "WVC_LabGradePmk_Icon"
                    );

                ApplyIcon(
                    SafroleId,
                    _safroleIcon
                );

                ApplyIcon(
                    PmkId,
                    _pmkIcon
                );

                ApplyIcon(
                    PmkRefinedId,
                    _pmkRefinedIcon
                );

                ApplyIcon(
                    PmkLabGradeId,
                    _pmkLabGradeIcon
                );

                // ============================================================
                // Prices
                // ============================================================

                SetIngredientPrice(
                    SafroleId,
                    SafrolePrice
                );

                SetIngredientPrice(
                    PmkId,
                    PmkPrice
                );

                SetIngredientPrice(
                    PmkRefinedId,
                    PmkRefinedPrice
                );

                SetIngredientPrice(
                    PmkLabGradeId,
                    PmkLabGradePrice
                );

                // ============================================================
                // Preserve model sources
                // ============================================================

                MoveSourceOffscreen(_safroleVisual);
                MoveSourceOffscreen(_pmkVisual);
                MoveSourceOffscreen(_pmkRefinedVisual);
                MoveSourceOffscreen(_pmkLabGradeVisual);

                _registered = true;

                MelonLogger.Msg(
                    "[WVC Ingredients] Registration complete. " +
                    $"Safrole=${SafrolePrice}, " +
                    $"PMK=${PmkPrice}, " +
                    $"Refined PMK=${PmkRefinedPrice}, " +
                    $"Lab-Grade PMK=${PmkLabGradePrice}."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error(
                    "[WVC Ingredients] Registration failed: " +
                    ex
                );

                return false;
            }
        }

        // ============================================================
        // Native item representations
        // ============================================================

        private static void ApplyCustomRepresentations(
            string itemId,
            GameObject customModel
        )
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] Raw definition not found: " +
                        itemId
                    );

                    return;
                }

                /*
                 * Held items need to be smaller.
                 * World/stored representations can stay full size.
                 */
                bool isPmk =
                    string.Equals(itemId, PmkId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(itemId, PmkRefinedId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(itemId, PmkLabGradeId, StringComparison.OrdinalIgnoreCase);

                float heldMultiplier =
                    isPmk
                        ? 1.25f
                        : 0.95f;

                float worldMultiplier =
                    isPmk
                        ? 1.40f
                        : 1.15f;

                Vector3 heldScale =
                    Vector3.one * heldMultiplier;

                Vector3 worldScale =
                    Vector3.one * worldMultiplier;

                bool equippableApplied =
                    ApplyEquippableRepresentation(
                        definition,
                        itemId,
                        customModel,
                        heldScale
                    );

                bool stationApplied =
                    ApplyStationRepresentation(
                        definition,
                        itemId,
                        customModel,
                        worldScale
                    );

                bool storedApplied =
                    ApplyStoredRepresentation(
                        definition,
                        itemId,
                        customModel,
                        worldScale
                    );

                MelonLogger.Msg(
                    "[WVC Ingredients] Representations for " +
                    itemId +
                    ": Equippable=" +
                    equippableApplied +
                    ", StationItem=" +
                    stationApplied +
                    ", StoredItem=" +
                    storedApplied
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] Representation setup failed for " +
                    itemId +
                    ": " +
                    ex
                );
            }
        }

        private static bool ApplyEquippableRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale
        )
        {
            try
            {
                if (definition.Equippable == null ||
                    definition.Equippable.gameObject == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] No Equippable template for " +
                        itemId
                    );

                    return false;
                }

                GameObject clone =
                    BuildNativeRepresentationClone(
                        definition.Equippable.gameObject,
                        itemId,
                        "Equippable",
                        customModel,
                        Vector3.zero,
                        Quaternion.identity,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.Equipping.Equippable component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Equipping.Equippable
                    >();

                if (component == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] Equippable component missing on clone for " +
                        itemId
                    );

                    return false;
                }

                definition.Equippable = component;

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] Equippable setup failed for " +
                    itemId +
                    ": " +
                    ex.Message
                );

                return false;
            }
        }

        private static bool ApplyStationRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale
        )
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.StorableItemDefinition storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] No StationItem template for " +
                        itemId
                    );

                    return false;
                }

                GameObject clone =
                    BuildNativeRepresentationClone(
                        storable.StationItem.gameObject,
                        itemId,
                        "StationItem",
                        customModel,
                        Vector3.zero,
                        Quaternion.identity,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.StationFramework.StationItem component =
                    clone.GetComponent<
                        Il2CppScheduleOne.StationFramework.StationItem
                    >();

                if (component == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] StationItem component missing for " +
                        itemId
                    );

                    return false;
                }

                storable.StationItem = component;

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] StationItem setup failed for " +
                    itemId +
                    ": " +
                    ex.Message
                );

                return false;
            }
        }

        private static bool ApplyStoredRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale
        )
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.StorableItemDefinition storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] No StoredItem template for " +
                        itemId
                    );

                    return false;
                }

                GameObject clone =
                    BuildNativeRepresentationClone(
                        storable.StoredItem.gameObject,
                        itemId,
                        "StoredItem",
                        customModel,
                        Vector3.zero,
                        Quaternion.identity,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.Storage.StoredItem component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Storage.StoredItem
                    >();

                if (component == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] StoredItem component missing for " +
                        itemId
                    );

                    return false;
                }

                storable.StoredItem = component;

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] StoredItem setup failed for " +
                    itemId +
                    ": " +
                    ex.Message
                );

                return false;
            }
        }

        private static GameObject BuildNativeRepresentationClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale
        )
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone =
                UnityEngine.Object.Instantiate(template);

            string safeId =
                itemId
                    .Replace(":", "_")
                    .Replace("/", "_");

            clone.name =
                "WVC_" +
                context +
                "_" +
                safeId;

            /*
             * Keep active. Inactive prefab-like sources cause invisible held items.
             */
            clone.SetActive(true);

            clone.transform.position =
                new Vector3(0f, -20000f, 0f);

            UnityEngine.Object.DontDestroyOnLoad(clone);

            /*
             * Hide all original iodine/motor-oil renderers.
             * Keep scripts, anchors, colliders, and hierarchy intact.
             */
            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            /*
             * Add the complete procedural model as a child.
             * This fixes third-person/world still showing iodine/oil.
             */
            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name =
                "WVC_CustomVisual_" +
                safeId +
                "_" +
                context;

            visual.transform.SetParent(
                clone.transform,
                false
            );

            visual.transform.localPosition =
                localPosition;

            visual.transform.localRotation =
                localRotation;

            visual.transform.localScale =
                localScale;

            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            MelonLogger.Msg(
                "[WVC Ingredients] Built " +
                context +
                " representation for " +
                itemId +
                " with " +
                customRenderers.Length +
                " custom renderers."
            );

            return clone;
        }

        // ============================================================
        // Pricing
        // ============================================================

        private static void SetIngredientPrice(
            string itemId,
            float price
        )
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                    return;

                Il2CppScheduleOne.ItemFramework.StorableItemDefinition storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null)
                {
                    MelonLogger.Warning(
                        "[WVC Ingredients] Item is not storable: " +
                        itemId
                    );

                    return;
                }

                storable.BasePurchasePrice = price;

                MelonLogger.Msg(
                    "[WVC Ingredients] Price set: " +
                    itemId +
                    " = $" +
                    price
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] Price setup failed for " +
                    itemId +
                    ": " +
                    ex.Message
                );
            }
        }

        // ============================================================
        // Icons
        // ============================================================

        private static void ApplyIcon(
            string itemId,
            Sprite icon
        )
        {
            if (icon == null)
                return;

            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                    return;

                bool applied =
                    TrySetMember(
                        definition,
                        "Icon",
                        icon
                    );

                MelonLogger.Msg(
                    "[WVC Ingredients] Icon applied=" +
                    applied +
                    " for " +
                    itemId
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] Icon setup failed for " +
                    itemId +
                    ": " +
                    ex.Message
                );
            }
        }

        private static Sprite RenderModelIcon(
    GameObject model,
    string iconName
)
        {
            if (model == null)
                return null;

            const int IconSize = 256;

            GameObject rig =
                new GameObject(iconName + "_Rig");

            rig.transform.position =
                new Vector3(6000f, 6000f, 6000f);

            GameObject instance =
                UnityEngine.Object.Instantiate(model);

            instance.transform.SetParent(
                rig.transform,
                false
            );

            instance.transform.localPosition =
                Vector3.zero;

            instance.transform.localRotation =
                Quaternion.Euler(
                    35f,
                    -25f,
                    0f
                );

            instance.transform.localScale =
                Vector3.one;

            instance.SetActive(true);

            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>(true);

            if (renderers == null ||
                renderers.Length == 0)
            {
                UnityEngine.Object.Destroy(rig);
                return null;
            }

            foreach (Renderer r in renderers)
            {
                if (r != null)
                    r.enabled = true;
            }

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            // Main directional light
            GameObject lightObject =
                new GameObject(iconName + "_Light");

            lightObject.transform.SetParent(
                rig.transform,
                false
            );

            Light light =
                lightObject.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 1.6f;

            lightObject.transform.rotation =
                Quaternion.Euler(
                    55f,
                    -30f,
                    0f
                );

            // Secondary fill light
            GameObject fillObject =
                new GameObject(iconName + "_FillLight");

            fillObject.transform.SetParent(
                rig.transform,
                false
            );

            Light fill =
                fillObject.AddComponent<Light>();

            fill.type = LightType.Directional;
            fill.intensity = 0.8f;

            fillObject.transform.rotation =
                Quaternion.Euler(
                    -20f,
                    140f,
                    0f
                );

            // Icon camera
            GameObject cameraObject =
                new GameObject(iconName + "_Camera");

            cameraObject.transform.SetParent(
                rig.transform,
                false
            );

            Camera camera =
                cameraObject.AddComponent<Camera>();

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.backgroundColor =
                new Color(0f, 0f, 0f, 0f);

            camera.orthographic = true;

            float largestExtent =
                Mathf.Max(
                    bounds.extents.x,
                    Mathf.Max(
                        bounds.extents.y,
                        bounds.extents.z
                    )
                );

            camera.orthographicSize =
                Mathf.Max(
                    0.05f,
                    largestExtent * 1.45f
                );

            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50f;

            camera.transform.position =
                bounds.center +
                new Vector3(
                    0.10f,
                    0.22f,
                    -0.50f
                );

            camera.transform.LookAt(
                bounds.center
            );

            RenderTexture target =
                new RenderTexture(
                    IconSize,
                    IconSize,
                    24,
                    RenderTextureFormat.ARGB32
                );

            camera.targetTexture = target;

            camera.Render();

            RenderTexture previous =
                RenderTexture.active;

            RenderTexture.active =
                target;

            Texture2D texture =
                new Texture2D(
                    IconSize,
                    IconSize,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                iconName + "_Texture";

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            texture.ReadPixels(
                new Rect(
                    0f,
                    0f,
                    IconSize,
                    IconSize
                ),
                0,
                0
            );

            texture.Apply();

            RenderTexture.active =
                previous;

            camera.targetTexture =
                null;

            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(rig);
            UnityEngine.Object.DontDestroyOnLoad(texture);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        IconSize,
                        IconSize
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    )
                );

            sprite.name = iconName;

            UnityEngine.Object.DontDestroyOnLoad(sprite);

            return sprite;
        }

        // ============================================================
        // Procedural Safrole Oil bottle
        // ============================================================

        private static GameObject CreateSafroleBottle()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_SafroleOil_Bottle"
                );

            AddCylinder(
                root.transform,
                "AmberBottleBody",
                0.028f,
                0.075f,
                new Vector3(
                    0f,
                    0.0375f,
                    0f
                ),
                new Color(
                    0.35f,
                    0.12f,
                    0.02f,
                    1f
                ),
                0.60f
            );

            AddCylinder(
                root.transform,
                "AmberBottleShoulder",
                0.022f,
                0.016f,
                new Vector3(
                    0f,
                    0.083f,
                    0f
                ),
                new Color(
                    0.42f,
                    0.15f,
                    0.03f,
                    1f
                ),
                0.60f
            );

            AddCylinder(
                root.transform,
                "BottleNeck",
                0.013f,
                0.022f,
                new Vector3(
                    0f,
                    0.101f,
                    0f
                ),
                new Color(
                    0.30f,
                    0.10f,
                    0.015f,
                    1f
                ),
                0.60f
            );

            AddCylinder(
                root.transform,
                "BlackCap",
                0.016f,
                0.014f,
                new Vector3(
                    0f,
                    0.118f,
                    0f
                ),
                new Color(
                    0.04f,
                    0.04f,
                    0.04f,
                    1f
                ),
                0.15f
            );

            AddLabelCube(
    root.transform,
    "SafroleLabel",
    new Vector3(
        0.052f,
        0.040f,
        0.002f
    ),
    new Vector3(
        0f,
        0.038f,
        -0.0295f
    ),
    "SAFROLE",
    "OIL",
    new Color(
        0.88f,
        0.82f,
        0.65f,
        1f
    ),
    Color.black
);

            return root;
        }

        // ============================================================
        // Procedural PMK Powder jar
        // ============================================================

        private static GameObject CreatePmkJar()
        {
            return CreatePmkJarStyled(
                "WVC_Custom_PmkPowder_Jar",
                new Color(0.92f, 0.93f, 0.94f, 1f),   // body
                new Color(0.06f, 0.10f, 0.18f, 1f),   // lid
                new Color(0.97f, 0.96f, 0.92f, 1f),   // powder
                new Color(0.12f, 0.34f, 0.58f, 1f),   // label bg
                Color.white,                          // label text
                "PMK",
                "POWDER"
            );
        }

        private static GameObject CreatePmkJarStyled(
            string rootName,
            Color bodyColor,
            Color lidColor,
            Color powderColor,
            Color labelColor,
            Color labelTextColor,
            string labelLine1,
            string labelLine2
        )
        {
            GameObject root = new GameObject(rootName);

            AddCylinder(
                root.transform,
                "WhiteJarBody",
                0.040f,
                0.058f,
                new Vector3(0f, 0.029f, 0f),
                bodyColor,
                0.15f
            );

            AddCylinder(
                root.transform,
                "JarInterior",
                0.034f,
                0.006f,
                new Vector3(0f, 0.0575f, 0f),
                new Color(0.055f, 0.060f, 0.070f, 1f),
                0.05f
            );

            AddCylinder(
                root.transform,
                "PowderSurface",
                0.033f,
                0.003f,
                new Vector3(0f, 0.0605f, 0f),
                powderColor,
                0.05f
            );

            AddCylinder(
                root.transform,
                "LidRing",
                0.041f,
                0.011f,
                new Vector3(0f, 0.067f, 0f),
                lidColor,
                0.25f
            );

            AddCylinder(
                root.transform,
                "ShakerPlate",
                0.034f,
                0.004f,
                new Vector3(0f, 0.0705f, 0f),
                lidColor,
                0.20f
            );

            Color holeColor =
                new Color(0.010f, 0.012f, 0.016f, 1f);

            AddCylinder(root.transform, "PourHoleCenter", 0.0045f, 0.0060f,
                new Vector3(0f, 0.0705f, 0f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleNorth", 0.0040f, 0.0060f,
                new Vector3(0f, 0.0705f, 0.014f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleSouth", 0.0040f, 0.0060f,
                new Vector3(0f, 0.0705f, -0.014f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleEast", 0.0040f, 0.0060f,
                new Vector3(0.014f, 0.0705f, 0f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleWest", 0.0040f, 0.0060f,
                new Vector3(-0.014f, 0.0705f, 0f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleNorthEast", 0.0035f, 0.0060f,
                new Vector3(0.010f, 0.0705f, 0.010f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleNorthWest", 0.0035f, 0.0060f,
                new Vector3(-0.010f, 0.0705f, 0.010f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleSouthEast", 0.0035f, 0.0060f,
                new Vector3(0.010f, 0.0705f, -0.010f), holeColor, 0.05f);

            AddCylinder(root.transform, "PourHoleSouthWest", 0.0035f, 0.0060f,
                new Vector3(-0.010f, 0.0705f, -0.010f), holeColor, 0.05f);

            AddLabelCube(
                root.transform,
                "PmkLabel",
                new Vector3(0.070f, 0.034f, 0.002f),
                new Vector3(0f, 0.029f, -0.0440f),
                labelLine1,
                labelLine2,
                labelColor,
                labelTextColor
            );

            return root;
        }

        // ============================================================
        // Procedural geometry helpers
        // ============================================================

        private static GameObject AddCylinder(
            Transform parent,
            string name,
            float radius,
            float height,
            Vector3 localPosition,
            Color color,
            float smoothness
        )
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder
                );

            part.name = name;

            part.transform.SetParent(
                parent,
                false
            );

            part.transform.localPosition =
                localPosition;

            part.transform.localRotation =
                Quaternion.identity;

            /*
             * A Unity cylinder is two units tall by default.
             */
            part.transform.localScale =
                new Vector3(
                    radius * 2f,
                    height * 0.5f,
                    radius * 2f
                );

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    CreateMaterial(
                        name + "_Material",
                        color,
                        smoothness
                    );
            }

            RemovePrimitiveCollider(
                part
            );

            return part;
        }

        private static GameObject AddCube(
            Transform parent,
            string name,
            Vector3 size,
            Vector3 localPosition,
            Color color,
            float smoothness
        )
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            part.name = name;

            part.transform.SetParent(
                parent,
                false
            );

            part.transform.localPosition =
                localPosition;

            part.transform.localRotation =
                Quaternion.identity;

            part.transform.localScale =
                size;

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    CreateMaterial(
                        name + "_Material",
                        color,
                        smoothness
                    );
            }

            RemovePrimitiveCollider(
                part
            );

            return part;
        }

        private static GameObject AddLabelCube(
    Transform parent,
    string name,
    Vector3 size,
    Vector3 localPosition,
    string line1,
    string line2,
    Color background,
    Color textColor
)
        {
            /*
             * A Quad has one predictable UV face. A cube displays the same
             * texture on multiple faces with different/mirrored UV directions.
             */
            GameObject part =
                GameObject.CreatePrimitive(
                    PrimitiveType.Quad
                );

            part.name = name;

            part.transform.SetParent(
                parent,
                false
            );

            part.transform.localPosition =
                localPosition;

            /*
             * Identity faces outward on the bottle/jar's negative-Z side.
             * Do not rotate this 180 degrees.
             */
            part.transform.localRotation =
                Quaternion.identity;

            part.transform.localScale =
                new Vector3(
                    size.x,
                    size.y,
                    1f
                );

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    CreateLabelMaterial(
                        name + "_Material",
                        background,
                        textColor,
                        line1,
                        line2
                    );


            }

            RemovePrimitiveCollider(part);

            return part;
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float smoothness
        )
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No compatible shader found."
                );
            }

            Material material =
                new Material(shader);

            material.name = name;

            if (material.HasProperty("_BaseColor"))
                material.SetColor(
                    "_BaseColor",
                    color
                );

            if (material.HasProperty("_Color"))
                material.SetColor(
                    "_Color",
                    color
                );

            if (material.HasProperty("_Smoothness"))
                material.SetFloat(
                    "_Smoothness",
                    smoothness
                );

            if (material.HasProperty("_Glossiness"))
                material.SetFloat(
                    "_Glossiness",
                    smoothness
                );

            if (material.HasProperty("_Metallic"))
                material.SetFloat(
                    "_Metallic",
                    0f
                );

            return material;
        }

        // ============================================================
        // Label text material
        // ============================================================

        private static Material CreateLabelMaterial(
    string name,
    Color background,
    Color textColor,
    string line1,
    string line2
)
        {
            Texture2D texture =
                new Texture2D(
                    256,
                    128,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                name + "_Texture";

            Color[] pixels =
                new Color[256 * 128];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            texture.SetPixels(pixels);

            DrawTextCentered(
                texture,
                line1,
                84,
                5,
                textColor
            );

            DrawTextCentered(
                texture,
                line2,
                40,
                5,
                textColor
            );

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            texture.Apply();



            UnityEngine.Object.DontDestroyOnLoad(texture);

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            Material material =
                new Material(shader);

            material.name =
                name;

            if (material.HasProperty("_BaseColor"))
                material.SetColor(
                    "_BaseColor",
                    Color.white
                );

            if (material.HasProperty("_Color"))
                material.SetColor(
                    "_Color",
                    Color.white
                );

            if (material.HasProperty("_BaseMap"))
                material.SetTexture(
                    "_BaseMap",
                    texture
                );

            if (material.HasProperty("_MainTex"))
                material.SetTexture(
                    "_MainTex",
                    texture
                );

            if (material.HasProperty("_Smoothness"))
                material.SetFloat(
                    "_Smoothness",
                    0.15f
                );

            if (material.HasProperty("_Glossiness"))
                material.SetFloat(
                    "_Glossiness",
                    0.15f
                );

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            return material;
        }

        private static void DrawTextCentered(
            Texture2D texture,
            string text,
            int centerY,
            int scale,
            Color color
        )
        {
            if (string.IsNullOrEmpty(text))
                return;

            int charWidth = 5 * scale;
            int spacing = scale;

            int totalWidth =
                text.Length * charWidth +
                (text.Length - 1) * spacing;

            int startX =
                (texture.width - totalWidth) / 2;

            int startY =
                centerY - (7 * scale) / 2;

            for (int i = 0; i < text.Length; i++)
            {
                DrawChar(
                    texture,
                    char.ToUpperInvariant(text[i]),
                    startX + i * (charWidth + spacing),
                    startY,
                    scale,
                    color
                );
            }
        }

        private static void DrawChar(
    Texture2D texture,
    char character,
    int x,
    int y,
    int scale,
    Color color
)
        {
            string[] glyph =
                GetGlyph(character);

            if (glyph == null)
                return;

            for (int row = 0; row < glyph.Length; row++)
            {
                string line = glyph[row];

                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '1')
                        continue;

                    for (int sx = 0; sx < scale; sx++)
                    {
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int px =
                                x +
                                col * scale +
                                sx;

                            /*
                             * Texture2D uses bottom-left as its origin, while
                             * the glyph arrays are written top-to-bottom.
                             * Reverse rows vertically, but do not mirror columns.
                             */
                            int py =
                                y +
                                (glyph.Length - 1 - row) * scale +
                                sy;

                            if (px < 0 ||
                                py < 0 ||
                                px >= texture.width ||
                                py >= texture.height)
                            {
                                continue;
                            }

                            texture.SetPixel(
                                px,
                                py,
                                color
                            );
                        }
                    }
                }
            }
        }

        private static string[] GetGlyph(
            char c
        )
        {
            switch (c)
            {
                case 'A':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "11111",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'D':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "11110"
                    };

                case 'E':
                    return new[]
                    {
                        "11111",
                        "10000",
                        "10000",
                        "11110",
                        "10000",
                        "10000",
                        "11111"
                    };

                case 'F':
                    return new[]
                    {
                        "11111",
                        "10000",
                        "10000",
                        "11110",
                        "10000",
                        "10000",
                        "10000"
                    };

                case 'I':
                    return new[]
                    {
                        "11111",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "11111"
                    };

                case 'K':
                    return new[]
                    {
                        "10001",
                        "10010",
                        "10100",
                        "11000",
                        "10100",
                        "10010",
                        "10001"
                    };

                case 'L':
                    return new[]
                    {
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "11111"
                    };

                case 'M':
                    return new[]
                    {
                        "10001",
                        "11011",
                        "10101",
                        "10101",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'O':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "01110"
                    };

                case 'P':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "11110",
                        "10000",
                        "10000",
                        "10000"
                    };

                case 'R':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "11110",
                        "10100",
                        "10010",
                        "10001"
                    };

                case 'S':
                    return new[]
                    {
                        "01111",
                        "10000",
                        "10000",
                        "01110",
                        "00001",
                        "00001",
                        "11110"
                    };

                case 'B':
                    return new[]
                    {
        "11110",
        "10001",
        "10001",
        "11110",
        "10001",
        "10001",
        "11110"
    };

                case 'G':
                    return new[]
                    {
        "01110",
        "10001",
        "10000",
        "10111",
        "10001",
        "10001",
        "01110"
    };

                case 'N':
                    return new[]
                    {
        "10001",
        "11001",
        "11001",
        "10101",
        "10011",
        "10011",
        "10001"
    };

                case 'W':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "10001",
                        "10101",
                        "10101",
                        "10101",
                        "01010"
                    };

                case ' ':
                    return new[]
                    {
                        "00000",
                        "00000",
                        "00000",
                        "00000",
                        "00000",
                        "00000",
                        "00000"
                    };
            }

            return new[]
            {
                "11111",
                "10001",
                "00010",
                "00100",
                "00100",
                "00000",
                "00100"
            };
        }


        // ============================================================
        // Collider removal without PhysicsModule reference
        // ============================================================

        private static void RemovePrimitiveCollider(
            GameObject gameObject
        )
        {
            if (gameObject == null)
                return;

            Component[] components =
                gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                string typeName =
                    component.GetType().Name;

                if (!typeName.EndsWith("Collider"))
                    continue;

                try
                {
                    UnityEngine.Object.Destroy(
                        component
                    );
                }
                catch
                {
                }
            }
        }

        private static void MoveSourceOffscreen(
            GameObject source
        )
        {
            if (source == null)
                return;

            source.transform.position =
                new Vector3(
                    0f,
                    -20000f,
                    0f
                );

            source.SetActive(true);

            UnityEngine.Object.DontDestroyOnLoad(
                source
            );
        }

        // ============================================================
        // Definition helpers
        // ============================================================

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(
                string itemId
            )
        {
            ItemDefinition wrapper =
                ItemManager.GetDefinition(
                    itemId
                );

            if (wrapper == null)
                return null;

            object raw =
                GetMemberValue(
                    wrapper,
                    "S1ItemDefinition"
                );

            return raw as
                Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static bool TrySetMember(
            object target,
            string name,
            object value
        )
        {
            if (target == null)
                return false;

            Type type =
                target.GetType();

            while (type != null)
            {
                FieldInfo field =
                    type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                if (field != null &&
                    value != null &&
                    field.FieldType.IsAssignableFrom(
                        value.GetType()
                    ))
                {
                    try
                    {
                        field.SetValue(
                            target,
                            value
                        );

                        return true;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo property =
                    type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                if (property != null &&
                    property.CanWrite &&
                    value != null &&
                    property.PropertyType.IsAssignableFrom(
                        value.GetType()
                    ))
                {
                    try
                    {
                        property.SetValue(
                            target,
                            value
                        );

                        return true;
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetMemberValue(
            object target,
            string name
        )
        {
            if (target == null)
                return null;

            try
            {
                Type type =
                    target.GetType();

                while (type != null)
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null)
                    {
                        try
                        {
                            return property.GetValue(
                                target
                            );
                        }
                        catch
                        {
                        }
                    }

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                    {
                        try
                        {
                            return field.GetValue(
                                target
                            );
                        }
                        catch
                        {
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }

        private static void RegisterAliasSafely(
            string alias,
            string itemId
        )
        {
            try
            {
                ConsoleItemAliases.Register(
                    alias,
                    itemId
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Ingredients] Alias '" +
                    alias +
                    "' failed: " +
                    ex.Message
                );
            }
        }
    }
}