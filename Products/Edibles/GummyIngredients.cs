using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Properties;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    public static class GummyIngredients
    {
        public const string ThcOilId =
            "westvilleconnection:ingredients/thc_oil";

        public const string GelatinId =
            "westvilleconnection:ingredients/gelatin";

        private const float ThcOilPrice = 50f;
        private const float GelatinPrice = 25f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _thcOilVisual;
        private static GameObject _gelatinVisual;

        private static Sprite _thcOilIcon;
        private static Sprite _gelatinIcon;

        public static GameObject GetThcOilVisual() => _thcOilVisual;
        public static GameObject GetGelatinVisual() => _gelatinVisual;

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
                        "[WVC Gummy Ingredients] Base templates not ready."
                    );
                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom("motoroil")
                    .WithBasicInfo(
                        ThcOilId,
                        "THC Oil",
                        "A potent cannabis extract used to infuse edibles.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(Property.AntiGravity)
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        GelatinId,
                        "Gelatin",
                        "Food-grade gelatin powder for binding and chew.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("thcoil", ThcOilId);
                RegisterAliasSafely("thc_oil", ThcOilId);
                RegisterAliasSafely("gelatin", GelatinId);

                _thcOilVisual = CreateThcOilBottle();
                _gelatinVisual = CreateGelatinJar();

                ApplyCustomRepresentations(ThcOilId, _thcOilVisual);
                ApplyCustomRepresentations(GelatinId, _gelatinVisual);

                _thcOilIcon =
                    CreateThcOilIconSprite("WVC_ThcOil_Icon_v4");

                _gelatinIcon =
                    CreateGelatinIconSprite("WVC_Gelatin_Icon_v4");

                MelonLogger.Msg(
                    "[WVC Gummy Ingredients] Using fixed 2D icons."
                );

                ApplyIcon(ThcOilId, _thcOilIcon);
                ApplyIcon(GelatinId, _gelatinIcon);

                SetIngredientPrice(ThcOilId, ThcOilPrice);
                SetIngredientPrice(GelatinId, GelatinPrice);

                MoveSourceOffscreen(_thcOilVisual);
                MoveSourceOffscreen(_gelatinVisual);

                _registered = true;

                MelonLogger.Msg(
                    "[WVC Gummy Ingredients] Registration complete. " +
                    "THC Oil=$" + ThcOilPrice +
                    ", Gelatin=$" + GelatinPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error(
                    "[WVC Gummy Ingredients] Registration failed: " + ex
                );
                return false;
            }
        }

        // ============================================================
        // Representations
        // ============================================================

        private static void ApplyCustomRepresentations(
            string itemId,
            GameObject customModel)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                {
                    MelonLogger.Warning(
                        "[WVC Gummy Ingredients] Raw definition not found: " +
                        itemId
                    );
                    return;
                }

                float heldMultiplier =
                    itemId == GelatinId ? 1.20f : 0.95f;

                float worldMultiplier =
                    itemId == GelatinId ? 1.35f : 1.15f;

                Vector3 heldScale = Vector3.one * heldMultiplier;
                Vector3 worldScale = Vector3.one * worldMultiplier;

                bool equippableApplied =
                    ApplyEquippableRepresentation(
                        definition, itemId, customModel, heldScale
                    );

                bool stationApplied =
                    ApplyStationRepresentation(
                        definition, itemId, customModel, worldScale
                    );

                bool storedApplied =
                    ApplyStoredRepresentation(
                        definition, itemId, customModel, worldScale
                    );

                MelonLogger.Msg(
                    "[WVC Gummy Ingredients] Representations for " +
                    itemId +
                    ": Equippable=" + equippableApplied +
                    ", StationItem=" + stationApplied +
                    ", StoredItem=" + storedApplied
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Gummy Ingredients] Representation setup failed for " +
                    itemId + ": " + ex
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
                    return false;

                GameObject clone =
                    BuildNativeRepresentationClone(
                        definition.Equippable.gameObject,
                        itemId, "Equippable", customModel,
                        Vector3.zero, Quaternion.identity, visualScale
                    );

                if (clone == null) return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Equipping.Equippable
                    >();

                if (component == null) return false;

                definition.Equippable = component;
                return true;
            }
            catch { return false; }
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
                    return false;

                GameObject clone =
                    BuildNativeRepresentationClone(
                        storable.StationItem.gameObject,
                        itemId, "StationItem", customModel,
                        Vector3.zero, Quaternion.identity, visualScale
                    );

                if (clone == null) return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.StationFramework.StationItem
                    >();

                if (component == null) return false;

                storable.StationItem = component;
                return true;
            }
            catch { return false; }
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
                    return false;

                GameObject clone =
                    BuildNativeRepresentationClone(
                        storable.StoredItem.gameObject,
                        itemId, "StoredItem", customModel,
                        Vector3.zero, Quaternion.identity, visualScale
                    );

                if (clone == null) return false;

                var component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Storage.StoredItem
                    >();

                if (component == null) return false;

                storable.StoredItem = component;
                return true;
            }
            catch { return false; }
        }

        private static GameObject BuildNativeRepresentationClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone =
                UnityEngine.Object.Instantiate(template);

            string safeId =
                itemId.Replace(":", "_").Replace("/", "_");

            clone.name = "WVC_" + context + "_" + safeId;
            clone.SetActive(true);
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;
            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            MelonLogger.Msg(
                "[WVC Gummy Ingredients] Built " + context +
                " representation for " + itemId +
                " with " + customRenderers.Length +
                " custom renderers."
            );

            return clone;
        }

        // ============================================================
        // Icons
        // ============================================================

        private static void ApplyIcon(
            string itemId,
            Sprite icon)
        {
            if (icon == null)
            {
                MelonLogger.Warning(
                    "[WVC Gummy Ingredients] Cannot apply null icon for " +
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
                    TrySetMember(wrapper, "Icon", icon);

                bool rawDirect =
                    TrySetMember(raw, "Icon", icon);

                bool wrapperAny =
                    TrySetAnyIconSpriteMember(wrapper, icon);

                bool rawAny =
                    TrySetAnyIconSpriteMember(raw, icon);

                MelonLogger.Msg(
                    "[WVC Gummy Ingredients] Icon applied for " +
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
                    "[WVC Gummy Ingredients] Icon setup failed for " +
                    itemId + ": " + ex.Message
                );
            }
        }

        private static bool TrySetAnyIconSpriteMember(
            object target,
            Sprite icon)
        {
            if (target == null || icon == null)
                return false;

            bool applied = false;

            try
            {
                Type type = target.GetType();

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
                        if (!typeof(Sprite).IsAssignableFrom(field.FieldType))
                            continue;

                        if (!field.Name.ToLowerInvariant().Contains("icon"))
                            continue;

                        try
                        {
                            field.SetValue(target, icon);
                            applied = true;
                        }
                        catch { }
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

                        if (!typeof(Sprite).IsAssignableFrom(property.PropertyType))
                            continue;

                        if (!property.Name.ToLowerInvariant().Contains("icon"))
                            continue;

                        try
                        {
                            property.SetValue(target, icon);
                            applied = true;
                        }
                        catch { }
                    }

                    type = type.BaseType;
                }
            }
            catch { }

            return applied;
        }

        // ============================================================
        // Fixed 2D icon sprites
        // ============================================================

        private static Sprite CreateThcOilIconSprite(
            string iconName)
        {
            Texture2D texture =
                CreateIconTexture(iconName + "_Texture");

            DrawIconEllipse(texture, 256, 78, 88, 22,
                new Color(0f, 0f, 0f, 0.25f));

            DrawIconRoundedRect(texture, 186, 112, 140, 245, 38,
                new Color(0.02f, 0.19f, 0.07f, 1f));

            DrawIconRect(texture, 210, 150, 24, 175,
                new Color(0.10f, 0.42f, 0.17f, 0.45f));

            DrawIconRect(texture, 226, 344, 60, 62,
                new Color(0.015f, 0.14f, 0.045f, 1f));

            DrawIconRect(texture, 198, 392, 116, 34,
                new Color(0.84f, 0.86f, 0.88f, 1f));

            DrawIconEllipse(texture, 256, 426, 58, 20,
                new Color(0.96f, 0.97f, 0.98f, 1f));

            DrawIconEllipse(texture, 256, 392, 58, 18,
                new Color(0.70f, 0.72f, 0.75f, 1f));

            DrawIconRect(texture, 195, 178, 122, 84,
                new Color(0.02f, 0.06f, 0.03f, 1f));

            DrawTextCentered(texture, "THC", 234, 6,
                new Color(0.85f, 1.0f, 0.60f, 1f));

            DrawTextCentered(texture, "OIL", 194, 6,
                new Color(0.85f, 1.0f, 0.60f, 1f));

            texture.Apply();

            MelonLogger.Msg(
                "[WVC Gummy Ingredients] Created fixed THC Oil icon."
            );

            return CreateSpriteFromIconTexture(texture, iconName);
        }

        private static Sprite CreateGelatinIconSprite(
            string iconName)
        {
            Texture2D texture =
                CreateIconTexture(iconName + "_Texture");

            DrawIconEllipse(texture, 256, 78, 112, 24,
                new Color(0f, 0f, 0f, 0.25f));

            DrawIconRect(texture, 145, 140, 222, 220,
                new Color(0.86f, 0.86f, 0.82f, 1f));

            DrawIconEllipse(texture, 256, 140, 111, 34,
                new Color(0.70f, 0.70f, 0.66f, 1f));

            DrawIconEllipse(texture, 256, 360, 111, 34,
                new Color(0.94f, 0.93f, 0.89f, 1f));

            // RED LID
            DrawIconRect(texture, 130, 355, 252, 44,
                new Color(0.58f, 0.015f, 0.01f, 1f));

            DrawIconEllipse(texture, 256, 399, 126, 38,
                new Color(0.78f, 0.025f, 0.015f, 1f));

            DrawIconEllipse(texture, 256, 355, 126, 30,
                new Color(0.40f, 0.005f, 0.005f, 1f));

            // Red label
            DrawIconRect(texture, 150, 188, 212, 96,
                new Color(0.68f, 0.045f, 0.035f, 1f));

            DrawTextCentered(texture, "GELATIN", 252, 5, Color.white);
            DrawTextCentered(texture, "POWDER", 212, 5, Color.white);

            texture.Apply();

            MelonLogger.Msg(
                "[WVC Gummy Ingredients] Created fixed Gelatin icon with red cap."
            );

            return CreateSpriteFromIconTexture(texture, iconName);
        }

        private static Texture2D CreateIconTexture(
            string textureName)
        {
            Texture2D texture =
                new Texture2D(512, 512, TextureFormat.RGBA32, false);

            texture.name = textureName;

            Color[] pixels = new Color[512 * 512];
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            texture.SetPixels(pixels);
            return texture;
        }

        private static Sprite CreateSpriteFromIconTexture(
            Texture2D texture,
            string spriteName)
        {
            UnityEngine.Object.DontDestroyOnLoad(texture);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

            sprite.name = spriteName;
            UnityEngine.Object.DontDestroyOnLoad(sprite);

            return sprite;
        }

        private static void DrawIconRect(
            Texture2D texture,
            int x, int y, int width, int height,
            Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
                    if (px < 0 || py < 0 ||
                        px >= texture.width || py >= texture.height)
                        continue;

                    texture.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawIconEllipse(
            Texture2D texture,
            int centerX, int centerY,
            int radiusX, int radiusY,
            Color color)
        {
            float rx2 = radiusX * radiusX;
            float ry2 = radiusY * radiusY;

            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                {
                    if (x < 0 || y < 0 ||
                        x >= texture.width || y >= texture.height)
                        continue;

                    float dx = x - centerX;
                    float dy = y - centerY;

                    if ((dx * dx) / rx2 + (dy * dy) / ry2 <= 1f)
                        texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawIconRoundedRect(
            Texture2D texture,
            int x, int y, int width, int height, int radius,
            Color color)
        {
            DrawIconRect(texture, x + radius, y,
                width - radius * 2, height, color);

            DrawIconRect(texture, x, y + radius,
                width, height - radius * 2, color);

            DrawIconEllipse(texture, x + radius, y + radius,
                radius, radius, color);

            DrawIconEllipse(texture, x + width - radius, y + radius,
                radius, radius, color);

            DrawIconEllipse(texture, x + radius, y + height - radius,
                radius, radius, color);

            DrawIconEllipse(texture, x + width - radius, y + height - radius,
                radius, radius, color);
        }

        // ============================================================
        // 3D models (kept for held/world/station visuals)
        // ============================================================

        private static GameObject CreateThcOilBottle()
        {
            GameObject root =
                new GameObject("WVC_Custom_ThcOil_Bottle");

            AddCylinder(root.transform, "GreenBottleBody",
                0.027f, 0.078f, new Vector3(0f, 0.039f, 0f),
                new Color(0.08f, 0.28f, 0.12f, 1f), 0.55f);

            AddCylinder(root.transform, "GreenBottleShoulder",
                0.021f, 0.015f, new Vector3(0f, 0.085f, 0f),
                new Color(0.10f, 0.34f, 0.15f, 1f), 0.55f);

            AddCylinder(root.transform, "BottleNeck",
                0.012f, 0.020f, new Vector3(0f, 0.102f, 0f),
                new Color(0.07f, 0.24f, 0.10f, 1f), 0.55f);

            AddCylinder(root.transform, "DropperCap",
                0.015f, 0.016f, new Vector3(0f, 0.119f, 0f),
                new Color(0.95f, 0.95f, 0.95f, 1f), 0.20f);

            AddLabelCube(root.transform, "ThcOilLabel",
                new Vector3(0.048f, 0.038f, 0.002f),
                new Vector3(0f, 0.040f, -0.0285f),
                "THC", "OIL",
                new Color(0.12f, 0.18f, 0.10f, 1f),
                new Color(0.85f, 0.95f, 0.70f, 1f));

            return root;
        }

        private static GameObject CreateGelatinJar()
        {
            GameObject root =
                new GameObject("WVC_Custom_Gelatin_Jar");

            AddCylinder(root.transform, "JarBody",
                0.038f, 0.055f, new Vector3(0f, 0.0275f, 0f),
                new Color(0.95f, 0.92f, 0.86f, 1f), 0.18f);

            AddCylinder(root.transform, "JarInterior",
                0.032f, 0.006f, new Vector3(0f, 0.055f, 0f),
                new Color(0.20f, 0.18f, 0.15f, 1f), 0.05f);

            AddCylinder(root.transform, "PowderSurface",
                0.031f, 0.003f, new Vector3(0f, 0.0575f, 0f),
                new Color(0.98f, 0.96f, 0.90f, 1f), 0.05f);

            AddCylinder(root.transform, "Lid",
                0.039f, 0.012f, new Vector3(0f, 0.064f, 0f),
                new Color(0.75f, 0.18f, 0.12f, 1f), 0.25f);

            AddLabelCube(root.transform, "GelatinLabel",
                new Vector3(0.066f, 0.030f, 0.002f),
                new Vector3(0f, 0.028f, -0.0415f),
                "GELATIN", "POWDER",
                new Color(0.78f, 0.20f, 0.14f, 1f),
                Color.white);

            return root;
        }

        private static GameObject AddCylinder(
            Transform parent, string name,
            float radius, float height,
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

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial =
                    CreateMaterial(name + "_Mat", color, smoothness);

            RemovePrimitiveCollider(part);
            return part;
        }

        private static GameObject AddLabelCube(
            Transform parent, string name,
            Vector3 size, Vector3 localPosition,
            string line1, string line2,
            Color background, Color textColor)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Quad);

            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = new Vector3(size.x, size.y, 1f);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial =
                    CreateLabelMaterial(name + "_Mat",
                        background, textColor, line1, line2);

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

            Material mat = new Material(shader);
            mat.name = name;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            return mat;
        }

        private static Material CreateLabelMaterial(
            string name, Color background, Color textColor,
            string line1, string line2)
        {
            Texture2D texture =
                new Texture2D(256, 128, TextureFormat.RGBA32, false);
            texture.name = name + "_Tex";

            Color[] pixels = new Color[256 * 128];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;
            texture.SetPixels(pixels);

            DrawTextCentered(texture, line1, 84, 5, textColor);
            DrawTextCentered(texture, line2, 40, 5, textColor);

            texture.Apply();
            UnityEngine.Object.DontDestroyOnLoad(texture);

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = name;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.15f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.15f);

            return mat;
        }

        // ============================================================
        // Text rendering
        // ============================================================

        private static void DrawTextCentered(
            Texture2D texture, string text,
            int centerY, int scale, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;

            int charWidth = 5 * scale;
            int spacing = scale;
            int totalWidth =
                text.Length * charWidth + (text.Length - 1) * spacing;
            int startX = (texture.width - totalWidth) / 2;
            int startY = centerY - (7 * scale) / 2;

            for (int i = 0; i < text.Length; i++)
            {
                DrawChar(texture, char.ToUpperInvariant(text[i]),
                    startX + i * (charWidth + spacing),
                    startY, scale, color);
            }
        }

        private static void DrawChar(
            Texture2D texture, char character,
            int x, int y, int scale, Color color)
        {
            string[] glyph = GetGlyph(character);
            if (glyph == null) return;

            for (int row = 0; row < glyph.Length; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '1') continue;
                    for (int sx = 0; sx < scale; sx++)
                    {
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int px = x + col * scale + sx;
                            int py = y + (glyph.Length - 1 - row) * scale + sy;
                            if (px >= 0 && py >= 0 &&
                                px < texture.width && py < texture.height)
                                texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }
        private static string[] GetGlyph(char c)
        {
            switch (c)
            {
                case 'A': return new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'B': return new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" };
                case 'C': return new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" };
                case 'D': return new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" };
                case 'E': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
                case 'F': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" };
                case 'G': return new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01110" };
                case 'H': return new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'I': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" };
                case 'J': return new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" };
                case 'K': return new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" };
                case 'L': return new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" };
                case 'M': return new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" };
                case 'N': return new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" };
                case 'O': return new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'P': return new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" };
                case 'Q': return new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" };
                case 'R': return new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
                case 'S': return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
                case 'T': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" };
                case 'U': return new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'V': return new[] { "10001", "10001", "10001", "10001", "01010", "01010", "00100" };
                case 'W': return new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" };
                case 'X': return new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" };
                case 'Y': return new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
                case 'Z': return new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" };
                case '0': return new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" };
                case '1': return new[] { "00100", "01100", "00100", "00100", "00100", "00100", "11111" };
                case '2': return new[] { "01110", "10001", "00001", "00110", "01000", "10000", "11111" };
                case '3': return new[] { "01110", "10001", "00001", "00110", "00001", "10001", "01110" };
                case '4': return new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" };
                case '5': return new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" };
                case '6': return new[] { "01110", "10000", "10000", "11110", "10001", "10001", "01110" };
                case '7': return new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" };
                case '8': return new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" };
                case '9': return new[] { "01110", "10001", "10001", "01111", "00001", "00001", "01110" };
                case ' ': return new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" };
                default: return new[] { "11111", "10001", "00010", "00100", "00100", "00000", "00100" };
            }
        }

        // ============================================================
        // Utility
        // ============================================================

        private static void RemovePrimitiveCollider(GameObject go)
        {
            if (go == null) return;
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c != null && c.GetType().Name.EndsWith("Collider"))
                {
                    try { UnityEngine.Object.Destroy(c); } catch { }
                }
            }
        }

        private static void MoveSourceOffscreen(GameObject source)
        {
            if (source == null) return;
            source.transform.position = new Vector3(0f, -20000f, 0f);
            source.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(source);
        }

        private static void SetIngredientPrice(string itemId, float price)
        {
            try
            {
                var def = GetRawDefinition(itemId);
                if (def == null) return;

                var storable = def.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();
                if (storable == null) return;

                storable.BasePurchasePrice = price;
                MelonLogger.Msg(
                    "[WVC Gummy Ingredients] Price set: " +
                    itemId + " = $" + price);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Gummy Ingredients] Price failed for " +
                    itemId + ": " + ex.Message);
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

        private static bool TrySetMember(
            object target, string name, object value)
        {
            if (target == null) return false;

            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null && value != null &&
                    field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    try { field.SetValue(target, value); return true; }
                    catch { }
                }

                PropertyInfo prop = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (prop != null && prop.CanWrite && value != null &&
                    prop.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try { prop.SetValue(target, value); return true; }
                    catch { }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetMemberValue(
            object target, string name)
        {
            if (target == null) return null;

            try
            {
                Type type = target.GetType();
                while (type != null)
                {
                    PropertyInfo prop = type.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (prop != null)
                    {
                        try { return prop.GetValue(target); } catch { }
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
            }
            catch { }

            return null;
        }

        private static void RegisterAliasSafely(
            string alias, string itemId)
        {
            try
            {
                ConsoleItemAliases.Register(alias, itemId);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Gummy Ingredients] Alias '" + alias +
                    "' failed: " + ex.Message);
            }
        }
    }
}