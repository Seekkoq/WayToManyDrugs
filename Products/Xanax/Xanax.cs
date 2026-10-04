using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Products;
using UnityEngine;

using CustomNPCExample.Framework;

using Il2CppStorableDef = Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products
{
    public static class Xanax
    {
        /// <summary>Full product id, i.e. what the native registry and the consumption router use.</summary>
        public const string ProductId = "westvilleconnection:products/xanax_v1";

        private const string GlbResource = "xanax.glb";

        private static ProductBuilder _builder;
        private static string _scaffoldId;
        private static bool _scaffoldMissingLogged;
        private static bool _scaffoldIdsDumped;
        private static string _lastBlockedMessage;
        private static string _lastUnexpectedMessage;
        private static bool _aliasRegistered;
        private static int _failedAttempts;

        public static bool IsBuilt => _builder != null && _builder.IsBuilt;

        public static bool TryEnsureBuilt()
        {
            return TryRegister();
        }

        public static bool TryRegister()
        {
            try
            {
                if (_builder == null)
                {
                    _scaffoldId = ResolveScaffoldId();

                    if (_scaffoldId == null)
                    {
                        if (!_scaffoldMissingLogged)
                        {
                            _scaffoldMissingLogged = true;


                        }

                        _failedAttempts++;

                        if (_failedAttempts > 8 && !_scaffoldIdsDumped)
                        {
                            DumpProductIds();
                            _scaffoldIdsDumped = true;
                        }

                        return false;
                    }

                    _builder = CreateBuilder(_scaffoldId);
                }

                if (!_builder.IsBuilt)
                    _builder.RegisterContent();

                _builder.CompleteLoad();

                if (_builder.IsBuilt && !_aliasRegistered)
                {
                    _aliasRegistered = true;

                    ConsoleItemAliases.Register(
                        "xanax",
                        _builder.ProductId
                    );


                }

                return _builder.IsFullyLoaded;
            }
            catch (InvalidOperationException ex)
            {
                _failedAttempts++;

                if (_lastBlockedMessage != ex.Message || _failedAttempts % 30 == 0)
                {
                    _lastBlockedMessage = ex.Message;


                }

                if (_failedAttempts > 8 && !_scaffoldIdsDumped)
                {
                    DumpProductIds();
                    _scaffoldIdsDumped = true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _failedAttempts++;

                if (_lastUnexpectedMessage != ex.Message || _failedAttempts % 30 == 0)
                {
                    _lastUnexpectedMessage = ex.Message;

                    MelonLogger.Error(
                        "[WVC Xanax] Registration failed (" + _failedAttempts +
                        "): " + ex
                    );
                }

                return false;
            }
        }

        private static ProductBuilder CreateBuilder(string scaffoldId)
        {
            return new ProductBuilder()
                .ID("xanax_v1")
                .Name("Xanax")
                .Description("A pressed bar. Chew it, feel nothing, feel everything.")
                .Price(45f)
                .LegalStatus((LegalStatus)1)
                // Benzos are habit forming; the shroom scaffold's 0.15 never hooked anyone.
                .Addictiveness(0.5f)
                .Scaffold(scaffoldId, DrugType.Shrooms)
                // The scaffold is a shroom, so its hallucinogen properties are deliberately left
                // out: the bar carries the benzodiazepine set instead - calm, then sedation, then
                // the foggy memory the come-down leaves behind.
                .CopyScaffoldEffects(false)
                .Properties(
                    S1API.Properties.Property.Calming,
                    S1API.Properties.Property.Sedating,
                    S1API.Properties.Property.Foggy)
                // Three minutes of dose curve for the player, six for NPCs.
                .EffectDurations(180, 360)
                .Model(GlbResource)
                .ModelFit(0.0375f)
                // The GLB is authored lying flat with its long axis pointing away from the viewer, so
                // the native icon rig and the held pose saw the bar end-on. Yaw it onto the X axis.
                .ModelRotation(new Vector3(0f, 90f, 0f))
                // Every world pose keeps the bar flat, which leaves the moulded imprint on its top
                // face edge-on to the native icon camera, so the icon is turned onto the camera
                // while it is rendered (see WvcIconPose).
                .IconPose()
                .ConsumptionTemplate(scaffoldId)
                // Share of the icon frame the bar fills. The camera fit scales by the longest axis
                // only, so this is the icon's size knob.
                .Icon(0.6f)
                .ProductKindColor(new Color(0.92f, 0.93f, 0.90f))
                .SortOrder(12)
                // The loose pose is the bar resting on a surface: yaw only, 12 is the reference
                // tilt, +180 flips the bar the other way. The icon no longer follows this pose.
                .LoosePose(
                    Vector3.zero,
                    new Vector3(0f, 192f, 0f),
                    Vector3.one * 1.15f)
                .HeldPose(
                    new Vector3(0f, 0.002f, 0f),
                    new Vector3(-70f, -1f, 0f),
                    Vector3.one * 1.65f)
                .Baggie(
                    Vector3.zero,
                    new Vector3(0f, 12f, 0f),
                    Vector3.one * 1.1f)
                .Jar(BuildJarPlacements())
                .Mixing(false, 0f, 0f);
        }

        /// <summary>
        /// Five bars sitting on the floor of a jar, laid out side by side across their width with a
        /// little scatter so it reads like a jar of pills rather than a grid. Offsets are in the
        /// jar's content space: the bars run along X, so the row steps along Z, and Y is the floor.
        /// Spacing is a hair wider than a bar is wide, which leaves a thin gap between them.
        /// </summary>
        private static ProductPresentationTransform[] BuildJarPlacements()
        {
            Vector3[] offsets =
            {
                new Vector3(-0.0038f, -0.0105f, -0.0280f),
                new Vector3(0.0034f, -0.0100f, -0.0140f),
                new Vector3(-0.0025f, -0.0110f, 0.0000f),
                new Vector3(0.0038f, -0.0100f, 0.0140f),
                new Vector3(-0.0030f, -0.0105f, 0.0280f)
            };

            float[] yaws = { -7f, 6f, 0f, -9f, 8f };

            ProductPresentationTransform[] placements =
                new ProductPresentationTransform[offsets.Length];

            for (int i = 0; i < offsets.Length; i++)
                placements[i] = ProductBuilder.JarPlacement(offsets[i], yaws[i], 1f);

            return placements;
        }

        private static string ResolveScaffoldId()
        {
            string[] candidates =
            {
                "shrooms",
                "shroom",
                "mushrooms",
                "magicmushroom"
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (ItemManager.GetDefinition(candidate) != null)
                        return candidate;
                }
                catch
                {
                }
            }

            try
            {
                foreach (Il2CppStorableDef definition in
                    UnityEngine.Resources.FindObjectsOfTypeAll<Il2CppStorableDef>())
                {
                    if (definition != null &&
                        definition.ID != null &&
                        definition.ID.ToLowerInvariant().Contains("shroom"))
                    {
                        return definition.ID;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static void DumpProductIds()
        {
            try
            {
                List<string> ids = new List<string>();

                foreach (Il2CppStorableDef definition in
                    UnityEngine.Resources.FindObjectsOfTypeAll<Il2CppStorableDef>())
                {
                    if (definition != null &&
                        definition.ID != null &&
                        !ids.Contains(definition.ID))
                    {
                        ids.Add(definition.ID);
                    }
                }


            }
            catch (Exception)
            {

            }
        }

        public static bool TryRegisterMetadata()
        {
            return _builder != null && _builder.IsBuilt;
        }
        private static Sprite _xanaxIcon;
        private static bool _iconRepairDone;
        private static float _iconRepairTimer;
        private static int _iconRepairAttempts;

        private const float IconRepairInterval = 5f;
        private const int MaxIconRepairAttempts = 24;

        public static void UpdateIconRepair()
        {
            if (_iconRepairDone || _builder == null || !_builder.IsBuilt)
                return;

            _iconRepairTimer += Time.deltaTime;

            if (_iconRepairTimer < IconRepairInterval)
                return;

            _iconRepairTimer = 0f;

            try
            {
                if (global::CustomNPCExample.Utils.WvcIcon.GetIcon(_builder.ProductId) != null)
                {
                    _iconRepairDone = true;



                    return;
                }

                // S1API renders the loose-visual icon once the product is discovered, so the sprite
                // fallback only steps in afterwards and never races the render rig.
                if (!_aliasRegistered)
                    return;

                _iconRepairAttempts++;

                Sprite clean = GetOrCreateXanaxIcon();

                if (clean != null && global::CustomNPCExample.Utils.WvcIcon.Apply(_builder.ProductId, clean))
                {
                    _iconRepairDone = true;



                    return;
                }

                if (_iconRepairAttempts >= MaxIconRepairAttempts)
                {
                    _iconRepairDone = true;


                }
            }
            catch (Exception)
            {
                _iconRepairDone = true;


            }
        }

        public static Sprite GetOrCreateXanaxIcon()
        {
            if (_xanaxIcon != null) return _xanaxIcon;

            const int FinalSize = 256;
            Texture2D tex = new Texture2D(FinalSize, FinalSize, TextureFormat.RGBA32, false)
            {
                name = "WVC_Xanax_Icon_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new Color(0, 0, 0, 0);
            Color[] colors = new Color[FinalSize * FinalSize];
            for (int i = 0; i < colors.Length; i++) colors[i] = transparent;

            // Render crisp, scored pill bar at a stylish angle
            float cx = FinalSize * 0.5f;
            float cy = FinalSize * 0.5f;
            float hw = 95f;   // half width (long axis)
            float hh = 28f;   // half height
            float rad = 14f;  // corner radius
            float angleRad = 28f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            Color pillColor = new Color(0.97f, 0.97f, 0.96f, 1f);
            Color pillShadow = new Color(0.72f, 0.73f, 0.77f, 1f);
            Color grooveColor = new Color(0.60f, 0.61f, 0.65f, 1f);
            Color grooveHighlight = new Color(1f, 1f, 1f, 1f);
            Color outlineColor = new Color(0.32f, 0.33f, 0.37f, 1f);
            Color shadowColor = new Color(0.20f, 0.20f, 0.24f, 1f);

            for (int y = 0; y < FinalSize; y++)
            {
                for (int x = 0; x < FinalSize; x++)
                {
                    Color pixel = transparent;

                    // Cast shadow: the same bar, offset down-right, so it sits on the panel
                    // instead of floating on it.
                    float shadowDx = x - cx - 7f;
                    float shadowDy = y - cy + 7f;
                    float shadowPx = shadowDx * cos + shadowDy * sin;
                    float shadowPy = -shadowDx * sin + shadowDy * cos;

                    float shadowQx = Mathf.Abs(shadowPx) - (hw - rad);
                    float shadowQy = Mathf.Abs(shadowPy) - (hh - rad);
                    float shadowAx = Mathf.Max(shadowQx, 0f);
                    float shadowAy = Mathf.Max(shadowQy, 0f);

                    float shadowDistance =
                        Mathf.Min(Mathf.Max(shadowQx, shadowQy), 0f) +
                        Mathf.Sqrt(shadowAx * shadowAx + shadowAy * shadowAy) - rad;

                    if (shadowDistance < 1f)
                    {
                        pixel = global::CustomNPCExample.Utils.WvcIcon.Over(
                            new Color(
                                shadowColor.r,
                                shadowColor.g,
                                shadowColor.b,
                                Mathf.Clamp01(0.5f - shadowDistance) * 0.30f),
                            pixel);
                    }

                    // Rotate point back to pill space
                    float dx = x - cx;
                    float dy = y - cy;
                    float px = dx * cos + dy * sin;
                    float py = -dx * sin + dy * cos;

                    // Rounded rect distance
                    float qx = Mathf.Abs(px) - (hw - rad);
                    float qy = Mathf.Abs(py) - (hh - rad);
                    float ax = Mathf.Max(qx, 0f);
                    float ay = Mathf.Max(qy, 0f);
                    float d = Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(ax * ax + ay * ay) - rad;

                    if (d <= 1.0f)
                    {
                        float alpha = Mathf.Clamp01(0.5f - d);

                        // Pill surface shading (top-to-bottom cylindrical gradient)
                        float v = Mathf.Clamp01((py + hh) / (hh * 2f));
                        Color c = Color.Lerp(pillColor, pillShadow, v * 0.55f);

                        // Score lines (Xanax bar has 3 score lines dividing it into 4 equal segments)
                        // Segments are centered at: -hw/2, 0, +hw/2 approx.
                        float[] scores = { -hw * 0.5f, 0f, hw * 0.5f };
                        foreach (float s in scores)
                        {
                            float distScore = px - s;
                            if (Mathf.Abs(distScore) <= 2.2f && Mathf.Abs(py) < hh - 3f)
                            {
                                if (distScore < 0f)
                                    c = Color.Lerp(c, grooveColor, 0.70f);
                                else
                                    c = Color.Lerp(c, grooveHighlight, 0.60f);
                            }
                        }

                        // Outline blend
                        if (d > -1.6f)
                        {
                            c = Color.Lerp(c, outlineColor, Mathf.Clamp01((d + 1.6f) / 1.6f));
                        }

                        c.a = alpha;
                        pixel = global::CustomNPCExample.Utils.WvcIcon.Over(c, pixel);
                    }

                    colors[y * FinalSize + x] = pixel;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            UnityEngine.Object.DontDestroyOnLoad(tex);

            _xanaxIcon = Sprite.Create(
                tex,
                new Rect(0f, 0f, FinalSize, FinalSize),
                new Vector2(0.5f, 0.5f),
                100f
            );
            _xanaxIcon.name = "WVC_Xanax_Pill_Icon";
            UnityEngine.Object.DontDestroyOnLoad(_xanaxIcon);

            return _xanaxIcon;
        }

    }
}