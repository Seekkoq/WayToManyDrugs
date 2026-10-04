using System;
using System.Collections.Generic;
using System.Reflection;
using CustomNPCExample.Products.Cauldrons;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using S1API.Items;

using NativeCauldron = Il2CppScheduleOne.ObjectScripts.Cauldron;
using NativeItemSlot = Il2CppScheduleOne.ItemFramework.ItemSlot;
using NativeItemInstance = Il2CppScheduleOne.ItemFramework.ItemInstance;
using NativeStorableDef = Il2CppScheduleOne.ItemFramework.StorableItemDefinition;
using NativeEQuality = Il2CppScheduleOne.ItemFramework.EQuality;

namespace CustomNPCExample.Products
{
    public static class DMTCauldronPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony("westvilleconnection.dmt.cauldron");

        private static readonly Dictionary<IntPtr, string>
            PendingExtractOutputs =
                new Dictionary<IntPtr, string>();

        private static readonly Dictionary<int, string>
            ActiveDmtCooks =
                new Dictionary<int, string>();

        private static bool _applied;
        private static bool _replacingOutput;

        private const int BarkPerBatch =
            CauldronRecipeAmounts.DmtBarkRequired;

        private const int BasePerBatch =
            CauldronRecipeAmounts.DmtCausticBaseRequired;

        private const int SolventPerBatch =
            CauldronRecipeAmounts.DmtLabSolventRequired;

        private const int CrystalizerPerPremiumBatch =
            CauldronRecipeAmounts.DmtCrystalizerRequired;

        private const bool RequireGasoline = true;

        private struct DmtRecipe
        {
            public bool HasAnyDmtItem;

            public int BarkQuantity;
            public int BaseQuantity;
            public int SolventQuantity;
            public int CrystalizerQuantity;

            public NativeItemInstance BarkItem;
            public NativeItemInstance BaseItem;

            public bool HasBark =>
                BarkQuantity >= BarkPerBatch;

            public bool HasBase =>
                BaseQuantity >= BasePerBatch;

            public bool HasSolvent =>
                SolventQuantity >= SolventPerBatch;

            public bool HasCrystalizer =>
                CrystalizerQuantity >= CrystalizerPerPremiumBatch;

            public bool IsComplete =>
                HasBark && HasBase && HasSolvent;
        }

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                PatchCauldron();
                PatchOutputSlot();

