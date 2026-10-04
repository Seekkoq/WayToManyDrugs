using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

using HarmonyInstance = HarmonyLib.Harmony;
using NativeSupplier = Il2CppScheduleOne.Economy.Supplier;

namespace CustomNPCExample.NPCs
{
    public static class SalViahBulkMeetupManager
    {
        public const float BulkPurchaseMinimum = 500f;

        /// <summary>
        /// Ceiling the phone puts on one dead drop order: forty powder and the press to press it
        /// with. See the sibling bulk managers for why the base game's own number is not used.
        /// </summary>
        public const float DeadDropItemLimit = 2000f;

        public static KeyCode RequestMeetupKey = KeyCode.F6;

        private const float BindInterval = 2f;
        private const float RequestTimeout = 10f;
        private const float MeetingEndGrace = 3f;

        private static HarmonyInstance _harmony;
        private static NativeSupplier _supplier;

        private static readonly HashSet<MethodBase> _patchedTargets =
            new HashSet<MethodBase>();

        private static bool _patched;
        private static bool _requestPending;
        private static bool _meetingActive;
        private static bool _scheduleSuspended;

        private static SalViah _scheduleOwner;

        private static float _bindTimer;
        private static float _requestDeadline;
        private static float _meetingEndTimer;

        private static int _completedBulkMeetups;
        private static float _lifetimeBulkSpend;

        public static int CompletedBulkMeetups =>
            _completedBulkMeetups;

        public static float LifetimeBulkSpend =>
            _lifetimeBulkSpend;

