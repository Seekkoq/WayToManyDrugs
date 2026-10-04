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
    public static class CookieIngredients
    {
        public const string ButterscotchChipsId =
            "westvilleconnection:ingredients/butterscotch_chips";

        public const string CannabisFlourId =
            "westvilleconnection:ingredients/cannabis_flour";

        private const float ButterscotchChipsPrice = 30f;
        private const float CannabisFlourPrice = 45f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _butterscotchVisual;
        private static GameObject _cannabisFlourVisual;

        private static Sprite _butterscotchIcon;
        private static Sprite _cannabisFlourIcon;

        public static GameObject GetButterscotchVisual() => _butterscotchVisual;
        public static GameObject GetCannabisFlourVisual() => _cannabisFlourVisual;

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

                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        ButterscotchChipsId,
                        "Butterscotch Chips",
                        "Sweet butterscotch baking chips. " +
                        "Used to bind and flavor infused cookies.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("motoroil")
                    .WithBasicInfo(
                        CannabisFlourId,
                        "Cannabis Flour",
                        "Finely milled cannabis flower, " +
                        "pre-decarbed for baking.",
                        ItemCategory.Ingredient
                    )
                    .WithEffects(Property.Energizing)
                    .Build();

                RegisterAliasSafely("butterscotch", ButterscotchChipsId);
                RegisterAliasSafely("butterscotchchips", ButterscotchChipsId);
                RegisterAliasSafely("chips", ButterscotchChipsId);
                RegisterAliasSafely("cannabisflour", CannabisFlourId);
                RegisterAliasSafely("cookieflour", CannabisFlourId);
                RegisterAliasSafely("flour", CannabisFlourId);

                _butterscotchVisual = CreateButterscotchBag();
                _cannabisFlourVisual = CreateCannabisFlourBag();

                ApplyCustomRepresentations(
                    ButterscotchChipsId,
                    _butterscotchVisual,
                    1.05f,
                    1.20f
                );

                ApplyCustomRepresentations(
                    CannabisFlourId,
                    _cannabisFlourVisual,
                    2.5f,
                    3.0f
                );

                _butterscotchIcon =
                    CreateButterscotchIconSprite("WVC_Butterscotch_Icon_v1");

                _cannabisFlourIcon =
                    CreateCannabisFlourIconSprite("WVC_CannabisFlour_Icon_v1");

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Ingredients] Using fixed 2D icons."
                );

                ApplyIcon(ButterscotchChipsId, _butterscotchIcon);
                ApplyIcon(CannabisFlourId, _cannabisFlourIcon);

                SetIngredientPrice(ButterscotchChipsId, ButterscotchChipsPrice);
                SetIngredientPrice(CannabisFlourId, CannabisFlourPrice);

                MoveSourceOffscreen(_butterscotchVisual);
                MoveSourceOffscreen(_cannabisFlourVisual);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Ingredients] Registration complete. " +
                    "ButterscotchChips=$" + ButterscotchChipsPrice +
                    ", CannabisFlour=$" + CannabisFlourPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error(
                    "[WVC Cookie Ingredients] Registration failed: " + ex
                );
                return false;
            }
        }

        private static GameObject CreateButterscotchBag()
        {
            GameObject root =
                new GameObject("WVC_Custom_ButterscotchChips_Bag");

            AddCylinder(root.transform, "BagBody",
                0.030f, 0.050f,
                new Vector3(0f, 0.025f, 0f),
                new Color(0.92f, 0.68f, 0.15f, 1f), 0.18f);

            AddCylinder(root.transform, "BagBottom",
                0.031f, 0.005f,
                new Vector3(0f, 0.003f, 0f),
                new Color(0.75f, 0.52f, 0.08f, 1f), 0.15f);

            AddCylinder(root.transform, "BagTop",
                0.028f, 0.008f,
                new Vector3(0f, 0.054f, 0f),
                new Color(0.75f, 0.52f, 0.08f, 1f), 0.15f);

            AddCylinder(root.transform, "BagSeal",
                0.022f, 0.005f,
                new Vector3(0f, 0.060f, 0f),
                new Color(0.60f, 0.40f, 0.06f, 1f), 0.20f);

            AddCube(root.transform, "ButterscotchLabel",
                new Vector3(0.042f, 0.030f, 0.002f),
                new Vector3(0f, 0.026f, -0.031f),
                new Color(0.98f, 0.92f, 0.60f, 1f), 0.12f);

            AddCube(root.transform, "ChipA",
                new Vector3(0.008f, 0.008f, 0.003f),
                new Vector3(-0.008f, 0.030f, -0.033f),
                new Color(0.55f, 0.35f, 0.08f, 1f), 0.10f,
                Quaternion.Euler(0f, 0f, 30f));

            AddCube(root.transform, "ChipB",
                new Vector3(0.006f, 0.006f, 0.003f),
                new Vector3(0.010f, 0.022f, -0.033f),
                new Color(0.55f, 0.35f, 0.08f, 1f), 0.10f,
                Quaternion.Euler(0f, 0f, -20f));

            return root;
        }

        private static GameObject CreateCannabisFlourBag()
        {
            GameObject root =
                new GameObject("WVC_Custom_CannabisFlour_Bag");

            Color flourWhite = new Color(0.88f, 0.92f, 0.82f, 1f);
            Color flourGreen = new Color(0.46f, 0.62f, 0.36f, 1f);
            Color flourDark = new Color(0.30f, 0.42f, 0.22f, 1f);
            Color label = new Color(0.96f, 0.98f, 0.90f, 1f);

            AddCube(root.transform, "BagBody",
                new Vector3(0.058f, 0.055f, 0.032f),
                new Vector3(0f, 0.028f, 0f),
                flourWhite, 0.08f);

            AddCube(root.transform, "BagTopSeam",
                new Vector3(0.060f, 0.010f, 0.034f),
                new Vector3(0f, 0.060f, 0f),
                flourGreen, 0.10f);

            AddCube(root.transform, "BagBottomSeam",
                new Vector3(0.060f, 0.008f, 0.034f),
                new Vector3(0f, 0.004f, 0f),
                flourGreen, 0.10f);

            AddCube(root.transform, "GreenLabelBand",
                new Vector3(0.050f, 0.020f, 0.0025f),
                new Vector3(0f, 0.036f, -0.0165f),
                flourGreen, 0.12f);

            AddCube(root.transform, "LabelPanel",
                new Vector3(0.038f, 0.012f, 0.003f),
                new Vector3(0f, 0.036f, -0.0180f),
                label, 0.10f);

            AddCube(root.transform, "FleckA",
                new Vector3(0.005f, 0.005f, 0.003f),
                new Vector3(-0.006f, 0.028f, -0.0170f),
                flourDark, 0.08f);

            AddCube(root.transform, "FleckB",
                new Vector3(0.004f, 0.004f, 0.003f),
                new Vector3(0.008f, 0.020f, -0.0170f),
                flourDark, 0.08f);

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

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Ingredients] Representations for " +
                    itemId +
                    ": Equippable=" + equippableApplied +
                    ", StationItem=" + stationApplied +
                    ", StoredItem=" + storedApplied
                );
            }
            catch (Exception)
            {

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
                    BuildRepresentationClone(
                        definition.Equippable.gameObject,
                        itemId, "Equippable", customModel, visualScale
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
                    BuildRepresentationClone(
                        storable.StationItem.gameObject,
                        itemId, "StationItem", customModel, visualScale
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
                    BuildRepresentationClone(
                        storable.StoredItem.gameObject,
                        itemId, "StoredItem", customModel, visualScale
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

        private static GameObject BuildRepresentationClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 visualScale)
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone =
                UnityEngine.Object.Instantiate(template);

            string safeId =
                itemId.Replace(":", "_").Replace("/", "_");

            clone.name = "WVC_" + context + "_" + safeId;
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            clone.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
                if (renderer != null)
                    renderer.enabled = false;

            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = visualScale;
            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
                if (renderer != null)
                    renderer.enabled = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cookie Ingredients] Built " + context +
                " representation for " + itemId +
                " with " + customRenderers.Length +
                " custom renderers."
            );

            return clone;
        }

        private static void ApplyIcon(string itemId, Sprite icon)
        {
            if (icon == null)
            {

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

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Ingredients] Icon applied for " + itemId +
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
                        try { field.SetValue(target, icon); applied = true; }
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

        private static Sprite CreateButterscotchIconSprite(string iconName)
        {
            Texture2D texture = CreateIconTexture(iconName + "_Texture");

            DrawIconEllipse(texture, 256, 78, 100, 24,
                new Color(0f, 0f, 0f, 0.22f));

            DrawIconRoundedRect(texture, 156, 100, 200, 260, 40,
                new Color(0.92f, 0.68f, 0.15f, 1f));

            DrawIconRect(texture, 156, 100, 200, 30,
                new Color(0.72f, 0.50f, 0.08f, 1f));

            DrawIconRect(texture, 156, 330, 200, 30,
                new Color(0.72f, 0.50f, 0.08f, 1f));

            DrawIconRoundedRect(texture, 176, 160, 160, 120, 20,
                new Color(0.98f, 0.93f, 0.62f, 1f));

            DrawIconEllipse(texture, 220, 210, 12, 8,
                new Color(0.55f, 0.35f, 0.08f, 1f));
            DrawIconEllipse(texture, 260, 200, 10, 7,
                new Color(0.55f, 0.35f, 0.08f, 1f));
            DrawIconEllipse(texture, 296, 215, 11, 8,
                new Color(0.55f, 0.35f, 0.08f, 1f));
            DrawIconEllipse(texture, 240, 235, 9, 6,
                new Color(0.55f, 0.35f, 0.08f, 1f));

            DrawTextCentered(texture, "BUTTER", 268, 5,
                new Color(0.30f, 0.18f, 0.04f, 1f));
            DrawTextCentered(texture, "SCOTCH", 228, 5,
                new Color(0.30f, 0.18f, 0.04f, 1f));

            texture.Apply();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cookie Ingredients] Created Butterscotch Chips icon."
            );

            return CreateSpriteFromIconTexture(texture, iconName);
        }

        private static Sprite CreateCannabisFlourIconSprite(string iconName)
        {
            Texture2D texture = CreateIconTexture(iconName + "_Texture");

            DrawIconEllipse(texture, 256, 78, 108, 24,
                new Color(0f, 0f, 0f, 0.22f));

            DrawIconRoundedRect(texture, 146, 95, 220, 280, 36,
                new Color(0.88f, 0.93f, 0.82f, 1f));

            DrawIconRect(texture, 146, 355, 220, 20,
                new Color(0.42f, 0.60f, 0.28f, 1f));
            DrawIconEllipse(texture, 256, 375, 110, 22,
                new Color(0.48f, 0.66f, 0.32f, 1f));

            DrawIconRect(texture, 146, 95, 220, 22,
                new Color(0.42f, 0.60f, 0.28f, 1f));
            DrawIconEllipse(texture, 256, 95, 110, 22,
                new Color(0.38f, 0.55f, 0.26f, 1f));

            DrawIconRect(texture, 150, 220, 212, 80,
                new Color(0.42f, 0.60f, 0.28f, 1f));

            DrawIconRect(texture, 162, 228, 188, 64,
                new Color(0.92f, 0.98f, 0.86f, 1f));

            DrawTextCentered(texture, "CANNABIS", 282, 4,
                new Color(0.22f, 0.36f, 0.12f, 1f));
            DrawTextCentered(texture, "FLOUR", 242, 5,
                new Color(0.22f, 0.36f, 0.12f, 1f));

            texture.Apply();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cookie Ingredients] Created Cannabis Flour icon."
            );

            return CreateSpriteFromIconTexture(texture, iconName);
        }

        private static Texture2D CreateIconTexture(string textureName)
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
            Texture2D texture, string spriteName)
        {
            UnityEngine.Object.DontDestroyOnLoad(texture);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            sprite.name = spriteName;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        private static void DrawIconRect(
            Texture2D texture, int x, int y, int width, int height, Color color)
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
            DrawIconRect(texture, x + radius, y, width - radius * 2, height, color);
            DrawIconRect(texture, x, y + radius, width, height - radius * 2, color);
            DrawIconEllipse(texture, x + radius, y + radius, radius, radius, color);
            DrawIconEllipse(texture, x + width - radius, y + radius, radius, radius, color);
            DrawIconEllipse(texture, x + radius, y + height - radius, radius, radius, color);
            DrawIconEllipse(texture, x + width - radius, y + height - radius, radius, radius, color);
        }

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
                default:  return new[] { "11111", "10001", "00010", "00100", "00100", "00000", "00100" };
            }
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

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial =
                    CreateMaterial(name + "_Mat", color, smoothness);

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
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cookie Ingredients] Price set: " +
                    itemId + " = $" + price);
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

        private static object GetMemberValue(object target, string name)
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
