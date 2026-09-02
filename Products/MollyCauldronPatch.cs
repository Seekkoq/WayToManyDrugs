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
        // ============================================================
        // Item IDs
        // ============================================================

        private const string SafroleId =
            "westvilleconnection:ingredients/safrole_oil";

        private const string PmkId =
            "westvilleconnection:ingredients/pmk_powder";

        private const string PmkRefinedId =
            "westvilleconnection:ingredients/pmk_refined";

        private const string PmkLabGradeId =
            "westvilleconnection:ingredients/pmk_lab_grade";

        private const int MdmaBatchQuantity = 10;

        private static string MdmaId => MDMA.ProductId;

        // ============================================================
        // Patch state
        // ============================================================

        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony("westvilleconnection.molly.cauldron");

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> FlaggedCauldronIds =
            new HashSet<int>();

        private static readonly Dictionary<int, QualityItemDefinition>
            SwappedDefinitions =
                new Dictionary<int, QualityItemDefinition>();

        /*
         * Stores ItemSlot.Pointer values for cauldron output slots that are
         * about to receive custom MDMA output.
         */
        private static readonly HashSet<IntPtr> PendingMdmaOutputSlots =
            new HashSet<IntPtr>();

        private static bool _patchApplied;
        private static bool _settingStoredItemInternally;

        public static bool TreatEveryCauldronAsMollyForTesting = true;

        private enum PmkGrade
        {
            None = 0,
            Standard = 1,
            Refined = 2,
            LabGrade = 3
        }

        private struct Recipe
        {
            public bool HasSafrole;
            public bool HasPmk;

            public PmkGrade PmkGrade;

            public ItemInstance SafroleItem;
            public ItemInstance PmkItem;

            public int SafroleIndex;
            public int PmkIndex;

            public bool HasAny => HasSafrole || HasPmk;
        }

        // ============================================================
        // Patch setup
        // ============================================================

        public static void ApplyPatch()
        {
            if (_patchApplied)
                return;

            try
            {
                PatchCauldron();
                PatchItemSlot();

                _patchApplied = true;

                MelonLogger.Msg("[MollyCauldron] Patch setup complete.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[MollyCauldron] Patch setup failed: " + ex
                );
            }
        }

        private static void PatchCauldron()
        {
            Type type = typeof(Cauldron);

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (MethodInfo method in methods)
            {
                if (method == null ||
                    method.IsSpecialName ||
                    method.Name.Contains("b__"))
                {
                    continue;
                }

                if (method.Name == "HasIngredients" &&
                    method.GetParameters().Length == 0)
                {
                    PatchPrefix(method, nameof(PrefixHasIngredients));
                    continue;
                }

                if (method.Name == "GetMainInputs" &&
                    method.GetParameters().Length == 4)
                {
                    PatchPrefix(method, nameof(PrefixGetMainInputs));
                    continue;
                }

                if (method.Name == "RemoveIngredients" &&
                    method.GetParameters().Length == 0)
                {
                    PatchPrefix(method, nameof(PrefixRemoveIngredients));
                    continue;
                }

                if (method.Name == "ButtonClicked" &&
                    method.GetParameters().Length == 1)
                {
                    PatchPrefix(method, nameof(PrefixButtonClicked));
                    continue;
                }

                if (method.Name.Contains("FinishCookOperation") &&
                    method.GetParameters().Length == 0)
                {
                    PatchPostfix(
                        method,
                        nameof(PostfixFinishCookOperation)
                    );
                }
            }
        }

        private static void PatchItemSlot()
        {
            Type type = typeof(ItemSlot);

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName)
                    continue;

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    PatchPrefix(method, nameof(PrefixItemFilter));
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    PatchPrefix(method, nameof(PrefixCapacity));
                }
                else if (method.Name == "SetStoredItem")
                {
                    /*
                     * This is the important quantity fix.
                     * Native cauldron output goes through SetStoredItem.
                     */
                    PatchPostfix(method, nameof(PostfixSetStoredItem));
                }
            }
        }

        private static void PatchPrefix(
            MethodInfo method,
            string handler
        )
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(
                    typeof(MollyCauldronPatch),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        private static void PatchPostfix(
            MethodInfo method,
            string handler
        )
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                postfix: new HarmonyLib.HarmonyMethod(
                    typeof(MollyCauldronPatch),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        // ============================================================
        // Cauldron selection
        // ============================================================

        public static void MarkAsCustomMollyCauldron(object cauldron)
        {
            Component component = cauldron as Component;

            if (component == null)
                return;

            FlaggedCauldronIds.Add(component.GetInstanceID());
        }

        public static bool IsCustomMollyCauldron(object cauldron)
        {
            if (cauldron == null)
                return false;

            if (TreatEveryCauldronAsMollyForTesting)
                return true;

            Component component = cauldron as Component;

            return component != null &&
                   FlaggedCauldronIds.Contains(component.GetInstanceID());
        }

        private static Cauldron AsCauldron(object instance)
        {
            Il2CppSystem.Object obj = instance as Il2CppSystem.Object;
            return obj?.TryCast<Cauldron>();
        }

        private static ItemSlot AsSlot(object instance)
        {
            Il2CppSystem.Object obj = instance as Il2CppSystem.Object;
            return obj?.TryCast<ItemSlot>();
        }

        private static ItemInstance AsItemInstance(object instance)
        {
            Il2CppSystem.Object obj = instance as Il2CppSystem.Object;
            return obj?.TryCast<ItemInstance>();
        }

        // ============================================================
        // Slot filter patches
        // ============================================================

        public static bool PrefixItemFilter(
            object[] __args,
            ref bool __result
        )
        {
            try
            {
                string id = FindItemId(__args);

                if (!IsPrecursor(id))
                    return true;

                __result = true;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool PrefixCapacity(
            object __instance,
            object[] __args,
            ref int __result
        )
        {
            try
            {
                string id = FindItemId(__args);

                if (!IsPrecursor(id))
                    return true;

                ItemSlot slot = AsSlot(__instance);

                int held =
                    slot == null
                        ? 0
                        : slot.Quantity;

                __result = Math.Max(0, 20 - held);

                return false;
            }
            catch
            {
                return true;
            }
        }

        // ============================================================
        // Start button
        // ============================================================

        public static void PrefixButtonClicked(object __instance)
        {
            Cauldron cauldron = AsCauldron(__instance);

            if (cauldron == null ||
                !IsCustomMollyCauldron(cauldron))
            {
                return;
            }

            Recipe recipe = ReadRecipe(cauldron);

            if (recipe.HasSafrole && recipe.HasPmk)
            {
                MelonLogger.Msg(
                    "[MollyCauldron] MDMA recipe detected. PMK grade: " +
                    recipe.PmkGrade
                );
            }
        }

        // ============================================================
        // Recipe validation
        // ============================================================

        public static bool PrefixHasIngredients(
            object __instance,
            ref bool __result
        )
        {
            if (!IsCustomMollyCauldron(__instance))
                return true;

            Cauldron cauldron = __instance as Cauldron;

            if (cauldron == null)
                return true;

            Recipe recipe = ReadRecipe(cauldron);

            if (!recipe.HasAny)
                return true;

            __result = recipe.HasSafrole && recipe.HasPmk;
            return false;
        }

        public static bool PrefixGetMainInputs(
            object __instance,
            ref ItemInstance primaryItem,
            ref int primaryItemQuantity,
            ref ItemInstance secondaryItem,
            ref int secondaryItemQuantity
        )
        {
            if (!IsCustomMollyCauldron(__instance))
                return true;

            Cauldron cauldron = __instance as Cauldron;

            if (cauldron == null)
                return true;

            Recipe recipe = ReadRecipe(cauldron);

            if (!recipe.HasSafrole || !recipe.HasPmk)
                return true;

            primaryItem = recipe.SafroleItem;
            primaryItemQuantity = 1;

            secondaryItem = recipe.PmkItem;
            secondaryItemQuantity = 1;

            return false;
        }

        // ============================================================
        // Ingredient consumption + quality selection
        // ============================================================

        public static bool PrefixRemoveIngredients(
            object __instance,
            ref EQuality __result
        )
        {
            if (!IsCustomMollyCauldron(__instance))
                return true;

            Cauldron cauldron = __instance as Cauldron;

            if (cauldron == null)
                return true;

            Recipe recipe = ReadRecipe(cauldron);

            if (!recipe.HasSafrole || !recipe.HasPmk)
                return true;

            MelonLogger.Msg(
                "[MollyCauldron] Consuming Safrole Oil + " +
                recipe.PmkGrade +
                " PMK."
            );

            ConsumeSlot(cauldron, recipe.SafroleIndex, 1);
            ConsumeSlot(cauldron, recipe.PmkIndex, 1);

            bool swapped = SwapOutputDefinition(cauldron);

            if (swapped)
            {
                TrackPendingOutputSlot(cauldron);
            }
            else
            {
                MelonLogger.Warning(
                    "[MollyCauldron] Could not swap cauldron output definition to MDMA."
                );
            }

            __result = GetOutputQuality(recipe.PmkGrade);

            MelonLogger.Msg(
                "[MollyCauldron] Cook quality selected: " +
                __result
            );

            return false;
        }

        /*
         * Existing mapping:
         * Standard = 2
         * Premium  = 3
         * Heavenly = 4
         */
        private static EQuality GetOutputQuality(PmkGrade grade)
        {
            switch (grade)
            {
                case PmkGrade.LabGrade:
                    return (EQuality)4;

                case PmkGrade.Refined:
                    return (EQuality)3;

                case PmkGrade.Standard:
                default:
                    return (EQuality)2;
            }
        }

        // ============================================================
        // MDMA output-definition swap
        // ============================================================

        private static bool SwapOutputDefinition(Cauldron cauldron)
        {
            if (cauldron == null)
                return false;

            int cauldronId = cauldron.GetInstanceID();

            if (SwappedDefinitions.ContainsKey(cauldronId))
                return true;

            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(MdmaId);

                if (wrapper == null)
                {
                    MelonLogger.Warning(
                        "[MollyCauldron] MDMA definition wrapper not ready."
                    );

                    return false;
                }

                object rawObject = GetMember(wrapper, "S1ItemDefinition");

                ItemDefinition rawDefinition =
                    rawObject as ItemDefinition;

                if (rawDefinition == null)
                {
                    MelonLogger.Warning(
                        "[MollyCauldron] Could not access raw MDMA definition."
                    );

                    return false;
                }

                QualityItemDefinition mdmaDefinition =
                    rawDefinition as QualityItemDefinition
                    ?? new QualityItemDefinition(rawDefinition.Pointer);

                SwappedDefinitions[cauldronId] =
                    cauldron.CocaineBaseDefinition;

                cauldron.CocaineBaseDefinition =
                    mdmaDefinition;

                MelonLogger.Msg(
                    "[MollyCauldron] Output definition swapped to MDMA."
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[MollyCauldron] SwapOutputDefinition failed: " +
                    ex
                );

                return false;
            }
        }

        private static void TrackPendingOutputSlot(Cauldron cauldron)
        {
            if (cauldron == null)
                return;

            try
            {
                ItemSlot outputSlot = cauldron.OutputSlot;

                if (outputSlot == null)
                {
                    MelonLogger.Warning(
                        "[MollyCauldron] OutputSlot was null; cannot track batch quantity."
                    );

                    return;
                }

                IntPtr slotPointer = outputSlot.Pointer;

                if (slotPointer == IntPtr.Zero)
                {
                    MelonLogger.Warning(
                        "[MollyCauldron] OutputSlot pointer was zero."
                    );

                    return;
                }

                PendingMdmaOutputSlots.Add(slotPointer);

                MelonLogger.Msg(
                    "[MollyCauldron] Tracking pending MDMA output slot."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MollyCauldron] TrackPendingOutputSlot failed: " +
                    ex.Message
                );
            }
        }

        // ============================================================
        // CRITICAL FIX:
        // Force cauldron output item to quantity 10 when SetStoredItem runs
        // ============================================================

        public static void PostfixSetStoredItem(
            object __instance,
            object[] __args
        )
        {
            if (_settingStoredItemInternally)
                return;

            ItemSlot slot = AsSlot(__instance);

            if (slot == null)
                return;

            IntPtr slotPointer = slot.Pointer;

            if (slotPointer == IntPtr.Zero ||
                !PendingMdmaOutputSlots.Contains(slotPointer))
            {
                return;
            }

            if (slot.ItemInstance == null)
                return;

            try
            {
                ItemInstance storedItem = slot.ItemInstance;

                int currentQuantity = slot.Quantity;

                if (currentQuantity == MdmaBatchQuantity)
                {
                    PendingMdmaOutputSlots.Remove(slotPointer);
                    return;
                }

                bool fixedStack =
                    ReplaceStoredItemWithQuantity(
                        slot,
                        storedItem,
                        MdmaBatchQuantity,
                        __args
                    );

                if (fixedStack)
                {
                    MelonLogger.Msg(
                        "[MollyCauldron] Fixed MDMA output quantity: " +
                        currentQuantity +
                        " -> " +
                        MdmaBatchQuantity
                    );
                }
                else
                {
                    MelonLogger.Warning(
                        "[MollyCauldron] Failed to replace MDMA output with quantity " +
                        MdmaBatchQuantity
                    );
                }

                PendingMdmaOutputSlots.Remove(slotPointer);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MollyCauldron] PostfixSetStoredItem failed: " +
                    ex.Message
                );

                PendingMdmaOutputSlots.Remove(slotPointer);
            }
        }

        private static bool ReplaceStoredItemWithQuantity(
            ItemSlot slot,
            ItemInstance originalItem,
            int desiredQuantity,
            object[] originalArgs
        )
        {
            if (slot == null || originalItem == null)
                return false;

            ItemInstance copy =
                CreateCopyWithQuantity(
                    originalItem,
                    desiredQuantity
                );

            if (copy == null)
                copy = originalItem;

            TrySetQuantityOnObject(copy, desiredQuantity);

            bool network =
                ExtractBoolArg(originalArgs, false);

            try
            {
                _settingStoredItemInternally = true;

                bool invoked =
                    InvokeSetStoredItem(
                        slot,
                        copy,
                        network
                    );

                _settingStoredItemInternally = false;

                if (invoked)
                    return true;
            }
            catch
            {
                _settingStoredItemInternally = false;
            }

            // Fallback: try direct mutation if SetStoredItem reflection failed.
            bool changedItem =
                TrySetQuantityOnObject(
                    originalItem,
                    desiredQuantity
                );

            bool changedSlot =
                TrySetQuantityOnObject(
                    slot,
                    desiredQuantity
                );

            return changedItem || changedSlot;
        }

        private static ItemInstance CreateCopyWithQuantity(
            ItemInstance item,
            int quantity
        )
        {
            if (item == null)
                return null;

            try
            {
                MethodInfo[] methods =
                    item.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "GetCopy")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1)
                        continue;

                    object result =
                        method.Invoke(
                            item,
                            new object[] { quantity }
                        );

                    ItemInstance copy =
                        result as ItemInstance;

                    if (copy != null)
                        return copy;

                    copy = AsItemInstance(result);

                    if (copy != null)
                        return copy;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool InvokeSetStoredItem(
            ItemSlot slot,
            ItemInstance item,
            bool network
        )
        {
            if (slot == null || item == null)
                return false;

            try
            {
                MethodInfo[] methods =
                    slot.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "SetStoredItem")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 2)
                        continue;

                    method.Invoke(
                        slot,
                        new object[]
                        {
                            item,
                            network
                        }
                    );

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TrySetQuantityOnObject(
            object target,
            int quantity
        )
        {
            if (target == null)
                return false;

            bool changed = false;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo prop =
                        type.GetProperty(
                            "Quantity",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(target, quantity);
                        changed = true;
                    }

                    FieldInfo field =
                        type.GetField(
                            "Quantity",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                    {
                        field.SetValue(target, quantity);
                        changed = true;
                    }

                    MethodInfo setQuantity =
                        type.GetMethod(
                            "SetQuantity",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (setQuantity != null)
                    {
                        ParameterInfo[] parameters =
                            setQuantity.GetParameters();

                        if (parameters.Length == 1)
                        {
                            setQuantity.Invoke(
                                target,
                                new object[] { quantity }
                            );

                            changed = true;
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return changed;
        }

        private static bool ExtractBoolArg(
            object[] args,
            bool fallback
        )
        {
            if (args == null)
                return fallback;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is bool b)
                    return b;
            }

            return fallback;
        }

        // ============================================================
        // Finish cooking cleanup
        // ============================================================

        public static void PostfixFinishCookOperation(object __instance)
        {
            Cauldron cauldron = AsCauldron(__instance);

            if (cauldron == null)
                return;

            int cauldronId = cauldron.GetInstanceID();

            if (!SwappedDefinitions.TryGetValue(
                    cauldronId,
                    out QualityItemDefinition originalDefinition
                ))
            {
                return;
            }

            try
            {
                ItemSlot outputSlot = cauldron.OutputSlot;

                if (outputSlot != null &&
                    outputSlot.ItemInstance != null &&
                    outputSlot.Quantity != MdmaBatchQuantity)
                {
                    /*
                     * Fallback only. Usually PostfixSetStoredItem already fixed it.
                     */
                    ReplaceStoredItemWithQuantity(
                        outputSlot,
                        outputSlot.ItemInstance,
                        MdmaBatchQuantity,
                        null
                    );

                    MelonLogger.Msg(
                        "[MollyCauldron] Fallback fixed output quantity to " +
                        MdmaBatchQuantity
                    );
                }

                if (outputSlot != null)
                    PendingMdmaOutputSlots.Remove(outputSlot.Pointer);
            }
            catch
            {
            }

            cauldron.CocaineBaseDefinition =
                originalDefinition;

            SwappedDefinitions.Remove(cauldronId);

            MelonLogger.Msg(
                "[MollyCauldron] CocaineBaseDefinition restored."
            );
        }

        // ============================================================
        // Recipe reading
        // ============================================================

        private static Recipe ReadRecipe(Cauldron cauldron)
        {
            Recipe recipe = new Recipe
            {
                SafroleIndex = -1,
                PmkIndex = -1,
                PmkGrade = PmkGrade.None
            };

            try
            {
                var slots = cauldron.ItemSlots;

                if (slots == null)
                    return recipe;

                for (int i = 0; i < slots.Count; i++)
                {
                    ItemSlot slot = slots[i];

                    if (slot == null ||
                        slot.ItemInstance == null)
                    {
                        continue;
                    }

                    string itemId =
                        GetDefinitionId(slot.ItemInstance);

                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    string lower =
                        itemId.ToLowerInvariant();

                    if (Matches(lower, SafroleId, "safrole"))
                    {
                        recipe.HasSafrole = true;
                        recipe.SafroleItem = slot.ItemInstance;
                        recipe.SafroleIndex = i;
                        continue;
                    }

                    /*
                     * IMPORTANT:
                     * Specific PMK grades must be checked before generic PMK,
                     * because every PMK grade contains "pmk".
                     */

                    if (Matches(lower, PmkLabGradeId, "pmk_lab_grade") ||
                        lower.Contains("lab_grade") ||
                        lower.Contains("lab-grade"))
                    {
                        recipe.HasPmk = true;
                        recipe.PmkItem = slot.ItemInstance;
                        recipe.PmkIndex = i;
                        recipe.PmkGrade = PmkGrade.LabGrade;
                        continue;
                    }

                    if (Matches(lower, PmkRefinedId, "pmk_refined") ||
                        lower.Contains("refined"))
                    {
                        recipe.HasPmk = true;
                        recipe.PmkItem = slot.ItemInstance;
                        recipe.PmkIndex = i;
                        recipe.PmkGrade = PmkGrade.Refined;
                        continue;
                    }

                    if (Matches(lower, PmkId, "pmk_powder") ||
                        lower.Contains("pmk"))
                    {
                        recipe.HasPmk = true;
                        recipe.PmkItem = slot.ItemInstance;
                        recipe.PmkIndex = i;
                        recipe.PmkGrade = PmkGrade.Standard;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MollyCauldron] ReadRecipe failed: " +
                    ex.Message
                );
            }

            return recipe;
        }

        private static void ConsumeSlot(
            Cauldron cauldron,
            int index,
            int amount
        )
        {
            if (cauldron == null ||
                index < 0 ||
                amount <= 0)
            {
                return;
            }

            try
            {
                var slots = cauldron.ItemSlots;

                if (slots == null ||
                    index >= slots.Count ||
                    slots[index] == null)
                {
                    return;
                }

                int remaining =
                    Math.Max(
                        0,
                        slots[index].Quantity - amount
                    );

                cauldron.SetItemSlotQuantity(
                    index,
                    remaining
                );
            }
            catch
            {
            }
        }

        // ============================================================
        // Item ID helpers
        // ============================================================

        private static bool IsPrecursor(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            string lower =
                id.ToLowerInvariant();

            return Matches(lower, SafroleId, "safrole") ||
                   Matches(lower, PmkId, "pmk_powder") ||
                   Matches(lower, PmkRefinedId, "pmk_refined") ||
                   Matches(lower, PmkLabGradeId, "pmk_lab_grade");
        }

        private static string FindItemId(object[] args)
        {
            if (args == null)
                return null;

            foreach (object arg in args)
            {
                if (arg == null || arg is string)
                    continue;

                string itemId =
                    GetDefinitionId(arg);

                if (!string.IsNullOrEmpty(itemId))
                    return itemId;
            }

            return null;
        }

        private static string GetDefinitionId(object value)
        {
            if (value == null)
                return null;

            object definition =
                GetMember(value, "Definition");

            if (definition != null &&
                !ReferenceEquals(definition, value))
            {
                string nestedId =
                    GetDirectId(definition);

                if (!string.IsNullOrEmpty(nestedId))
                    return nestedId;
            }

            return GetDirectId(value);
        }

        private static string GetDirectId(object value)
        {
            if (value == null)
                return null;

            string[] memberNames =
            {
                "ID",
                "Id",
                "ItemID",
                "ItemId",
                "Name",
                "name"
            };

            foreach (string name in memberNames)
            {
                object member =
                    GetMember(value, name);

                if (member != null &&
                    !string.IsNullOrEmpty(member.ToString()))
                {
                    return member.ToString();
                }
            }

            return null;
        }

        private static object GetMember(
            object target,
            string name
        )
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
                        return property.GetValue(target);

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                        return field.GetValue(target);

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool Matches(
            string actual,
            string fullId,
            string token
        )
        {
            if (string.IsNullOrWhiteSpace(actual))
                return false;

            string lower =
                actual.ToLowerInvariant();

            return lower == fullId.ToLowerInvariant() ||
                   lower.Contains(token.ToLowerInvariant());
        }

        public static void DumpCauldronState()
        {
            // Keep your original debug dump here if needed.
        }
    }
}