                _applied = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC DMT Cauldron] Patch applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC DMT Cauldron] Patch failed: " + ex);
            }
        }

        private static void PatchCauldron()
        {
            MethodInfo[] methods =
                typeof(NativeCauldron).GetMethods(
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

                int args = method.GetParameters().Length;

                if (method.Name == "HasIngredients" && args == 0)
                {
                    PatchPrefix(method, nameof(PrefixHasIngredients));
                }
                else if (method.Name == "RemoveIngredients" && args == 0)
                {
                    PatchPrefix(method, nameof(PrefixRemoveIngredients));
                }
                else if (method.Name == "GetMainInputs" && args == 4)
                {
                    PatchPrefix(method, nameof(PrefixGetMainInputs));
                }
                else if (method.Name.Contains("FinishCookOperation") &&
                         args == 0)
                {
                    Harmony.Patch(
                        method,
                        postfix: new HarmonyMethod(
                            typeof(DMTCauldronPatch),
                            nameof(PostfixFinishCookOperation)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC DMT Cauldron] Patched " + method.Name
                    );
                }
            }
        }

        private static void PatchOutputSlot()
        {
            MethodInfo[] methods =
                typeof(NativeItemSlot).GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

            foreach (MethodInfo method in methods)
            {
                if (method == null ||
                    method.IsSpecialName ||
                    method.Name != "SetStoredItem")
                {
                    continue;
                }

                Harmony.Patch(
                    method,
                    postfix: new HarmonyMethod(
                        typeof(DMTCauldronPatch),
                        nameof(PostfixSetStoredItem)
                    )
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC DMT Cauldron] Patched ItemSlot.SetStoredItem postfix."
                );
            }
        }

        private static void PatchPrefix(MethodInfo method, string handler)
        {
            Harmony.Patch(
                method,
                prefix: new HarmonyMethod(
                    typeof(DMTCauldronPatch),
                    handler
                )
                {
                    priority = Priority.First
                }
            );

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC DMT Cauldron] Patched " + method.Name);
        }

        public static bool PrefixHasIngredients(
            NativeCauldron __instance,
            ref bool __result)
        {
            if (__instance == null)
                return true;

            DmtRecipe recipe =
                ReadRecipe(__instance);

            if (recipe.HasAnyDmtItem)
            {
                bool hasGasoline =
                    !RequireGasoline ||
                    CauldronRecipes.HasRequiredGasoline(
                        __instance
                    );

                __result =
                    recipe.IsComplete &&
                    hasGasoline;

                return false;
            }

            return true;
        }

        public static bool PrefixGetMainInputs(
    NativeCauldron __instance,
    ref NativeItemInstance primaryItem,
    ref int primaryItemQuantity,
    ref NativeItemInstance secondaryItem,
    ref int secondaryItemQuantity)
        {
            if (__instance == null)
                return true;

            DmtRecipe recipe =
                ReadRecipe(__instance);

            if (!recipe.HasAnyDmtItem)
                return true;

            primaryItem =
                recipe.BarkItem;

            primaryItemQuantity =
                BarkPerBatch;

            secondaryItem =
                recipe.BaseItem;

            secondaryItemQuantity =
                BasePerBatch;

            return false;
        }

        public static bool PrefixRemoveIngredients(
    NativeCauldron __instance,
    ref NativeEQuality __result)
        {
            if (__instance == null)
                return true;

            DmtRecipe recipe =
                ReadRecipe(__instance);

            if (!recipe.HasAnyDmtItem)
                return true;

            LogRecipe(recipe, __instance);

            bool hasGasoline =
                !RequireGasoline ||
                CauldronRecipes.HasRequiredGasoline(
                    __instance
                );

            if (!recipe.IsComplete ||
                !hasGasoline)
            {


                __result =
                    NativeEQuality.Trash;

                return false;
            }

            bool gasolineConsumed = true;

            if (RequireGasoline)
            {
                gasolineConsumed =
                    CauldronRecipes.ConsumeRequiredGasoline(
                        __instance
                    );
            }

            if (!gasolineConsumed)
            {


                __result =
                    NativeEQuality.Trash;

                return false;
            }

            bool barkConsumed =
                CauldronRecipes.ConsumeByItemId(
                    __instance,
                    DMTIngredients.DreamrootId,
                    BarkPerBatch
                );

            bool baseConsumed =
                CauldronRecipes.ConsumeByItemId(
                    __instance,
                    DMTIngredients.CausticBaseId,
                    BasePerBatch
                );

            bool solventConsumed =
                ConsumeSolvent(
                    __instance,
                    SolventPerBatch
                );

            if (!barkConsumed ||
                !baseConsumed ||
                !solventConsumed)
            {


                __result =
                    NativeEQuality.Trash;

                return false;
            }

            bool premium =
                recipe.HasCrystalizer;

            if (premium)
            {
                bool crystalizerConsumed =
                    CauldronRecipes.ConsumeByItemId(
                        __instance,
                        DMTIngredients.CrystalizerId,
                        CrystalizerPerPremiumBatch
                    );

                if (!crystalizerConsumed)
                {
                    premium = false;


                }
            }

            string outputItemId =
                premium
                    ? DMTIntermediates.PremiumCrudeExtractId
                    : DMTIntermediates.CrudeExtractId;

            bool swapped =
                TrySwapToQualityProxy(
                    __instance
                );

            if (!swapped)
            {

            }

            TrackPendingOutput(
                __instance,
                outputItemId
            );

            ActiveDmtCooks[
                __instance.GetInstanceID()
            ] = outputItemId;

            __result =
                premium
                    ? (NativeEQuality)3
                    : (NativeEQuality)2;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC DMT Cauldron] Extraction started. " +
                "Output=" + outputItemId +
                ", quality=" + __result +
                ", swapped=" + swapped
            );

            return false;
        }

        private static bool TrySwapToQualityProxy(NativeCauldron cauldron)
        {
            if (cauldron == null)
                return false;

            bool swapped =
                CauldronRecipes.SwapOutputTo(
                    cauldron,
                    DMT.ProductId
                );

            if (swapped)
                return true;



            try
            {
                swapped =
                    CauldronRecipes.SwapOutputTo(
                        cauldron,
                        MDMA.ProductId
                    );
            }
            catch
            {
                swapped = false;
            }

            return swapped;
        }

        public static void PostfixFinishCookOperation(
            NativeCauldron __instance)
        {
            if (__instance == null)
                return;

            int cauldronId = __instance.GetInstanceID();

            if (ActiveDmtCooks.TryGetValue(cauldronId, out string outputItemId))
            {
                ActiveDmtCooks.Remove(cauldronId);

                try
                {
                    NativeItemSlot output = __instance.OutputSlot;

                    if (output != null && output.ItemInstance != null)
                    {
                        TryReplaceWithExtract(
                            output,
                            outputItemId,
                            true
                        );
                    }
                }
                catch (Exception)
                {

                }
            }

            CauldronRecipes.RestoreOutput(__instance);
        }

        public static void PostfixSetStoredItem(NativeItemSlot __instance)
        {
            if (_replacingOutput || __instance == null)
                return;

            try
            {
                IntPtr pointer = __instance.Pointer;

                if (pointer == IntPtr.Zero)
                    return;

                if (!PendingExtractOutputs.TryGetValue(
                        pointer,
                        out string outputItemId))
                {
                    return;
                }

                if (__instance.ItemInstance == null)
                    return;

                TryReplaceWithExtract(
                    __instance,
                    outputItemId,
                    false
                );
            }
            catch (Exception)
            {

            }
        }

        private static void TryReplaceWithExtract(
            NativeItemSlot slot,
            string outputItemId,
            bool force)
        {
            if (slot == null ||
                slot.ItemInstance == null ||
                string.IsNullOrWhiteSpace(outputItemId))
            {
                return;
            }

            IntPtr pointer = slot.Pointer;

            string currentId =
                slot.ItemInstance.Definition?.ID;

            if (string.Equals(
                    currentId,
                    outputItemId,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (pointer != IntPtr.Zero)
                    PendingExtractOutputs.Remove(pointer);

                return;
            }

            if (!force)
            {
                if (pointer == IntPtr.Zero ||
                    !PendingExtractOutputs.ContainsKey(pointer))
                {
                    return;
                }
            }

            const int extractQuantity = 1;

            if (!TryCreateExtract(
                    outputItemId,
                    extractQuantity,
                    out NativeItemInstance extract) ||
                extract == null)
            {


                return;
            }

            try
            {
                _replacingOutput = true;
                slot.SetStoredItem(extract, true);
            }
            finally
            {
                _replacingOutput = false;
            }

            if (pointer != IntPtr.Zero)
                PendingExtractOutputs.Remove(pointer);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC DMT Cauldron] Replaced output with " +
                outputItemId +
                " x1"
            );
        }

        private static void TrackPendingOutput(
            NativeCauldron cauldron,
            string outputItemId)
        {
            if (cauldron == null ||
                string.IsNullOrWhiteSpace(outputItemId))
            {
                return;
            }

            try
            {
                NativeItemSlot output = cauldron.OutputSlot;

                if (output == null || output.Pointer == IntPtr.Zero)
                    return;

                PendingExtractOutputs[output.Pointer] = outputItemId;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC DMT Cauldron] Tracking output: " + outputItemId
                );
            }
            catch
            {
            }
        }

        private static bool TryCreateExtract(
            string itemId,
            int quantity,
            out NativeItemInstance instance)
        {
            instance = null;

            try
            {
                if (quantity < 1)
                    quantity = 1;

                ItemDefinition wrapper =
                    ItemManager.GetDefinition(itemId);

                if (wrapper == null)
                {


                    return false;
                }

                object raw =
                    GetMemberValue(wrapper, "S1ItemDefinition")
                    ?? GetMemberValue(wrapper, "S1ProductDefinition");

                if (raw == null)
                {


                    return false;
                }

                NativeStorableDef definition =
                    raw as NativeStorableDef;

                if (definition == null)
                {
                    try
                    {
                        Il2CppObjectBase native =
                            raw as Il2CppObjectBase;

                        definition =
                            native?.TryCast<NativeStorableDef>();
                    }
                    catch
                    {
                    }
                }

                if (definition == null)
                {


                    return false;
                }

                instance = definition.GetDefaultInstance(quantity);
                return instance != null;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static DmtRecipe ReadRecipe(
     NativeCauldron cauldron)
        {
            DmtRecipe recipe =
                new DmtRecipe();

            try
            {
                HashSet<IntPtr> countedSlots =
                    new HashSet<IntPtr>();

                var slots =
                    cauldron.ItemSlots;

                if (slots != null)
                {
                    for (int i = 0;
                         i < slots.Count;
                         i++)
                    {
                        NativeItemSlot slot =
                            slots[i];

                        if (slot?.ItemInstance == null)
                            continue;

                        if (slot.Pointer != IntPtr.Zero)
                        {
                            countedSlots.Add(
                                slot.Pointer
                            );
                        }

                        string id =
                            slot.ItemInstance.Definition?.ID;

                        if (string.IsNullOrEmpty(id))
                            continue;

                        if (CauldronRecipes.IdEquals(
                                id,
                                DMTIngredients.DreamrootId))
                        {
                            recipe.HasAnyDmtItem = true;
                            recipe.BarkQuantity += slot.Quantity;

                            if (recipe.BarkItem == null)
                            {
                                recipe.BarkItem =
                                    slot.ItemInstance;
                            }
                        }
                        else if (CauldronRecipes.IdEquals(
                                     id,
                                     DMTIngredients.CausticBaseId))
                        {
                            recipe.HasAnyDmtItem = true;
                            recipe.BaseQuantity += slot.Quantity;

                            if (recipe.BaseItem == null)
                            {
                                recipe.BaseItem =
                                    slot.ItemInstance;
                            }
                        }
                        else if (CauldronRecipes.IdEquals(
                                     id,
                                     DMTIngredients.LabSolventId))
                        {
                            recipe.HasAnyDmtItem = true;
                            recipe.SolventQuantity += slot.Quantity;
                        }
                        else if (CauldronRecipes.IdEquals(
                                     id,
                                     DMTIngredients.CrystalizerId))
                        {
                            recipe.HasAnyDmtItem = true;
                            recipe.CrystalizerQuantity += slot.Quantity;
                        }
                    }
                }

                if (!RequireGasoline)
                {
                    NativeItemSlot liquid =
                        cauldron.LiquidSlot;

                    if (liquid?.ItemInstance != null &&
                        (liquid.Pointer == IntPtr.Zero ||
                         !countedSlots.Contains(liquid.Pointer)))
                    {
                        string liquidId =
                            liquid.ItemInstance.Definition?.ID;

                        if (CauldronRecipes.IdEquals(
                                liquidId,
                                DMTIngredients.LabSolventId))
                        {
                            recipe.HasAnyDmtItem = true;
                            recipe.SolventQuantity += liquid.Quantity;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            return recipe;
        }

        private static bool ConsumeSolvent(
            NativeCauldron cauldron,
            int amount)
        {
            if (cauldron == null || amount <= 0)
                return false;

            if (CauldronRecipes.ConsumeByItemId(
                    cauldron,
                    DMTIngredients.LabSolventId,
                    amount))
            {
                return true;
            }

            try
            {
                NativeItemSlot liquid = cauldron.LiquidSlot;

                if (liquid?.ItemInstance == null)
                    return false;

                string id = liquid.ItemInstance.Definition?.ID;

                if (!CauldronRecipes.IdEquals(
                        id, DMTIngredients.LabSolventId))
                {
                    return false;
                }

                if (liquid.Quantity < amount)
                    return false;

                int remaining = liquid.Quantity - amount;

                if (remaining <= 0)
                    liquid.SetStoredItem(null, true);
                else
                    liquid.SetStoredItem(
                        liquid.ItemInstance.GetCopy(remaining),
                        true
                    );

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static void LogRecipe(
            DmtRecipe recipe,
            NativeCauldron cauldron)
        {
            bool hasGasoline =
                !RequireGasoline ||
                CauldronRecipes.HasRequiredGasoline(
                    cauldron
                );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC DMT Cauldron] Recipe available/required: " +
                "bark=" + recipe.BarkQuantity + "/" + BarkPerBatch +
                ", base=" + recipe.BaseQuantity + "/" + BasePerBatch +
                ", solvent=" + recipe.SolventQuantity + "/" + SolventPerBatch +
                ", crystalizer=" + recipe.CrystalizerQuantity +
                ", gasoline=" + hasGasoline +
                ", complete=" + recipe.IsComplete +
                ", premium=" + recipe.HasCrystalizer
            );
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
                        try
                        {
                            object value = property.GetValue(target);
                            if (value != null)
                                return value;
                        }
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
                        try
                        {
                            object value = field.GetValue(target);
                            if (value != null)
                                return value;
                        }
                        catch { }
                    }

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }
    }
}
