using System;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.Quests;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.CustomerLoyalty
{
    [HarmonyPatch(typeof(Customer), "TryGenerateContract")]
    public static class LoyalCustomerPaymentPatch
    {
        public const float LoyalThresholdPercent = 0.80f;
        public const float LoyalPaymentMultiplier = 1.25f;

        [HarmonyPostfix]
        private static void Postfix(
            Customer __instance,
            ref ContractInfo __result)
        {
            try
            {
                if (__instance == null || __result == null)
                    return;

                NPC npc = __instance.NPC;

                if (npc == null ||
                    string.IsNullOrWhiteSpace(npc.ID))
                {
                    return;
                }

                NPCRelationData relationData = npc.RelationData;

                if (relationData == null)
                    return;

                if (!IsLoyal(relationData))
                    return;

                float oldPayment = __result.Payment;

                float newPayment = Mathf.Round(
                    oldPayment * LoyalPaymentMultiplier
                );

                __result.Payment = Mathf.Max(
                    oldPayment,
                    newPayment
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Loyalty] " +
                    npc.FullName +
                    " is loyal. Deal payment increased from $" +
                    oldPayment.ToString("0") +
                    " to $" +
                    __result.Payment.ToString("0") +
                    "."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Loyalty] Payment bonus failed: " + ex
                );
            }
        }

        public static bool IsLoyal(NPCRelationData relationData)
        {
            if (relationData == null)
                return false;

            float maximum = NPCRelationData.MaxRelationship;

            if (maximum <= 0f)
                return false;

            float loyalThreshold =
                maximum * LoyalThresholdPercent;

            return relationData.RelationDelta >= loyalThreshold;
        }

        public static float GetLoyalThreshold()
        {
            float maximum = NPCRelationData.MaxRelationship;

            if (maximum <= 0f)
                return 0f;

            return maximum * LoyalThresholdPercent;
        }

        public static float GetPaymentMultiplier(
            NPCRelationData relationData)
        {
            return IsLoyal(relationData)
                ? LoyalPaymentMultiplier
                : 1f;
        }
    }
}