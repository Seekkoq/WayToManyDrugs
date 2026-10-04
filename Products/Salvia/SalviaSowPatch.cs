using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Growing;

namespace CustomNPCExample.Products
{
    [HarmonyPatch(typeof(SowSeedInPotBehaviour),
        nameof(SowSeedInPotBehaviour.GetRequiredItemSuitableIDs))]
    public static class SalviaSowPatch
    {
        static void Postfix(ref Il2CppStringArray __result)
        {
            if (__result == null) return;

            string extra = SalviaSeed.SeedId;

            for (int i = 0; i < __result.Length; i++)
                if (__result[i] == extra) return;

            var expanded = new Il2CppStringArray(__result.Length + 1);
            for (int i = 0; i < __result.Length; i++)
                expanded[i] = __result[i];
            expanded[__result.Length] = extra;
            __result = expanded;
        }
    }
}