using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class DMTIngredients
    {
        // Keep these IDs stable. Existing saves and supplier stock
        // reference the internal IDs, not the display names.
        public const string DreamrootId =
            "westvilleconnection:ingredients/dreamroot";

        public const string CausticBaseId =
            "westvilleconnection:ingredients/caustic_base";

        public const string LabSolventId =
            "westvilleconnection:ingredients/lab_solvent";

        public const string CrystalizerId =
            "westvilleconnection:ingredients/crystalizer";

        private const float MimosaBarkPrice = 18f;
        private const float CausticBasePrice = 8f;
        private const float LabSolventPrice = 22f;
        private const float CrystalizerPrice = 38f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _mimosaBarkVisual;
        private static GameObject _causticBaseVisual;
        private static GameObject _labSolventVisual;
        private static GameObject _crystalizerVisual;

        private static Sprite _mimosaBarkIcon;
        private static Sprite _causticBaseIcon;
        private static Sprite _labSolventIcon;
        private static Sprite _crystalizerIcon;

        // ============================================================
        // Registration
        // ============================================================

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                ItemDefinition iodine =
                    ItemManager.GetDefinition("iodine");

                ItemDefinition motorOil =
                    ItemManager.GetDefinition("motoroil");

                if (iodine == null ||
                    motorOil == null)
                {
                    MelonLogger.Warning(
                        "[WVC DMT Ingredients] Base templates not ready."
                    );

                    return false;
                }

                // ----------------------------------------------------
                // Mimosa Root Bark
                // ----------------------------------------------------

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        DreamrootId,
                        "Mimosa Root Bark",
                        "Shredded purple-red inner bark in a sealed " +
                        "kraft pouch. Imported by the kilo and invoiced " +
                        "as natural textile dye.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                // ----------------------------------------------------
                // Caustic Base
                // ----------------------------------------------------

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        CausticBaseId,
                        "Caustic Base",
                        "Strong alkaline pellets in a marked chemical " +
                        "container. Handle with care.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                // ----------------------------------------------------
                // Lab Solvent
                // ----------------------------------------------------

                MixIngredientItemCreator
                    .CloneFrom("motoroil")
                    .WithBasicInfo(
                        LabSolventId,
                        "Lab Solvent",
                        "Amber reagent solvent in a sealed laboratory " +
                        "bottle. Keep away from open flames.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                // ----------------------------------------------------
                // Crystalizer
                // ----------------------------------------------------

                MixIngredientItemCreator
                    .CloneFrom("motoroil")
                    .WithBasicInfo(
                        CrystalizerId,
                        "Crystalizer",
                        "A blue reagent bottle used for specialty " +
                        "purification work in the lab.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("mimosa", DreamrootId);
                RegisterAliasSafely("mhrb", DreamrootId);
                RegisterAliasSafely("bark", DreamrootId);

                RegisterAliasSafely("lye", CausticBaseId);
                RegisterAliasSafely("caustic", CausticBaseId);
                RegisterAliasSafely("base", CausticBaseId);

                RegisterAliasSafely("solvent", LabSolventId);
                RegisterAliasSafely("naphtha", LabSolventId);

                RegisterAliasSafely("crystalizer", CrystalizerId);
                RegisterAliasSafely("heptane", CrystalizerId);

                // Build refined source models.
                _mimosaBarkVisual =
                    CreateMimosaRootBarkBag();

                _causticBaseVisual =
                    CreateCausticBaseContainer();

                _labSolventVisual =
                    CreateLabSolventBottle();

                _crystalizerVisual =
                    CreateCrystalizerBottle();

                // Apply models for held, loose, storage, and station use.
                ApplyCustomRepresentations(
                    DreamrootId,
                    _mimosaBarkVisual,
                    1.05f,
                    1.20f
                );

                ApplyCustomRepresentations(
                    CausticBaseId,
                    _causticBaseVisual,
                    1.00f,
                    1.15f
                );

                ApplyCustomRepresentations(
                    LabSolventId,
                    _labSolventVisual,
                    0.95f,
                    1.10f
                );

                ApplyCustomRepresentations(
                    CrystalizerId,
                    _crystalizerVisual,
                    0.95f,
                    1.10f
                );

                // Render the actual custom models into inventory sprites.
                _mimosaBarkIcon =
                    RenderModelIcon(
                        _mimosaBarkVisual,
                        "WVC_MimosaRootBark_Icon"
                    );

                _causticBaseIcon =
                    RenderModelIcon(
                        _causticBaseVisual,
                        "WVC_CausticBase_Icon"
                    );

                _labSolventIcon =
                    RenderModelIcon(
                        _labSolventVisual,
                        "WVC_LabSolvent_Icon"
                    );

                _crystalizerIcon =
                    RenderModelIcon(
                        _crystalizerVisual,
                        "WVC_Crystalizer_Icon"
                    );

                ApplyIcon(
                    DreamrootId,
                    _mimosaBarkIcon
                );

                ApplyIcon(
                    CausticBaseId,
                    _causticBaseIcon
                );

                ApplyIcon(
                    LabSolventId,
                    _labSolventIcon
                );

                ApplyIcon(
                    CrystalizerId,
                    _crystalizerIcon
                );

                SetIngredientPrice(
                    DreamrootId,
                    MimosaBarkPrice
                );

                SetIngredientPrice(
                    CausticBaseId,
                    CausticBasePrice
                );

                SetIngredientPrice(
                    LabSolventId,
                    LabSolventPrice
                );

                SetIngredientPrice(
                    CrystalizerId,
                    CrystalizerPrice
                );

                MoveSourceOffscreen(
                    _mimosaBarkVisual
                );

                MoveSourceOffscreen(
                    _causticBaseVisual
                );

                MoveSourceOffscreen(
                    _labSolventVisual
                );

                MoveSourceOffscreen(
                    _crystalizerVisual
                );

                _registered = true;

                MelonLogger.Msg(
                    "[WVC DMT Ingredients] Registration complete. " +
                    "Mimosa=$" + MimosaBarkPrice +
                    ", Caustic=$" + CausticBasePrice +
                    ", Solvent=$" + LabSolventPrice +
                    ", Crystalizer=$" + CrystalizerPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error(
                    "[WVC DMT Ingredients] Registration failed: " +
                    ex
                );

                return false;
            }
        }

        // ============================================================
        // Custom model application
        // ============================================================

        private static void ApplyCustomRepresentations(
            string itemId,
            GameObject customModel,
            float heldMultiplier,
            float worldMultiplier)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                {
                    MelonLogger.Warning(
                        "[WVC DMT Ingredients] Raw definition missing: " +
                        itemId
                    );

                    return;
                }

                Vector3 heldScale =
                    Vector3.one * heldMultiplier;

                Vector3 worldScale =
                    Vector3.one * worldMultiplier;

                bool equippable =
                    ApplyEquippableRepresentation(
                        definition,
                        itemId,
                        customModel,
                        heldScale
                    );

                bool station =
                    ApplyStationRepresentation(
                        definition,
                        itemId,
                        customModel,
                        worldScale
                    );

                bool stored =
                    ApplyStoredRepresentation(
                        definition,
                        itemId,
                        customModel,
                        worldScale
                    );

                MelonLogger.Msg(
                    "[WVC DMT Ingredients] Representations for " +
                    itemId +
                    ": Equippable=" + equippable +
                    ", StationItem=" + station +
                    ", StoredItem=" + stored
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Ingredients] Representation setup failed for " +
                    itemId + ": " + ex.Message
                );
            }
        }

        private static bool ApplyEquippableRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                if (definition.Equippable == null ||
                    definition.Equippable.gameObject == null)
                {
                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        definition.Equippable.gameObject,
                        itemId,
                        "Equippable",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Equipping.Equippable
                    >();

                if (component == null)
                    return false;

                definition.Equippable =
                    component;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplyStationRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                var storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                {
                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        storable.StationItem.gameObject,
                        itemId,
                        "StationItem",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.StationFramework.StationItem
                    >();

                if (component == null)
                    return false;

                storable.StationItem =
                    component;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplyStoredRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                var storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                {
                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        storable.StoredItem.gameObject,
                        itemId,
                        "StoredItem",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Storage.StoredItem
                    >();

                if (component == null)
                    return false;

                storable.StoredItem =
                    component;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static GameObject BuildRepresentationClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 visualScale)
        {
            if (template == null ||
                customModel == null)
            {
                return null;
            }

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

            clone.transform.position =
                new Vector3(
                    0f,
                    -20000f,
                    0f
                );

            clone.SetActive(true);

            UnityEngine.Object.DontDestroyOnLoad(
                clone
            );

            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            GameObject visual =
                UnityEngine.Object.Instantiate(
                    customModel
                );

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
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.identity;

            visual.transform.localScale =
                visualScale;

            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            return clone;
        }

        // ============================================================
        // Refined ingredient models
        // ============================================================

        /*
         * Kraft stand-up pouch with folded top, reseal stripe,
         * cream label panel, and visible purple-red bark chips.
         */
        private static GameObject CreateMimosaRootBarkBag()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_MimosaRootBark_Bag"
                );

            Color kraft =
                new Color(
                    0.57f,
                    0.39f,
                    0.20f,
                    1f
                );

            Color kraftLight =
                new Color(
                    0.67f,
                    0.48f,
                    0.27f,
                    1f
                );

            Color kraftDark =
                new Color(
                    0.35f,
                    0.22f,
                    0.12f,
                    1f
                );

            Color barkDark =
                new Color(
                    0.26f,
                    0.05f,
                    0.07f,
                    1f
                );

            Color barkRed =
                new Color(
                    0.50f,
                    0.11f,
                    0.12f,
                    1f
                );

            Color label =
                new Color(
                    0.88f,
                    0.79f,
                    0.60f,
                    1f
                );

            // Main pouch body.
            AddCube(
                root.transform,
                "BagLower",
                new Vector3(
                    0.066f,
                    0.056f,
                    0.036f
                ),
                new Vector3(
                    0f,
                    0.030f,
                    0f
                ),
                kraft,
                0.08f
            );

            // Narrower upper pouch area.
            AddCube(
                root.transform,
                "BagUpper",
                new Vector3(
                    0.058f,
                    0.030f,
                    0.034f
                ),
                new Vector3(
                    0f,
                    0.070f,
                    0f
                ),
                kraftLight,
                0.08f
            );

            // Bottom seam.
            AddCube(
                root.transform,
                "BagBottomSeam",
                new Vector3(
                    0.069f,
                    0.006f,
                    0.039f
                ),
                new Vector3(
                    0f,
                    0.003f,
                    0f
                ),
                kraftDark,
                0.06f
            );

            // Folded top.
            AddCube(
                root.transform,
                "BagTopFold",
                new Vector3(
                    0.062f,
                    0.012f,
                    0.037f
                ),
                new Vector3(
                    0f,
                    0.091f,
                    0f
                ),
                kraftDark,
                0.06f
            );

            // Resealable red stripe.
            AddCube(
                root.transform,
                "ResealStripe",
                new Vector3(
                    0.054f,
                    0.004f,
                    0.0025f
                ),
                new Vector3(
                    0f,
                    0.080f,
                    -0.0195f
                ),
                barkRed,
                0.10f
            );

            // Cream front-label panel.
            AddCube(
                root.transform,
                "MimosaLabel",
                new Vector3(
                    0.050f,
                    0.036f,
                    0.0025f
                ),
                new Vector3(
                    0f,
                    0.040f,
                    -0.0195f
                ),
                label,
                0.12f
            );

            // Dark red bark marks on the label.
            AddCube(
                root.transform,
                "LabelBarkA",
                new Vector3(
                    0.007f,
                    0.023f,
                    0.003f
                ),
                new Vector3(
                    -0.012f,
                    0.041f,
                    -0.0215f
                ),
                barkDark,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    24f
                )
            );

            AddCube(
                root.transform,
                "LabelBarkB",
                new Vector3(
                    0.006f,
                    0.019f,
                    0.003f
                ),
                new Vector3(
                    0.001f,
                    0.041f,
                    -0.0215f
                ),
                barkRed,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    -18f
                )
            );

            AddCube(
                root.transform,
                "LabelBarkC",
                new Vector3(
                    0.006f,
                    0.021f,
                    0.003f
                ),
                new Vector3(
                    0.013f,
                    0.041f,
                    -0.0215f
                ),
                barkDark,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    31f
                )
            );

            // Loose bark chips peeking from the top.
            AddCube(
                root.transform,
                "LooseBarkA",
                new Vector3(
                    0.020f,
                    0.004f,
                    0.009f
                ),
                new Vector3(
                    -0.010f,
                    0.101f,
                    0.002f
                ),
                barkRed,
                0.08f,
                Quaternion.Euler(
                    8f,
                    18f,
                    16f
                )
            );

            AddCube(
                root.transform,
                "LooseBarkB",
                new Vector3(
                    0.016f,
                    0.004f,
                    0.008f
                ),
                new Vector3(
                    0.012f,
                    0.099f,
                    -0.003f
                ),
                barkDark,
                0.08f,
                Quaternion.Euler(
                    -6f,
                    -16f,
                    -21f
                )
            );

            return root;
        }

        /*
         * Squat white HDPE chemical container with black cap,
         * yellow hazard panel, and black warning diamond.
         */
        private static GameObject CreateCausticBaseContainer()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_CausticBase_Container"
                );

            Color plastic =
                new Color(
                    0.90f,
                    0.91f,
                    0.91f,
                    1f
                );

            Color plasticShade =
                new Color(
                    0.72f,
                    0.74f,
                    0.75f,
                    1f
                );

            Color cap =
                new Color(
                    0.07f,
                    0.08f,
                    0.09f,
                    1f
                );

            Color warningYellow =
                new Color(
                    0.94f,
                    0.71f,
                    0.06f,
                    1f
                );

            Color warningBlack =
                new Color(
                    0.05f,
                    0.05f,
                    0.05f,
                    1f
                );

            AddCylinder(
                root.transform,
                "ContainerBody",
                0.031f,
                0.058f,
                new Vector3(
                    0f,
                    0.029f,
                    0f
                ),
                plastic,
                0.22f
            );

            AddCylinder(
                root.transform,
                "ContainerBottomRing",
                0.032f,
                0.005f,
                new Vector3(
                    0f,
                    0.003f,
                    0f
                ),
                plasticShade,
                0.18f
            );

            AddCylinder(
                root.transform,
                "ContainerShoulder",
                0.026f,
                0.011f,
                new Vector3(
                    0f,
                    0.063f,
                    0f
                ),
                plastic,
                0.20f
            );

            AddCylinder(
                root.transform,
                "BlackCap",
                0.028f,
                0.014f,
                new Vector3(
                    0f,
                    0.076f,
                    0f
                ),
                cap,
                0.32f
            );

            AddCylinder(
                root.transform,
                "CapRidgeA",
                0.029f,
                0.002f,
                new Vector3(
                    0f,
                    0.070f,
                    0f
                ),
                warningBlack,
                0.25f
            );

            AddCylinder(
                root.transform,
                "CapRidgeB",
                0.029f,
                0.002f,
                new Vector3(
                    0f,
                    0.082f,
                    0f
                ),
                warningBlack,
                0.25f
            );

            AddCube(
                root.transform,
                "HazardLabel",
                new Vector3(
                    0.047f,
                    0.031f,
                    0.0025f
                ),
                new Vector3(
                    0f,
                    0.032f,
                    -0.0320f
                ),
                warningYellow,
                0.12f
            );

            AddCube(
                root.transform,
                "HazardDiamond",
                new Vector3(
                    0.014f,
                    0.014f,
                    0.003f
                ),
                new Vector3(
                    -0.010f,
                    0.032f,
                    -0.0340f
                ),
                warningBlack,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                )
            );

            AddCube(
                root.transform,
                "HazardStripeA",
                new Vector3(
                    0.013f,
                    0.003f,
                    0.003f
                ),
                new Vector3(
                    0.011f,
                    0.038f,
                    -0.0340f
                ),
                warningBlack,
                0.08f,
                Quaternion.Euler(
                    0f,
                    0f,
                    -20f
                )
            );

            AddCube(
                root.transform,
                "HazardStripeB",
                new Vector3(
                    0.013f,
                    0.003f,
                    0.003f
                ),
                new Vector3(
                    0.011f,
                    0.028f,
                    -0.0340f
                ),
                warningBlack,
                0.08f,
                Quaternion.Euler(
                    0f,
                    0f,
                    -20f
                )
            );

            return root;
        }

        /*
         * Amber reagent bottle with liquid band, white cap,
         * black label, and yellow side stripe.
         */
        private static GameObject CreateLabSolventBottle()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_LabSolvent_Bottle"
                );

            Color amber =
                new Color(
                    0.64f,
                    0.38f,
                    0.08f,
                    1f
                );

            Color amberLight =
                new Color(
                    0.88f,
                    0.61f,
                    0.14f,
                    1f
                );

            Color amberDeep =
                new Color(
                    0.30f,
                    0.15f,
                    0.03f,
                    1f
                );

            Color cap =
                new Color(
                    0.92f,
                    0.92f,
                    0.93f,
                    1f
                );

            Color label =
                new Color(
                    0.08f,
                    0.09f,
                    0.10f,
                    1f
                );

            Color stripe =
                new Color(
                    0.96f,
                    0.70f,
                    0.12f,
                    1f
                );

            AddCylinder(
                root.transform,
                "AmberBottleBody",
                0.026f,
                0.072f,
                new Vector3(
                    0f,
                    0.036f,
                    0f
                ),
                amber,
                0.38f
            );

            // Darker lower liquid band.
            AddCylinder(
                root.transform,
                "SolventLiquidBand",
                0.0265f,
                0.026f,
                new Vector3(
                    0f,
                    0.015f,
                    0f
                ),
                amberDeep,
                0.28f
            );

            AddCylinder(
                root.transform,
                "BottleShoulder",
                0.020f,
                0.014f,
                new Vector3(
                    0f,
                    0.078f,
                    0f
                ),
                amber,
                0.38f
            );

            AddCylinder(
                root.transform,
                "BottleNeck",
                0.011f,
                0.018f,
                new Vector3(
                    0f,
                    0.094f,
                    0f
                ),
                amberLight,
                0.34f
            );

            AddCylinder(
                root.transform,
                "WhiteCap",
                0.014f,
                0.013f,
                new Vector3(
                    0f,
                    0.109f,
                    0f
                ),
                cap,
                0.25f
            );

            AddCube(
                root.transform,
                "SolventLabel",
                new Vector3(
                    0.043f,
                    0.033f,
                    0.0025f
                ),
                new Vector3(
                    0f,
                    0.040f,
                    -0.0270f
                ),
                label,
                0.12f
            );

            AddCube(
                root.transform,
                "SolventStripe",
                new Vector3(
                    0.005f,
                    0.027f,
                    0.003f
                ),
                new Vector3(
                    -0.013f,
                    0.040f,
                    -0.0290f
                ),
                stripe,
                0.10f
            );

            AddCube(
                root.transform,
                "SolventMarkA",
                new Vector3(
                    0.014f,
                    0.003f,
                    0.003f
                ),
                new Vector3(
                    0.005f,
                    0.046f,
                    -0.0290f
                ),
                amberLight,
                0.08f
            );

            AddCube(
                root.transform,
                "SolventMarkB",
                new Vector3(
                    0.010f,
                    0.003f,
                    0.003f
                ),
                new Vector3(
                    0.005f,
                    0.035f,
                    -0.0290f
                ),
                amberLight,
                0.08f
            );

            return root;
        }

        /*
         * Cobalt-blue specialty reagent bottle with silver cap,
         * icy label panel, and crystal-shaped marks.
         */
        private static GameObject CreateCrystalizerBottle()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_Crystalizer_Bottle"
                );

            Color blue =
                new Color(
                    0.08f,
                    0.28f,
                    0.61f,
                    1f
                );

            Color blueLight =
                new Color(
                    0.20f,
                    0.53f,
                    0.90f,
                    1f
                );

            Color blueDark =
                new Color(
                    0.04f,
                    0.12f,
                    0.31f,
                    1f
                );

            Color silver =
                new Color(
                    0.70f,
                    0.74f,
                    0.79f,
                    1f
                );

            Color label =
                new Color(
                    0.80f,
                    0.92f,
                    1.00f,
                    1f
                );

            AddCylinder(
                root.transform,
                "BlueBottleBody",
                0.026f,
                0.072f,
                new Vector3(
                    0f,
                    0.036f,
                    0f
                ),
                blue,
                0.46f
            );

            AddCylinder(
                root.transform,
                "BlueLiquidBand",
                0.0265f,
                0.024f,
                new Vector3(
                    0f,
                    0.014f,
                    0f
                ),
                blueDark,
                0.38f
            );

            AddCylinder(
                root.transform,
                "BlueBottleShoulder",
                0.020f,
                0.014f,
                new Vector3(
                    0f,
                    0.078f,
                    0f
                ),
                blue,
                0.46f
            );

            AddCylinder(
                root.transform,
                "BlueBottleNeck",
                0.011f,
                0.018f,
                new Vector3(
                    0f,
                    0.094f,
                    0f
                ),
                blueLight,
                0.42f
            );

            AddCylinder(
                root.transform,
                "SilverCap",
                0.014f,
                0.013f,
                new Vector3(
                    0f,
                    0.109f,
                    0f
                ),
                silver,
                0.62f
            );

            AddCube(
                root.transform,
                "CrystalizerLabel",
                new Vector3(
                    0.043f,
                    0.033f,
                    0.0025f
                ),
                new Vector3(
                    0f,
                    0.040f,
                    -0.0270f
                ),
                label,
                0.12f
            );

            AddCube(
                root.transform,
                "CrystalMarkCenter",
                new Vector3(
                    0.010f,
                    0.010f,
                    0.003f
                ),
                new Vector3(
                    0f,
                    0.040f,
                    -0.0290f
                ),
                blueDark,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                )
            );

            AddCube(
                root.transform,
                "CrystalMarkLeft",
                new Vector3(
                    0.006f,
                    0.006f,
                    0.003f
                ),
                new Vector3(
                    -0.012f,
                    0.047f,
                    -0.0290f
                ),
                blueLight,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                )
            );

            AddCube(
                root.transform,
                "CrystalMarkRight",
                new Vector3(
                    0.006f,
                    0.006f,
                    0.003f
                ),
                new Vector3(
                    0.012f,
                    0.034f,
                    -0.0290f
                ),
                blueLight,
                0.10f,
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                )
            );

            return root;
        }

        // ============================================================
        // Model geometry helpers
        // ============================================================

        private static GameObject AddCube(
            Transform parent,
            string name,
            Vector3 size,
            Vector3 localPosition,
            Color color,
            float smoothness)
        {
            return AddCube(
                parent,
                name,
                size,
                localPosition,
                color,
                smoothness,
                Quaternion.identity
            );
        }

        private static GameObject AddCube(
            Transform parent,
            string name,
            Vector3 size,
            Vector3 localPosition,
            Color color,
            float smoothness,
            Quaternion localRotation)
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
                localRotation;

            part.transform.localScale =
                size;

            ApplyMaterial(
                part,
                name,
                color,
                smoothness,
                0f
            );

            RemovePrimitiveCollider(part);

            return part;
        }

        private static GameObject AddCylinder(
            Transform parent,
            string name,
            float radius,
            float height,
            Vector3 localPosition,
            Color color,
            float smoothness)
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

            part.transform.localScale =
                new Vector3(
                    radius * 2f,
                    height * 0.5f,
                    radius * 2f
                );

            ApplyMaterial(
                part,
                name,
                color,
                smoothness,
                0f
            );

            RemovePrimitiveCollider(part);

            return part;
        }

        private static void ApplyMaterial(
            GameObject part,
            string name,
            Color color,
            float smoothness,
            float metallic)
        {
            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer == null)
                return;

            renderer.sharedMaterial =
                CreateMaterial(
                    name + "_Material",
                    color,
                    smoothness,
                    metallic
                );
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float smoothness,
            float metallic)
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
            {
                material.SetColor(
                    "_BaseColor",
                    color
                );
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    color
                );
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    smoothness
                );
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat(
                    "_Glossiness",
                    smoothness
                );
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    metallic
                );
            }

            return material;
        }

        private static void RemovePrimitiveCollider(
            GameObject gameObject)
        {
            if (gameObject == null)
                return;

            Component[] components =
                gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                if (!component.GetType().Name.EndsWith(
                        "Collider"))
                {
                    continue;
                }

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

        // ============================================================
        // Render custom model into inventory icon
        // ============================================================

        private static int _iconRenderIndex;

        private static Sprite RenderModelIcon(
            GameObject source,
            string iconName)
        {
            const int IconSize = 256;

            if (source == null)
                return null;

            GameObject rig = null;
            RenderTexture target = null;

            RenderTexture previousActive =
                RenderTexture.active;

            try
            {
                int iconLayer =
                    FindIsolationLayer();

                // Unique position per icon so no two models
                // can ever share render space.
                _iconRenderIndex++;

                Vector3 rigOrigin =
                    new Vector3(
                        8000f + _iconRenderIndex * 500f,
                        8000f,
                        8000f
                    );

                rig =
                    new GameObject(iconName + "_RenderRig");

                rig.transform.position = rigOrigin;

                GameObject model =
                    UnityEngine.Object.Instantiate(source);

                model.transform.SetParent(rig.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation =
                    Quaternion.Euler(0f, 14f, 0f);
                model.transform.localScale = Vector3.one;
                model.SetActive(true);

                // Force the model onto the isolation layer FIRST.
                SetLayerRecursive(model, iconLayer);

                Renderer[] renderers =
                    model.GetComponentsInChildren<Renderer>(true);

                if (renderers == null || renderers.Length == 0)
                    return null;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        bounds.Encapsulate(renderers[i].bounds);
                }

                // Light also on the isolation layer.
                GameObject lightObject =
                    new GameObject(iconName + "_Light");
                lightObject.transform.SetParent(rig.transform, false);
                lightObject.layer = iconLayer;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.7f;
                light.color = new Color(1f, 0.96f, 0.90f, 1f);
                light.cullingMask = 1 << iconLayer;
                light.transform.rotation =
                    Quaternion.Euler(48f, -32f, 0f);

                // Camera renders ONLY this layer.
                GameObject cameraObject =
                    new GameObject(iconName + "_Camera");
                cameraObject.transform.SetParent(rig.transform, false);
                cameraObject.layer = iconLayer;

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.cullingMask = 1 << iconLayer;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;

                float largestExtent =
                    Mathf.Max(
                        bounds.extents.x,
                        Mathf.Max(bounds.extents.y, bounds.extents.z)
                    );

                camera.orthographicSize =
                    Mathf.Max(0.075f, largestExtent * 1.35f);

                float distance =
                    Mathf.Max(0.5f, largestExtent * 10f);

                camera.transform.position =
                    bounds.center +
                    new Vector3(0.36f, 0.24f, -1f).normalized * distance;

                camera.transform.LookAt(bounds.center);

                target =
                    new RenderTexture(
                        IconSize, IconSize, 16,
                        RenderTextureFormat.ARGB32
                    );

                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;

                Texture2D texture =
                    new Texture2D(
                        IconSize, IconSize,
                        TextureFormat.RGBA32, false
                    );
                texture.name = iconName + "_Texture";
                texture.ReadPixels(
                    new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                texture.Apply();
                UnityEngine.Object.DontDestroyOnLoad(texture);

                Sprite sprite =
                    Sprite.Create(
                        texture,
                        new Rect(0f, 0f, IconSize, IconSize),
                        new Vector2(0.5f, 0.5f)
                    );
                sprite.name = iconName;
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                return sprite;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Ingredients] Icon render failed for " +
                    iconName + ": " + ex.Message
                );
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (target != null)
                    UnityEngine.Object.Destroy(target);

                // Destroy the entire rig (model + light + camera)
                // immediately so it cannot bleed into the next icon.
                if (rig != null)
                    UnityEngine.Object.DestroyImmediate(rig);
            }
        }

        private static int FindIsolationLayer()
        {
            for (int layer = 31;
                 layer >= 8;
                 layer--)
            {
                if (string.IsNullOrEmpty(
                        LayerMask.LayerToName(
                            layer
                        )))
                {
                    return layer;
                }
            }

            return 31;
        }

        private static void SetLayerRecursive(
            GameObject target,
            int layer)
        {
            if (target == null)
                return;

            target.layer = layer;

            for (int i = 0;
                 i < target.transform.childCount;
                 i++)
            {
                Transform child =
                    target.transform.GetChild(i);

                if (child != null)
                {
                    SetLayerRecursive(
                        child.gameObject,
                        layer
                    );
                }
            }
        }

        // ============================================================
        // Icon assignment
        // ============================================================

        private static void ApplyIcon(
            string itemId,
            Sprite icon)
        {
            if (icon == null)
            {
                MelonLogger.Warning(
                    "[WVC DMT Ingredients] Icon was null for " +
                    itemId
                );

                return;
            }

            try
            {
                ItemDefinition wrapper =
                    ItemManager.GetDefinition(itemId);

                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    GetRawDefinition(itemId);

                bool wrapperDirect =
                    TrySetMember(
                        wrapper,
                        "Icon",
                        icon
                    );

                bool rawDirect =
                    TrySetMember(
                        raw,
                        "Icon",
                        icon
                    );

                bool wrapperAny =
                    TrySetAnyIconSpriteMember(
                        wrapper,
                        icon
                    );

                bool rawAny =
                    TrySetAnyIconSpriteMember(
                        raw,
                        icon
                    );

                MelonLogger.Msg(
                    "[WVC DMT Ingredients] Icon applied for " +
                    itemId +
                    ": wrapperDirect=" + wrapperDirect +
                    ", rawDirect=" + rawDirect +
                    ", wrapperAny=" + wrapperAny +
                    ", rawAny=" + rawAny
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Ingredients] Icon assignment failed for " +
                    itemId + ": " + ex.Message
                );
            }
        }

        private static bool TrySetAnyIconSpriteMember(
            object target,
            Sprite icon)
        {
            if (target == null ||
                icon == null)
            {
                return false;
            }

            bool applied = false;

            try
            {
                Type type =
                    target.GetType();

                while (type != null)
                {
                    FieldInfo[] fields =
                        type.GetFields(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    foreach (FieldInfo field in fields)
                    {
                        if (!typeof(Sprite).IsAssignableFrom(
                                field.FieldType))
                        {
                            continue;
                        }

                        if (!field.Name
                            .ToLowerInvariant()
                            .Contains("icon"))
                        {
                            continue;
                        }

                        try
                        {
                            field.SetValue(
                                target,
                                icon
                            );

                            applied = true;
                        }
                        catch
                        {
                        }
                    }

                    PropertyInfo[] properties =
                        type.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    foreach (PropertyInfo property in properties)
                    {
                        if (!property.CanWrite)
                            continue;

                        if (!typeof(Sprite).IsAssignableFrom(
                                property.PropertyType))
                        {
                            continue;
                        }

                        if (!property.Name
                            .ToLowerInvariant()
                            .Contains("icon"))
                        {
                            continue;
                        }

                        try
                        {
                            property.SetValue(
                                target,
                                icon
                            );

                            applied = true;
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

            return applied;
        }

        // ============================================================
        // Item pricing and definition access
        // ============================================================

        private static void SetIngredientPrice(
            string itemId,
            float price)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                    return;

                var storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null)
                    return;

                storable.BasePurchasePrice =
                    price;

                MelonLogger.Msg(
                    "[WVC DMT Ingredients] Price set: " +
                    itemId +
                    " = $" +
                    price
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC DMT Ingredients] Price failed for " +
                    itemId + ": " + ex.Message
                );
            }
        }

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(
                string itemId)
        {
            ItemDefinition wrapper =
                ItemManager.GetDefinition(itemId);

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

        // ============================================================
        // Reflection helpers
        // ============================================================

        private static bool TrySetMember(
            object target,
            string name,
            object value)
        {
            if (target == null ||
                value == null)
            {
                return false;
            }

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
                    field.FieldType.IsAssignableFrom(
                        value.GetType()))
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
                    property.PropertyType.IsAssignableFrom(
                        value.GetType()))
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
            string name)
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

        // ============================================================
        // General utility
        // ============================================================

        private static void MoveSourceOffscreen(
            GameObject source)
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

        private static void RegisterAliasSafely(
            string alias,
            string itemId)
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
                    "[WVC DMT Ingredients] Alias '" +
                    alias +
                    "' failed: " +
                    ex.Message
                );
            }
        }
    }
}