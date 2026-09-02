using System;
using System.Reflection;
using MelonLoader;
using CustomNPCExample.Products.Edibles;

using NativeEQuality = Il2CppScheduleOne.ItemFramework.EQuality;
using NativeItemDefinition = Il2CppScheduleOne.ItemFramework.ItemDefinition;
using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using NativeWeedInstance = Il2CppScheduleOne.Product.WeedInstance;

namespace CustomNPCExample.Products
{
    public static class MDMAConsumptionPatch
    {
        private static HarmonyLib.Harmony _harmony;
        private static object _player;

        private static bool _patchApplied;
        private static int _playerMethodsPatched;

        private static float _lastTriggerTime = -999f;

        public static void ApplyPatch()
        {
            if (_patchApplied)
                return;

            try
            {
                _harmony = new HarmonyLib.Harmony("westvilleconnection.mdma.playerconsume");

                Type playerType = Type.GetType("Il2CppScheduleOne.PlayerScripts.Player, Assembly-CSharp", false);

                if (playerType == null)
                {
                    MelonLogger.Error("[WVC Patch] Player type was not found.");
                    return;
                }

                MethodInfo playerPrefix = typeof(MDMAConsumptionPatch).GetMethod(
                    nameof(PrefixPlayerConsumption), BindingFlags.Static | BindingFlags.Public);

                string[] playerConsumeMethods = { "ConsumeProductInternal", "ConsumeProduct", "ReceiveConsumeProduct" };

                foreach (string methodName in playerConsumeMethods)
                {
                    MethodInfo target = playerType.GetMethod(
                        methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (target == null) continue;

                    try
                    {
                        _harmony.Patch(target, prefix: new HarmonyLib.HarmonyMethod(playerPrefix));
                        _playerMethodsPatched++;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[WVC Patch] Could not hook Player.{methodName}: {ex.Message}");
                    }
                }

                MethodInfo playerAwake = playerType.GetMethod(
                    "Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                MethodInfo capturePostfix = typeof(MDMAConsumptionPatch).GetMethod(
                    nameof(CapturePlayerPostfix), BindingFlags.Static | BindingFlags.Public);

                if (playerAwake != null && capturePostfix != null)
                    _harmony.Patch(playerAwake, postfix: new HarmonyLib.HarmonyMethod(capturePostfix));

                _patchApplied = true;
                MelonLogger.Msg($"[WVC Patch] Consumption setup complete. Hooked {_playerMethodsPatched} methods.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Patch] ApplyPatch failed: " + ex);
            }
        }

        public static void CapturePlayerPostfix(object __instance)
        {
            if (__instance == null) return;

            if (_player == null)
            {
                _player = __instance;
                MDMAEyeEffect.SetPlayer(__instance);
                MelonLogger.Msg("[WVC Patch] Local player reference captured.");
            }
        }

        public static void PrefixPlayerConsumption(object __instance, object __0)
        {
            try
            {
                if (__instance != null && _player == null)
                {
                    _player = __instance;
                    MDMAEyeEffect.SetPlayer(__instance);
                }

                string itemId = ExtractId(__0);
                if (string.IsNullOrEmpty(itemId)) itemId = GetCurrentEquippedItemId();

                MelonLogger.Msg($"[WVC Patch] Player consumed item: {itemId ?? "(unknown)"}");
                TryTriggerForId(itemId);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Patch] Player consumption prefix failed: " + ex.Message);
            }
        }

        private static void TryTriggerForId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            string lower = itemId.ToLowerInvariant();

            // 1. CUKE (Sobriety check for real in-game Cuke drinks)
            bool isCuke = lower == "cuke" || lower.Contains(":cuke") || lower.Contains("cuke");

            if (isCuke)
            {
                MelonLogger.Msg("[WVC Patch] Cuke consumed. Clearing all effects.");
                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();
                return;
            }

            // 2. DMT
            bool isDmt = lower == DMT.ProductId.ToLowerInvariant() || lower.Contains("dmt") || lower.Contains("dim");

            if (isDmt)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] DMT consumed. Activating visual sweep & walls.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();

