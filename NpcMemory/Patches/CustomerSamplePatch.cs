using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using MelonLoader;

namespace CustomNPCExample.NpcMemory.Patches
{
    internal static class CustomerSampleMemory
    {
        public static string GetNpcId(Customer customer)
        {
            if (customer == null)
                return null;

            NPC npc = customer.NPC;

            if (npc == null ||
                string.IsNullOrWhiteSpace(npc.ID))
            {
                return null;
            }

            return npc.ID;
        }

        public static bool NeedsSample(Customer customer)
        {
            string npcId = GetNpcId(customer);

            return !string.IsNullOrWhiteSpace(npcId) &&
                   NpcMemoryManager.RequiresSample(npcId);
        }
    }

    [HarmonyPatch(
        typeof(Customer),
        "IsUnlockable"
    )]
    public static class CustomerIsUnlockableMemoryPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            Customer __instance,
            ref bool __result)
        {
            try
            {
                if (CustomerSampleMemory.NeedsSample(
                        __instance))
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    $"[WVC NPC Memory] IsUnlockable patch failed: {ex}"
                );
            }
        }
    }

    [HarmonyPatch(
        typeof(Customer),
        "SampleOptionValid"
    )]
    public static class CustomerSampleOptionMemoryPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            Customer __instance,
            ref bool __result,
            ref string invalidReason)
        {
            try
            {
                if (!CustomerSampleMemory.NeedsSample(
                        __instance))
                {
                    return;
                }

                __instance.sampleOfferedToday = false;

                __result = true;
                invalidReason = string.Empty;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    $"[WVC NPC Memory] SampleOptionValid patch failed: {ex}"
                );
            }
        }
    }

    internal static class CustomerSampleUnlockForcer
    {
        private static readonly HashSet<string> PendingUnlockIds =
            new HashSet<string>();

        private static readonly HashSet<string> UnlockedThisSession =
            new HashSet<string>();

        public static bool IsPending(string npcId)
        {
            return !string.IsNullOrWhiteSpace(npcId) &&
                   PendingUnlockIds.Contains(npcId);
        }

        [HarmonyPatch(
            typeof(Customer),
            "SampleWasSufficient"
        )]
        public static class CustomerSampleAcceptedPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Customer __instance)
            {
                try
                {
                    NPC npc = __instance.NPC;

                    if (npc == null ||
                        string.IsNullOrWhiteSpace(npc.ID) ||
                        !npc.ID.StartsWith("custom_", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (!UnlockedThisSession.Add(npc.ID))
                    {
                        return;
                    }

                    PendingUnlockIds.Add(npc.ID);

                    try
                    {
                        var unlockMethod = HarmonyLib.AccessTools.Method(
                            typeof(Customer),
                            "OnCustomerUnlocked");

                        var unlockType =
                            Il2CppScheduleOne.NPCs.Relation.NPCRelationData.EUnlockType
                                .DirectApproach;

                        unlockMethod?.Invoke(
                            __instance,
                            new object[] { unlockType, true });

                    }
                    finally
                    {
                        PendingUnlockIds.Remove(npc.ID);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error(
                        $"[WVC NPC Memory] SampleAccepted unlock patch failed: {ex}"
                    );
                }
            }
        }

        [HarmonyPatch(
            typeof(Customer),
            "IsUnlockable"
        )]
        public static class CustomerPendingUnlockPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                Customer __instance,
                ref bool __result)
            {
                try
                {
                    if (!__result &&
                        IsPending(CustomerSampleMemory.GetNpcId(__instance)))
                    {
                        __result = true;
                    }
                }
                catch
                {
                }
            }
        }
    }
}
