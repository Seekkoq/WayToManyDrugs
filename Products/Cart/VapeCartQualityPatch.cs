using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class VapeCartQualityPatch
    {
        private const string HarmonyId = "westvilleconnection.vapecart.quality";

        private static bool _applied;
        private static bool _refreshStarted;
        private static bool _enumLogged;

        // ============================================================
        // PATCH INSTALLATION
        // ============================================================

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                HarmonyLib.Harmony harmony =
                    new HarmonyLib.Harmony(HarmonyId);

                MethodInfo getDefaultInstance =
                    AccessTools.Method(
                        typeof(QualityItemDefinition),
                        nameof(QualityItemDefinition.GetDefaultInstance),
                        new Type[] { typeof(int) });


                harmony.Patch(
                    getDefaultInstance,
                    postfix: new HarmonyMethod(
                        typeof(VapeCartQualityPatch),
                        nameof(GetDefaultInstancePostfix)));


                _applied = true;

                LogNativeQualityEnum();

            }
            catch (Exception ex)
            {
            }
        }

        // ============================================================
        // INSTANCE PATCHES
        // ============================================================

        private static void GetDefaultInstancePostfix(
            QualityItemDefinition __instance,
            ref ItemInstance __result)
        {
            if (__instance == null || __result == null)
                return;

            try
            {
                QualityItemInstance qualityInstance =
                    __result.TryCast<QualityItemInstance>();

                if (qualityInstance == null)
                    return;

                string itemId =
                    GetDefinitionId(__instance);

                StampQuality(
                    qualityInstance,
                    itemId,
                    "GetDefaultInstance");
            }
            catch (Exception ex)
            {
            }
        }

        private static void QualityInstanceConstructorPostfix(
            QualityItemInstance __instance,
            ItemDefinition __0)
        {
            if (__instance == null || __0 == null)
                return;

            try
            {
                string itemId =
                    GetDefinitionId(__0);

                StampQuality(
                    __instance,
                    itemId,
                    "constructor");
            }
            catch (Exception ex)
            {
            }
        }

        private static void StampQuality(
            QualityItemInstance instance,
            string itemId,
            string source)
        {
            if (instance == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (!TryGetCartQuality(
                    itemId,
                    out EQuality desiredQuality))
            {
                return;
            }

            if (instance.Quality == desiredQuality)
                return;

            EQuality previousQuality =
                instance.Quality;

            instance.SetQuality(desiredQuality);

        }

        // ============================================================
        // DEFINITION DEFAULTS
        // ============================================================

        public static void ApplyCartDefinitionDefaults()
        {
            ApplyDefinitionDefault(
                VapeCartProduct.ProductId,
                "Standard");

            ApplyDefinitionDefault(
                VapeCartProduct.ProductId_Premium,
                "Premium");

            ApplyDefinitionDefault(
                VapeCartProduct.ProductId_Heavenly,
                "Heavenly");
        }

        private static void ApplyDefinitionDefault(
            string itemId,
            string qualityName)
        {
            try
            {
                if (!TryResolveQuality(
                        qualityName,
                        out EQuality quality))
                {
                    return;
                }

                ItemDefinition raw =
                    CartItem.GetRawDefinition(itemId);

                if (raw == null)
                {
                    MelonLogger.Warning(
                        $"[WVC Cart Quality] Raw definition unavailable: {itemId}");
                    return;
                }

                QualityItemDefinition qualityDefinition =
                    raw.TryCast<QualityItemDefinition>();

                if (qualityDefinition == null)
                {
                    return;
                }

                if (qualityDefinition.DefaultQuality == quality)
                    return;

                EQuality previous =
                    qualityDefinition.DefaultQuality;

                qualityDefinition.DefaultQuality =
                    quality;

            }
            catch (Exception ex)
            {
            }
        }

        // ============================================================
        // DEFINITION REFRESH
        // ============================================================



        // ============================================================
        // DIAGNOSTICS
        // ============================================================

        private static void DumpOne(
            string itemId,
            string expected)
        {
            try
            {
                ItemDefinition raw =
                    CartItem.GetRawDefinition(itemId);

                if (raw == null)
                {
                    MelonLogger.Warning(
                        $"[WVC Cart Dump] {itemId}: raw definition NULL");

                    return;
                }

                QualityItemDefinition q =
                    raw.TryCast<QualityItemDefinition>();

                string nativeId =
                    GetDefinitionId(raw);

                string nativeType;

                try
                {
                    nativeType =
                        raw.GetType().FullName ??
                        raw.GetType().Name;
                }
                catch
                {
                    nativeType = "unknown";
                }

                string defaultQuality =
                    q != null
                        ? q.DefaultQuality.ToString()
                        : "n/a";

                MelonLogger.Msg(
                    $"[WVC Cart Dump] {itemId} " +
                    $"| nativeID='{nativeId ?? "NULL"}' " +
                    $"| type={nativeType} " +
                    $"| isQualityDef={q != null} " +
                    $"| DefaultQuality={defaultQuality} " +
                    $"| expected={expected}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    $"[WVC Cart Dump] {itemId} failed: " +
                    ex.Message);
            }
        }

        // ============================================================
        // CART -> QUALITY MAPPING
        // ============================================================

        private static bool TryGetCartQuality(
            string itemId,
            out EQuality quality)
        {
            quality = default;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            string id =
                itemId
                    .Replace('\\', '/')
                    .Trim()
                    .ToLowerInvariant();

            // Most specific first so "vape_cart_heavenly"
            // cannot fall into generic vape_cart.

            if (id.Equals(
                    VapeCartProduct.ProductId_Heavenly,
                    StringComparison.OrdinalIgnoreCase) ||
                id.Contains("vape_cart_heavenly"))
            {
                return TryResolveQuality(
                    "Heavenly",
                    out quality);
            }

            if (id.Equals(
                    VapeCartProduct.ProductId_Premium,
                    StringComparison.OrdinalIgnoreCase) ||
                id.Contains("vape_cart_premium"))
            {
                return TryResolveQuality(
                    "Premium",
                    out quality);
            }

            if (id.Equals(
                    VapeCartProduct.ProductId,
                    StringComparison.OrdinalIgnoreCase) ||
                id.Contains("vape_cart"))
            {
                return TryResolveQuality(
                    "Standard",
                    out quality);
            }

            return false;
        }

        private static bool TryResolveQuality(
            string name,
            out EQuality quality)
        {
            try
            {
                quality =
                    (EQuality)Enum.Parse(
                        typeof(EQuality),
                        name,
                        true);

                return true;
            }
            catch
            {
                quality = default;

                MelonLogger.Warning(
                    $"[WVC Cart Quality] EQuality has no value " +
                    $"named '{name}'.");

                return false;
            }
        }

        // ============================================================
        // DEFINITION ID
        // ============================================================

        private static string GetDefinitionId(
            ItemDefinition definition)
        {
            if (definition == null)
                return null;

            try
            {
                Type type =
                    definition.GetType();

                while (type != null)
                {
                    string[] names =
                    {
                        "ID",
                        "Id",
                        "id",
                        "ItemID",
                        "itemID"
                    };

                    foreach (string name in names)
                    {
                        PropertyInfo prop =
                            type.GetProperty(
                                name,
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.DeclaredOnly);

                        if (prop != null)
                        {
                            try
                            {
                                object value =
                                    prop.GetValue(definition);

                                if (value != null)
                                    return value.ToString();
                            }
                            catch
                            {
                            }
                        }

                        FieldInfo field =
                            type.GetField(
                                name,
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.DeclaredOnly);

                        if (field != null)
                        {
                            try
                            {
                                object value =
                                    field.GetValue(definition);

                                if (value != null)
                                    return value.ToString();
                            }
                            catch
                            {
                            }
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }

        // ============================================================
        // ENUM DIAGNOSTIC
        // ============================================================

        private static void LogNativeQualityEnum()
        {
            if (_enumLogged)
                return;

            _enumLogged = true;

            try
            {
                Array values =
                    Enum.GetValues(typeof(EQuality));

                foreach (object value in values)
                {
                    MelonLogger.Msg(
                        $"[WVC Cart Quality] EQuality.{value} = " +
                        Convert.ToInt32(value));
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cart Quality] Could not dump EQuality: " +
                    ex.Message);
            }
        }
    }
}