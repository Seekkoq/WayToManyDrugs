using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public static class THCCookie
    {
        public const string ProductKindId = "westvilleconnection:cookie_v1";
        public const string ProductId = "westvilleconnection:products/cookie_v1";

        private static ProductKind _productKind;
        private static CustomProductDefinition _definition;
        private static ProductKindMetadata _metadata;

        private static ProductPresentationProfile _presentationProfile;
        private static ProductPackagingContentProfile _baggieProfile;
        private static ProductPackagingContentProfile _jarProfile;
        private static ProductPackagingContentProfile _brickProfile;

        private static GameObject _visualSource;
        private static Material _visualMaterial;
        private static Material _chipMaterial;
        private static Material _brickMaterial;
        private static Sprite _staticIcon;

        private static bool _built;
        private static bool _discovered;
        private static bool _failureLogged;
        private static bool _metadataFailureLogged;
        private static int _iconEnforceAttempts;
        private const int MaxIconEnforceAttempts = 180;

        // ---- Jar tuning (calibrate these, everything else follows) ----
        // Cookies clip through glass  -> halve both.
        // Cookies still tiny          -> raise both.
        // Keep Spread ~= 0.8 * Scale so the ring doesn't overlap the center cookie.
        private const float JarCookieScale = 0.070f;
        private const float JarSpreadRadius = 0.055f;
        private const float JarFloorY = 0.000f;

        public static Sprite DeliveryIcon
        {
            get
            {
                try { return _definition != null ? _definition.Icon : null; }
                catch { return null; }
            }
        }

        public static bool TryRegister()
        {
            if (_discovered) return true;
            if (!_built && !TryBuild()) return false;
            return TryDiscover();
        }

        private static bool TryBuild()
        {
            try
            {
                var shroomTemplate = ItemManager.GetDefinition("shroom") as S1ProductDefinition;
                if (shroomTemplate == null) return false;

                var baggie = ItemManager.GetDefinition("baggie") as S1PackagingDefinition;
                var jar = ItemManager.GetDefinition("jar") as S1PackagingDefinition;
                var brick = ItemManager.GetDefinition("brick") as S1PackagingDefinition;
                if (baggie == null || jar == null || brick == null) return false;

                if (_productKind == null)
                {
                    _productKind = new ProductKindBuilder(ProductKindId)
                        .WithCompatibilityDrugType(S1DrugType.Cocaine)
                        .Build();
                }

                _staticIcon = GetOrCreateCookieIcon();
                EnsurePresentationRegistered();
                EnsurePackagingRegistered();

                _definition = CustomProductItemCreator
                    .CreateBuilder(ProductId, _productKind)
                    .WithName("THC Cookie")
                    .WithDescription("A warm, golden-brown cannabis-infused cookie.")
                    .WithProductPrice(80f)
                    .WithLegalStatus((S1LegalStatus)1)
                    .WithBaseAddictiveness(0.40f)
                    .WithDefaultQuality((S1Quality)2)
                    .WithRepresentationsFrom(shroomTemplate)
                    .WithValidPackaging(new S1PackagingDefinition[] { baggie, jar, brick })
                    .WithEffectDurations(300, 600)
                    .WithNativeMixerMap((ProductMixingMap)2)
                    .Build();

                EnforceIcon();
                MelonCoroutines.Start(FixIconRoutine());

                _built = true;
                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Cookie] Build failed: " + ex);
                }
                return false;
            }
        }

        public static void EnforceIcon()
        {
            if (_definition == null || _iconEnforceAttempts >= MaxIconEnforceAttempts) return;
            _iconEnforceAttempts++;

            Sprite icon = GetOrCreateCookieIcon();
            if (icon == null) return;

            bool wrapperOk = TrySetIconMember(_definition, icon);
            bool rawOk = false;

            object rawObject = GetMember(_definition, "S1ItemDefinition")
                ?? GetMember(_definition, "NativeDefinition")
                ?? GetMember(_definition, "Definition");

            Il2CppScheduleOne.ItemFramework.ItemDefinition rawDef =
                rawObject as Il2CppScheduleOne.ItemFramework.ItemDefinition;

            if (rawDef != null)
            {
                try { rawDef.Icon = icon; rawOk = true; }
                catch { rawOk = TrySetIconMember(rawObject, icon); }
            }
            else if (rawObject != null)
            {
                rawOk = TrySetIconMember(rawObject, icon);
            }

            if (_iconEnforceAttempts <= 3)
            {
                MelonLogger.Msg("[WVC Cookie] Icon enforce " + _iconEnforceAttempts +
                    ": wrapper=" + wrapperOk + ", raw=" + rawOk +
                    ", rawType=" + (rawObject?.GetType().Name ?? "null"));
            }
        }

        private static IEnumerator FixIconRoutine()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSeconds(0.5f);
                EnforceIcon();
            }
        }

        private static bool TrySetIconMember(object target, Sprite icon)
        {
            if (target == null) return false;
            bool changed = false;
            Type type = target.GetType();
            string[] names = { "Icon", "icon", "_icon", "ItemIcon" };
            while (type != null)
            {
                foreach (string name in names)
                {
                    try
                    {
                        PropertyInfo p = type.GetProperty(name,
                            BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(typeof(Sprite)))
                        { p.SetValue(target, icon); changed = true; }

                        FieldInfo f = type.GetField(name,
                            BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (f != null && f.FieldType.IsAssignableFrom(typeof(Sprite)))
                        { f.SetValue(target, icon); changed = true; }
                    }
                    catch { }
                }
                type = type.BaseType;
            }
            return changed;
        }

        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            while (type != null)
            {
                try
                {
                    PropertyInfo p = type.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (p != null) return p.GetValue(target);

                    FieldInfo f = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(target);
                }
                catch { }
                type = type.BaseType;
            }
            return null;
        }

        private static Sprite GetOrCreateCookieIcon()
        {
            if (_staticIcon != null) return _staticIcon;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Color bg = new Color(0f, 0f, 0f, 0f);
            Color dark = new Color(0.55f, 0.35f, 0.12f, 1f);
            Color mid = new Color(0.72f, 0.50f, 0.20f, 1f);
            Color light = new Color(0.85f, 0.65f, 0.30f, 1f);
            Color chip = new Color(0.22f, 0.12f, 0.04f, 1f);
            Color border = new Color(0.45f, 0.28f, 0.10f, 1f);

            int[] chipX = { 90, 150, 130, 105, 165 };
            int[] chipY = { 130, 110, 170, 95, 155 };
            int[] chipR = { 6, 5, 5, 4, 5 };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 128f;
                    float dy = y - 128f;
                    float dist2 = dx * dx + dy * dy;

                    if (dist2 > 100f * 100f) { tex.SetPixel(x, y, bg); continue; }

                    float v = (y - 28f) / 200f;
                    Color c = Color.Lerp(dark, mid, v);
                    if (v > 0.6f) c = Color.Lerp(c, light, (v - 0.6f) * 1.5f);
                    if (dist2 > 94f * 94f) c = border;

                    bool isChip = false;
                    for (int k = 0; k < chipX.Length; k++)
                    {
                        float cdx = x - chipX[k];
                        float cdy = y - chipY[k];
                        if (cdx * cdx + cdy * cdy < chipR[k] * chipR[k]) { isChip = true; break; }
                    }
                    if (isChip) c = chip;

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, true);
            _staticIcon = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _staticIcon.name = "WVC_Cookie_Static";
            MelonLogger.Msg("[WVC Cookie] Static icon created.");
            return _staticIcon;
        }

        private static bool TryDiscover()
        {
            try
            {
                _definition.Discover(false);
                ConsoleItemAliases.Register("cookie", ProductId);
                ConsoleItemAliases.Register("thccookie", ProductId);
                _discovered = true;
                MelonLogger.Msg("[Westville Connection] THC Cookie registered and discovered.");
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Cookie] Discovery failed: " + ex);
                }
                return false;
            }
        }

        public static bool TryRegisterMetadata()
        {
            if (_metadata != null) return true;
            if (_definition == null || _productKind == null) return false;
            try
            {
                EnforceIcon();
                Sprite icon = GetOrCreateCookieIcon();
                _metadata = new ProductKindMetadataBuilder(_productKind)
                    .WithDisplayName("THC Cookie")
                    .WithColor(new Color(0.82f, 0.58f, 0.18f))
                    .WithIcon(icon)
                    .WithSortOrder(12)
                    .WithSearchAliases(new string[] { "cookie", "thccookie" })
                    .WithProductManagerVisibility(true)
                    .Build();
                MelonLogger.Msg("[Westville Connection] THC Cookie Products app category registered.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_metadataFailureLogged)
                {
                    _metadataFailureLogged = true;
                    MelonLogger.Error("[WVC Cookie] Metadata failed: " + ex);
                }
                return false;
            }
        }

        // ============================================================
        // Visual source — domed cookie with chocolate chips
        // ============================================================

        private static GameObject GetOrCreateVisualSource()
        {
            if (_visualSource != null) return _visualSource;

            if (_visualMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                _visualMaterial = new Material(shader);
                _visualMaterial.name = "WVC_Cookie_Material";
                Color c = new Color(0.84f, 0.58f, 0.24f, 1f);
                if (_visualMaterial.HasProperty("_BaseColor")) _visualMaterial.SetColor("_BaseColor", c);
                if (_visualMaterial.HasProperty("_Color")) _visualMaterial.SetColor("_Color", c);
                if (_visualMaterial.HasProperty("_Smoothness")) _visualMaterial.SetFloat("_Smoothness", 0.18f);
                if (_visualMaterial.HasProperty("_Glossiness")) _visualMaterial.SetFloat("_Glossiness", 0.18f);
                if (_visualMaterial.HasProperty("_Metallic")) _visualMaterial.SetFloat("_Metallic", 0f);
            }

            if (_chipMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                _chipMaterial = new Material(shader);
                _chipMaterial.name = "WVC_Cookie_Chip_Material";
                Color cc = new Color(0.14f, 0.07f, 0.02f, 1f);
                if (_chipMaterial.HasProperty("_BaseColor")) _chipMaterial.SetColor("_BaseColor", cc);
                if (_chipMaterial.HasProperty("_Color")) _chipMaterial.SetColor("_Color", cc);
                if (_chipMaterial.HasProperty("_Smoothness")) _chipMaterial.SetFloat("_Smoothness", 0.45f);
                if (_chipMaterial.HasProperty("_Glossiness")) _chipMaterial.SetFloat("_Glossiness", 0.45f);
                if (_chipMaterial.HasProperty("_Metallic")) _chipMaterial.SetFloat("_Metallic", 0f);
            }

            GameObject go = new GameObject("WVC_Cookie_3D");
            go.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(go);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildCookieMesh();
            mr.sharedMaterials = new Material[] { _visualMaterial, _chipMaterial };
            mr.receiveShadows = false;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _visualSource = go;
            return _visualSource;
        }

        // Domed cookie body (submesh 0) + chocolate chips (submesh 1).
        // Diameter = 1 unit, y spans -0.04 (base) .. +0.08 (dome crown).
        private static Mesh BuildCookieMesh()
        {
            const int seg = 28;
            const int lat = 10;
            const float r = 0.50f;
            const float baseY = -0.04f;
            const float domeH = 0.08f;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris0 = new List<int>();
            var tris1 = new List<int>();

            // ---- Bottom disc ----
            int bottomCentre = verts.Count;
            verts.Add(new Vector3(0f, baseY, 0f));
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                float px = Mathf.Cos(a) * r;
                float pz = Mathf.Sin(a) * r;
                verts.Add(new Vector3(px, baseY, pz));
                norms.Add(Vector3.down);
                uvs.Add(new Vector2(px / r * 0.5f + 0.5f, pz / r * 0.5f + 0.5f));
            }

            for (int i = 0; i < seg; i++)
            {
                int a = bottomCentre + 1 + i;
                int b = bottomCentre + 1 + (i + 1) % seg;
                tris0.Add(bottomCentre); tris0.Add(b); tris0.Add(a);
            }

            // ---- Side wall ----
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg;
                float a1 = (i + 1) * Mathf.PI * 2f / seg;

                Vector3 t0 = new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r);
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * r, baseY, Mathf.Sin(a0) * r);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * r, baseY, Mathf.Sin(a1) * r);

                Vector3 sn0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 sn1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

                int v = verts.Count;
                verts.Add(t0); norms.Add(sn0); uvs.Add(new Vector2((float)i / seg, 1f));
                verts.Add(t1); norms.Add(sn1); uvs.Add(new Vector2((float)(i + 1) / seg, 1f));
                verts.Add(b0); norms.Add(sn0); uvs.Add(new Vector2((float)i / seg, 0f));
                verts.Add(b1); norms.Add(sn1); uvs.Add(new Vector2((float)(i + 1) / seg, 0f));

                tris0.Add(v); tris0.Add(v + 1); tris0.Add(v + 2);
                tris0.Add(v + 1); tris0.Add(v + 3); tris0.Add(v + 2);
            }

            // ---- Domed top ----
            for (int la = 0; la < lat; la++)
            {
                float phi0 = la * Mathf.PI * 0.5f / lat;
                float phi1 = (la + 1) * Mathf.PI * 0.5f / lat;

                float y0 = Mathf.Sin(phi0) * domeH;
                float y1 = Mathf.Sin(phi1) * domeH;
                float r0 = Mathf.Cos(phi0) * r;
                float r1 = Mathf.Cos(phi1) * r;

                for (int lo = 0; lo < seg; lo++)
                {
                    float a0 = lo * Mathf.PI * 2f / seg;
                    float a1 = (lo + 1) * Mathf.PI * 2f / seg;

                    Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, y0, Mathf.Sin(a0) * r0);
                    Vector3 p10 = new Vector3(Mathf.Cos(a1) * r0, y0, Mathf.Sin(a1) * r0);
                    Vector3 p01 = new Vector3(Mathf.Cos(a0) * r1, y1, Mathf.Sin(a0) * r1);
                    Vector3 p11 = new Vector3(Mathf.Cos(a1) * r1, y1, Mathf.Sin(a1) * r1);

                    Vector3 n00 = new Vector3(Mathf.Cos(a0) * Mathf.Cos(phi0), Mathf.Sin(phi0), Mathf.Sin(a0) * Mathf.Cos(phi0)).normalized;
                    Vector3 n10 = new Vector3(Mathf.Cos(a1) * Mathf.Cos(phi0), Mathf.Sin(phi0), Mathf.Sin(a1) * Mathf.Cos(phi0)).normalized;
                    Vector3 n01 = new Vector3(Mathf.Cos(a0) * Mathf.Cos(phi1), Mathf.Sin(phi1), Mathf.Sin(a0) * Mathf.Cos(phi1)).normalized;
                    Vector3 n11 = new Vector3(Mathf.Cos(a1) * Mathf.Cos(phi1), Mathf.Sin(phi1), Mathf.Sin(a1) * Mathf.Cos(phi1)).normalized;

                    int v = verts.Count;
                    verts.Add(p00); norms.Add(n00); uvs.Add(new Vector2((float)lo / seg, (float)la / lat));
                    verts.Add(p10); norms.Add(n10); uvs.Add(new Vector2((float)(lo + 1) / seg, (float)la / lat));
                    verts.Add(p01); norms.Add(n01); uvs.Add(new Vector2((float)lo / seg, (float)(la + 1) / lat));
                    verts.Add(p11); norms.Add(n11); uvs.Add(new Vector2((float)(lo + 1) / seg, (float)(la + 1) / lat));

                    tris0.Add(v); tris0.Add(v + 2); tris0.Add(v + 1);
                    tris0.Add(v + 1); tris0.Add(v + 2); tris0.Add(v + 3);
                }
            }

            // ---- Chocolate chips ----
            float[] chipAngles = { 15f, 52f, 96f, 138f, 185f, 226f, 268f, 310f, 350f, 40f, 160f, 280f, 0f };
            float[] chipRadii = { 0.36f, 0.22f, 0.34f, 0.20f, 0.35f, 0.24f, 0.36f, 0.21f, 0.33f, 0.10f, 0.12f, 0.08f, 0.00f };
            const float chipR = 0.080f;
            const float chipHh = 0.040f;

            for (int ci = 0; ci < chipAngles.Length; ci++)
            {
                float ang = chipAngles[ci];
                float cr = chipRadii[ci];
                float radA = ang * Mathf.Deg2Rad;
                float cy = Mathf.Sin(Mathf.Acos(Mathf.Clamp01(cr / r))) * domeH;
                float cx = Mathf.Cos(radA) * cr;
                float cz = Mathf.Sin(radA) * cr;
                Vector3 chipBase = new Vector3(cx, cy, cz);

                Vector3 chipUp = new Vector3(cx / r, domeH / r, cz / r).normalized;
                Vector3 chipRight = Vector3.Cross(chipUp, Vector3.forward).normalized;
                if (chipRight.sqrMagnitude < 0.01f)
                    chipRight = Vector3.Cross(chipUp, Vector3.right).normalized;
                Vector3 chipFwd = Vector3.Cross(chipRight, chipUp).normalized;

                const int cs = 8;
                int chipCentre = verts.Count;
                verts.Add(chipBase + chipUp * chipHh);
                norms.Add(chipUp);
                uvs.Add(new Vector2(0.5f, 0.5f));

                for (int i = 0; i < cs; i++)
                {
                    float a = i * Mathf.PI * 2f / cs;
                    float scaleMod = (i % 2 == 0) ? 1.0f : 0.85f;
                    Vector3 offset = (chipRight * Mathf.Cos(a) + chipFwd * Mathf.Sin(a)) * (chipR * scaleMod);
                    verts.Add(chipBase + offset);
                    norms.Add((offset.normalized + chipUp * 0.7f).normalized);
                    uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                }

                for (int i = 0; i < cs; i++)
                {
                    int a = chipCentre + 1 + i;
                    int b = chipCentre + 1 + (i + 1) % cs;
                    tris1.Add(chipCentre); tris1.Add(a); tris1.Add(b);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "WVC_Cookie_Domed";
            mesh.subMeshCount = 2;
            mesh.vertices = verts.ToArray();
            mesh.normals = norms.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.SetTriangles(tris0.ToArray(), 0);
            mesh.SetTriangles(tris1.ToArray(), 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ============================================================
        // Jar layout (generated from the tunables at the top)
        // ============================================================

        private static ProductPresentationTransform[] BuildJarCookiePlacements()
        {
            var list = new List<ProductPresentationTransform>();

            // Mesh base is at y = -0.04 (unscaled). Lift so the flat bottom rests on the floor.
            float baseLift = 0.04f * JarCookieScale;

            // Layer 1: one center cookie + ring of 6, lying flat with varied yaw.
            AddCookie(list, 0f, 0f, JarFloorY + baseLift, 0f, 37f, 0f, 1.00f);

            float[] ringYaw = { 12f, 71f, 133f, 190f, 248f, 305f };
            float[] ringTilt = { 6f, -5f, 8f, -7f, 4f, -6f };

            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                AddCookie(list,
                    Mathf.Cos(a) * JarSpreadRadius,
                    Mathf.Sin(a) * JarSpreadRadius,
                    JarFloorY + baseLift,
                    ringTilt[i], ringYaw[i], -ringTilt[i] * 0.5f,
                    0.92f + (i % 3) * 0.04f);
            }

            // Layer 2: three cookies resting on top, tilted like they fell in.
            float layer2Y = JarFloorY + baseLift + 0.12f * JarCookieScale + 0.004f;
            AddCookie(list, JarSpreadRadius * 0.45f, 0f, layer2Y, 18f, 100f, -12f, 0.95f);
            AddCookie(list, -JarSpreadRadius * 0.30f, JarSpreadRadius * 0.40f, layer2Y, -14f, 215f, 20f, 0.97f);
            AddCookie(list, -JarSpreadRadius * 0.25f, -JarSpreadRadius * 0.45f, layer2Y, 22f, 320f, 8f, 0.93f);

            return list.ToArray();
        }

        private static void AddCookie(
            List<ProductPresentationTransform> list,
            float x, float z, float y,
            float pitch, float yaw, float roll,
            float sizeMul)
        {
            list.Add(new ProductPresentationTransform(
                new Vector3(x, y, z),
                new Vector3(pitch, yaw, roll),
                Vector3.one * (JarCookieScale * sizeMul)));
        }

        // ============================================================
        // Presentation + Packaging
        // ============================================================

        private static void EnsurePresentationRegistered()
        {
            if (_presentationProfile != null) return;

            var loose = new ProductPresentationTransform(
                Vector3.zero, Vector3.zero, Vector3.one * 0.048f);
            var held = new ProductPresentationTransform(
                Vector3.zero, Vector3.zero, Vector3.one * 0.048f);

            _presentationProfile = new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => GetOrCreateVisualSource(), loose)
                .WithHeldVisual(() => GetOrCreateVisualSource(), held)
                .WithFunctionalProductConvexMeshColliders()
                .Build();

            ProductPresentationProfileRegistry.RegisterForProduct(
                "westvilleconnection", ProductId, _presentationProfile);
            ProductPresentationProfileRegistry.RegisterForProductKind(
                "westvilleconnection", _productKind, _presentationProfile);
        }

        private static void EnsurePackagingRegistered()
        {
            if (_baggieProfile == null)
            {
                _baggieProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateVisualSource())
                    .AddPlacement(new ProductPresentationTransform(
                        new Vector3(0f, -0.002f, 0f),
                        new Vector3(10f, 35f, 5f),
                        Vector3.one * (JarCookieScale * 0.8f)))
                    .Build();
            }

            if (_jarProfile == null)
            {
                _jarProfile = new ProductPackagingContentProfileBuilder()
                    .WithContent(() => GetOrCreateVisualSource())
                    .AddPlacements(BuildJarCookiePlacements())
                    .Build();
            }

            if (_brickProfile == null)
            {
                _brickProfile = new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        ProductPackagingVisualTemplate.Cocaine,
                        clone => ApplyBrickMaterial(clone),
                        null)
                    .Build();
            }

            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.Register(
                "westvilleconnection", ProductId, "brick", _brickProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "baggie", _baggieProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "jar", _jarProfile);
            ProductPackagingContentProfileRegistry.RegisterForProductKind(
                "westvilleconnection", ProductKindId, "brick", _brickProfile);
        }

        private static void ApplyBrickMaterial(GameObject clone)
        {
            if (clone == null) return;
            if (_brickMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                if (shader == null) return;
                _brickMaterial = new Material(shader);
                _brickMaterial.name = "WVC_Cookie_Brick_Material";
                Color c = new Color(0.58f, 0.38f, 0.12f, 1f);
                if (_brickMaterial.HasProperty("_BaseColor")) _brickMaterial.SetColor("_BaseColor", c);
                if (_brickMaterial.HasProperty("_Color")) _brickMaterial.SetColor("_Color", c);
                if (_brickMaterial.HasProperty("_Smoothness")) _brickMaterial.SetFloat("_Smoothness", 0.12f);
                if (_brickMaterial.HasProperty("_Glossiness")) _brickMaterial.SetFloat("_Glossiness", 0.12f);
                if (_brickMaterial.HasProperty("_Metallic")) _brickMaterial.SetFloat("_Metallic", 0f);
            }
            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = _brickMaterial;
                r.sharedMaterials = mats;
            }
        }
    }
}