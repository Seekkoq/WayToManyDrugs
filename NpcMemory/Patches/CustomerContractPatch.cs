using System;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Quests;
using MelonLoader;

namespace CustomNPCExample.NpcMemory.Patches
{
    [HarmonyPatch(typeof(Customer), "TryGenerateContract")]
    public static class CustomerContractPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Customer __instance, ref ContractInfo __result)
        {
            try
            {
                if (__instance == null) return true;

                NPC npc = __instance.NPC;
                if (npc == null || string.IsNullOrWhiteSpace(npc.ID)) return true;

                if (NpcMemoryManager.GetStrikes(npc.ID) < NpcMemoryManager.MaxStrikes)
                    return true;

                __instance.offeredContractInfo = null;
                __result = null;
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC NPC Memory] Contract patch failed: " + ex);
                return true;
            }
        }
    }
}