using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class DMTIntermediates
    {
        public const string CrudeExtractId =
            "westvilleconnection:ingredients/crude_dmt_extract";

        public const string PremiumCrudeExtractId =
            "westvilleconnection:ingredients/premium_crude_dmt_extract";

        private const float CrudeExtractPrice = 75f;
        private const float PremiumCrudeExtractPrice = 110f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _crudeExtractVisual;
        private static GameObject _premiumCrudeExtractVisual;

        private static Sprite _crudeExtractIcon;
        private static Sprite _premiumCrudeExtractIcon;

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                if (ItemManager.GetDefinition("iodine") == null)
                {


                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        CrudeExtractId,
                        "Crude DMT Extract",
                        "A pale, waxy botanical extract in a ceramic dish. " +
                        "Dry and crystallize it in the Lab Oven.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                MixIngredientItemCreator
                    .CloneFrom("iodine")
                    .WithBasicInfo(
                        PremiumCrudeExtractId,
                        "Premium Crude DMT Extract",
                        "A purified white botanical extract prepared with " +
                        "crystalizer. Produces premium DMT in the Lab Oven.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("crudedmt", CrudeExtractId);
                RegisterAliasSafely("dmtextract", CrudeExtractId);
                RegisterAliasSafely("extract", CrudeExtractId);

                RegisterAliasSafely("premiumcrudedmt", PremiumCrudeExtractId);
                RegisterAliasSafely("premiumextract", PremiumCrudeExtractId);

                _crudeExtractVisual =
                    CreateCrudeExtractDish(false);

                _premiumCrudeExtractVisual =
                    CreateCrudeExtractDish(true);

                ApplyCustomRepresentations(
                    CrudeExtractId,
                    _crudeExtractVisual,
                    1.25f,
                    1.40f
                );

                ApplyCustomRepresentations(
                    PremiumCrudeExtractId,
                    _premiumCrudeExtractVisual,
                    1.25f,
                    1.40f
                );

                _crudeExtractIcon =
                    CreateCrudeIconSprite(
                        "WVC_CrudeExtract_Icon",
                        false
                    );

                _premiumCrudeExtractIcon =
                    CreateCrudeIconSprite(
                        "WVC_PremiumCrudeExtract_Icon",
                        true
                    );

                ApplyIcon(CrudeExtractId, _crudeExtractIcon);
                ApplyIcon(PremiumCrudeExtractId, _premiumCrudeExtractIcon);

                SetIngredientPrice(CrudeExtractId, CrudeExtractPrice);
                SetIngredientPrice(PremiumCrudeExtractId, PremiumCrudeExtractPrice);

                MoveSourceOffscreen(_crudeExtractVisual);
                MoveSourceOffscreen(_premiumCrudeExtractVisual);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC DMT Intermediates] Crude extracts registered. " +
                    "Standard=$" + CrudeExtractPrice +
                    ", Premium=$" + PremiumCrudeExtractPrice + "."
                );

                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error(
                    "[WVC DMT Intermediates] Registration failed: " + ex
                );

                return false;
            }
        }

        public static bool IsCrudeExtractId(string itemId)
        {
            return string.Equals(
                       itemId,
                       CrudeExtractId,
                       StringComparison.OrdinalIgnoreCase
                   ) ||
                   string.Equals(
                       itemId,
                       PremiumCrudeExtractId,
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        private static GameObject CreateCrudeExtractDish(bool premium)
        {
            GameObject root =
                new GameObject(
                    premium
                        ? "WVC_Custom_PremiumCrudeExtract_Dish"
                        : "WVC_Custom_CrudeExtract_Dish"
                );

            Color ceramicWhite =
                new Color(0.90f, 0.90f, 0.92f, 1f);

            Color ceramicShadow =
                premium
                    ? new Color(0.68f, 0.78f, 0.88f, 1f)
                    : new Color(0.75f, 0.75f, 0.78f, 1f);

            Color extractColor =
                premium
                    ? new Color(1.00f, 1.00f, 0.98f, 1f)
                    : new Color(0.95f, 0.94f, 0.90f, 1f);

            AddCylinder(
                root.transform,
                "DishBody",
                0.045f,
                0.012f,
                new Vector3(0f, 0.006f, 0f),
                ceramicWhite,
                0.40f
            );

            AddCylinder(
                root.transform,
                "DishInner",
                0.041f,
                0.004f,
                new Vector3(0f, 0.013f, 0f),
                ceramicShadow,
                0.30f
            );

            AddCylinder(
                root.transform,
                premium ? "PremiumCrudeWax" : "CrudeWax",
                0.039f,
                0.005f,
                new Vector3(0f, 0.014f, 0f),
                extractColor,
                premium ? 0.30f : 0.15f
            );

            return root;
        }

        private static Sprite CreateCrudeIconSprite(
            string iconName,
            bool premium)
        {
            Texture2D texture =
                CreateIconTexture(iconName + "_Texture");

            Color innerRim =
                premium
                    ? new Color(0.68f, 0.78f, 0.88f, 1f)
                    : new Color(0.75f, 0.75f, 0.78f, 1f);

            Color extractColor =
                premium
                    ? new Color(1.00f, 1.00f, 0.98f, 1f)
                    : new Color(0.95f, 0.94f, 0.90f, 1f);

            DrawIconEllipse(
                texture,
                256,
                120,
                140,
                40,
                new Color(0f, 0f, 0f, 0.25f)
            );

            DrawIconEllipse(
                texture,
                256,
                160,
                160,
                60,
                new Color(0.90f, 0.90f, 0.92f, 1f)
            );

            DrawIconEllipse(
                texture,
                256,
                165,
                145,
                52,
                innerRim
            );

            DrawIconEllipse(
                texture,
                256,
                170,
                135,
                48,
                extractColor
            );

            if (premium)
            {
                DrawIconEllipse(
                    texture,
                    224,
                    187,
                    28,
                    10,
                    new Color(1f, 1f, 1f, 0.85f)
                );
            }

            texture.Apply();

            return CreateSpriteFromIconTexture(texture, iconName);
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
                    return;

                ApplyEquippableRepresentation(
                    definition,
                    itemId,
                    customModel,
                    Vector3.one * heldMultiplier
                );

                ApplyStationRepresentation(
                    definition,
                    itemId,
                    customModel,
                    Vector3.one * worldMultiplier
                );

                ApplyStoredRepresentation(
                    definition,
                    itemId,
                    customModel,
                    Vector3.one * worldMultiplier
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
            Vector3 scale)
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
                        scale
                    );

                if (clone == null)
                    return false;

                var comp =
                    clone.GetComponent<Il2CppScheduleOne.Equipping.Equippable>();

                if (comp == null)
                    return false;

                definition.Equippable = comp;
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
            Vector3 scale)
        {
            try
            {
                var storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

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
                        scale
                    );

                if (clone == null)
                    return false;

                var comp =
                    clone.GetComponent<
                        Il2CppScheduleOne.StationFramework.StationItem>();

                if (comp == null)
                    return false;

                storable.StationItem = comp;
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
            Vector3 scale)
        {
            try
            {
                var storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

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
                        scale
                    );

                if (clone == null)
                    return false;

                var comp =
                    clone.GetComponent<Il2CppScheduleOne.Storage.StoredItem>();

                if (comp == null)
                    return false;

                storable.StoredItem = comp;
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

            // The template brings a drag constraint with it. On a clone that nobody drags it has no
            // container to align to, so it threw a null reference every frame. See WvcCloneSanitizer.
            global::CustomNPCExample.Utils.WvcCloneSanitizer.Strip(clone);
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name = "WVC_CustomVisual_" + safeId + "_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.SetActive(true);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            return clone;
        }

        private static void ApplyIcon(string itemId, Sprite icon)
        {
            if (icon == null)
                return;

            try
            {
                ItemDefinition wrapper =
                    ItemManager.GetDefinition(itemId);

                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    GetRawDefinition(itemId);

                TrySetMember(wrapper, "Icon", icon);
                TrySetMember(raw, "Icon", icon);
            }
            catch (Exception)
            {

            }
        }

        private static void SetIngredientPrice(string itemId, float price)
        {
            try
            {
                var def = GetRawDefinition(itemId);
                if (def == null)
                    return;

                var storable =
                    def.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();

                if (storable == null)
                    return;

                storable.BasePurchasePrice = price;
            }
            catch
            {
            }
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
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale =
                new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial =
                    CreateMaterial(name + "_Mat", color, smoothness);

            RemovePrimitiveCollider(part);
            return part;
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float smoothness)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            Material material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            return material;
        }

        private static Texture2D CreateIconTexture(string textureName)
        {
            Texture2D texture =
                new Texture2D(512, 512, TextureFormat.RGBA32, false)
                {
                    name = textureName
                };

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

        private static void DrawIconEllipse(
            Texture2D texture,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
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
                    {
                        continue;
                    }

                    float dx = x - centerX;
                    float dy = y - centerY;

                    if ((dx * dx) / rx2 + (dy * dy) / ry2 <= 1f)
                        texture.SetPixel(x, y, color);
                }
            }
        }

        private static void RemovePrimitiveCollider(GameObject go)
        {
            if (go == null)
                return;

            foreach (Component component in go.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().Name.EndsWith("Collider"))
                {
                    try { UnityEngine.Object.Destroy(component); }
                    catch { }
                }
            }
        }

        private static void MoveSourceOffscreen(GameObject source)
        {
            if (source == null)
                return;

            source.transform.position = new Vector3(0f, -20000f, 0f);
            source.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(source);
        }

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(string itemId)
        {
            ItemDefinition wrapper =
                ItemManager.GetDefinition(itemId);

            if (wrapper == null)
                return null;

            object raw =
                GetMemberValue(wrapper, "S1ItemDefinition");

            return raw as Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static bool TrySetMember(
            object target,
            string name,
            object value)
        {
            if (target == null || value == null)
                return false;

            Type type = target.GetType();

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
                    field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    try
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                    catch { }
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
                    property.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try
                    {
                        property.SetValue(target, value);
                        return true;
                    }
                    catch { }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

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
                        try { return property.GetValue(target); }
                        catch { }
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
                        try { return field.GetValue(target); }
                        catch { }
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
