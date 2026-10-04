using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class UnbakedBrownieMix
    {
        public const string ItemId =
            "westvilleconnection:ingredients/unbaked_brownie_mix";

        private const float MixPrice = 140f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _visual;
        private static Sprite _icon;

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

                if (iodine == null)
                {


                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        ItemId,
                        "Unbaked Brownie Mix",
                        "A tray of thick chocolate batter. " +
                        "Needs to be baked before it does anything.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("browniemix", ItemId);
                RegisterAliasSafely("batter", ItemId);
                RegisterAliasSafely("unbaked", ItemId);

                _visual = CreateBatterTray();

                ApplyCustomRepresentations(
                    ItemId,
                    _visual,
                    1.00f,
                    1.15f
                );

                _icon =
                    RenderModelIcon(
                        _visual,
                        "WVC_UnbakedBrownieMix_Icon"
                    );

                ApplyIcon(ItemId, _icon);
                SetIngredientPrice(ItemId, MixPrice);
                MoveSourceOffscreen(_visual);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Mix] Registered. Price=$" + MixPrice
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error(
                    "[WVC Brownie Mix] Registration failed: " + ex
                );

                return false;
            }
        }

        private static GameObject CreateBatterTray()
        {
            GameObject root =
                new GameObject("WVC_Custom_UnbakedBrownieMix_Tray");

            Color tray = new Color(0.62f, 0.64f, 0.67f, 1f);
            Color trayDark = new Color(0.44f, 0.46f, 0.49f, 1f);
            Color batter = new Color(0.22f, 0.12f, 0.06f, 1f);
            Color batterLight = new Color(0.32f, 0.18f, 0.09f, 1f);

            AddCube(root.transform, "TrayFloor",
                new Vector3(0.084f, 0.005f, 0.062f),
                new Vector3(0f, 0.003f, 0f), tray, 0.55f);

            AddCube(root.transform, "TrayWallFront",
                new Vector3(0.084f, 0.020f, 0.004f),
                new Vector3(0f, 0.012f, -0.029f), tray, 0.55f);

            AddCube(root.transform, "TrayWallBack",
                new Vector3(0.084f, 0.020f, 0.004f),
                new Vector3(0f, 0.012f, 0.029f), tray, 0.55f);

            AddCube(root.transform, "TrayWallLeft",
                new Vector3(0.004f, 0.020f, 0.062f),
                new Vector3(-0.040f, 0.012f, 0f), tray, 0.55f);

            AddCube(root.transform, "TrayWallRight",
                new Vector3(0.004f, 0.020f, 0.062f),
                new Vector3(0.040f, 0.012f, 0f), tray, 0.55f);

            AddCube(root.transform, "TrayRim",
                new Vector3(0.088f, 0.003f, 0.066f),
                new Vector3(0f, 0.022f, 0f), trayDark, 0.50f);

            AddCube(root.transform, "Batter",
                new Vector3(0.076f, 0.013f, 0.054f),
                new Vector3(0f, 0.012f, 0f), batter, 0.30f);

            AddCube(root.transform, "BatterLumpA",
                new Vector3(0.022f, 0.005f, 0.018f),
                new Vector3(-0.018f, 0.019f, 0.008f), batterLight, 0.28f,
                Quaternion.Euler(0f, 12f, 0f));

            AddCube(root.transform, "BatterLumpB",
                new Vector3(0.018f, 0.004f, 0.015f),
                new Vector3(0.016f, 0.018f, -0.010f), batterLight, 0.28f,
                Quaternion.Euler(0f, -22f, 0f));

            AddCube(root.transform, "BatterLumpC",
                new Vector3(0.014f, 0.004f, 0.013f),
                new Vector3(0.004f, 0.018f, 0.014f), batter, 0.28f,
                Quaternion.Euler(0f, 40f, 0f));

            return root;
        }

        private static void ApplyCustomRepresentations(
            string itemId, GameObject customModel,
            float heldMultiplier, float worldMultiplier)
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

                bool equippable = ApplyEquippableRepresentation(
                    definition, itemId, customModel, heldScale);

                bool station = ApplyStationRepresentation(
                    definition, itemId, customModel, worldScale);

                bool stored = ApplyStoredRepresentation(
                    definition, itemId, customModel, worldScale);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Mix] Representations: Equippable=" + equippable +
                    ", StationItem=" + station + ", StoredItem=" + stored
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
                    definition.Equippable.gameObject, itemId,
                    "Equippable", customModel, visualScale);

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

                if (storable == null || storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StationItem.gameObject, itemId,
                    "StationItem", customModel, visualScale);

                if (clone == null) return false;

                var component = clone.GetComponent<
                    Il2CppScheduleOne.StationFramework.StationItem>();

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

                if (storable == null || storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StoredItem.gameObject, itemId,
                    "StoredItem", customModel, visualScale);

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

            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = false;

            GameObject visual = UnityEngine.Object.Instantiate(customModel);
            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = visualScale;
            visual.SetActive(true);

            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = true;

            return clone;
        }

        private static GameObject AddCube(
            Transform parent, string name, Vector3 size,
            Vector3 localPosition, Color color, float smoothness)
        {
            return AddCube(parent, name, size, localPosition,
                color, smoothness, Quaternion.identity);
        }

        private static GameObject AddCube(
            Transform parent, string name, Vector3 size,
            Vector3 localPosition, Color color, float smoothness,
            Quaternion localRotation)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = size;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial =
                    CreateMaterial(name + "_Material", color, smoothness);

            RemovePrimitiveCollider(part);
            return part;
        }

        private static Material CreateMaterial(
            string name, Color color, float smoothness)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
                throw new InvalidOperationException("No shader found.");

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
                material.SetFloat("_Metallic", 0f);

            return material;
        }

        private static void RemovePrimitiveCollider(GameObject gameObject)
        {
            if (gameObject == null) return;

            foreach (Component c in gameObject.GetComponents<Component>())
            {
                if (c == null) continue;
                if (!c.GetType().Name.EndsWith("Collider")) continue;
                try { UnityEngine.Object.Destroy(c); } catch { }
            }
        }

        private static Sprite RenderModelIcon(
            GameObject source, string iconName)
        {
            const int IconSize = 256;
            if (source == null) return null;

            GameObject rig = null;
            RenderTexture target = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                int iconLayer = FindIsolationLayer();

                rig = new GameObject(iconName + "_RenderRig");
                rig.transform.position = new Vector3(11000f, 11000f, 11000f);

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
                    new Vector3(0.36f, 0.40f, -1f).normalized * distance;

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
            if (icon == null) return;

            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    GetRawDefinition(itemId);

                TrySetMember(wrapper, "Icon", icon);
                TrySetMember(raw, "Icon", icon);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Brownie Mix] Icon applied for " + itemId
                );
            }
            catch (Exception)
            {

            }
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
            }
            catch { }
        }

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(string itemId)
        {
            ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
            if (wrapper == null) return null;

            object raw = GetMemberValue(wrapper, "S1ItemDefinition");
            return raw as Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static bool TrySetMember(
            object target, string name, object value)
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

            Type type = target.GetType();

            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (property != null)
                {
                    try { return property.GetValue(target); } catch { }
                }

                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    try { return field.GetValue(target); } catch { }
                }

                type = type.BaseType;
            }

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
            try { ConsoleItemAliases.Register(alias, itemId); }
            catch (Exception)
            {

            }
        }
    }
}
