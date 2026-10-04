using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Law;
using UnityEngine;

using ApiPlayer = S1API.Entities.Player;
using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;

namespace CustomNPCExample.Law
{
    public static class UndercoverPoliceReveal
    {
        private const int PatrolRoutesNormal = 1;
        private const int OfficersPerPatrolNormal = 2;
        private const int DispatchCallsNormal = 1;

        private const int PatrolRoutesHigh = 4;
        private const int OfficersPerPatrolHigh = 4;
        private const int DispatchCallsHigh = 3;

        private static bool IsHighHeat(out string status)
        {
            bool burnActive =
                CustomNPCExample.Quests
                    .SnitchSaveManager
                    .IsBurnRouteActive;

            bool completed =
                CustomNPCExample.Quests
                    .SnitchSaveManager
                    .IsCompleted;

            if (burnActive)
            {
                status = "Burn Route Active";
                return true;
            }

            if (completed)
            {
                status = "Snitch Quest Completed";
                return true;
            }

            status = "Normal gameplay";
            return false;
        }

        public static void StartReveal(Vector3 revealPosition)
        {
            MelonCoroutines.Start(
                StartRevealRoutine(revealPosition)
            );
        }

        private static IEnumerator StartRevealRoutine(
            Vector3 revealPosition)
        {
            NativePlayer target = null;

            try
            {
                target = NativePlayer.Local;
            }
            catch { }

            if (target == null)
            {


                yield break;
            }

            string heatStatus;
            bool highHeat =
                IsHighHeat(out heatStatus);

            int routesToUse =
                highHeat
                    ? PatrolRoutesHigh
                    : PatrolRoutesNormal;

            int officersPerRoute =
                highHeat
                    ? OfficersPerPatrolHigh
                    : OfficersPerPatrolNormal;

            int dispatchCalls =
                highHeat
                    ? DispatchCallsHigh
                    : DispatchCallsNormal;

            global::CustomNPCExample.Utils.WvcLog.Msg("--------------------------------------------------");
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Undercover] === BUST AMBUSH TRIGGERED ===");
            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Undercover] Heat Mode: " +
                (highHeat ? "HIGH HEAT" : "NORMAL") +
                " (" +
                heatStatus +
                ")"
            );
            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Undercover] Spawn Routes: " +
                routesToUse +
                " | Officers Per Route: " +
                officersPerRoute
            );
            global::CustomNPCExample.Utils.WvcLog.Msg("--------------------------------------------------");

            int patrolsSpawned =
                TrySpawnManyPatrols(
                    revealPosition,
                    routesToUse,
                    officersPerRoute
                );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Undercover] Spawned " +
                patrolsSpawned +
                " reinforcement patrol squads."
            );

            yield return new WaitForSeconds(0.25f);

            int calls =
                CallPoliceSafely(dispatchCalls);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Undercover] === BUST AMBUSH COMPLETE === " +
                "DispatchCalls=" +
                calls +
                " PatrolSquads=" +
                patrolsSpawned +
                ". Native pursuit was NOT forced."
            );
        }

        private static int CallPoliceSafely(int times)
        {
            ApiPlayer player = null;

            try
            {
                player = ApiPlayer.Local;
            }
            catch { }

            if (player == null)
            {


                return 0;
            }

            int called = 0;

            for (int i = 0; i < times; i++)
            {
                try
                {
                    LawManager.CallPolice(player);
                    called++;
                }
                catch (Exception)
                {

                }
            }

            return called;
        }

        private static int TrySpawnManyPatrols(
            Vector3 position,
            int routeCount,
            int officersPerPatrol)
        {
            int started = 0;

            try
            {
                FootPatrolRoute[] routes =
                    LawManager.GetAllFootPatrolRoutes();

                if (routes == null ||
                    routes.Length == 0)
                {
                    return 0;
                }

                List<FootPatrolRoute> sorted =
                    new List<FootPatrolRoute>();

                for (int i = 0; i < routes.Length; i++)
                {
                    if (routes[i] != null)
                        sorted.Add(routes[i]);
                }

                sorted.Sort(
                    (a, b) =>
                    {
                        float da =
                            Vector3.Distance(
                                a.Position,
                                position
                            );

                        float db =
                            Vector3.Distance(
                                b.Position,
                                position
                            );

                        return da.CompareTo(db);
                    }
                );

                int use =
                    Mathf.Min(
                        routeCount,
                        sorted.Count
                    );

                for (int i = 0; i < use; i++)
                {
                    try
                    {
                        object patrol =
                            LawManager.StartFootPatrol(
                                sorted[i],
                                officersPerPatrol
                            );

                        if (patrol != null)
                            started++;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception)
            {

            }

            return started;
        }
    }
}
