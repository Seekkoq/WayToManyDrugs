using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Properties;
using UnityEngine;
using S1API.Shops;

namespace CustomNPCExample.Products.Edibles
{
    public static class Sugar
    {
        public const string SugarId =
            "westvilleconnection:ingredients/sugar";

        private const float SugarPrice = 12f;

        private static bool _registered;
        private static bool _failed;
        private static bool _warnedDefinitionNotReady;

        private static GameObject _sugarVisual;
        private static Sprite _sugarIcon;

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
                        SugarId,
                        "Infused Sugar",
                        "Green cannabis-infused sugar used for making gummies.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("sugar", SugarId);
                RegisterAliasSafely("infusedsugar", SugarId);
                RegisterAliasSafely("infused_sugar", SugarId);

                _sugarVisual = CreateSugarBag();

                ApplyCustomRepresentations(SugarId, _sugarVisual);

                _sugarIcon = CreateSugarIconSprite("WVC_InfusedSugar_Icon");

                ApplyIcon(SugarId, _sugarIcon);
                SetIngredientPrice(SugarId, SugarPrice);

                MoveSourceOffscreen(_sugarVisual);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Sugar] Registration complete. Infused Sugar = $" +
                    SugarPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error(
                    "[WVC Sugar] Registration failed: " + ex
                );
                return false;
            }
        }

        public static bool TryAddToGasMarts()
        {
            if (!_registered)
                return false;

            try
            {
                ItemDefinition sugarDefinition =
                    ItemManager.GetDefinition(SugarId);

                if (sugarDefinition == null)
                {
                    if (!_warnedDefinitionNotReady)
                    {
                        _warnedDefinitionNotReady = true;

                    }

                    return false;
                }

                int added =
                    ShopManager.AddToShops(
                        sugarDefinition,
                        SugarPrice,
                        "Gas-Mart (West)",
                        "Gas-Mart (Central)"
                    );

                bool westHasSugar =
                    ShopManager
                        .GetShopByName("Gas-Mart (West)")
                        ?.HasItem(SugarId)
                    ?? false;

                bool centralHasSugar =
                    ShopManager
                        .GetShopByName("Gas-Mart (Central)")
                        ?.HasItem(SugarId)
                    ?? false;

                bool completed =
                    westHasSugar &&
                    centralHasSugar;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Sugar] Gas-Mart injection: added=" +
                    added +
                    ", west=" +
                    westHasSugar +
                    ", central=" +
                    centralHasSugar
                );

                return completed;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static void ApplyCustomRepresentations(
            string itemId,
            GameObject customModel)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(itemId);

                if (definition == null)
                    return;

                Vector3 heldScale = Vector3.one * 1.05f;
                Vector3 worldScale = Vector3.one * 1.20f;

                ApplyEquippableRepresentation(
                    definition, itemId, customModel, heldScale);

                ApplyStationRepresentation(
                    definition, itemId, customModel, worldScale);

                ApplyStoredRepresentation(
                    definition, itemId, customModel, worldScale);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Sugar] Representations applied for " + itemId
                );
            }
            catch (Exception)
            {

            }
        }

        private static void DrawIconRect(
    Texture2D texture,
    int x,
    int y,
    int width,
    int height,
    Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
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

        private static void DrawIconEllipse(
            Texture2D texture,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            Color color)
        {
            float radiusXSquared =
                radiusX * radiusX;

            float radiusYSquared =
                radiusY * radiusY;

            for (int x = centerX - radiusX;
                 x <= centerX + radiusX;
                 x++)
            {
                for (int y = centerY - radiusY;
                     y <= centerY + radiusY;
                     y++)
                {
                    if (x < 0 ||
                        y < 0 ||
                        x >= texture.width ||
                        y >= texture.height)
                    {
                        continue;
                    }

                    float deltaX =
                        x - centerX;

                    float deltaY =
                        y - centerY;

                    if ((deltaX * deltaX) / radiusXSquared +
                        (deltaY * deltaY) / radiusYSquared <= 1f)
                    {
                        texture.SetPixel(
                            x,
                            y,
                            color
                        );
                    }
                }
            }
        }

        private static bool ApplyEquippableRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 scale)
        {
            try
            {
                if (definition.Equippable == null ||
                    definition.Equippable.gameObject == null)
                    return false;

                GameObject clone = BuildClone(
                    definition.Equippable.gameObject,
                    itemId, "Equippable", customModel, scale);

                if (clone == null) return false;

                var comp = clone.GetComponent<
                    Il2CppScheduleOne.Equipping.Equippable>();

                if (comp == null) return false;

                definition.Equippable = comp;
                return true;
            }
            catch { return false; }
        }

        private static bool ApplyStationRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 scale)
        {
            try
            {
                var storable = definition.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null ||
                    storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                    return false;

                GameObject clone = BuildClone(
                    storable.StationItem.gameObject,
                    itemId, "StationItem", customModel, scale);

                if (clone == null) return false;

                var comp = clone.GetComponent<
                    Il2CppScheduleOne.StationFramework.StationItem>();

                if (comp == null) return false;

                storable.StationItem = comp;
                return true;
            }
            catch { return false; }
        }

        private static bool ApplyStoredRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 scale)
        {
            try
            {
                var storable = definition.TryCast<
                    Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null ||
                    storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                    return false;

                GameObject clone = BuildClone(
                    storable.StoredItem.gameObject,
                    itemId, "StoredItem", customModel, scale);

                if (clone == null) return false;

                var comp = clone.GetComponent<
                    Il2CppScheduleOne.Storage.StoredItem>();

                if (comp == null) return false;

                storable.StoredItem = comp;
                return true;
            }
            catch { return false; }
        }

        private static GameObject BuildClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 scale)
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone =
                UnityEngine.Object.Instantiate(template);

            string safeId = itemId.Replace(":", "_").Replace("/", "_");
            clone.name = "WVC_" + context + "_" + safeId;
            clone.SetActive(true);
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.enabled = false;
            }

            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = scale;
            visual.SetActive(true);

            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.enabled = true;
            }

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

                bool w = TrySetMember(wrapper, "Icon", icon);
                bool r = TrySetMember(raw, "Icon", icon);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Sugar] Icon applied for " + itemId +
                    ": wrapper=" + w + ", raw=" + r
                );
            }
            catch (Exception)
            {

            }
        }

        private static Sprite CreateSugarIconSprite(string iconName)
        {
            Texture2D texture = CreateIconTexture(iconName + "_Texture");

            DrawIconEllipse(texture, 256, 78, 112, 24,
                new Color(0f, 0f, 0f, 0.25f));

            DrawIconRect(texture, 145, 140, 222, 220,
                new Color(0.08f, 0.38f, 0.16f, 1f));

            DrawIconRect(texture, 145, 350, 222, 35,
                new Color(0.04f, 0.22f, 0.09f, 1f));

            DrawIconRect(texture, 160, 185, 192, 95,
                new Color(0.42f, 0.78f, 0.25f, 1f));

            DrawTextCentered(texture, "INFUSED", 255, 5, Color.white);
            DrawTextCentered(texture, "SUGAR", 210, 6, Color.white);

            texture.Apply();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Sugar] Created fixed Infused Sugar icon with green box and red cap area."
            );

            return CreateSpriteFromIconTexture(texture, iconName);
        }

        private static GameObject CreateSugarBag()
        {
            GameObject root =
                new GameObject("WVC_Custom_InfusedSugar_Box");

            AddCube(root.transform, "BoxBody",
                new Vector3(0.055f, 0.085f, 0.030f),
                new Vector3(0f, 0.0425f, 0f),
                new Color(0.08f, 0.38f, 0.16f, 1f),
                0.18f);

            AddCube(root.transform, "BoxTop",
                new Vector3(0.056f, 0.014f, 0.031f),
                new Vector3(0f, 0.090f, 0f),
                new Color(0.04f, 0.22f, 0.09f, 1f),
                0.12f);

            AddLabelCube(root.transform, "InfusedSugarLabel",
                new Vector3(0.048f, 0.038f, 0.002f),
                new Vector3(0f, 0.045f, -0.016f),
                "INFUSED",
                "SUGAR",
                new Color(0.42f, 0.78f, 0.25f, 1f),
                Color.white);

            return root;
        }

        private static GameObject AddCube(
            Transform parent, string name,
            Vector3 size, Vector3 localPosition,
            Color color, float smoothness)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = size;

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

            return mat;
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
                default: return new[] { "11111", "10001", "00010", "00100", "00100", "00000", "00100" };
            }
        }

        private static void RemovePrimitiveCollider(GameObject go)
        {
            if (go == null) return;
            foreach (Component c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name.EndsWith("Collider"))
                    try { UnityEngine.Object.Destroy(c); } catch { }
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
                    "[WVC Sugar] Price set: " + itemId + " = $" + price);
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
                        try { return prop.GetValue(target); } catch { }

                    FieldInfo field = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field != null)
                        try { return field.GetValue(target); } catch { }

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }

        private static void RegisterAliasSafely(
            string alias, string itemId)
        {
            try { ConsoleItemAliases.Register(alias, itemId); }
            catch (Exception)
            {

            }
        }

        private static Texture2D CreateIconTexture(string name)
        {
            Texture2D texture =
                new Texture2D(512, 512, TextureFormat.RGBA32, false);
            texture.name = name;

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
                new Vector2(0.5f, 0.5f));

            sprite.name = spriteName;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }
    }
}