        public static void ApplyPatch()
        {
            if (_patched)
                return;

            try
            {
                if (_harmony == null)
                {
                    _harmony = new HarmonyInstance(
                        "westvilleconnection.salvia.bulkmeetups"
                    );
                }

                bool limitPatched = PatchPostfix(
                    "GetDeadDropLimit",
                    Type.EmptyTypes,
                    nameof(GetDeadDropLimitPostfix)
                );

                bool validityPatched = PatchPostfix(
                    "IsMeetupValid",
                    null,
                    nameof(IsMeetupValidPostfix)
                );

                bool completionPatched = PatchPostfix(
                    "MeetupOrderCompleted",
                    new[] { typeof(float) },
                    nameof(MeetupOrderCompletedPostfix)
                );

                _patched =
                    limitPatched &&
                    validityPatched &&
                    completionPatched;

                if (_patched)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[SalViah Bulk] Native supplier patches applied."
                    );
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[SalViah Bulk] Patch setup failed: " + ex
                );
            }
        }

        private static bool PatchPostfix(
            string methodName,
            Type[] parameterTypes,
            string patchName)
        {
            MethodInfo target = parameterTypes == null
                ? AccessTools.Method(typeof(NativeSupplier), methodName)
                : AccessTools.Method(
                    typeof(NativeSupplier),
                    methodName,
                    parameterTypes
                );

            MethodInfo patch = AccessTools.Method(
                typeof(SalViahBulkMeetupManager),
                patchName
            );

            if (target == null || patch == null)
            {


                return false;
            }

            if (_patchedTargets.Contains(target))
                return true;

            _harmony.Patch(
                target,
                postfix: new HarmonyMethod(patch)
            );

            _patchedTargets.Add(target);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[SalViah Bulk] Patched Supplier." + methodName
            );

            return true;
        }

        public static void Update()
        {
            if (!IsBound())
            {
                if (_requestPending ||
                    _meetingActive ||
                    _scheduleSuspended)
                {
                    ResetRuntime();
                }

                _bindTimer += Time.unscaledDeltaTime;

                if (_bindTimer >= BindInterval)
                {
                    _bindTimer = 0f;
                    TryBind();
                }

                return;
            }

            if (RequestMeetupKey != KeyCode.None &&
                Input.GetKeyDown(RequestMeetupKey))
            {
                RequestBulkMeetup();
            }

            UpdateMeetingState();
        }

        public static bool TryBind()
        {
            SalViah sal = SalViah.Instance;

            if (sal == null || sal.gameObject == null)
                return false;

            try
            {
                NativeSupplier found =
                    sal.gameObject.GetComponent<NativeSupplier>()
                    ?? sal.gameObject.GetComponentInChildren<NativeSupplier>(
                        true
                    );

                if (found == null)
                    return false;

                bool changed =
                    _supplier == null ||
                    _supplier.GetInstanceID() != found.GetInstanceID();

                if (changed)
                {
                    ResumeSchedule();

                    _requestPending = false;
                    _requestDeadline = 0f;
                    _meetingActive = false;
                    _meetingEndTimer = 0f;
                }

                _supplier = found;

                if (changed)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[SalViah Bulk] Bound to Sal Viah's native Supplier. " +
                        "Status=" + SafeStatus(found)
                    );
                }

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static bool IsBound()
        {
            return IsSalViah(_supplier);
        }

        private static bool IsSalViah(NativeSupplier candidate)
        {
            if (candidate == null)
                return false;

            try
            {
                SalViah sal = SalViah.Instance;

                if (sal == null ||
                    sal.gameObject == null ||
                    candidate.gameObject == null)
                {
                    return false;
                }

                Transform salTransform = sal.gameObject.transform;
                Transform supplierTransform = candidate.gameObject.transform;

                return supplierTransform == salTransform ||
                       supplierTransform.IsChildOf(salTransform);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSalUnlocked()
        {
            try
            {
                SalViah sal = SalViah.Instance;

                return sal != null &&
                       sal.Relationship != null &&
                       sal.Relationship.IsUnlocked;
            }
            catch
            {
                return false;
            }
        }

        public static bool RequestBulkMeetup()
        {
            if (!IsBound() && !TryBind())
            {


                return false;
            }

            if (!IsSalUnlocked())
            {


                return false;
            }

            if (_requestPending)
            {


                return false;
            }

            try
            {
                if (_supplier.Status ==
                    NativeSupplier.ESupplierStatus.Meeting)
                {
                    UpdateMeetingState();
                    return true;
                }

                if (_supplier.Status !=
                    NativeSupplier.ESupplierStatus.Idle)
                {


                    return false;
                }

                _requestPending = true;
                _requestDeadline =
                    Time.realtimeSinceStartup + RequestTimeout;

                _supplier.MeetupRequested();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[SalViah Bulk] Native meetup request sent."
                );

                UpdateMeetingState();

                return true;
            }
            catch (Exception ex)
            {
                _requestPending = false;
                _requestDeadline = 0f;

                MelonLogger.Error(
                    "[SalViah Bulk] Meetup request failed: " + ex
                );

                return false;
            }
        }

        private static void UpdateMeetingState()
        {
            if (!IsBound())
                return;

            bool meeting;

            try
            {
                meeting =
                    _supplier.Status ==
                    NativeSupplier.ESupplierStatus.Meeting;
            }
            catch
            {
                return;
            }

            if (meeting)
            {
                _requestPending = false;
                _requestDeadline = 0f;
                _meetingEndTimer = 0f;

                if (!_meetingActive)
                {
                    _meetingActive = true;
                    SuspendSchedule();

                    try
                    {
                        SalViahDialogue.SuppressForMeeting();
                    }
                    catch (Exception)
                    {

                    }

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[SalViah Bulk] Native meetup started."
                    );
                }

                return;
            }

            if (_requestPending &&
                Time.realtimeSinceStartup >= _requestDeadline)
            {
                _requestPending = false;
                _requestDeadline = 0f;


            }

            if (!_meetingActive)
                return;

            _meetingEndTimer += Time.unscaledDeltaTime;

            if (_meetingEndTimer < MeetingEndGrace)
                return;

            _meetingActive = false;
            _meetingEndTimer = 0f;

            ResumeSchedule();

            try
            {
                SalViahDialogue.RestoreAfterMeeting();
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[SalViah Bulk] Native meetup ended."
            );
        }

        private static void SuspendSchedule()
        {
            if (_scheduleSuspended)
                return;

            try
            {
                SalViah sal = SalViah.Instance;

                if (sal == null || sal.Schedule == null)
                    return;

                sal.Schedule.Disable();

                _scheduleOwner = sal;
                _scheduleSuspended = true;
            }
            catch (Exception)
            {

            }
        }

        private static void ResumeSchedule()
        {
            if (!_scheduleSuspended)
                return;

            SalViah owner = _scheduleOwner;

            _scheduleOwner = null;
            _scheduleSuspended = false;

            try
            {
                if (owner != null &&
                    owner.gameObject != null &&
                    owner.Schedule != null)
                {
                    owner.Schedule.Enable();
                }
            }
            catch (Exception)
            {

            }
        }

        private static void IsMeetupValidPostfix(
            NativeSupplier __instance,
            ref bool __result)
        {
            if (!IsSalViah(__instance) || !IsSalUnlocked())
                return;

            try
            {
                if (__instance.Status ==
                    NativeSupplier.ESupplierStatus.Idle)
                {
                    __result = true;
                }
            }
            catch
            {
            }
        }

        private static void GetDeadDropLimitPostfix(
            NativeSupplier __instance,
            ref float __result)
        {
            if (!IsSalViah(__instance))
                return;

            if (__result > 0f)
            {
                __result = Mathf.Min(
                    __result,
                    DeadDropItemLimit
                );
            }
        }

        private static void MeetupOrderCompletedPostfix(
            NativeSupplier __instance,
            float __0)
        {
            if (!IsSalViah(__instance))
                return;

            float amount = __0;

            if (float.IsNaN(amount) ||
                float.IsInfinity(amount) ||
                amount < BulkPurchaseMinimum)
            {
                return;
            }

            _completedBulkMeetups++;
            _lifetimeBulkSpend += amount;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[SalViah Bulk] Bulk cutting order completed. " +
                "Spend=$" + amount.ToString("0.00") +
                " | Meetups=" + _completedBulkMeetups +
                " | Session spend=$" +
                _lifetimeBulkSpend.ToString("0.00")
            );
        }

        private static string SafeStatus(NativeSupplier supplier)
        {
            try
            {
                return supplier != null
                    ? supplier.Status.ToString()
                    : "unbound";
            }
            catch
            {
                return "?";
            }
        }

        public static void ResetRuntime()
        {
            ResumeSchedule();

            _supplier = null;
            _requestPending = false;
            _requestDeadline = 0f;
            _meetingActive = false;
            _meetingEndTimer = 0f;
            _bindTimer = 0f;
        }
    }
}