                DMTEffectManager.TriggerEffect();
                return;
            }

            // 3. THC GUMMIES
            bool isGummy = lower == THCGummies.ProductId.ToLowerInvariant() || lower.Contains("thc_gummies") || lower.Contains("gummy");

            if (isGummy)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] Gummies consumed. Applying weed FX.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();

                ApplyNativeWeedEffectsToPlayer();
                return;
            }

            // 4. MDMA
            bool isMdma = lower == MDMA.ProductId.ToLowerInvariant() || (lower.Contains("mdma") && !isGummy) || (lower.Contains("molly") && !isGummy);

            if (isMdma)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] MDMA consumption confirmed.");

                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();

                MDMAEffectManager.TriggerEffect();
                MDMAEyeEffect.StartEyeEffect();
                MDMANpcLoveEyes.Start();
                return;
            }

            // 5. BROWNIE (Edible)
            bool isBrownie = lower == Brownie.ProductId.ToLowerInvariant() || lower.Contains("brownie");

            if (isBrownie)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] Brownie consumed. Applying warm edible high.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();

                BrownieEffectManager.TriggerEffect();
                return;
            }

            // 6. COOKIE (Edible)
            bool isCookie = lower == THCCookie.ProductId.ToLowerInvariant() || lower.Contains("cookie");

            if (isCookie)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] THC Cookie consumed. Applying cozy cookie high.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                VapeCartEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();

                THCCookieEffectManager.TriggerEffect();
                return;
            }

            // 7. VAPE CART
            bool isCart = lower == VapeCartProduct.ProductId.ToLowerInvariant() ||
                          lower == VapeCartProduct.ProductId_Premium.ToLowerInvariant() ||
                          lower == VapeCartProduct.ProductId_Heavenly.ToLowerInvariant() ||
                          lower.Contains("vape_cart") ||
                          lower.Contains("vapecart");

            if (isCart)
            {
                if (UnityEngine.Time.time - _lastTriggerTime < 0.75f) return;
                _lastTriggerTime = UnityEngine.Time.time;

                MelonLogger.Msg("[WVC Patch] Vape Cart consumed. Applying airy buzz.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();
                THCGummyEffectManager.StopEffect();
                DMTEffectManager.StopEffect();
                BrownieEffectManager.StopEffect();
                THCCookieEffectManager.StopEffect();
                ClearNativeWeedEffectsFromPlayer();

                VapeCartEffectManager.TriggerEffect();
                return;
            }
        }

        // ============================================================
        // Native Weed FX
        // ============================================================

        private static void ApplyNativeWeedEffectsToPlayer()
        {
            try
            {
                NativePlayer player = ResolvePlayer();
                if (player == null) return;

                NativeItemDefinition weedDef = FindNativeWeedDefinition();
                if (weedDef == null) return;

                var weedInstance = new NativeWeedInstance(weedDef, 1, (NativeEQuality)1, null);
                weedInstance.ApplyEffectsToPlayer(player);
            }
            catch { }
        }

        private static void ClearNativeWeedEffectsFromPlayer()
        {
            try
            {
                NativePlayer player = ResolvePlayer();
                NativeItemDefinition weedDef = FindNativeWeedDefinition();
                if (player == null || weedDef == null) return;

                var weedInstance = new NativeWeedInstance(weedDef, 1, (NativeEQuality)1, null);
                weedInstance.ClearEffectsFromPlayer(player);
            }
            catch { }
        }

        private static NativePlayer ResolvePlayer()
        {
            if (_player is NativePlayer captured && captured != null) return captured;
            try { return NativePlayer.Local; } catch { return null; }
        }

        private static NativeItemDefinition FindNativeWeedDefinition()
        {
            string[] strainIds = { "ogkush", "sourdiesel", "greencrack", "granddaddypurple" };
            foreach (string id in strainIds)
            {
                try
                {
                    object wrapper = S1API.Items.ItemManager.GetDefinition(id);
                    if (wrapper == null) continue;
                    NativeItemDefinition def = UnwrapDefinition(wrapper);
                    if (def != null) return def;
                }
                catch { }
            }
            return null;
        }

        private static NativeItemDefinition UnwrapDefinition(object wrapper)
        {
            if (wrapper == null) return null;
            if (wrapper is NativeItemDefinition already) return already;

            try
            {
                IntPtr rawPointer = IntPtr.Zero;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                string[] pointerNames = { "Pointer", "NativePointer", "ObjectPointer", "_pointer" };
                Type t = wrapper.GetType();

                foreach (string name in pointerNames)
                {
                    PropertyInfo prop = t.GetProperty(name, flags);
                    if (prop != null && prop.PropertyType == typeof(IntPtr))
                    {
                        rawPointer = (IntPtr)prop.GetValue(wrapper);
                        if (rawPointer != IntPtr.Zero) break;
                    }

                    FieldInfo field = t.GetField(name, flags);
                    if (field != null && field.FieldType == typeof(IntPtr))
                    {
                        rawPointer = (IntPtr)field.GetValue(wrapper);
                        if (rawPointer != IntPtr.Zero) break;
                    }
                }

                if (rawPointer == IntPtr.Zero) return null;
                return new NativeItemDefinition(rawPointer);
            }
            catch { return null; }
        }

        // ============================================================
        // ID Extraction Helpers
        // ============================================================

        private static string GetCurrentEquippedItemId()
        {
            if (_player == null) return null;
            try
            {
                MethodInfo getEquippedItem = _player.GetType().GetMethod("GetEquippedItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getEquippedItem == null) return null;
                object item = getEquippedItem.Invoke(_player, null);
                return ExtractId(item);
            }
            catch { return null; }
        }

        private static string ExtractId(object source)
        {
            if (source == null) return null;
            string directId = TryGetString(source, new string[] { "ID", "Id", "id", "ItemID", "ItemId", "itemID", "itemId", "Name", "name", "ItemName", "itemName" });
            if (!string.IsNullOrEmpty(directId)) return directId;

            object definition = FindMember(source, new string[] { "Definition", "definition", "_definition", "ItemDefinition", "itemDefinition", "ProductDefinition", "productDefinition", "Def", "def" });
            if (definition != null)
            {
                string definitionId = TryGetString(definition, new string[] { "ID", "Id", "id", "ItemID", "ItemId", "Name", "name", "ItemName", "itemName" });
                if (!string.IsNullOrEmpty(definitionId)) return definitionId;
            }
            return source.ToString();
        }

        private static string TryGetString(object obj, string[] names)
        {
            object value = FindMember(obj, names);
            return value?.ToString();
        }

        private static object FindMember(object obj, string[] names)
        {
            if (obj == null) return null;
            Type type = obj.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(name, flags);
                    if (property != null)
                    {
                        object value = property.GetValue(obj);
                        if (value != null) return value;
                    }
                }
                catch { }

                try
                {
                    FieldInfo field = type.GetField(name, flags);
                    if (field != null)
                    {
                        object value = field.GetValue(obj);
                        if (value != null) return value;
                    }
                }
                catch { }
            }
            return null;
        }
    }
}