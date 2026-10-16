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

        // 0.4.7 turned NPCHealth.npc (property) into a private backing field exposed as the
        // public _npc. Our reference assembly still has the old property, so read whichever
        // member the running game exposes, by reflection, to work across both builds.
        private static NPC ResolveNpc(NPCHealth health)
        {
            if (health == null)
                return null;

            var type = health.GetType();

            var npcProp = type.GetProperty("npc",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (npcProp != null && npcProp.PropertyType == typeof(NPC))
                return npcProp.GetValue(health, null) as NPC;

            var npcField = type.GetField("_npc",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
                ?? type.GetField("npc",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            if (npcField != null && npcField.FieldType == typeof(NPC))
                return npcField.GetValue(health) as NPC;

            return null;
        }

        private static readonly Dictionary<string, float> LastPlayerHitTimes =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        [HarmonyPatch("NotifyAttackedByPlayer")]
        [HarmonyPostfix]
        private static void NotifyAttackedByPlayer_Postfix(NPCHealth __instance)
        {
            try
            {
                var npc = ResolveNpc(__instance);
                if (npc == null) return;
                string npcId = npc.ID;
                if (string.IsNullOrWhiteSpace(npcId)) return;

                LastPlayerHitTimes[npcId] = Time.time;

                if (HitPenalty < 0f && npc.RelationData != null)
                {
                    npc.RelationData.ChangeRelationship(HitPenalty, true);
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
                var npc = ResolveNpc(health);
                if (npc == null) return;
                string npcId = npc.ID;
                if (string.IsNullOrWhiteSpace(npcId)) return;

                if (!LastPlayerHitTimes.TryGetValue(npcId, out float lastHit))
                    return;

                LastPlayerHitTimes.Remove(npcId);

                if (Time.time - lastHit > PlayerKillWindowSeconds)
                    return;

                int strikes = NpcMemoryManager.AddStrike(npcId);

                NpcMemoryManager.RequireSample(npcId);

                if (npc.RelationData != null)
                {
                    npc.RelationData.ChangeRelationship(KillPenalty, true);
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
