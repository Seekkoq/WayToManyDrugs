using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class CauldronPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony(
                "westvilleconnection.cauldron"
            );

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> FlaggedCauldronIds =
            new HashSet<int>();

        private static bool _applied;

        public static bool TreatEveryCauldronAsCustom = true;
        public static bool EnableMethodDump = false;

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                if (EnableMethodDump)
                    DumpCauldronMethods();

                PatchCauldronMethods();
                PatchItemSlotMethods();

                _applied = true;

                MelonLogger.Msg(
                    "[WVC Cauldron] Patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Cauldron] Patch setup failed: " +
                    ex
                );
            }
        }

        private static void DumpCauldronMethods()
        {
            Type type = typeof(Cauldron);

            while (type != null)
            {
                MethodInfo[] methods =
                    type.GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method == null)
                        continue;

                    string name = method.Name;

                    if (name.IndexOf(
                            "Cook",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0 ||
                        name.IndexOf(
                            "Finish",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0 ||
                        name.IndexOf(
                            "Output",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0 ||
                        name.IndexOf(
                            "Rpc",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0)
                    {
                        MelonLogger.Msg(
                            "[WVC Cauldron] Method: " +
                            type.Name +
                            "." +
                            method.Name +
                            " params=" +
                            method.GetParameters().Length
                        );
                    }
                }

                type = type.BaseType;
            }
        }

        private static void PatchCauldronMethods()
        {
            MethodInfo[] methods =
                typeof(Cauldron).GetMethods(
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

                int parameterCount =
                    method.GetParameters().Length;

                if (method.Name == "HasIngredients" &&
                    parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(HasIngredients_Prefix)
                    );
                }
                else if (method.Name == "GetMainInputs" &&
                         parameterCount == 4)
                {
                    AddPrefix(
                        method,
                        nameof(GetMainInputs_Prefix)
                    );
                }
                else if (method.Name == "GetState" &&
                         parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(GetState_Prefix)
                    );
                }
                else if (method.Name == "RemoveIngredients" &&
                         parameterCount == 0)
                {
                    AddPrefix(
                        method,
                        nameof(RemoveIngredients_Prefix)
                    );
                }
                else if (method.Name == "ButtonClicked" &&
                         parameterCount == 1)
                {
                    AddPrefix(
                        method,
                        nameof(ButtonClicked_Prefix)
                    );
                }
                else if (
                    (method.Name == "FinishCookOperation" &&
                     parameterCount == 0) ||
                    method.Name.StartsWith(
                        "RpcLogic___FinishCookOperation",
                        StringComparison.Ordinal))
                {
                    AddFinishPatches(method);
                }
            }
        }

        private static void PatchItemSlotMethods()
        {
            MethodInfo[] methods =
                typeof(ItemSlot).GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (MethodInfo method in methods)
            {
                if (method == null ||
                    method.IsSpecialName)
                {
                    continue;
                }

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    AddPrefix(
                        method,
                        typeof(CauldronSlotPatches),
                        nameof(
                            CauldronSlotPatches.ItemFilter_Prefix
                        )
                    );
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    AddPrefix(
                        method,
                        typeof(CauldronSlotPatches),
                        nameof(
                            CauldronSlotPatches.Capacity_Prefix
                        )
                    );
                }
                else if (method.Name == "SetStoredItem")
                {
                    try
                    {
                        Harmony.Patch(
                            method,
                            prefix: new HarmonyLib.HarmonyMethod(
                                typeof(CauldronOutputInterceptor),
                                nameof(
                                    CauldronOutputInterceptor
                                        .SetStoredItem_Prefix
                                )
                            )
                        );

                        MelonLogger.Msg(
                            "[WVC Cauldron] Patched " +
                            "ItemSlot.SetStoredItem: " +
                            method
                        );
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning(
                            "[WVC Cauldron] SetStoredItem patch " +
                            "failed: " +
                            ex.Message
                        );
                    }
                }
            }
        }

        private static void AddPrefix(
            MethodInfo method,
            string handler)
        {
            AddPrefix(
                method,
                typeof(CauldronPatch),
                handler
            );
        }

        private static void AddPrefix(
            MethodInfo method,
            Type handlerType,
            string handler)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                prefix:
                    new HarmonyLib.HarmonyMethod(
                        handlerType,
                        handler
                    )
            );

            PatchedMethods.Add(method);
        }

        private static void AddFinishPatches(
            MethodInfo method)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(
                    typeof(CauldronPatch),
                    nameof(FinishCook_Prefix)
                ),
                postfix: new HarmonyLib.HarmonyMethod(
                    typeof(CauldronPatch),
                    nameof(FinishCook_Postfix)
                )
            );

            PatchedMethods.Add(method);

            MelonLogger.Msg(
                "[WVC Cauldron] Patched finish method: " +
                method.Name
            );
        }

        public static void MarkAsCustomCauldron(
            object cauldron)
        {
            Component component =
                cauldron as Component;

            if (component == null)
                return;

            FlaggedCauldronIds.Add(
                component.GetInstanceID()
            );
        }

        public static bool IsCustomCauldron(
            object cauldron)
        {
            if (cauldron == null)
                return false;

            if (TreatEveryCauldronAsCustom)
                return true;

            Component component =
                cauldron as Component;

            return component != null &&
                   FlaggedCauldronIds.Contains(
                       component.GetInstanceID()
                   );
        }

        private static Cauldron AsCauldron(
            object instance)
        {
            Cauldron direct =
                instance as Cauldron;

            if (direct != null)
                return direct;

            Il2CppSystem.Object obj =
                instance as Il2CppSystem.Object;

            return obj?.TryCast<Cauldron>();
        }

        // ============================================================
        // State
        // ============================================================

        public static bool GetState_Prefix(
            Cauldron __instance,
            ref Cauldron.EState __result)
        {
            if (__instance == null ||
                !IsCustomCauldron(__instance))
            {
                return true;
            }

            try
            {
                if (__instance.isCooking)
                {
                    __result = Cauldron.EState.Cooking;
                    return false;
                }

                /*
                 * DMT is checked first so its own required amounts
                 * are used for the start button, not another recipe's.
                 */
                DmtCauldronRecipe.State dmt =
                    DmtCauldronRecipe.Read(__instance);

                if (dmt.HasAny)
                {
                    if (!dmt.IsComplete ||
                        !CauldronRecipes.HasRequiredGasoline(__instance))
                    {
                        __result =
                            Cauldron.EState.MissingIngredients;

                        return false;
                    }

                    if (!HasCustomOutputSpace(__instance))
                    {
                        __result =
                            Cauldron.EState.OutputFull;

                        return false;
                    }

                    __result = Cauldron.EState.Ready;
                    return false;
                }

                GummyCauldronRecipe.State gummy =
                    GummyCauldronRecipe.Read(__instance);

                if (gummy.HasAny)
                {
                    if (!gummy.IsComplete ||
                        !CauldronRecipes.HasRequiredGasoline(__instance))
                    {
                        __result =
                            Cauldron.EState.MissingIngredients;

                        return false;
                    }

                    if (!HasCustomOutputSpace(__instance))
                    {
                        __result =
                            Cauldron.EState.OutputFull;

                        return false;
                    }

                    __result = Cauldron.EState.Ready;
                    return false;
                }

                MollyCauldronRecipe.State mdma =
                    MollyCauldronRecipe.Read(__instance);

                if (mdma.HasAny)
                {
                    if (!mdma.IsComplete ||
                        !CauldronRecipes.HasRequiredGasoline(__instance))
                    {
                        __result =
                            Cauldron.EState.MissingIngredients;

                        return false;
                    }

                    if (!HasCustomOutputSpace(__instance))
                    {
                        __result =
                            Cauldron.EState.OutputFull;

                        return false;
                    }

                    __result = Cauldron.EState.Ready;
                    return false;
                }

                CookieCauldronRecipe.State cookie =
                    CookieCauldronRecipe.Read(__instance);

                if (cookie.HasAny)
                {
                    if (!cookie.IsComplete)
                    {
                        __result =
                            Cauldron.EState.MissingIngredients;

                        return false;
                    }

                    if (!HasCustomOutputSpace(__instance))
                    {
                        __result =
                            Cauldron.EState.OutputFull;

                        return false;
                    }

                    __result = Cauldron.EState.Ready;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] GetState failed: " +
                    ex.Message
                );

                return true;
            }
        }

        private static bool HasCustomOutputSpace(
            Cauldron cauldron)
        {
            return cauldron?.OutputSlot != null &&
                   cauldron.OutputSlot.ItemInstance == null;
        }

        // ============================================================
        // Start requirement
        // ============================================================

        public static bool HasIngredients_Prefix(
            object __instance,
            ref bool __result)
        {
            if (!IsCustomCauldron(__instance))
                return true;

            Cauldron cauldron =
                AsCauldron(__instance);

            if (cauldron == null)
                return true;

            DmtCauldronRecipe.State dmt =
                DmtCauldronRecipe.Read(cauldron);

            if (dmt.HasAny)
            {
                __result =
                    dmt.IsComplete &&
                    CauldronRecipes.HasRequiredGasoline(cauldron);

                return false;
            }

            GummyCauldronRecipe.State gummy =
                GummyCauldronRecipe.Read(cauldron);

            if (gummy.HasAny)
            {
                __result =
                    gummy.IsComplete &&
                    CauldronRecipes.HasRequiredGasoline(cauldron);

                return false;
            }

            MollyCauldronRecipe.State mdma =
                MollyCauldronRecipe.Read(cauldron);

            if (mdma.HasAny)
            {
                __result =
                    mdma.IsComplete &&
                    CauldronRecipes.HasRequiredGasoline(cauldron);

                return false;
            }

            CookieCauldronRecipe.State cookie =
                CookieCauldronRecipe.Read(cauldron);

            if (cookie.HasAny)
            {
                __result = cookie.IsComplete;
                return false;
            }

            return true;
        }

        // ============================================================
        // Displayed / validated main inputs
        // ============================================================

        public static bool GetMainInputs_Prefix(
            object __instance,
            ref ItemInstance primaryItem,
            ref int primaryItemQuantity,
            ref ItemInstance secondaryItem,
            ref int secondaryItemQuantity)
        {
            if (!IsCustomCauldron(__instance))
                return true;

            Cauldron cauldron =
                AsCauldron(__instance);

            if (cauldron == null)
                return true;

            DmtCauldronRecipe.State dmt =
                DmtCauldronRecipe.Read(cauldron);

            if (dmt.HasAny)
            {
                primaryItem = dmt.BarkItem;

                primaryItemQuantity =
                    CauldronRecipeAmounts.DmtBarkRequired;

                secondaryItem = dmt.BaseItem;

                secondaryItemQuantity =
                    CauldronRecipeAmounts.DmtCausticBaseRequired;

                return false;
            }

            GummyCauldronRecipe.State gummy =
                GummyCauldronRecipe.Read(cauldron);

            if (gummy.IsComplete)
            {
                primaryItem = gummy.OilItem;

                primaryItemQuantity =
                    CauldronRecipeAmounts.ThcOilRequired;

                secondaryItem = gummy.GelatinItem;

                secondaryItemQuantity =
                    CauldronRecipeAmounts.GelatinRequired;

                return false;
            }

            MollyCauldronRecipe.State mdma =
                MollyCauldronRecipe.Read(cauldron);

            if (mdma.IsComplete)
            {
                primaryItem = mdma.SafroleItem;

                primaryItemQuantity =
                    CauldronRecipeAmounts.SafroleRequired;

                secondaryItem = mdma.PmkItem;

                secondaryItemQuantity =
                    CauldronRecipeAmounts.PmkRequired;

                return false;
            }

            CookieCauldronRecipe.State cookie =
                CookieCauldronRecipe.Read(cauldron);

            if (cookie.IsComplete)
            {
                primaryItem = cookie.FlourItem;

                primaryItemQuantity =
                    CauldronRecipeAmounts.CannabisFlourRequired;

                secondaryItem = cookie.ChipsItem;

                secondaryItemQuantity =
                    CauldronRecipeAmounts.ButterscotchChipsRequired;

                return false;
            }

            return true;
        }

        // ============================================================
        // Consumption
        // ============================================================

        public static bool RemoveIngredients_Prefix(
            object __instance,
            ref EQuality __result)
        {
            if (!IsCustomCauldron(__instance))
                return true;

            Cauldron cauldron =
                AsCauldron(__instance);

            if (cauldron == null)
                return true;

            // ---------------- DMT ----------------

            DmtCauldronRecipe.State dmt =
                DmtCauldronRecipe.Read(cauldron);

            if (dmt.HasAny)
            {
                bool hasGasoline =
                    CauldronRecipes.HasRequiredGasoline(cauldron);

                if (!dmt.IsComplete || !hasGasoline)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] DMT incomplete. available/required " +
                        "bark=" + dmt.BarkCount + "/" +
                        CauldronRecipeAmounts.DmtBarkRequired +
                        ", base=" + dmt.BaseCount + "/" +
                        CauldronRecipeAmounts.DmtCausticBaseRequired +
                        ", solvent=" + dmt.SolventCount + "/" +
                        CauldronRecipeAmounts.DmtLabSolventRequired +
                        ", gasoline=" + hasGasoline
                    );

                    __result = EQuality.Trash;
                    return false;
                }

                string dmtOutputId;
                EQuality dmtQuality;

                if (!DmtCauldronRecipe.Consume(
                        cauldron,
                        dmt,
                        out dmtOutputId,
                        out dmtQuality))
                {
                    __result = EQuality.Trash;
                    return false;
                }

                CauldronRecipes.ConsumeRequiredGasoline(cauldron);

                CauldronFinishedOutput.MarkPending(
                    cauldron,
                    dmtOutputId,
                    dmtQuality
                );

                __result = dmtQuality;
                return false;
            }

            // ---------------- Gummies ----------------

            GummyCauldronRecipe.State gummy =
                GummyCauldronRecipe.Read(cauldron);

            if (gummy.IsComplete &&
                CauldronRecipes.HasRequiredGasoline(cauldron))
            {
                CauldronFinishedOutput.MarkPending(
                    cauldron,
                    CauldronRecipeIds.GummyMixId
                );

                GummyCauldronRecipe.Consume(
                    cauldron,
                    gummy
                );

                CauldronRecipes.ConsumeRequiredGasoline(
                    cauldron
                );

                __result = EQuality.Standard;
                return false;
            }

            // ---------------- MDMA ----------------

            MollyCauldronRecipe.State mdma =
                MollyCauldronRecipe.Read(cauldron);

            if (mdma.IsComplete &&
                CauldronRecipes.HasRequiredGasoline(cauldron))
            {
                EQuality cookQuality =
                    MollyCauldronRecipe.GetQuality(
                        mdma.PmkGrade
                    );

                CauldronFinishedOutput.MarkPending(
                    cauldron,
                    CauldronRecipeIds.MdmaId,
                    cookQuality
                );

                MollyCauldronRecipe.Consume(
                    cauldron,
                    mdma
                );

                CauldronRecipes.ConsumeRequiredGasoline(
                    cauldron
                );

                __result = cookQuality;

                MelonLogger.Msg(
                    "[WVC Cauldron] MDMA cook quality: " +
                    cookQuality +
                    " grade=" +
                    mdma.PmkGrade
                );

                return false;
            }

            if (mdma.HasAny)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] MDMA ingredients: " +
                    "safrole=" + mdma.SafroleCount + "/" +
                    CauldronRecipeAmounts.SafroleRequired +
                    ", PMK=" + mdma.PmkCount + "/" +
                    CauldronRecipeAmounts.PmkRequired +
                    ", grade=" + mdma.PmkGrade +
                    ", complete=" + mdma.IsComplete
                );
            }

            // ---------------- Cookies ----------------

            CookieCauldronRecipe.State cookie =
                CookieCauldronRecipe.Read(cauldron);

            if (cookie.IsComplete)
            {
                CauldronFinishedOutput.MarkPending(
                    cauldron,
                    CauldronRecipeIds.CookieDoughId,
                    EQuality.Standard
                );

                CookieCauldronRecipe.Consume(
                    cauldron,
                    cookie
                );

                __result = EQuality.Standard;
                return false;
            }

            if (cookie.HasAny)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Cookie incomplete. available/required " +
                    "flour=" + cookie.FlourCount + "/" +
                    CauldronRecipeAmounts.CannabisFlourRequired +
                    ", chips=" + cookie.ChipsCount + "/" +
                    CauldronRecipeAmounts.ButterscotchChipsRequired
                );

                __result = EQuality.Trash;
                return false;
            }

            return true;
        }

        // ============================================================
        // Debug
        // ============================================================

        public static void ButtonClicked_Prefix(
            object __instance)
        {
            Cauldron cauldron =
                AsCauldron(__instance);

            if (cauldron == null ||
                !IsCustomCauldron(cauldron))
            {
                return;
            }

            DmtCauldronRecipe.State dmt =
                DmtCauldronRecipe.Read(cauldron);

            if (dmt.HasAny)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] DMT available/required: " +
                    "bark=" + dmt.BarkCount + "/" +
                    CauldronRecipeAmounts.DmtBarkRequired +
                    ", base=" + dmt.BaseCount + "/" +
                    CauldronRecipeAmounts.DmtCausticBaseRequired +
                    ", solvent=" + dmt.SolventCount + "/" +
                    CauldronRecipeAmounts.DmtLabSolventRequired +
                    ", crystalizer=" + dmt.CrystalizerCount +
                    ", gasoline=" +
                    CauldronRecipes.HasRequiredGasoline(cauldron) +
                    ", complete=" + dmt.IsComplete +
                    ", premium=" + dmt.IsPremium
                );

                return;
            }

            GummyCauldronRecipe.State gummy =
                GummyCauldronRecipe.Read(cauldron);

            if (gummy.HasAny)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] Gummy ingredients: " +
                    "oil=" + gummy.OilCount + "/" +
                    CauldronRecipeAmounts.ThcOilRequired +
                    ", gelatin=" + gummy.GelatinCount + "/" +
                    CauldronRecipeAmounts.GelatinRequired +
                    ", sugar=" + gummy.SugarCount + "/" +
                    CauldronRecipeAmounts.SugarRequired +
                    ", complete=" + gummy.IsComplete
                );

                return;
            }

            MollyCauldronRecipe.State mdma =
                MollyCauldronRecipe.Read(cauldron);

            if (mdma.HasAny)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] MDMA ingredients: " +
                    "safrole=" + mdma.SafroleCount + "/" +
                    CauldronRecipeAmounts.SafroleRequired +
                    ", PMK=" + mdma.PmkCount + "/" +
                    CauldronRecipeAmounts.PmkRequired +
                    ", complete=" + mdma.IsComplete
                );

                return;
            }

            CookieCauldronRecipe.State cookie =
                CookieCauldronRecipe.Read(cauldron);

            if (cookie.HasAny)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] Cookie ingredients: " +
                    "flour=" + cookie.FlourCount + "/" +
                    CauldronRecipeAmounts.CannabisFlourRequired +
                    ", chips=" + cookie.ChipsCount + "/" +
                    CauldronRecipeAmounts.ButterscotchChipsRequired +
                    ", complete=" + cookie.IsComplete
                );
            }
        }

        // ============================================================
        // Finish
        // ============================================================

        public static void FinishCook_Prefix(
            object __instance)
        {
            Cauldron cauldron =
                AsCauldron(__instance);

            if (cauldron == null ||
                !IsCustomCauldron(cauldron))
            {
                return;
            }

            if (!CauldronFinishedOutput.TryGetPendingOutput(
                    cauldron,
                    out string outputItemId))
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] Finish prefix: " +
                    "no pending custom output."
                );

                return;
            }

            /*
             * Crude extracts are ingredient-cloned items, so they are not
             * QualityItemDefinitions and cannot be assigned to
             * CocaineBaseDefinition. Swap to the DMT product as a proxy
             * so the native cook does not emit cocaine base, then the
             * output interceptor replaces it with the real extract.
             */
            string swapTargetId =
                DmtCauldronRecipe.IsDmtOutput(outputItemId)
                    ? DMT.ProductId
                    : outputItemId;

            bool swapped =
                CauldronRecipes.SwapOutputTo(
                    cauldron,
                    swapTargetId
                );

            MelonLogger.Msg(
                "[WVC Cauldron] Finish prefix. output=" +
                outputItemId +
                ", swapTarget=" +
                swapTargetId +
                ", definition swapped=" +
                swapped
            );
        }

        public static void FinishCook_Postfix(
            object __instance)
        {
            Cauldron cauldron =
                AsCauldron(__instance);

            MelonLogger.Msg(
                "[WVC Cauldron] FinishCook postfix. cauldron=" +
                (cauldron != null)
            );

            if (cauldron == null)
                return;

            CauldronFinishedOutput.ScheduleReplacement(
                cauldron
            );

            MelonCoroutines.Start(
                RestoreOutputDefinitionAfterFinish(cauldron)
            );
        }

        private static IEnumerator RestoreOutputDefinitionAfterFinish(
            Cauldron cauldron)
        {
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);

            CauldronRecipes.RestoreOutput(cauldron);
        }

        public static void DumpCauldronState()
        {
            MelonLogger.Msg(
                "[WVC Cauldron] Dump simplified."
            );
        }
    }
}