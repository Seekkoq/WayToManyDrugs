using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using S1API.Law;

namespace CustomNPCExample.Quests
{
    public static class SnitchHeat
    {
        private const float TickInterval = 3f;
        private const float ReportInterval = 15f;
        private const float ActivateRadius = 500f;
        private const float PursuitRadius = 120f;
        private const int MaxPursuers = 3;

        private static bool _active;
        private static bool _postQuest;
        private static bool _lockdownStarted;
        private static bool _lockdownComplete;

        private static bool _insideUpdate;
        private static bool _recursionWarningLogged;

        private static int _generation;
        private static float _tickTimer;
        private static float _reportTimer;
        private static float _lastPursuitCallTime = -999f;

        public static bool IsActive => _active;
        public static bool IsPostQuest => _postQuest;

        public static void RunSinglePatrolTest() { }
        public static void DumpPoliceSpawnApi() { }

        public static void Begin()
        {
            if (_postQuest ||
                SnitchSaveManager.IsCompleted ||
                SnitchStorySave.Stage != SnitchStorySave.STAGE_NONE)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Snitch] Begin() ignored: post-quest/story state is active."
                );
                return;
            }

            _active = true;
            _postQuest = false;
            _tickTimer = 0f;
            _reportTimer = 0f;

            ApplyHardcorePoliceStats();

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Burn-route heat active.");
        }

        public static void TransitionToPostQuestLockdown()
        {
            _active = true;
            _postQuest = true;
            _tickTimer = 0f;
            _reportTimer = 0f;

            ApplyHardcorePoliceStats();
            TryRaiseLawIntensity();

            if (_lockdownComplete)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Post-quest lockdown already complete.");
                return;
            }

            if (_lockdownStarted)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Post-quest lockdown already running.");
                return;
            }

            _lockdownStarted = true;
            _generation++;

            MelonCoroutines.Start(
                LockdownRoutine(_generation)
            );
        }

        public static void End()
        {
            _generation++;

            _active = false;
            _postQuest = false;
            _lockdownStarted = false;
            _lockdownComplete = false;

            _tickTimer = 0f;
            _reportTimer = 0f;
            _lastPursuitCallTime = -999f;

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Heat ended.");
        }

        public static void Update()
        {
            if (_insideUpdate)
            {
                if (!_recursionWarningLogged)
                {
                    _recursionWarningLogged = true;
                    MelonLogger.Error(
                        "[WVC Snitch] Recursive SnitchHeat.Update() call blocked. " +
                        "Search your project for 'SnitchHeat.Update();' and make sure " +
                        "it only appears in SnitchQuestManager.Update()."
                    );
                }

                return;
            }

            _insideUpdate = true;

            try
            {
                UpdateInternal();
            }
            finally
            {
                _insideUpdate = false;
            }
        }

        private static void UpdateInternal()
        {
            if (!_active)
                return;

            _tickTimer += Time.deltaTime;
            _reportTimer += Time.deltaTime;

            if (_reportTimer >= ReportInterval)
            {
                _reportTimer = 0f;
                PrintPatrolCount(_postQuest ? "LOCKDOWN" : "TICK");
            }

            if (!_postQuest && _tickTimer >= TickInterval)
            {
                _tickTimer = 0f;
                ApplyHeat();
            }
        }

        private static IEnumerator LockdownRoutine(int generation)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] === POST-QUEST LOCKDOWN QUEUED ===");

            float deadline = Time.realtimeSinceStartup + 45f;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (generation != _generation || !_active)
                {
                    _lockdownStarted = false;
                    yield break;
                }

                if (IsPoliceSystemReady())
                    break;

                yield return new WaitForSeconds(1f);
            }

            if (generation != _generation || !_active || !IsPoliceSystemReady())
            {
                _lockdownStarted = false;
                yield break;
            }

            yield return new WaitForSeconds(2f);

            if (generation != _generation || !_active)
            {
                _lockdownStarted = false;
                yield break;
            }

            int vehicles = DeployVehiclePatrols();

            yield return new WaitForSeconds(1f);

            if (generation != _generation || !_active)
            {
                _lockdownStarted = false;
                yield break;
            }

            int squads = DeployFootSquads();

            ApplyHardcorePoliceStats();

            _lockdownStarted = false;
            _lockdownComplete = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] === LOCKDOWN COMPLETE === squads=" +
                squads + " vehicles=" + vehicles
            );

            PrintPatrolCount("POST-QUEST LOCKDOWN");
        }

        private static int DeployVehiclePatrols()
        {
            int started = 0;

            try
            {
                VehiclePatrolRoute[] routes =
                    LawManager.GetAllVehiclePatrolRoutes();

                if (routes == null)
                    return 0;

                int use = Mathf.Min(3, routes.Length);

                for (int i = 0; i < use; i++)
                {
                    if (routes[i] == null)
                        continue;

                    try
                    {
                        if (LawManager.StartVehiclePatrol(routes[i]))
                            started++;
                    }
                    catch { }
                }
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Vehicle patrols started: " + started
            );

            return started;
        }

        private static int DeployFootSquads()
        {
            int started = 0;

            try
            {
                FootPatrolRoute[] routes =
                    LawManager.GetAllFootPatrolRoutes();

                if (routes == null || routes.Length == 0)
                    return 0;

                Vector3 playerPos = Vector3.zero;

                try
                {
                    if (Player.Local != null)
                        playerPos = Player.Local.transform.position;
                }
                catch { }

                List<FootPatrolRoute> sorted =
                    new List<FootPatrolRoute>();

                for (int i = 0; i < routes.Length; i++)
                {
                    if (routes[i] != null)
                        sorted.Add(routes[i]);
                }

                sorted.Sort(
                    (a, b) =>
                        Vector3.Distance(a.Position, playerPos)
                            .CompareTo(Vector3.Distance(b.Position, playerPos))
                );

                int use = Mathf.Min(3, sorted.Count);

                for (int i = 0; i < use; i++)
                {
                    int members = i == 0 ? 3 : 2;

                    try
                    {
                        if (LawManager.StartFootPatrol(sorted[i], members) != null)
                            started++;
                    }
                    catch { }
                }
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Foot squads started: " + started
            );

            return started;
        }

        public static void ApplyHardcorePoliceStats()
        {
            if (!IsPoliceSystemReady())
                return;

            try
            {
                PoliceOfficer.INVESTIGATION_MAX_DISTANCE = 100f;
                PoliceOfficer.INVESTIGATION_MIN_VISIBILITY = 0.05f;
                PoliceOfficer.INVESTIGATION_CHECK_INTERVAL = 0.25f;
                PoliceOfficer.INVESTIGATION_COOLDOWN = 2f;
                PoliceOfficer.BODY_SEARCH_CHANCE_DEFAULT = 0.75f;
                PoliceOfficer.OutOfSightTimeToDeactivate = 99999f;
                PoliceOfficer.MIN_CHATTER_INTERVAL = 4f;
                PoliceOfficer.MAX_CHATTER_INTERVAL = 8f;

                var officers = PoliceOfficer.Officers;

                if (officers == null)
                    return;

                for (int i = 0; i < officers.Count; i++)
                {
                    PoliceOfficer officer = officers[i];

                    if (officer == null)
                        continue;

                    try { officer.AutoDeactivate = false; } catch { }
                    try { officer.SetIgnorePlayers(false); } catch { }
                    try { officer.BodySearchChance = 0.75f; } catch { }
                    try { officer.ChatterEnabled = true; } catch { }
                }
            }
            catch { }
        }

        private static void TryRaiseLawIntensity()
        {
            try
            {
                LawController.SetIntensityLevel(10);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Snitch] City law intensity set to 10."
                );
            }
            catch (Exception)
            {

            }
        }

        private static void ApplyHeat()
        {
            if (!IsPoliceSystemReady())
                return;

            if (SnitchSaveManager.IsCompleted)
                return;

            if (_postQuest)
                return;

            if (Time.realtimeSinceStartup - _lastPursuitCallTime < 12f)
                return;

            _lastPursuitCallTime =
                Time.realtimeSinceStartup;

            ApplyHardcorePoliceStats();

            bool calledPolice = false;
            int patrolsStarted = 0;

            try
            {
                S1API.Entities.Player apiPlayer =
                    S1API.Entities.Player.Local;

                if (apiPlayer != null)
                {
                    LawManager.CallPolice(apiPlayer);
                    calledPolice = true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                Vector3 playerPos = Vector3.zero;
                bool hasPlayerPos = false;

                try
                {
                    Player local =
                        Player.Local;

                    if (local != null)
                    {
                        playerPos =
                            local.transform.position;

                        hasPlayerPos = true;
                    }
                }
                catch { }

                if (hasPlayerPos)
                {
                    patrolsStarted =
                        StartNearestFootPatrols(
                            playerPos,
                            1,
                            2
                        );
                }
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Heat dispatch applied. " +
                "CallPolice=" +
                calledPolice +
                " PatrolsStarted=" +
                patrolsStarted +
                ". Native pursuit was NOT forced."
            );
        }

        private static int StartNearestFootPatrols(
    Vector3 position,
    int routeCount,
    int membersPerPatrol)
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
                                membersPerPatrol
                            );

                        if (patrol != null)
                            started++;
                    }
                    catch { }
                }
            }
            catch { }

            return started;
        }

        private static bool IsOfficerBusy(PoliceOfficer officer)
        {
            if (officer == null)
                return true;

            try
            {
                if (officer.PursuitTarget != null)
                    return true;
            }
            catch { }

            try
            {
                if (officer.PursuitBehaviour != null &&
                    officer.PursuitBehaviour.Active)
                {
                    return true;
                }
            }
            catch { }

            try
            {
                if (officer.Health != null &&
                    (officer.Health.IsDead ||
                     officer.Health.IsKnockedOut))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static void PrintPatrolCount(string tag = "")
        {
            int pool = 0;
            int active = 0;
            int patrolling = 0;
            int pursuing = 0;

            try
            {
                var officers = PoliceOfficer.Officers;

                if (officers != null)
                {
                    pool = officers.Count;

                    for (int i = 0; i < officers.Count; i++)
                    {
                        PoliceOfficer officer = officers[i];

                        if (officer == null)
                            continue;

                        bool isActive = false;

                        try
                        {
                            isActive =
                                officer.gameObject != null &&
                                officer.gameObject.activeInHierarchy;
                        }
                        catch { }

                        if (!isActive)
                            continue;

                        active++;

                        if (IsBehaviourOn(officer.FootPatrolBehaviour) ||
                            IsBehaviourOn(officer.VehiclePatrolBehaviour) ||
                            IsBehaviourOn(officer.SentryBehaviour))
                        {
                            patrolling++;
                        }

                        try
                        {
                            if (officer.PursuitTarget != null)
                                pursuing++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            string suffix =
                string.IsNullOrEmpty(tag)
                    ? ""
                    : " " + tag;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch Cops" + suffix + "] pool=" + pool +
                " active=" + active +
                " patrolling=" + patrolling +
                " pursuing=" + pursuing
            );
        }

        private static bool IsPoliceSystemReady()
        {
            try
            {
                return Player.Local != null &&
                       PoliceOfficer.Officers != null &&
                       PoliceOfficer.Officers.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBehaviourOn(object behaviour)
        {
            if (behaviour == null)
                return false;

            try
            {
                MonoBehaviour mb = behaviour as MonoBehaviour;

                return mb != null &&
                       mb.enabled &&
                       mb.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        private static string GetLocalPlayer(out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                Player local = Player.Local;

                if (local == null)
                    return null;

                try
                {
                    position = local.transform.position;
                }
                catch { }

                return GetMemberString(local, "PlayerCode")
                    ?? GetMemberString(local, "playerCode")
                    ?? GetMemberString(local, "Code");
            }
            catch
            {
                return null;
            }
        }

        private static string GetMemberString(
            object target,
            string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null)
                        return property.GetValue(target)?.ToString();

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                        return field.GetValue(target)?.ToString();
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }
    }
}
