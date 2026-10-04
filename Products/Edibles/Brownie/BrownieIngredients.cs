using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class BrownieIngredients
    {
        public const string CocoaProductId =
            "westvilleconnection:ingredients/bakers_cocoa";

        public const string ButterProductId =
            "westvilleconnection:ingredients/infused_butter";

        public const string LeavenProductId =
            "westvilleconnection:ingredients/leavening_mix";

        private const float CocoaPrice = 40f;
        private const float ButterPrice = 65f;
        private const float LeavenPrice = 20f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _cocoaVisual;
        private static GameObject _butterVisual;
        private static GameObject _leavenVisual;

        private static Sprite _cocoaIcon;
        private static Sprite _butterIcon;
        private static Sprite _leavenIcon;

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

                if (iodine == null || motorOil == null)
                {


                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        CocoaProductId,
                        "Baker's Cocoa",
                        "Dark, potent cocoa powder in a resealable pouch. " +
                        "The base for any strong brownie.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        ButterProductId,
                        "Infused Butter",
                        "A stick of rich butter, discreetly enhanced. " +
                        "The active ingredient in every batch.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        LeavenProductId,
                        "Leavening Mix",
                        "A small tin of precise leavening blend that gives " +
                        "brownies their dense, chewy body.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("cocoa", CocoaProductId);
                RegisterAliasSafely("bakerscocoa", CocoaProductId);

                RegisterAliasSafely("butter", ButterProductId);
                RegisterAliasSafely("infusedbutter", ButterProductId);

                RegisterAliasSafely("leaven", LeavenProductId);
                RegisterAliasSafely("leavening", LeavenProductId);

                _cocoaVisual = CreateCocoaPouch();
                _butterVisual = CreateButterStick();
                _leavenVisual = CreateLeaveningTin();

                ApplyCustomRepresentations(
                    CocoaProductId,
                    _cocoaVisual,
                    1.05f,
                    1.20f
                );

                ApplyCustomRepresentations(
                    ButterProductId,
                    _butterVisual,
                    1.00f,
                    1.15f
                );

                ApplyCustomRepresentations(
                    LeavenProductId,
                    _leavenVisual,
                    1.00f,
                    1.15f
                );

                _cocoaIcon =
                    RenderModelIcon(_cocoaVisual, "WVC_BakersCocoa_Icon");

                _butterIcon =
                    RenderModelIcon(_butterVisual, "WVC_InfusedButter_Icon");

                _leavenIcon =
                    RenderModelIcon(_leavenVisual, "WVC_LeaveningMix_Icon");

                ApplyIcon(CocoaProductId, _cocoaIcon);
                ApplyIcon(ButterProductId, _butterIcon);
                ApplyIcon(LeavenProductId, _leavenIcon);

                SetIngredientPrice(CocoaProductId, CocoaPrice);
                SetIngredientPrice(ButterProductId, ButterPrice);
                SetIngredientPrice(LeavenProductId, LeavenPrice);

                MoveSourceOffscreen(_cocoaVisual);
                MoveSourceOffscreen(_butterVisual);
                MoveSourceOffscreen(_leavenVisual);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Ingredients] Registration complete. " +
                    "Cocoa=$" + CocoaPrice +
                    ", Butter=$" + ButterPrice +
                    ", Leaven=$" + LeavenPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error(
                    "[WVC Brownie Ingredients] Registration failed: " + ex
                );

                return false;
            }
        }

        private static GameObject CreateCocoaPouch()
        {
            GameObject root =
                new GameObject("WVC_Custom_BakersCocoa_Pouch");

            Color brown = new Color(0.34f, 0.20f, 0.10f, 1f);
            Color brownLight = new Color(0.44f, 0.28f, 0.15f, 1f);
            Color brownDark = new Color(0.20f, 0.11f, 0.05f, 1f);
            Color label = new Color(0.90f, 0.83f, 0.66f, 1f);
            Color cocoaDark = new Color(0.15f, 0.08f, 0.04f, 1f);

            AddCube(root.transform, "PouchBody",
                new Vector3(0.062f, 0.058f, 0.034f),
                new Vector3(0f, 0.032f, 0f), brown, 0.08f);

            AddCube(root.transform, "PouchUpper",
                new Vector3(0.055f, 0.028f, 0.032f),
                new Vector3(0f, 0.072f, 0f), brownLight, 0.08f);

            AddCube(root.transform, "PouchBottomSeam",
                new Vector3(0.065f, 0.006f, 0.037f),
                new Vector3(0f, 0.003f, 0f), brownDark, 0.06f);

            AddCube(root.transform, "PouchTopFold",
                new Vector3(0.058f, 0.012f, 0.035f),
                new Vector3(0f, 0.092f, 0f), brownDark, 0.06f);

            AddCube(root.transform, "CocoaLabel",
                new Vector3(0.047f, 0.036f, 0.0025f),
                new Vector3(0f, 0.040f, -0.0185f), label, 0.12f);

            AddCube(root.transform, "LabelChipA",
                new Vector3(0.010f, 0.010f, 0.003f),
                new Vector3(-0.010f, 0.044f, -0.0205f), cocoaDark, 0.10f,
                Quaternion.Euler(0f, 0f, 45f));

            AddCube(root.transform, "LabelChipB",
                new Vector3(0.008f, 0.008f, 0.003f),
                new Vector3(0.006f, 0.038f, -0.0205f), cocoaDark, 0.10f,
                Quaternion.Euler(0f, 0f, 45f));

            AddCube(root.transform, "LabelChipC",
                new Vector3(0.007f, 0.007f, 0.003f),
                new Vector3(0.013f, 0.047f, -0.0205f), cocoaDark, 0.10f,
                Quaternion.Euler(0f, 0f, 45f));

            AddCube(root.transform, "LooseCocoaA",
                new Vector3(0.018f, 0.004f, 0.009f),
                new Vector3(-0.009f, 0.101f, 0.002f), cocoaDark, 0.06f,
                Quaternion.Euler(7f, 16f, 14f));

            AddCube(root.transform, "LooseCocoaB",
                new Vector3(0.014f, 0.004f, 0.008f),
                new Vector3(0.011f, 0.099f, -0.003f), brownDark, 0.06f,
                Quaternion.Euler(-6f, -14f, -18f));

            return root;
        }

        private static GameObject CreateButterStick()
        {
            GameObject root =
                new GameObject("WVC_Custom_InfusedButter_Stick");

            Color butter = new Color(0.96f, 0.85f, 0.42f, 1f);
            Color butterDeep = new Color(0.88f, 0.74f, 0.28f, 1f);
            Color foil = new Color(0.92f, 0.78f, 0.24f, 1f);
            Color foilDark = new Color(0.66f, 0.53f, 0.14f, 1f);
            Color label = new Color(0.95f, 0.93f, 0.86f, 1f);
            Color print = new Color(0.30f, 0.18f, 0.05f, 1f);

            AddCube(root.transform, "ButterBlock",
                new Vector3(0.088f, 0.030f, 0.032f),
                new Vector3(0f, 0.020f, 0f), butter, 0.20f);

            AddCube(root.transform, "ButterTop",
                new Vector3(0.086f, 0.006f, 0.030f),
                new Vector3(0f, 0.036f, 0f), butterDeep, 0.20f);

            AddCube(root.transform, "FoilWrap",
                new Vector3(0.090f, 0.024f, 0.034f),
                new Vector3(0f, 0.020f, 0f), foil, 0.55f);

            AddCube(root.transform, "FoilSeamLeft",
                new Vector3(0.004f, 0.026f, 0.036f),
                new Vector3(-0.045f, 0.020f, 0f), foilDark, 0.45f);

            AddCube(root.transform, "FoilSeamRight",
                new Vector3(0.004f, 0.026f, 0.036f),
                new Vector3(0.045f, 0.020f, 0f), foilDark, 0.45f);

            AddCube(root.transform, "ButterLabel",
                new Vector3(0.050f, 0.018f, 0.0025f),
                new Vector3(0f, 0.020f, -0.0185f), label, 0.12f);

            AddCube(root.transform, "LabelPrintA",
                new Vector3(0.030f, 0.003f, 0.003f),
                new Vector3(0f, 0.024f, -0.0205f), print, 0.08f);

            AddCube(root.transform, "LabelPrintB",
                new Vector3(0.022f, 0.003f, 0.003f),
                new Vector3(0f, 0.016f, -0.0205f), print, 0.08f);

            return root;
        }

        private static GameObject CreateLeaveningTin()
        {
            GameObject root =
                new GameObject("WVC_Custom_LeaveningMix_Tin");

            Color metal = new Color(0.78f, 0.80f, 0.83f, 1f);
            Color metalDark = new Color(0.58f, 0.60f, 0.63f, 1f);
            Color lidRed = new Color(0.74f, 0.14f, 0.12f, 1f);
            Color lidRedDark = new Color(0.55f, 0.09f, 0.08f, 1f);
            Color label = new Color(0.92f, 0.88f, 0.78f, 1f);
            Color print = new Color(0.20f, 0.14f, 0.10f, 1f);

            AddCylinder(root.transform, "TinBody",
                0.028f, 0.050f,
                new Vector3(0f, 0.025f, 0f), metal, 0.35f);

            AddCylinder(root.transform, "TinBottomRing",
                0.029f, 0.005f,
                new Vector3(0f, 0.003f, 0f), metalDark, 0.30f);

            AddCylinder(root.transform, "TinShoulder",
                0.027f, 0.006f,
                new Vector3(0f, 0.052f, 0f), metalDark, 0.32f);

            AddCylinder(root.transform, "RedLid",
                0.029f, 0.012f,
                new Vector3(0f, 0.061f, 0f), lidRed, 0.30f);

            AddCylinder(root.transform, "LidRim",
                0.030f, 0.003f,
                new Vector3(0f, 0.055f, 0f), lidRedDark, 0.28f);

            AddCube(root.transform, "TinLabel",
                new Vector3(0.042f, 0.030f, 0.0025f),
                new Vector3(0f, 0.026f, -0.0290f), label, 0.12f);

            AddCube(root.transform, "TinPrintA",
                new Vector3(0.028f, 0.004f, 0.003f),
                new Vector3(0f, 0.032f, -0.0310f), print, 0.08f);

            AddCube(root.transform, "TinPrintB",
                new Vector3(0.022f, 0.003f, 0.003f),
                new Vector3(0f, 0.024f, -0.0310f), print, 0.08f);

            AddCube(root.transform, "TinPrintC",
                new Vector3(0.016f, 0.003f, 0.003f),
                new Vector3(0f, 0.017f, -0.0310f), print, 0.08f);

            return root;
        }

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

                    return;
                }

                Vector3 heldScale = Vector3.one * heldMultiplier;
                Vector3 worldScale = Vector3.one * worldMultiplier;

                bool equippable =
                    ApplyEquippableRepresentation(definition, itemId, customModel, heldScale);

                bool station =
                    ApplyStationRepresentation(definition, itemId, customModel, worldScale);

                bool stored =
                    ApplyStoredRepresentation(definition, itemId, customModel, worldScale);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Ingredients] Representations for " + itemId +
                    ": Equippable=" + equippable +
                    ", StationItem=" + station +
                    ", StoredItem=" + stored
                );
            }
            catch (Exception)
            {

            }
        }

        private static bool ApplyEquippableRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId, GameObject customModel, Vector3 visualScale)
        {
            try
            {
                if (definition.Equippable == null ||
                    definition.Equippable.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    definition.Equippable.gameObject, itemId, "Equippable",
                    customModel, visualScale);

                if (clone == null) return false;

                var component =
                    clone.GetComponent<Il2CppScheduleOne.Equipping.Equippable>();

                if (component == null) return false;

                definition.Equippable = component;
                return true;
            }
            catch { return false; }
        }

        private static bool ApplyStationRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId, GameObject customModel, Vector3 visualScale)
        {
            try
            {
                var storable = definition.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null ||
                    storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StationItem.gameObject, itemId, "StationItem",
                    customModel, visualScale);

                if (clone == null) return false;

                var component =
                    clone.GetComponent<Il2CppScheduleOne.StationFramework.StationItem>();

                if (component == null) return false;

                storable.StationItem = component;
                return true;
            }
            catch { return false; }
        }

        private static bool ApplyStoredRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId, GameObject customModel, Vector3 visualScale)
        {
            try
            {
                var storable = definition.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null ||
                    storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StoredItem.gameObject, itemId, "StoredItem",
                    customModel, visualScale);

                if (clone == null) return false;

                var component =
                    clone.GetComponent<Il2CppScheduleOne.Storage.StoredItem>();

                if (component == null) return false;

                storable.StoredItem = component;
                return true;
            }
            catch { return false; }
        }

        private static GameObject BuildRepresentationClone(
            GameObject template, string itemId, string context,
            GameObject customModel, Vector3 visualScale)
        {
            if (template == null || customModel == null) return null;

            GameObject clone = UnityEngine.Object.Instantiate(template);

            string safeId = itemId.Replace(":", "_").Replace("/", "_");

            clone.name = "WVC_" + context + "_" + safeId;
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            clone.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
                if (renderer != null) renderer.enabled = false;

            GameObject visual = UnityEngine.Object.Instantiate(customModel);
            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = visualScale;
            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
                if (renderer != null) renderer.enabled = true;

            return clone;
        }

        private static GameObject AddCube(
            Transform parent, string name, Vector3 size,
            Vector3 localPosition, Color color, float smoothness)
        {
            return AddCube(parent, name, size, localPosition, color,
                smoothness, Quaternion.identity);
        }

        private static GameObject AddCube(
            Transform parent, string name, Vector3 size,
            Vector3 localPosition, Color color, float smoothness,
            Quaternion localRotation)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = size;

            ApplyMaterial(part, name, color, smoothness, 0f);
            RemovePrimitiveCollider(part);

            return part;
        }

        private static GameObject AddCylinder(
            Transform parent, string name, float radius, float height,
            Vector3 localPosition, Color color, float smoothness)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale =
                new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            ApplyMaterial(part, name, color, smoothness, 0f);
            RemovePrimitiveCollider(part);

            return part;
        }

        private static void ApplyMaterial(
            GameObject part, string name, Color color,
            float smoothness, float metallic)
        {
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null) return;

            renderer.sharedMaterial =
                CreateMaterial(name + "_Material", color, smoothness, metallic);
        }

        private static Material CreateMaterial(
            string name, Color color, float smoothness, float metallic)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
                throw new InvalidOperationException("No compatible shader found.");

            Material material = new Material(shader);
            material.name = name;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            return material;
        }

        private static void RemovePrimitiveCollider(GameObject gameObject)
        {
            if (gameObject == null) return;

            Component[] components = gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null) continue;
                if (!component.GetType().Name.EndsWith("Collider")) continue;

                try { UnityEngine.Object.Destroy(component); }
                catch { }
            }
        }

        private static int _iconRenderIndex;

        private static Sprite RenderModelIcon(GameObject source, string iconName)
        {
            const int IconSize = 256;

            if (source == null) return null;

            GameObject rig = null;
            RenderTexture target = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                int iconLayer = FindIsolationLayer();
                _iconRenderIndex++;

                Vector3 rigOrigin =
                    new Vector3(9000f + _iconRenderIndex * 500f, 9000f, 9000f);

                rig = new GameObject(iconName + "_RenderRig");
                rig.transform.position = rigOrigin;

                GameObject model = UnityEngine.Object.Instantiate(source);
                model.transform.SetParent(rig.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);
                model.transform.localScale = Vector3.one;
                model.SetActive(true);

                SetLayerRecursive(model, iconLayer);

                Renderer[] renderers =
                    model.GetComponentsInChildren<Renderer>(true);

                if (renderers == null || renderers.Length == 0) return null;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    if (renderers[i] != null)
                        bounds.Encapsulate(renderers[i].bounds);

                GameObject lightObject = new GameObject(iconName + "_Light");
                lightObject.transform.SetParent(rig.transform, false);
                lightObject.layer = iconLayer;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.7f;
                light.color = new Color(1f, 0.96f, 0.90f, 1f);
                light.cullingMask = 1 << iconLayer;
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

                GameObject cameraObject = new GameObject(iconName + "_Camera");
                cameraObject.transform.SetParent(rig.transform, false);
                cameraObject.layer = iconLayer;

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.cullingMask = 1 << iconLayer;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;

                float largestExtent = Mathf.Max(bounds.extents.x,
                    Mathf.Max(bounds.extents.y, bounds.extents.z));

                camera.orthographicSize =
                    Mathf.Max(0.075f, largestExtent * 1.35f);

                float distance = Mathf.Max(0.5f, largestExtent * 10f);

                camera.transform.position = bounds.center +
                    new Vector3(0.36f, 0.24f, -1f).normalized * distance;

                camera.transform.LookAt(bounds.center);

                target = new RenderTexture(IconSize, IconSize, 16,
                    RenderTextureFormat.ARGB32);

                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;

                Texture2D texture = new Texture2D(IconSize, IconSize,
                    TextureFormat.RGBA32, false);
                texture.name = iconName + "_Texture";
                texture.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                texture.Apply();
                UnityEngine.Object.DontDestroyOnLoad(texture);

                Sprite sprite = Sprite.Create(texture,
                    new Rect(0f, 0f, IconSize, IconSize),
                    new Vector2(0.5f, 0.5f));
                sprite.name = iconName;
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                return sprite;
            }
            catch (Exception)
            {

                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (target != null) UnityEngine.Object.Destroy(target);
                if (rig != null) UnityEngine.Object.DestroyImmediate(rig);
            }
        }

        private static int FindIsolationLayer()
        {
            for (int layer = 31; layer >= 8; layer--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
                    return layer;
            return 31;
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            if (target == null) return;
            target.layer = layer;

            for (int i = 0; i < target.transform.childCount; i++)
            {
                Transform child = target.transform.GetChild(i);
                if (child != null)
                    SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static void ApplyIcon(string itemId, Sprite icon)
        {
            if (icon == null)
            {

                return;
            }

            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    GetRawDefinition(itemId);

                bool wrapperDirect = TrySetMember(wrapper, "Icon", icon);
                bool rawDirect = TrySetMember(raw, "Icon", icon);
                bool wrapperAny = TrySetAnyIconSpriteMember(wrapper, icon);
                bool rawAny = TrySetAnyIconSpriteMember(raw, icon);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Ingredients] Icon applied for " + itemId +
                    ": wrapperDirect=" + wrapperDirect +
                    ", rawDirect=" + rawDirect +
                    ", wrapperAny=" + wrapperAny +
                    ", rawAny=" + rawAny
                );
            }
            catch (Exception)
            {

            }
        }

        private static bool TrySetAnyIconSpriteMember(object target, Sprite icon)
        {
            if (target == null || icon == null) return false;

            bool applied = false;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (FieldInfo field in fields)
                    {
                        if (!typeof(Sprite).IsAssignableFrom(field.FieldType))
                            continue;
                        if (!field.Name.ToLowerInvariant().Contains("icon"))
                            continue;

                        try { field.SetValue(target, icon); applied = true; }
                        catch { }
                    }

                    PropertyInfo[] properties = type.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (PropertyInfo property in properties)
                    {
                        if (!property.CanWrite) continue;
                        if (!typeof(Sprite).IsAssignableFrom(property.PropertyType))
                            continue;
                        if (!property.Name.ToLowerInvariant().Contains("icon"))
                            continue;

                        try { property.SetValue(target, icon); applied = true; }
                        catch { }
                    }

                    type = type.BaseType;
                }
            }
            catch { }

            return applied;
        }

        private static void SetIngredientPrice(string itemId, float price)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null) return;

                var storable = definition.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null) return;

                storable.BasePurchasePrice = price;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Ingredients] Price set: " + itemId + " = $" + price
                );
            }
            catch (Exception)
            {

            }
        }

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(string itemId)
        {
            ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
            if (wrapper == null) return null;

            object raw = GetMemberValue(wrapper, "S1ItemDefinition");
            return raw as Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static bool TrySetMember(object target, string name, object value)
        {
            if (target == null || value == null) return false;

            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null &&
                    field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    try { field.SetValue(target, value); return true; }
                    catch { }
                }

                PropertyInfo property = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (property != null && property.CanWrite &&
                    property.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try { property.SetValue(target, value); return true; }
                    catch { }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null) return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    if (property != null)
                    {
                        try { return property.GetValue(target); }
                        catch { }
                    }

                    FieldInfo field = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    if (field != null)
                    {
                        try { return field.GetValue(target); }
                        catch { }
                    }

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }

        private static void MoveSourceOffscreen(GameObject source)
        {
            if (source == null) return;

            source.transform.position = new Vector3(0f, -20000f, 0f);
            source.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(source);
        }

        private static void RegisterAliasSafely(string alias, string itemId)
        {
            try
            {
                ConsoleItemAliases.Register(alias, itemId);
            }
            catch (Exception)
            {

            }
        }
    }
}
