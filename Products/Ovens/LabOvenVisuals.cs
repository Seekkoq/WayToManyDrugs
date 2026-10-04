using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.StationFramework;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Ovens
{
    public static class LabOvenVisuals
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony(
                "westvilleconnection.labovenvisuals"
            );

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> GummyOvens =
            new HashSet<int>();

        private static bool _applied;

        public static readonly Color GummyRed =
            new Color(0.84f, 0.16f, 0.14f, 1f);

        public static readonly Color GummyRedDeep =
            new Color(0.58f, 0.07f, 0.07f, 1f);

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                foreach (MethodInfo method in typeof(LabOven).GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly))
                {
                    if (method == null ||
                        method.IsSpecialName ||
                        method.Name.Contains("b__"))
                    {
                        continue;
                    }

                    int count =
                        method.GetParameters().Length;

                    if (method.Name == "SetLiquidColor" &&
                        count == 1)
                    {
                        AddPrefix(
                            method,
                            nameof(SetLiquidColor_Prefix)
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Oven Visuals] Patched SetLiquidColor."
                        );
                    }
                    else if (method.Name == "UpdateLiquid" &&
                             count == 0)
                    {
                        AddPostfix(
                            method,
                            nameof(UpdateLiquid_Postfix)
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Oven Visuals] Patched UpdateLiquid."
                        );
                    }
                    else if (method.Name == "Shatter" &&
                             count == 2)
                    {
                        AddPostfix(
                            method,
                            nameof(Shatter_Postfix)
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Oven Visuals] Patched Shatter."
                        );
                    }
                    else if (method.Name == "CreateStationItems" &&
                             count == 1)
                    {
                        AddPostfix(
                            method,
                            nameof(CreateStationItems_Postfix)
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WVC Oven Visuals] Patched CreateStationItems."
                        );
                    }
                    else if (method.Name == "SendCookOperation" &&
                             count == 1)
                    {
                        AddPostfix(
                            method,
                            nameof(TrackCook_Postfix)
                        );
                    }
                    else if (
                        method.Name.StartsWith(
                            "RpcLogic___SendCookOperation",
                            StringComparison.Ordinal
                        ) &&
                        count == 1)
                    {
                        AddPostfix(
                            method,
                            nameof(TrackCook_Postfix)
                        );
                    }
                }

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven Visuals] Patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Oven Visuals] Patch setup failed: " +
                    ex
                );
            }
        }

        public static void TrackCook_Postfix(
            LabOven __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                OvenCookOperation operation =
                    __instance.CurrentOperation;

                if (operation == null)
                    return;

                if (!LabOvenRecipes.IdEquals(
                        operation.ProductID,
                        LabOvenRecipes.ThcGummiesId))
                {
                    GummyOvens.Remove(
                        __instance.GetInstanceID()
                    );

                    return;
                }

                GummyOvens.Add(
                    __instance.GetInstanceID()
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven Visuals] Gummy bake tracked."
                );

                MelonCoroutines.Start(
                    RepaintForSeconds(__instance, 3f)
                );
            }
            catch
            {
            }
        }

        private static bool IsGummyOven(
            LabOven oven)
        {
            if (oven == null)
                return false;

            if (GummyOvens.Contains(oven.GetInstanceID()))
                return true;

            try
            {
                OvenCookOperation operation =
                    oven.CurrentOperation;

                return operation != null &&
                       LabOvenRecipes.IdEquals(
                           operation.ProductID,
                           LabOvenRecipes.ThcGummiesId
                       );
            }
            catch
            {
                return false;
            }
        }

        public static void SetLiquidColor_Prefix(
            LabOven __instance,
            ref Color __0)
        {
            if (!IsGummyOven(__instance))
                return;

            __0 = GummyRed;
        }

        public static void UpdateLiquid_Postfix(
            LabOven __instance)
        {
            if (!IsGummyOven(__instance))
                return;

            RepaintOven(__instance);
        }

        private static IEnumerator RepaintForSeconds(
            LabOven oven,
            float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                RepaintOven(oven);

                yield return new WaitForSeconds(0.25f);

                elapsed += 0.25f;
            }
        }

        private static void RepaintOven(
            LabOven oven)
        {
            if (oven == null)
                return;

            try
            {
                if (oven.LiquidMesh != null)
                {
                    TintRenderer(
                        oven.LiquidMesh,
                        GummyRed
                    );
                }

                if (oven.CookedLiquidMesh != null)
                {
                    TintRenderer(
                        oven.CookedLiquidMesh,
                        GummyRed
                    );
                }
            }
            catch
            {
            }
        }

        public static void Shatter_Postfix(
            LabOven __instance)
        {
            if (!IsGummyOven(__instance))
                return;

            try
            {
                TintShards(__instance);

                MelonCoroutines.Start(
                    TintShardsForSeconds(__instance, 2f)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven Visuals] Recolored gummy shards."
                );
            }
            catch (Exception)
            {

            }
        }

        private static IEnumerator TintShardsForSeconds(
            LabOven oven,
            float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                TintShards(oven);

                yield return new WaitForSeconds(0.15f);

                elapsed += 0.15f;
            }
        }

        private static void TintShards(
            LabOven oven)
        {
            if (oven == null ||
                oven.shards == null)
            {
                return;
            }

            try
            {
                for (int i = 0; i < oven.shards.Count; i++)
                {
                    GameObject shard =
                        oven.shards[i];

                    if (shard == null)
                        continue;

                    Renderer[] renderers =
                        shard.GetComponentsInChildren<Renderer>(true);

                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer == null)
                            continue;

                        TintRenderer(
                            renderer,
                            i % 3 == 0
                                ? GummyRedDeep
                                : GummyRed
                        );
                    }
                }
            }
            catch
            {
            }
        }

        public static void CreateStationItems_Postfix(
            LabOven __instance,
            Il2CppReferenceArray<StationItem> __result)
        {
            if (!IsGummyOven(__instance) ||
                __result == null)
            {
                return;
            }

            try
            {
                for (int i = 0; i < __result.Length; i++)
                {
                    StationItem item =
                        __result[i];

                    if (item == null)
                        continue;

                    Renderer[] renderers =
                        item.GetComponentsInChildren<Renderer>(true);

                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer == null)
                            continue;

                        TintRenderer(
                            renderer,
                            GummyRed
                        );
                    }
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Oven Visuals] Recolored " +
                    __result.Length +
                    " tray station items."
                );
            }
            catch (Exception)
            {

            }
        }

        private static void TintRenderer(
            Renderer renderer,
            Color color)
        {
            if (renderer == null)
                return;

            try
            {
                Material[] materials =
                    renderer.materials;

                if (materials == null)
                    return;

                foreach (Material material in materials)
                {
                    if (material == null)
                        continue;

                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", color);

                    if (material.HasProperty("_Color"))
                        material.SetColor("_Color", color);

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor(
                            "_EmissionColor",
                            color * 0.15f
                        );
                    }
                }
            }
            catch
            {
            }
        }

        private static void AddPrefix(
            MethodInfo method,
            string handler)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(
                    typeof(LabOvenVisuals),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }

        private static void AddPostfix(
            MethodInfo method,
            string handler)
        {
            if (method == null ||
                PatchedMethods.Contains(method))
            {
                return;
            }

            Harmony.Patch(
                method,
                postfix: new HarmonyLib.HarmonyMethod(
                    typeof(LabOvenVisuals),
                    handler
                )
            );

            PatchedMethods.Add(method);
        }
    }
}
