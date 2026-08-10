using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class MollyCauldronPatch
    {
        private const string SafroleId = "westvilleconnection:ingredients/safrole_oil";
        private const string PmkId = "westvilleconnection:ingredients/pmk_powder";
        private static string MdmaId => MDMA.ProductId;

        private static readonly HarmonyLib.Harmony Harmony = new HarmonyLib.Harmony("westvilleconnection.molly.cauldron");
        private static readonly HashSet<MethodInfo> PatchedMethods = new HashSet<MethodInfo>();
        private static readonly HashSet<int> FlaggedCauldronIds = new HashSet<int>();
        private static readonly Dictionary<int, QualityItemDefinition> SwappedDefinitions = new Dictionary<int, QualityItemDefinition>();

        private static bool _patchApplied;
        public static bool TreatEveryCauldronAsMollyForTesting = true;

        public static void ApplyPatch()
        {
            if (_patchApplied) return;
            try
            {
                PatchCauldron();
                PatchItemSlot();
                _patchApplied = true;
                MelonLogger.Msg("[MollyCauldron] Patch setup complete.");
            }
            catch (Exception ex) { MelonLogger.Error("[MollyCauldron] Patch setup failed: " + ex); }
        }

        private static void PatchCauldron()
        {
            Type type = typeof(Cauldron);
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName || method.Name.Contains("b__")) continue;

                if (method.Name == "HasIngredients" && method.GetParameters().Length == 0)
                {
                    PatchPrefix(method, nameof(PrefixHasIngredients));
                    continue;
                }
                if (method.Name == "GetMainInputs" && method.GetParameters().Length == 4)
                {
                    PatchPrefix(method, nameof(PrefixGetMainInputs));
                    continue;
                }
                if (method.Name == "RemoveIngredients" && method.GetParameters().Length == 0)
                {
                    PatchPrefix(method, nameof(PrefixRemoveIngredients));
                    continue;
                }
                if (method.Name == "ButtonClicked" && method.GetParameters().Length == 1)
                {
                    PatchPrefix(method, nameof(PrefixButtonClicked));
                    continue;
                }
                if (method.Name.Contains("FinishCookOperation") && method.GetParameters().Length == 0)
                {
                    PatchPostfix(method, nameof(PostfixFinishCookOperation));
                }
            }
        }

        private static void PatchItemSlot()
        {
            Type type = typeof(ItemSlot);
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName) continue;

                if ((method.Name == "DoesItemMatchHardFilters" || method.Name == "DoesItemMatchPlayerFilters"))
                {
                    PatchPrefix(method, nameof(PrefixItemFilter));
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    PatchPrefix(method, nameof(PrefixCapacity));
                }
            }
        }

        private static void PatchPrefix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method)) return;
            Harmony.Patch(method, prefix: new HarmonyLib.HarmonyMethod(typeof(MollyCauldronPatch), handler));
            PatchedMethods.Add(method);
        }

        private static void PatchPostfix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method)) return;
            Harmony.Patch(method, postfix: new HarmonyLib.HarmonyMethod(typeof(MollyCauldronPatch), handler));
            PatchedMethods.Add(method);
        }

        public static void MarkAsCustomMollyCauldron(object cauldron)
        {
            Component component = cauldron as Component;
            if (component == null) return;
            FlaggedCauldronIds.Add(component.GetInstanceID());
        }

        public static bool IsCustomMollyCauldron(object cauldron)
        {
            if (cauldron == null) return false;
            if (TreatEveryCauldronAsMollyForTesting) return true;
            Component component = cauldron as Component;
            return component != null && FlaggedCauldronIds.Contains(component.GetInstanceID());
        }

        private static Cauldron AsCauldron(object instance)
        {
            Il2CppSystem.Object obj = instance as Il2CppSystem.Object;
            return obj?.TryCast<Cauldron>();
        }

        // ============================================================
        // Slot filters
        // ============================================================

        public static bool PrefixItemFilter(object[] __args, ref bool __result)
        {
            try
            {
                string id = FindItemId(__args);
                if (!IsPrecursor(id)) return true;
                __result = true;
                return false;
            }
            catch { return true; }
        }

        public static bool PrefixCapacity(object __instance, object[] __args, ref int __result)
        {
            try
            {
                string id = FindItemId(__args);
                if (!IsPrecursor(id)) return true;
                ItemSlot slot = AsSlot(__instance);
                int held = (slot == null) ? 0 : slot.Quantity;
                __result = Math.Max(0, 20 - held);
                return false;
            }
            catch { return true; }
        }

        private static ItemSlot AsSlot(object instance)
        {
            Il2CppSystem.Object obj = instance as Il2CppSystem.Object;
            return obj?.TryCast<ItemSlot>();
        }

        // ============================================================
        // Start button - FIXED (Does not crash anymore)
        // ============================================================

        public static void PrefixButtonClicked(object __instance)
        {
            Cauldron cauldron = AsCauldron(__instance);
            if (cauldron == null || !IsCustomMollyCauldron(cauldron)) return;

            Recipe recipe = ReadRecipe(cauldron);
            if (recipe.HasSafrole && recipe.HasPmk)
            {
                MelonLogger.Msg("[MollyCauldron] Custom recipe detected. Using native visuals for minigame.");
            }
        }

        // ============================================================
        // Recipe validation
        // ============================================================

        public static bool PrefixHasIngredients(object __instance, ref bool __result)
        {
            if (!IsCustomMollyCauldron(__instance)) return true;
            Cauldron cauldron = __instance as Cauldron;
            if (cauldron == null) return true;

            Recipe recipe = ReadRecipe(cauldron);
            if (!recipe.HasAny) return true;

            __result = recipe.HasSafrole && recipe.HasPmk;
            return false;
        }

        public static bool PrefixGetMainInputs(object __instance, ref ItemInstance primaryItem, ref int primaryItemQuantity, ref ItemInstance secondaryItem, ref int secondaryItemQuantity)
        {
            if (!IsCustomMollyCauldron(__instance)) return true;
            Cauldron cauldron = __instance as Cauldron;
            if (cauldron == null) return true;

            Recipe recipe = ReadRecipe(cauldron);
            if (!recipe.HasSafrole || !recipe.HasPmk) return true;

            primaryItem = recipe.SafroleItem;
            primaryItemQuantity = 1;
            secondaryItem = recipe.PmkItem;
            secondaryItemQuantity = 1;

            return false;
        }

        // ============================================================
        // Consumption + output definition swap
        // ============================================================

        public static bool PrefixRemoveIngredients(object __instance, ref EQuality __result)
        {
            if (!IsCustomMollyCauldron(__instance)) return true;
            Cauldron cauldron = __instance as Cauldron;
            if (cauldron == null) return true;

            Recipe recipe = ReadRecipe(cauldron);
            if (!recipe.HasSafrole || !recipe.HasPmk) return true;

            MelonLogger.Msg("[MollyCauldron] Consuming Safrole Oil and PMK Powder.");

            ConsumeSlot(cauldron, recipe.SafroleIndex, 1);
            ConsumeSlot(cauldron, recipe.PmkIndex, 1);

            SwapOutputDefinition(cauldron);
            __result = EQuality.Standard;

            return false;
        }

        private static bool SwapOutputDefinition(Cauldron cauldron)
        {
            if (cauldron == null) return false;
            int id = cauldron.GetInstanceID();
            if (SwappedDefinitions.ContainsKey(id)) return true;

            try
            {
                S1API.Items.ItemDefinition wrapper = S1API.Items.ItemManager.GetDefinition(MdmaId);
                if (wrapper == null) return false;

                object rawObject = GetMember(wrapper, "S1ItemDefinition");
                ItemDefinition rawDefinition = rawObject as ItemDefinition;
                if (rawDefinition == null) return false;

                QualityItemDefinition mdmaQuality = rawDefinition as QualityItemDefinition ?? new QualityItemDefinition(rawDefinition.Pointer);

                SwappedDefinitions[id] = cauldron.CocaineBaseDefinition;
                cauldron.CocaineBaseDefinition = mdmaQuality;
                return true;
            }
            catch { return false; }
        }

        public static void PostfixFinishCookOperation(object __instance)
        {
            Cauldron cauldron = AsCauldron(__instance);
            if (cauldron == null) return;

            int id = cauldron.GetInstanceID();
            if (!SwappedDefinitions.TryGetValue(id, out QualityItemDefinition original)) return;

            cauldron.CocaineBaseDefinition = original;
            SwappedDefinitions.Remove(id);
            MelonLogger.Msg("[MollyCauldron] CocaineBaseDefinition restored.");
        }

        // ============================================================
        // Recipe reading & Utility
        // ============================================================

        public struct Recipe
        {
            public bool HasSafrole;
            public bool HasPmk;
            public ItemInstance SafroleItem;
            public ItemInstance PmkItem;
            public int SafroleIndex;
            public int PmkIndex;
            public bool HasAny => HasSafrole || HasPmk;
        }

        private static Recipe ReadRecipe(Cauldron cauldron)
        {
            Recipe recipe = new Recipe { SafroleIndex = -1, PmkIndex = -1 };
            try
            {
                var slots = cauldron.ItemSlots;
                if (slots == null) return recipe;

                for (int i = 0; i < slots.Count; i++)
                {
                    ItemSlot slot = slots[i];
                    if (slot == null || slot.ItemInstance == null) continue;

                    string id = GetDefinitionId(slot.ItemInstance);
                    if (Matches(id, SafroleId, "safrole"))
                    {
                        recipe.HasSafrole = true;
                        recipe.SafroleItem = slot.ItemInstance;
                        recipe.SafroleIndex = i;
                    }
                    else if (Matches(id, PmkId, "pmk"))
                    {
                        recipe.HasPmk = true;
                        recipe.PmkItem = slot.ItemInstance;
                        recipe.PmkIndex = i;
                    }
                }
            }
            catch { }
            return recipe;
        }

        private static void ConsumeSlot(Cauldron cauldron, int index, int amount)
        {
            if (index < 0 || amount <= 0) return;
            try
            {
                var slots = cauldron.ItemSlots;
                if (slots != null && index < slots.Count && slots[index] != null)
                {
                    int remaining = Math.Max(0, slots[index].Quantity - amount);
                    cauldron.SetItemSlotQuantity(index, remaining);
                }
            }
            catch { }
        }

        private static string FindItemId(object[] args)
        {
            if (args == null) return null;
            foreach (object arg in args)
            {
                if (arg == null || arg is string) continue;
                string id = GetDefinitionId(arg);
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return null;
        }

        private static string GetDefinitionId(object value)
        {
            if (value == null) return null;
            object definition = GetMember(value, "Definition");
            if (definition != null && !ReferenceEquals(definition, value))
            {
                string nested = GetDirectId(definition);
                if (!string.IsNullOrEmpty(nested)) return nested;
            }
            return GetDirectId(value);
        }

        private static string GetDirectId(object value)
        {
            if (value == null) return null;
            string[] names = { "ID", "Id", "ItemID", "Name" };
            foreach (string name in names)
            {
                object member = GetMember(value, name);
                if (member != null && !string.IsNullOrEmpty(member.ToString())) return member.ToString();
            }
            return null;
        }

        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            try
            {
                Type type = target.GetType();
                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (property != null) return property.GetValue(target);

                    FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(target);

                    type = type.BaseType;
                }
            }
            catch { }
            return null;
        }

        private static bool IsPrecursor(string id) => Matches(id, SafroleId, "safrole") || Matches(id, PmkId, "pmk");
        private static bool Matches(string actual, string fullId, string shortId)
        {
            if (string.IsNullOrWhiteSpace(actual)) return false;
            string normalized = actual.ToLowerInvariant();
            return normalized == fullId.ToLowerInvariant() || normalized.Contains(shortId);
        }

        public static void DumpCauldronState() { /* Simplified/Removed for space */ }
    }
}