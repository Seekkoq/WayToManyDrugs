using System;
using MelonLoader;
using S1API.Console;
using S1API.Growing;
using UnityEngine;

using Il2CppSeedDefinition =
    Il2CppScheduleOne.Growing.SeedDefinition;

using Il2CppFunctionalSeed =
    Il2CppScheduleOne.Growing.FunctionalSeed;

using Il2CppEquippableSeed =
    Il2CppScheduleOne.Equipping.Equippable_Seed;

using Il2CppStorableDef =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products
{
    public static class SalviaSeed
    {
        public const string SeedId = "westvilleconnection:salvia_cutting";

        private static bool _registered;
        private static bool _failureLogged;
        private static GameObject _seedVisualHolder;
        private static Il2CppSeedDefinition _nativeSeed;

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            try
            {
                if (!Salvia.TryRegister())
                    return false;

                Il2CppSeedDefinition donor = SalviaGrowing.FindDonorSeed();
                if (donor == null)
                    return false;

                Il2CppStorableDef salviaProduct =
                    SalviaGrowing.FindNativeProduct(Salvia.ProductId);

                if (salviaProduct == null)
                    return false;

                SalviaGrowing.ApplyProductItemVisuals(salviaProduct);

                if (!SalviaGrowing.BuildPrefabs(donor, salviaProduct))
                    return false;

                Sprite seedIcon = GetOrCreateSalviaSeedIcon();

                _nativeSeed = FindNativeSeedById(SeedId);

                if (_nativeSeed == null)
                {
                    SeedCreator.CreateSeed(
                        id: SeedId,
                        name: "Salvia Seed",
                        description: "A Salvia cutting ready for potting.",
                        stackLimit: donor.StackLimit,
                        functionSeedPrefab: SalviaGrowing.FunctionalSeedPrefab,
                        plantPrefab: SalviaGrowing.PlantPrefab,
                        icon: seedIcon
                    );

                    _nativeSeed = FindNativeSeedById(SeedId);
                }

                if (_nativeSeed == null)
                    return false;

                _nativeSeed.Name = "Salvia Seed";
                _nativeSeed.Description = "A Salvia cutting ready for potting.";
                _nativeSeed.name = "Salvia Seed";
                _nativeSeed.Icon = seedIcon;
                _nativeSeed.BasePurchasePrice = 45f;
                _nativeSeed.ResellMultiplier = donor.ResellMultiplier;

                _nativeSeed.FunctionSeedPrefab =
                    SalviaGrowing.FunctionalSeedPrefab
                        .GetComponent<Il2CppFunctionalSeed>();

                _nativeSeed.PlantPrefab =
                    SalviaGrowing.PlantPrefab
                        .GetComponent<Il2CppScheduleOne.Growing.Plant>();

                SalviaGrowing.LinkSeedDefinition(_nativeSeed);

                ApplyClonedSeedVisuals(_nativeSeed, donor);

                ConsoleItemAliases.Register("salviaseed", SeedId);
                ConsoleItemAliases.Register("salviacutting", SeedId);

                _registered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Salvia] Seed registered. " +
                    "equippable=" + (_nativeSeed.Equippable != null) +
                    " storedItem=" + (_nativeSeed.StoredItem != null) +
                    " functionSeedPrefab=" + (_nativeSeed.FunctionSeedPrefab != null) +
                    " plantPrefab=" + (_nativeSeed.PlantPrefab != null)
                );

                return true;
            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Salvia] Seed registration failed: " + ex);
                }

                return false;
            }
        }

        private static void ApplyClonedSeedVisuals(
            Il2CppSeedDefinition nativeSeed,
            Il2CppSeedDefinition donor)
        {
            EnsureSeedVisualHolder();

            int labelled = 0;
            bool seedAssigned = false;

            if (donor.Equippable != null)
            {
                var eqClone = UnityEngine.Object.Instantiate(
                    donor.Equippable,
                    _seedVisualHolder.transform
                );

                eqClone.name = "WVC_SalviaSeed_Equippable";

                labelled += SalviaGrowing.ApplySalviaLabel(eqClone.gameObject);

                Il2CppEquippableSeed seedEquippable =
                    eqClone.gameObject.GetComponent<Il2CppEquippableSeed>();

                if (seedEquippable != null)
                {
                    seedEquippable.Seed = nativeSeed;
                    seedAssigned = true;
                }

                nativeSeed.Equippable = eqClone;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Salvia] Seed equippable cloned. " +
                    "type=" + eqClone.GetIl2CppType().Name +
                    " seedAssigned=" + seedAssigned
                );
            }
            else
            {

            }

            if (donor.StoredItem != null)
            {
                var storedClone = UnityEngine.Object.Instantiate(
                    donor.StoredItem,
                    _seedVisualHolder.transform
                );

                storedClone.name = "WVC_SalviaSeed_StoredItem";

                labelled += SalviaGrowing.ApplySalviaLabel(storedClone.gameObject);

                nativeSeed.StoredItem = storedClone;
            }
            else
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Salvia] Seed visuals cloned. labelledSlots=" + labelled
            );
        }

        private static void EnsureSeedVisualHolder()
        {
            if (_seedVisualHolder != null)
                return;

            _seedVisualHolder = new GameObject("WVC_SalviaSeedVisualHolder");
            _seedVisualHolder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_seedVisualHolder);
        }

        public static Il2CppSeedDefinition FindNativeSeedById(string id)
        {
            if (_nativeSeed != null && _nativeSeed.ID == id)
                return _nativeSeed;

            foreach (var seed in Resources.FindObjectsOfTypeAll<Il2CppSeedDefinition>())
            {
                if (seed != null && seed.ID == id)
                    return seed;
            }

            return null;
        }

        private static Sprite _customSeedIcon;

        public static Sprite SeedIcon => GetOrCreateSalviaSeedIcon();

        public static Sprite GetOrCreateSalviaSeedIcon()
        {
            if (_customSeedIcon != null)
                return _customSeedIcon;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "WVC_SalviaSeed_Icon_Texture";

            Color32[] pixels = new Color32[size * size];

            void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
            {
                if (x < 0 || x >= size || y < 0 || y >= size) return;
                int row = (size - 1 - y);
                int idx = row * size + x;
                float alpha = a / 255f;
                float invAlpha = 1f - alpha;
                byte prevR = pixels[idx].r;
                byte prevG = pixels[idx].g;
                byte prevB = pixels[idx].b;
                byte prevA = pixels[idx].a;

                pixels[idx] = new Color32(
                    (byte)(r * alpha + prevR * invAlpha),
                    (byte)(g * alpha + prevG * invAlpha),
                    (byte)(b * alpha + prevB * invAlpha),
                    (byte)Math.Min(255, a + prevA)
                );
            }

            int cx = size / 2;
            int tubeWidth = 64;
            int left = cx - tubeWidth / 2;
            int right = cx + tubeWidth / 2;
            int capTop = 32;
            int capHeight = 36;
            int capBottom = capTop + capHeight;
            int tubeBottom = 200;
            int tubeRadius = tubeWidth / 2;

            int labelTop = 78;
            int labelHeight = 65;
            int labelBottom = labelTop + labelHeight;

            int capWidth = tubeWidth + 10;
            int capLeft = cx - capWidth / 2;
            int capRight = cx + capWidth / 2;

            for (int y = capTop; y <= capBottom; y++)
            {
                for (int x = capLeft; x <= capRight; x++)
                {
                    float nx = (float)(x - capLeft) / capWidth;
                    float ridge = Mathf.Sin(nx * Mathf.PI * 12f);
                    float shade = Mathf.Sin(nx * Mathf.PI);
                    int r = (int)(32 * shade + (ridge > 0.2f ? 28 : -8));
                    int g = (int)(32 * shade + (ridge > 0.2f ? 28 : -8));
                    int b = (int)(36 * shade + (ridge > 0.2f ? 32 : -8));
                    if (y == capTop || y == capBottom) { r -= 10; g -= 10; b -= 10; }
                    SetPixel(x, y, (byte)Mathf.Clamp(r, 12, 85), (byte)Mathf.Clamp(g, 12, 85), (byte)Mathf.Clamp(b, 15, 90), 255);
                }
            }

            for (int y = capBottom + 1; y <= tubeBottom + tubeRadius; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    float dx = x - cx;
                    float distFromCenter = Mathf.Abs(dx) / (tubeWidth / 2f);
                    if (distFromCenter > 1f) continue;

                    if (y > tubeBottom)
                    {
                        float dy = y - tubeBottom;
                        float rDist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (rDist > tubeRadius) continue;
                        distFromCenter = rDist / tubeRadius;
                    }

                    if (y >= labelTop && y <= labelBottom)
                    {
                        if (y == labelTop || y == labelBottom || y == labelTop + 1 || y == labelBottom - 1)
                        {
                            SetPixel(x, y, 170, 35, 118, 255);
                        }
                        else
                        {
                            float shade = Mathf.Sin(Mathf.Acos(Mathf.Clamp(dx / (tubeWidth / 2f), -1f, 1f)));
                            float k = 0.65f + 0.35f * shade;
                            byte lr = (byte)Mathf.Clamp((int)(217 * k), 0, 255);
                            byte lg = (byte)Mathf.Clamp((int)(51 * k), 0, 255);
                            byte lb = (byte)Mathf.Clamp((int)(153 * k), 0, 255);
                            SetPixel(x, y, lr, lg, lb, 250);
                        }
                    }
                    else
                    {
                        float edge = Mathf.Pow(distFromCenter, 2.5f);
                        byte gr = (byte)(200 * (1f - edge) + 160 * edge);
                        byte gg = (byte)(220 * (1f - edge) + 190 * edge);
                        byte gb = (byte)(235 * (1f - edge) + 215 * edge);
                        byte ga = (byte)(50 * (1f - edge) + 120 * edge);

                        if (dx > -tubeRadius * 0.75f && dx < -tubeRadius * 0.45f)
                        {
                            ga = (byte)Math.Min(255, ga + 80);
                            gr = (byte)Math.Min(255, gr + 55);
                            gg = (byte)Math.Min(255, gg + 55);
                            gb = (byte)Math.Min(255, gb + 55);
                        }
                        SetPixel(x, y, gr, gg, gb, ga);
                    }
                }
            }

            int seedCx = cx;
            int seedCy = tubeBottom + 2;
            for (int y = seedCy - 15; y <= seedCy + 14; y++)
            {
                for (int x = seedCx - 13; x <= seedCx + 13; x++)
                {
                    float sdx = (x - seedCx) / 9.5f;
                    float sdy = (y - seedCy) / 12f;
                    float distSq = sdx * sdx + sdy * sdy;
                    if (distSq <= 1f)
                    {
                        float d = Mathf.Sqrt(distSq);
                        byte alpha = distSq > 0.85f ? (byte)(255 * (1f - distSq) / 0.15f) : (byte)255;
                        byte sr = (byte)Mathf.Clamp((int)(140 * (1f - d * 0.35f) + (x % 3 == 0 ? 12 : -8)), 40, 190);
                        byte sg = (byte)Mathf.Clamp((int)(100 * (1f - d * 0.35f) + (y % 2 == 0 ? 8 : -6)), 30, 150);
                        byte sb = (byte)Mathf.Clamp((int)(65 * (1f - d * 0.35f)), 20, 100);
                        SetPixel(x, y, sr, sg, sb, alpha);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.DontSave;

            _customSeedIcon = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            _customSeedIcon.name = "WVC_SalviaSeed_Icon";
            _customSeedIcon.hideFlags = HideFlags.DontSave;

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Salvia] Custom seed vial icon generated.");
            return _customSeedIcon;
        }
    }
}
