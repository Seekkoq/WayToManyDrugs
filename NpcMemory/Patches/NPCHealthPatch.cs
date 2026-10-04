using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NpcMemory.Patches
{
    [HarmonyPatch(typeof(NPCHealth))]
    public static class NpcHealthPatch
    {
        private const float PlayerKillWindowSeconds = 12f;

        private const float HitPenalty = -0.15f;
        private const float KillPenalty = -0.35f;

        private static readonly Dictionary<string, float> LastPlayerHitTimes =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        [HarmonyPatch("NotifyAttackedByPlayer")]
        [HarmonyPostfix]
        private static void NotifyAttackedByPlayer_Postfix(NPCHealth __instance)
        {
            try
            {
                if (__instance == null || __instance.npc == null) return;
                string npcId = __instance.npc.ID;
                if (string.IsNullOrWhiteSpace(npcId)) return;

                LastPlayerHitTimes[npcId] = Time.time;

                if (HitPenalty < 0f && __instance.npc.RelationData != null)
                {
                    __instance.npc.RelationData.ChangeRelationship(HitPenalty, true);
                }

                global::CustomNPCExample.Utils.WvcLog.Msg($"[WVC NPC Memory] {npcId} was hit by player.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC NPC Memory] Hit patch failed: " + ex);
            }
        }

        [HarmonyPatch("Die")]
        [HarmonyPostfix]
        private static void Die_Postfix(NPCHealth __instance)
        {
            ProcessDowned(__instance, "killed");
        }

        [HarmonyPatch("KnockOut")]
        [HarmonyPostfix]
        private static void KnockOut_Postfix(NPCHealth __instance)
        {
            ProcessDowned(__instance, "knocked out");
        }

        private static void ProcessDowned(NPCHealth health, string reason)
        {
            try
            {
                if (health == null || health.npc == null) return;
                string npcId = health.npc.ID;
                if (string.IsNullOrWhiteSpace(npcId)) return;

                if (!LastPlayerHitTimes.TryGetValue(npcId, out float lastHit))
                    return;

                LastPlayerHitTimes.Remove(npcId);

                if (Time.time - lastHit > PlayerKillWindowSeconds)
                    return;

                int strikes = NpcMemoryManager.AddStrike(npcId);

                NpcMemoryManager.RequireSample(npcId);

                if (health.npc.RelationData != null)
                {
                    health.npc.RelationData.ChangeRelationship(KillPenalty, true);
                }

                NpcMemoryManager.Save();


            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[WVC NPC Memory] {reason} patch failed: " + ex);
            }
        }
    }
}
