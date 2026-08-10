using System;
using System.Reflection;
using MelonLoader;

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
                _harmony = new HarmonyLib.Harmony(
                    "westvilleconnection.mdma.playerconsume"
                );

                Type playerType = Type.GetType(
                    "Il2CppScheduleOne.PlayerScripts.Player, Assembly-CSharp",
                    false
                );

                if (playerType == null)
                {
                    MelonLogger.Error(
                        "[MDMA Patch] Player type was not found."
                    );

                    return;
                }

                MethodInfo playerPrefix =
                    typeof(MDMAConsumptionPatch).GetMethod(
                        nameof(PrefixPlayerConsumption),
                        BindingFlags.Static |
                        BindingFlags.Public
                    );

                if (playerPrefix == null)
                {
                    MelonLogger.Error(
                        "[MDMA Patch] Player prefix was not found."
                    );

                    return;
                }

                // Patch all player product consumption methods we know from your dump
                string[] playerConsumeMethods =
                {
                    "ConsumeProductInternal",
                    "ConsumeProduct",
                    "ReceiveConsumeProduct"
                };

                foreach (string methodName in playerConsumeMethods)
                {
                    MethodInfo target = playerType.GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                    if (target == null)
                    {
                        MelonLogger.Msg(
                            $"[MDMA Patch] Player.{methodName} not found."
                        );

                        continue;
                    }

                    try
                    {
                        _harmony.Patch(
                            target,
                            prefix: new HarmonyLib.HarmonyMethod(playerPrefix)
                        );

                        _playerMethodsPatched++;

                        MelonLogger.Msg(
                            $"[MDMA Patch] Hooked Player.{methodName}."
                        );
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning(
                            $"[MDMA Patch] Could not hook Player.{methodName}: {ex.Message}"
                        );
                    }
                }

                // Capture player instance on Awake
                MethodInfo playerAwake = playerType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                MethodInfo capturePostfix =
                    typeof(MDMAConsumptionPatch).GetMethod(
                        nameof(CapturePlayerPostfix),
                        BindingFlags.Static |
                        BindingFlags.Public
                    );

                if (playerAwake != null && capturePostfix != null)
                {
                    try
                    {
                        _harmony.Patch(
                            playerAwake,
                            postfix: new HarmonyLib.HarmonyMethod(capturePostfix)
                        );

                        MelonLogger.Msg(
                            "[MDMA Patch] Hooked Player.Awake for player capture."
                        );
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning(
                            "[MDMA Patch] Player capture hook failed: " +
                            ex.Message
                        );
                    }
                }

                // Patch cuke drink so it clears custom MDMA effects
                ApplyCukePatch();

                _patchApplied = true;

                MelonLogger.Msg(
                    $"[MDMA Patch] Consumption setup complete. " +
                    $"Player methods patched: {_playerMethodsPatched}"
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[MDMA Patch] ApplyPatch failed: " + ex
                );
            }
        }

        public static void CapturePlayerPostfix(object __instance)
        {
            if (__instance == null)
                return;

            if (_player == null)
            {
                _player = __instance;
                MDMAEyeEffect.SetPlayer(__instance);

                MelonLogger.Msg(
                    "[MDMA Patch] Local player reference captured."
                );
            }
        }

        /// <summary>
        /// Harmony prefix for Player consumption methods.
        /// __0 is usually ProductItemInstance.
        /// </summary>
        public static void PrefixPlayerConsumption(
            object __instance,
            object __0
        )
        {
            try
            {
                if (__instance != null && _player == null)
                {
                    _player = __instance;
                    MDMAEyeEffect.SetPlayer(__instance);
                }

                string itemId = ExtractId(__0);

                if (string.IsNullOrEmpty(itemId))
                {
                    itemId = GetCurrentEquippedItemId();
                }

                MelonLogger.Msg(
                    $"[MDMA Patch] Player consumption hook fired. " +
                    $"Item: {itemId ?? "(unknown)"}"
                );

                TryTriggerForId(itemId);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MDMA Patch] Player consumption prefix failed: " +
                    ex.Message
                );
            }
        }

        private static void TryTriggerForId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            string lower = itemId.ToLowerInvariant();

            // Cuke instantly cancels MDMA custom effects
            bool isCuke =
                lower == "cuke" ||
                lower.Contains(":cuke") ||
                lower.Contains("cuke");

            if (isCuke)
            {
                MelonLogger.Msg("[MDMA Patch] Cuke consumed. Clearing MDMA effects.");

                MDMAEffectManager.StopEffect();
                MDMAEyeEffect.StopEyeEffect();
                MDMANpcLoveEyes.Stop();

                return;
            }

            bool isMdma =
                lower.Contains("mdma") ||
                lower.Contains("molly") ||
                lower.Contains("ecstasy") ||
                lower.Contains("westvilleconnection");

            if (!isMdma)
                return;

            // Prevent duplicate triggers from multiple consumption methods
            if (UnityEngine.Time.time - _lastTriggerTime < 0.75f)
            {
                MelonLogger.Msg(
                    "[MDMA Patch] Duplicate consumption event ignored."
                );

                return;
            }

            _lastTriggerTime = UnityEngine.Time.time;

            MelonLogger.Msg(
                "[MDMA Patch] MDMA player consumption confirmed."
            );

            MDMAEffectManager.TriggerEffect();
            MDMAEyeEffect.StartEyeEffect();
            MDMANpcLoveEyes.Start();
        }

        private static void ApplyCukePatch()
        {
            try
            {
                Type cukeType = Type.GetType(
                    "Il2CppScheduleOne.Equipping.Equippable_Cuke, Assembly-CSharp",
                    false
                );

                if (cukeType == null)
                {
                    MelonLogger.Warning(
                        "[MDMA Patch] Equippable_Cuke type not found."
                    );

                    return;
                }

                MethodInfo drinkMethod = cukeType.GetMethod(
                    "Drink",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                MethodInfo prefix =
                    typeof(MDMAConsumptionPatch).GetMethod(
                        nameof(CukeDrinkPrefix),
                        BindingFlags.Static |
                        BindingFlags.Public
                    );

                if (drinkMethod == null || prefix == null)
                {
                    MelonLogger.Warning(
                        "[MDMA Patch] Could not prepare Cuke.Drink patch."
                    );

                    return;
                }

                _harmony.Patch(
                    drinkMethod,
                    prefix: new HarmonyLib.HarmonyMethod(prefix)
                );

                MelonLogger.Msg("[MDMA Patch] Hooked Equippable_Cuke.Drink.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MDMA Patch] Cuke patch failed: " + ex.Message
                );
            }
        }

        public static void CukeDrinkPrefix()
        {
            MelonLogger.Msg("[MDMA Patch] Cuke drank. Clearing MDMA effects.");

            MDMAEffectManager.StopEffect();
            MDMAEyeEffect.StopEyeEffect();
            MDMANpcLoveEyes.Stop();
        }

        private static string GetCurrentEquippedItemId()
        {
            if (_player == null)
            {
                MelonLogger.Msg(
                    "[MDMA Patch] Player reference is not available."
                );

                return null;
            }

            try
            {
                MethodInfo getEquippedItem =
                    _player.GetType().GetMethod(
                        "GetEquippedItem",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (getEquippedItem == null)
                {
                    MelonLogger.Msg(
                        "[MDMA Patch] Player.GetEquippedItem was not found."
                    );

                    return null;
                }

                object item = getEquippedItem.Invoke(_player, null);

                return ExtractId(item);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MDMA Patch] GetEquippedItem failed: " +
                    ex.Message
                );

                return null;
            }
        }

        private static string ExtractId(object source)
        {
            if (source == null)
                return null;

            string directId = TryGetString(
                source,
                new string[]
                {
                    "ID",
                    "Id",
                    "id",
                    "ItemID",
                    "ItemId",
                    "itemID",
                    "itemId",
                    "Name",
                    "name",
                    "ItemName",
                    "itemName"
                }
            );

            if (!string.IsNullOrEmpty(directId))
                return directId;

            object definition = FindMember(
                source,
                new string[]
                {
                    "Definition",
                    "definition",
                    "_definition",
                    "ItemDefinition",
                    "itemDefinition",
                    "ProductDefinition",
                    "productDefinition",
                    "Def",
                    "def"
                }
            );

            if (definition != null)
            {
                string definitionId = TryGetString(
                    definition,
                    new string[]
                    {
                        "ID",
                        "Id",
                        "id",
                        "ItemID",
                        "ItemId",
                        "Name",
                        "name",
                        "ItemName",
                        "itemName"
                    }
                );

                if (!string.IsNullOrEmpty(definitionId))
                    return definitionId;
            }

            return source.ToString();
        }

        private static string TryGetString(
            object obj,
            string[] names
        )
        {
            object value = FindMember(obj, names);
            return value?.ToString();
        }

        private static object FindMember(
            object obj,
            string[] names
        )
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(name, flags);

                    if (property != null)
                    {
                        object value = property.GetValue(obj);

                        if (value != null)
                            return value;
                    }
                }
                catch { }

                try
                {
                    FieldInfo field =
                        type.GetField(name, flags);

                    if (field != null)
                    {
                        object value = field.GetValue(obj);

                        if (value != null)
                            return value;
                    }
                }
                catch { }
            }

            return null;
        }
    }
}