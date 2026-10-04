using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class CartItem
    {
        public const string CartId = "westvilleconnection:products/vape_cart";
        private const string DonorId = "iodine";
        private const float CartPrice = 45f;
        private const float HeldScale = 0.06f;
        private const float WorldScale = 0.05f;

        private static bool _registered;
        private static bool _failed;

        private static GameObject _cartVisual;
        private static Sprite _cartIcon;

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                ItemDefinition donor = ItemManager.GetDefinition(DonorId);
                if (donor == null)
                {

                    return false;
                }

                _cartVisual = CartObjVisual.GetOrCreate();
                if (_cartVisual == null)
                {

                    return false;
                }

                MixIngredientItemCreator
                    .CloneFrom(DonorId)
                    .WithBasicInfo(
                        CartId,
                        "Vape Cart",
                        "A premium white ceramic THC vape cartridge. Click while holding to take a hit.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("cart", CartId);
                RegisterAliasSafely("vape", CartId);
                RegisterAliasSafely("vapecart", CartId);
                RegisterAliasSafely("cartridge", CartId);

                ApplyCustomRepresentations(CartId, _cartVisual);

                _cartIcon = CreateCartIconSprite("WVC_VapeCart_Icon_Fixed");
                ApplyIcon(CartId, _cartIcon);

                SetIngredientPrice(CartId, CartPrice);
                MoveSourceOffscreen(_cartVisual);

                MelonCoroutines.Start(ReapplyCartIconAfterUiReady());

                _registered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] Vape Cart registered successfully with custom 2D icon.");
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                MelonLogger.Error("[WVC Cart] Registration failed: " + ex);
                return false;
            }
        }

        private static IEnumerator ReapplyCartIconAfterUiReady()
        {
            yield return null;
            yield return new WaitForSeconds(1.5f);

            if (_cartIcon != null)
            {
                ApplyIcon(CartId, _cartIcon);
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] Cart icon refreshed for UI.");
            }
        }

        private static Sprite CreateCartIconSprite(string iconName)
        {
            const int FinalSize = 512;
            const int Supersample = 2;

            IconCanvas canvas = new IconCanvas(FinalSize * Supersample, FinalSize * Supersample, Supersample);
            DrawCartIcon(canvas);

            Texture2D tex = new Texture2D(FinalSize, FinalSize, TextureFormat.RGBA32, false)
            {
                name = iconName + "_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            tex.SetPixels(canvas.Downsample(Supersample));
            tex.Apply();
            UnityEngine.Object.DontDestroyOnLoad(tex);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, FinalSize, FinalSize),
                new Vector2(0.5f, 0.5f),
                100f
            );

            sprite.name = iconName;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        private static void DrawCartIcon(IconCanvas c)
        {
            const float Cx = 256f;

            Color metal   = new Color(0.74f, 0.77f, 0.82f, 1f);
            Color ceramic = new Color(0.95f, 0.95f, 0.97f, 1f);
            Color groove  = new Color(0.40f, 0.42f, 0.47f, 0.85f);

            c.Ellipse(Cx, 50f, 92f, 15f, new Color(0f, 0f, 0f, 0.20f));
            c.Ellipse(Cx, 46f, 64f, 10f, new Color(0f, 0f, 0f, 0.16f));

            c.Cylinder(Cx, 92f, 30f, 30f, 12f, metal, 0.42f, 0.55f, -0.42f);
            c.Ellipse(Cx, 76f, 27f, 2.6f, groove);
            c.Ellipse(Cx, 90f, 27f, 2.6f, groove);
            c.Ellipse(Cx, 104f, 27f, 2.6f, groove);
            c.Ellipse(Cx, 64f, 11f, 3.5f, new Color(0.80f, 0.63f, 0.22f, 0.90f));

            c.Cylinder(Cx, 152f, 58f, 34f, 14f, ceramic, 0.70f, 0.35f, -0.40f);
            c.Ellipse(Cx, 120f, 50f, 2.2f, new Color(0.55f, 0.57f, 0.62f, 0.35f));
            c.Cylinder(Cx, 152f, 30f, 2.5f, 2.5f, new Color(0.95f, 0.55f, 0.10f, 0.60f), 0.55f, 0.25f, -0.40f);

            c.RoundedRect(Cx, 256f, 49f, 68f, 16f, (x, y) =>
            {
                float t = Mathf.Clamp((x - Cx) / 49f, -1f, 1f);
                float facing = (float)Math.Sqrt(Math.Max(0f, 1f - t * t));
                float v = Mathf.Clamp01((y - 188f) / 136f);

                Color oil = Color.Lerp(
                    new Color(0.78f, 0.36f, 0.03f),
                    new Color(1.00f, 0.75f, 0.25f),
                    v);

                oil *= 0.72f + 0.28f * facing;
                oil.a = 0.97f;
                return oil;
            });

            c.Ellipse(Cx, 318f, 43f, 5f, new Color(1f, 0.87f, 0.50f, 0.45f));
            c.Ellipse(238f, 286f, 7f, 7f, new Color(1f, 0.93f, 0.68f, 0.35f));
            c.Ellipse(274f, 240f, 5f, 5f, new Color(1f, 0.93f, 0.68f, 0.30f));
            c.Ellipse(250f, 212f, 3.5f, 3.5f, new Color(1f, 0.93f, 0.68f, 0.25f));

            c.RoundedRect(216f, 258f, 6f, 66f, 6f, new Color(1f, 1f, 1f, 0.38f));
            c.RoundedRect(298f, 258f, 4f, 60f, 4f, new Color(1f, 1f, 1f, 0.12f));
            c.StrokeRoundedRect(Cx, 260f, 58f, 80f, 20f, 2.5f, new Color(0.95f, 0.98f, 1f, 0.35f));

            c.Cylinder(Cx, 355f, 56f, 17f, 8f, metal, 0.45f, 0.50f, -0.42f);

            c.Cylinder(Cx, 422f, 40f, 51f, 40f, ceramic, 0.70f, 0.40f, -0.40f);
            c.Ellipse(Cx, 462f, 12f, 4.5f, new Color(0.16f, 0.16f, 0.20f, 0.85f));
        }

        private sealed class IconCanvas
        {
            public delegate Color ShadeFn(float x, float y);

            private readonly int _w;
            private readonly int _h;
            private readonly Color[] _buf;
            private readonly float _scale;

            public IconCanvas(int width, int height, float designToPixelScale)
            {
                _w = width;
                _h = height;
                _buf = new Color[width * height];
                _scale = designToPixelScale;
            }

            public void Cylinder(float cx, float cy, float hw, float hh, float r,
                Color baseColor, float ambient, float specStrength, float specPos)
            {
                RoundedRect(cx, cy, hw, hh, r, (x, y) =>
                {
                    float t = Mathf.Clamp((x - cx) / hw, -1f, 1f);
                    float facing = (float)Math.Sqrt(Math.Max(0f, 1f - t * t));
                    float lambert = ambient + (1f - ambient) * facing;

                    float d = t - specPos;
                    float spec = specStrength * Mathf.Exp(-(d * d) / 0.03f);

                    Color col = baseColor * lambert;
                    col.r = Mathf.Clamp01(col.r + spec);
                    col.g = Mathf.Clamp01(col.g + spec);
                    col.b = Mathf.Clamp01(col.b + spec);
                    col.a = baseColor.a;
                    return col;
                });
            }

            public void RoundedRect(float cx, float cy, float hw, float hh, float r, Color color)
                => RoundedRect(cx, cy, hw, hh, r, (x, y) => color);

            public void RoundedRect(float cx, float cy, float hw, float hh, float r, ShadeFn shade)
            {
                float scx = cx * _scale, scy = cy * _scale;
                float shw = hw * _scale, shh = hh * _scale;
                float sr = Mathf.Min(r * _scale, shw, shh);

                ForEachPixel(
                    scx - shw - 1f, scx + shw + 1f,
                    scy - shh - 1f, scy + shh + 1f,
                    (x, y) =>
                    {
                        float cov = Mathf.Clamp01(0.5f - SdRoundRect(x, y, scx, scy, shw, shh, sr));
                        if (cov > 0f)
                            Blend(x, y, shade(x / _scale, y / _scale), cov);
                    });
            }

            public void StrokeRoundedRect(float cx, float cy, float hw, float hh, float r,
                float thickness, Color color)
            {
                float scx = cx * _scale, scy = cy * _scale;
                float shw = hw * _scale, shh = hh * _scale;
                float sr = Mathf.Min(r * _scale, shw, shh);
                float half = thickness * _scale * 0.5f;

                ForEachPixel(
                    scx - shw - 2f, scx + shw + 2f,
                    scy - shh - 2f, scy + shh + 2f,
                    (x, y) =>
                    {
                        float sd = Mathf.Abs(SdRoundRect(x, y, scx, scy, shw, shh, sr));
                        float cov = Mathf.Clamp01(half + 0.5f - sd);
                        if (cov > 0f)
                            Blend(x, y, color, cov);
                    });
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color color)
            {
                float scx = cx * _scale, scy = cy * _scale;
                float srx = Mathf.Max(1f, rx * _scale);
                float sry = Mathf.Max(1f, ry * _scale);
                float avgR = (srx + sry) * 0.5f;

                ForEachPixel(
                    scx - srx - 1f, scx + srx + 1f,
                    scy - sry - 1f, scy + sry + 1f,
                    (x, y) =>
                    {
                        float dx = (x - scx) / srx;
                        float dy = (y - scy) / sry;
                        float sd = ((float)Math.Sqrt(dx * dx + dy * dy) - 1f) * avgR;
                        float cov = Mathf.Clamp01(0.5f - sd);
                        if (cov > 0f)
                            Blend(x, y, color, cov);
                    });
            }

            public Color[] Downsample(int factor)
            {
                int nw = _w / factor;
                int nh = _h / factor;
                Color[] result = new Color[nw * nh];
                float inv = 1f / (factor * factor);

                for (int y = 0; y < nh; y++)
                {
                    for (int x = 0; x < nw; x++)
                    {
                        float r = 0f, g = 0f, b = 0f, a = 0f;
                        for (int dy = 0; dy < factor; dy++)
                        {
                            int row = (y * factor + dy) * _w;
                            for (int dx = 0; dx < factor; dx++)
                            {
                                Color p = _buf[row + x * factor + dx];
                                r += p.r * p.a;
                                g += p.g * p.a;
                                b += p.b * p.a;
                                a += p.a;
                            }
                        }

                        result[y * nw + x] = a > 0f
                            ? new Color(r / a, g / a, b / a, a * inv)
                            : new Color(0f, 0f, 0f, 0f);
                    }
                }

                return result;
            }

            private delegate void PixelFn(int x, int y);

            private void ForEachPixel(float x0f, float x1f, float y0f, float y1f, PixelFn fn)
            {
                int x0 = Math.Max(0, (int)Math.Floor(x0f));
                int x1 = Math.Min(_w - 1, (int)Math.Ceiling(x1f));
                int y0 = Math.Max(0, (int)Math.Floor(y0f));
                int y1 = Math.Min(_h - 1, (int)Math.Ceiling(y1f));

                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        fn(x, y);
            }

            private void Blend(int x, int y, Color c, float cov)
            {
                float a = c.a * cov;
                if (a <= 0f) return;

                int i = y * _w + x;
                Color dst = _buf[i];
                float inv = 1f - a;

                _buf[i] = new Color(
                    dst.r * inv + c.r * a,
                    dst.g * inv + c.g * a,
                    dst.b * inv + c.b * a,
                    dst.a + a * (1f - dst.a));
            }

            private static float SdRoundRect(float px, float py, float cx, float cy, float hw, float hh, float r)
            {
                float dx = Math.Abs(px - cx) - (hw - r);
                float dy = Math.Abs(py - cy) - (hh - r);
                float ax = Math.Max(dx, 0f);
                float ay = Math.Max(dy, 0f);
                return Math.Min(Math.Max(dx, dy), 0f) + (float)Math.Sqrt(ax * ax + ay * ay) - r;
            }
        }

        private static void ApplyCustomRepresentations(string itemId, GameObject customModel)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition = GetRawDefinition(itemId);
                if (definition == null) return;

                ApplyEquippableRepresentation(definition, itemId, customModel, Vector3.one * 0.06f);
                ApplyStationRepresentation(definition, itemId, customModel, Vector3.one * 0.05f);
                ApplyStoredRepresentation(definition, itemId, customModel, Vector3.one * 0.05f);
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
                if (definition.Equippable == null || definition.Equippable.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    definition.Equippable.gameObject,
                    itemId,
                    "Equippable",
                    customModel,
                    visualScale
                );

                if (clone == null) return false;

                Il2CppScheduleOne.Equipping.Equippable equippable =
                    clone.GetComponent<Il2CppScheduleOne.Equipping.Equippable>();

                if (equippable == null) return false;

                VapeCartUseBehaviour useBehaviour = clone.GetComponent<VapeCartUseBehaviour>();
                if (useBehaviour == null)
                {
                    clone.AddComponent<VapeCartUseBehaviour>();
                }

                definition.Equippable = equippable;
                return true;
            }
            catch (Exception)
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
                var storable = definition.TryCast<Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();
                if (storable == null || storable.StationItem == null || storable.StationItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StationItem.gameObject, itemId, "StationItem", customModel, visualScale);

                if (clone == null) return false;

                var comp = clone.GetComponent<Il2CppScheduleOne.StationFramework.StationItem>();
                if (comp == null) return false;

                storable.StationItem = comp;
                return true;
            }
            catch { return false; }
        }

        public static Sprite GetOrCreateCartIcon()
        {
            if (_cartIcon == null)
            {
                _cartIcon = CreateCartIconSprite(
                    "WVC_VapeCart_Icon_Fixed"
                );
            }

            return _cartIcon;
        }

        public static void ApplyCartIconToProduct(string productId)
        {
            Sprite icon = GetOrCreateCartIcon();

            if (icon == null)
            {


                return;
            }

            ApplyIcon(productId, icon);
        }

        public static bool ApplyOriginalHeldRepresentationToProduct(
            string productId)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition productDefinition =
                    GetRawDefinition(productId);

                Il2CppScheduleOne.ItemFramework.ItemDefinition iodineDefinition =
                    GetRawDefinition(DonorId);

                GameObject cartVisual =
                    CartObjVisual.GetOrCreate();

                if (productDefinition == null ||
                    iodineDefinition == null ||
                    iodineDefinition.Equippable == null ||
                    iodineDefinition.Equippable.gameObject == null ||
                    cartVisual == null)
                {
                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        iodineDefinition.Equippable.gameObject,
                        productId,
                        "Equippable",
                        cartVisual,
                        Vector3.one * HeldScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.Equipping.Equippable equippable =
                    clone.GetComponent<
                        Il2CppScheduleOne.Equipping.Equippable
                    >();

                if (equippable == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return false;
                }

                if (clone.GetComponent<VapeCartUseBehaviour>() == null)
                {
                    clone.AddComponent<VapeCartUseBehaviour>();
                }

                productDefinition.Equippable = equippable;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cart] Original iodine held representation restored " +
                    "for product."
                );

                return true;
            }
            catch (Exception)
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
                var storable = definition.TryCast<Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();
                if (storable == null || storable.StoredItem == null || storable.StoredItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StoredItem.gameObject, itemId, "StoredItem", customModel, visualScale);

                if (clone == null) return false;

                var comp = clone.GetComponent<Il2CppScheduleOne.Storage.StoredItem>();
                if (comp == null) return false;

                storable.StoredItem = comp;
                return true;
            }
            catch { return false; }
        }

        private static GameObject BuildRepresentationClone(
            GameObject template, string itemId, string context,
            GameObject customModel, Vector3 localScale)
        {
            if (template == null || customModel == null) return null;

            GameObject clone = UnityEngine.Object.Instantiate(template);
            string safeId = itemId.Replace(":", "_").Replace("/", "_");

            clone.name = "WVC_" + context + "_" + safeId;
            clone.SetActive(true);
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.enabled = false;
            }

            GameObject visual = UnityEngine.Object.Instantiate(customModel);
            visual.name = "WVC_CartVisual_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.SetActive(true);

            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.enabled = true;
            }

            return clone;
        }

        private static void SetIngredientPrice(string itemId, float price)
        {
            try
            {
                var def = GetRawDefinition(itemId);
                if (def == null) return;

                var storable = def.TryCast<Il2CppScheduleOne.ItemFramework.StorableItemDefinition>();
                if (storable == null) return;

                storable.BasePurchasePrice = price;
            }
            catch { }
        }

        private static void ApplyIcon(string itemId, Sprite icon)
        {
            if (icon == null) return;

            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
                Il2CppScheduleOne.ItemFramework.ItemDefinition raw = GetRawDefinition(itemId);

                TrySetMember(wrapper, "Icon", icon);
                TrySetMember(raw, "Icon", icon);
                TrySetAnyIconSpriteMember(wrapper, icon);
                TrySetAnyIconSpriteMember(raw, icon);
            }
            catch (Exception)
            {

            }
        }

        private static bool TrySetMember(object target, string name, object value)
        {
            if (target == null || value == null) return false;
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    try { field.SetValue(target, value); return true; } catch { }
                }

                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null && prop.CanWrite && prop.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try { prop.SetValue(target, value); return true; } catch { }
                }
                type = type.BaseType;
            }
            return false;
        }

        private static bool TrySetAnyIconSpriteMember(object target, Sprite icon)
        {
            if (target == null || icon == null) return false;
            bool applied = false;
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo f in fields)
                {
                    if (typeof(Sprite).IsAssignableFrom(f.FieldType) && f.Name.ToLowerInvariant().Contains("icon"))
                    {
                        try { f.SetValue(target, icon); applied = true; } catch { }
                    }
                }

                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (PropertyInfo p in props)
                {
                    if (p.CanWrite && typeof(Sprite).IsAssignableFrom(p.PropertyType) && p.Name.ToLowerInvariant().Contains("icon"))
                    {
                        try { p.SetValue(target, icon); applied = true; } catch { }
                    }
                }
                type = type.BaseType;
            }
            return applied;
        }

        public static Il2CppScheduleOne.ItemFramework.ItemDefinition GetRawDefinition(string itemId)
        {
            ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
            if (wrapper == null) return null;

            object raw = GetMemberValue(wrapper, "S1ItemDefinition");
            return raw as Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();

            while (type != null)
            {
                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null)
                {
                    try { return prop.GetValue(target); } catch { }
                }

                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
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
            catch (Exception) {  }
        }
    }
}
