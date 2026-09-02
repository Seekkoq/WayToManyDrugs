using System.Collections.Generic;
using CustomNPCExample.Quests;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Law
{
    public static class UndercoverStingManager
    {
        private static readonly HashSet<int> _flagged = new HashSet<int>();
        private static bool? _lastLoggedHeat = null;

        private static int KeyFor(Customer customer)
        {
            if (customer == null || customer.NPC == null)
                return 0;

            return customer.NPC.gameObject.GetInstanceID();
        }

        private static bool IsHighHeat(out string reason)
        {
            bool burnActive = CustomNPCExample.Quests.SnitchSaveManager.IsBurnRouteActive;
            bool completed = CustomNPCExample.Quests.SnitchSaveManager.IsCompleted;

            if (burnActive)
            {
                reason = "Burn Route Active (0/7 drops in progress)";
                return true;
            }

            if (completed)
            {
                reason = "The Snitch Arc Completed (Names in the boxes)";
                return true;
            }

            reason = "Standard Gameplay (Quest inactive)";
            return false;
        }

        private static float GetStingChance(
            out bool isHighHeat,
            out string reason)
        {
            bool burnRouteActive =
                SnitchSaveManager.IsBurnRouteActive;

            bool snitchCompleted =
                SnitchSaveManager.IsCompleted;

            if (burnRouteActive)
            {
                isHighHeat = true;
                reason = "Burn Route Active";
            }
            else if (snitchCompleted)
            {
                isHighHeat = false;
                reason = "Remy extorted; post-arc sting chance reduced";
            }
            else
            {
                isHighHeat = false;
                reason = "Standard Gameplay";
            }

            float chance;

            if (burnRouteActive)
            {
                chance = 0.40f;
            }
            else if (snitchCompleted)
            {
                chance = 0.08f;
            }
            else
            {
                chance = 0.08f;
            }

            if (_lastLoggedHeat == null ||
                _lastLoggedHeat.Value != isHighHeat)
            {
                _lastLoggedHeat = isHighHeat;

                MelonLogger.Msg(
                    "[WVC Sting State] Sting chance -> " +
                    $"{chance * 100f:F0}% | Reason: {reason}"
                );
            }

            return chance;
        }

        public static void RollForContract(Customer customer)
        {
            if (customer == null || customer.NPC == null)
                return;

            int key = KeyFor(customer);
            if (key == 0 || _flagged.Contains(key))
                return;

            bool isHighHeat;
            string reason;
            float chance = GetStingChance(out isHighHeat, out reason);
            float roll = UnityEngine.Random.value;

            bool isSting = roll <= chance;

            MelonLogger.Msg(
                $"[WVC Sting Roll] Customer: {customer.NPC.FullName} | " +
                $"Heat: {(isHighHeat ? "HIGH [40%]" : "NORMAL [8%]")} | " +
                $"Roll: {roll:F3} vs Threshold: {chance:F2} -> " +
                $"{(isSting ? "STING TRIGGERED!" : "Safe Deal")}"
            );

            if (!isSting)
                return;

            _flagged.Add(key);

            MelonLogger.Msg(
                $"[WVC Sting] *** UNDERCOVER FLAG SET *** Contract from {customer.NPC.FullName} will trigger a bust on handover! Reason: {reason}"
            );
        }

        public static bool IsSting(Customer customer)
        {
            int key = KeyFor(customer);
            return key != 0 && _flagged.Contains(key);
        }

        public static void Clear(Customer customer)
        {
            int key = KeyFor(customer);
            if (key != 0)
                _flagged.Remove(key);
        }

        public static void ForceNextContractSting(Customer customer)
        {
            if (customer == null || customer.NPC == null)
                return;

            int key = KeyFor(customer);
            if (key == 0)
                return;

            _flagged.Add(key);

            MelonLogger.Msg(
                "[WVC Sting] Forced sting flag on " +
                customer.NPC.FullName
            );
        }

        public static void ClearAll()
        {
            _flagged.Clear();
            MelonLogger.Msg("[WVC Sting] Cleared all sting flags.");
        }
    }
}