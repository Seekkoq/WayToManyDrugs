using System;
using HarmonyLib;
using Il2CppInterop.Runtime;

using Il2CppEquippableSeed =
    Il2CppScheduleOne.Equipping.Equippable_Seed;

using Il2CppPlant =
    Il2CppScheduleOne.Growing.Plant;

using Il2CppWeedPlant =
    Il2CppScheduleOne.Growing.WeedPlant;

using Il2CppPlantHarvestable =
    Il2CppScheduleOne.Growing.PlantHarvestable;

using Il2CppItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

namespace CustomNPCExample.Products
{
    [HarmonyPatch(
        typeof(Il2CppEquippableSeed),
        nameof(Il2CppEquippableSeed.StartSowSeedTask)
    )]
    public static class SalviaEquippableSeedPatch
    {
        private static void Prefix(
            Il2CppEquippableSeed __instance)
        {
            if (__instance == null ||
                __instance.gameObject == null)
            {
                return;
            }

            string objectName =
                __instance.gameObject.name ??
                string.Empty;

            if (objectName.IndexOf(
                    "Salvia",
                    StringComparison.OrdinalIgnoreCase
                ) < 0)
            {
                return;
            }

            var salviaSeed =
                SalviaSeed.FindNativeSeedById(
                    SalviaSeed.SeedId
                );

            if (salviaSeed != null)
                __instance.Seed = salviaSeed;
        }
    }

    [HarmonyPatch(
        typeof(Il2CppPlantHarvestable),
        nameof(Il2CppPlantHarvestable.Harvest)
    )]
    public static class SalviaHarvestablePatch
    {
        private static void Prefix(
            Il2CppPlantHarvestable __instance)
        {
            SalviaGrowing.RetargetHarvestableIfSalvia(
                __instance
            );
        }
    }

    [HarmonyPatch(
        typeof(Il2CppWeedPlant),
        nameof(Il2CppWeedPlant.GetHarvestedProduct)
    )]
    public static class SalviaWeedPlantHarvestPatch
    {
        private static void Postfix(
            Il2CppWeedPlant __instance,
            int quantity,
            ref Il2CppItemInstance __result)
        {
            Il2CppPlant plant =
                __instance.TryCast<Il2CppPlant>();

            if (!SalviaGrowing.IsSalviaPlant(plant))
                return;

            Il2CppItemInstance salvia =
                SalviaGrowing.CreateSalviaInstance(
                    quantity
                );

            if (salvia != null)
                __result = salvia;
        }
    }
}
