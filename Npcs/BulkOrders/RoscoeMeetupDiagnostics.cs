using System;
using System.Reflection;
using CustomNPCExample.NPCs;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using UnityEngine;
using NativeSupplier =
    Il2CppScheduleOne.Economy.Supplier;

namespace WayToManyDrugs.Npcs.BulkOrders
{
    public static class RoscoeMeetupDiagnostics
    {
        public static KeyCode DumpKey =
            KeyCode.F8;

        private static HarmonyLib.Harmony _harmony;
        private static bool _patched;

        public static void ApplyPatch()
        {
            if (_patched)
                return;

            try
            {
                _harmony =
                    new HarmonyLib.Harmony(
                        "westvilleconnection.roscoe.meetupdiag"
                    );

                PatchIsMeetupValid();
                PatchMeetupRequested();
                PatchMeetAtLocation();
                PatchEndMeeting();

                _patched = true;

                MelonLogger.Msg(
                    "[WVC Diag] Roscoe meetup diagnostics active. " +
                    "Press F8 to dump supplier state."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Diag] Patch failed: " + ex
                );
            }
        }

        public static void Update()
        {
            if (Input.GetKeyDown(DumpKey))
                DumpRoscoeState();
        }

        // ------------------------------------------------------------
        // Patches
        // ------------------------------------------------------------

        private static void PatchIsMeetupValid()
        {
            MethodInfo method =
                AccessTools.Method(
                    typeof(NativeSupplier),
                    "IsMeetupValid"
                );

            if (method == null)
            {
                MelonLogger.Warning(
                    "[WVC Diag] IsMeetupValid not found."
                );

                return;
            }

            _harmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(RoscoeMeetupDiagnostics),
                    nameof(IsMeetupValidPostfix)
                )
            );

            MelonLogger.Msg(
                "[WVC Diag] Patched Supplier.IsMeetupValid."
            );
        }

        private static void PatchMeetupRequested()
        {
            MethodInfo method =
                AccessTools.Method(
                    typeof(NativeSupplier),
                    "MeetupRequested"
                );

            if (method == null)
                return;

            _harmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(RoscoeMeetupDiagnostics),
                    nameof(MeetupRequestedPostfix)
                )
            );

            MelonLogger.Msg(
                "[WVC Diag] Patched Supplier.MeetupRequested."
            );
        }

        private static void PatchMeetAtLocation()
        {
            MethodInfo method =
                AccessTools.Method(
                    typeof(NativeSupplier),
                    "MeetAtLocation"
                );

            if (method == null)
                return;

            _harmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(RoscoeMeetupDiagnostics),
                    nameof(MeetAtLocationPostfix)
                )
            );

            MelonLogger.Msg(
                "[WVC Diag] Patched Supplier.MeetAtLocation."
            );
        }

        private static void PatchEndMeeting()
        {
            MethodInfo method =
                AccessTools.Method(
                    typeof(NativeSupplier),
                    "EndMeeting"
                );

            if (method == null)
                return;

            _harmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(RoscoeMeetupDiagnostics),
                    nameof(EndMeetingPostfix)
                )
            );

            MelonLogger.Msg(
                "[WVC Diag] Patched Supplier.EndMeeting."
            );
        }

        // ------------------------------------------------------------
        // Postfixes
        // ------------------------------------------------------------

        private static void IsMeetupValidPostfix(
            NativeSupplier __instance,
            ref bool __result)
        {
            if (!IsRoscoe(__instance))
                return;

            MelonLogger.Msg(
                "[WVC Diag] IsMeetupValid -> " +
                __result +
                " | Status=" +
                SafeStatus(__instance) +
                " | Rel=" +
                SafeRelationship(__instance) +
                " | Requirement=" +
                SafeMeetupRequirement()
            );
        }

        private static void MeetupRequestedPostfix(
            NativeSupplier __instance)
        {
            if (!IsRoscoe(__instance))
                return;

            MelonLogger.Msg(
                "[WVC Diag] MeetupRequested ran. Status=" +
                SafeStatus(__instance)
            );
        }

        private static void MeetAtLocationPostfix(
            NativeSupplier __instance)
        {
            if (!IsRoscoe(__instance))
                return;

            MelonLogger.Msg(
                "[WVC Diag] MeetAtLocation ran. Status=" +
                SafeStatus(__instance) +
                " | Location=" +
                SafeLocationName(__instance)
            );
        }

        private static void EndMeetingPostfix(
            NativeSupplier __instance)
        {
            if (!IsRoscoe(__instance))
                return;

            MelonLogger.Msg(
                "[WVC Diag] EndMeeting ran. Status=" +
                SafeStatus(__instance)
            );
        }

        // ------------------------------------------------------------
        // Dump
        // ------------------------------------------------------------

        public static void DumpRoscoeState()
        {
            NativeSupplier supplier =
                FindRoscoeSupplier();

            MelonLogger.Msg(
                "========== WVC ROSCOE SUPPLIER DUMP =========="
            );

            if (supplier == null)
            {
                MelonLogger.Warning(
                    "[WVC Diag] Roscoe native Supplier not found."
                );

                MelonLogger.Msg(
                    "============================================="
                );

                return;
            }

            MelonLogger.Msg(
                "Status=" +
                SafeStatus(supplier)
            );

            MelonLogger.Msg(
                "Relationship=" +
                SafeRelationship(supplier) +
                " | MeetupRequirement=" +
                SafeMeetupRequirement() +
                " | DeliveryRequirement=" +
                SafeDeliveryRequirement()
            );

            MelonLogger.Msg(
                "DeliveriesEnabled=" +
                SafeBool(supplier, "DeliveriesEnabled") +
                " | Debt=" +
                SafeFloat(supplier, "Debt")
            );

            object shop =
                GetMemberValue(supplier, "Shop");

            MelonLogger.Msg(
                "Shop=" +
                (shop != null ? shop.GetType().Name : "null")
            );

            object stash =
                GetMemberValue(supplier, "Stash");

            MelonLogger.Msg(
                "Stash=" +
                (stash != null ? stash.GetType().Name : "null")
            );

            object meetingAction =
                GetMemberValue(supplier, "_meetingAction");

            MelonLogger.Msg(
                "MeetingAction=" +
                (meetingAction != null
                    ? meetingAction.GetType().Name
                    : "null")
            );

            MelonLogger.Msg(
                "CurrentLocation=" +
                SafeLocationName(supplier)
            );

            MelonLogger.Msg(
                "MinsSinceLastMeetingEnd=" +
                SafeInt(supplier, "_minsSinceLastMeetingEnd") +
                " | MeetupCooldown=" +
                SafeMeetupCooldown()
            );

            MelonLogger.Msg(
                "============================================="
            );
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static NativeSupplier FindRoscoeSupplier()
        {
            try
            {
                RoscoeBellweather roscoe =
                    RoscoeBellweather.Instance;

                if (roscoe == null ||
                    roscoe.gameObject == null)
                {
                    return null;
                }

                NativeSupplier supplier =
                    roscoe.gameObject
                        .GetComponent<NativeSupplier>();

                if (supplier != null)
                    return supplier;

                supplier =
                    roscoe.gameObject
                        .GetComponentInChildren<NativeSupplier>(
                            true
                        );

                if (supplier != null)
                    return supplier;

                Transform root =
                    roscoe.gameObject.transform.root;

                if (root != null)
                {
                    supplier =
                        root.gameObject
                            .GetComponentInChildren<NativeSupplier>(
                                true
                            );
                }

                return supplier;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsRoscoe(
            NativeSupplier supplier)
        {
            if (supplier == null)
                return false;

            try
            {
                RoscoeBellweather roscoe =
                    RoscoeBellweather.Instance;

                if (roscoe != null &&
                    roscoe.gameObject != null &&
                    supplier.gameObject != null)
                {
                    Transform a =
                        roscoe.gameObject.transform.root;

                    Transform b =
                        supplier.gameObject.transform.root;

                    if (a != null &&
                        b != null &&
                        a.GetInstanceID() == b.GetInstanceID())
                    {
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                string name =
                    supplier.name ?? string.Empty;

                if (name.IndexOf(
                        "roscoe",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static string SafeStatus(
            NativeSupplier supplier)
        {
            try
            {
                return supplier.Status.ToString();
            }
            catch
            {
                return "?";
            }
        }

        private static string SafeRelationship(
            NativeSupplier supplier)
        {
            object value =
                GetMemberValue(supplier, "RelationDelta") ??
                GetMemberValue(supplier, "Relationship") ??
                GetMemberValue(supplier, "relationship");

            if (value == null)
            {
                object data =
                    GetMemberValue(supplier, "RelationData");

                if (data != null)
                {
                    value =
                        GetMemberValue(data, "RelationDelta") ??
                        GetMemberValue(data, "Delta");
                }
            }

            return value != null
                ? value.ToString()
                : "?";
        }

        private static string SafeMeetupRequirement()
        {
            try
            {
                return NativeSupplier
                    .MeetupRelationshipRequirement
                    .ToString("0.00");
            }
            catch
            {
                return "?";
            }
        }

        private static string SafeDeliveryRequirement()
        {
            try
            {
                return NativeSupplier
                    .DeliveryRelationshipRequirement
                    .ToString("0.00");
            }
            catch
            {
                return "?";
            }
        }

        private static string SafeMeetupCooldown()
        {
            try
            {
                return NativeSupplier
                    .MeetupCooldown
                    .ToString();
            }
            catch
            {
                return "?";
            }
        }

        private static string SafeLocationName(
            NativeSupplier supplier)
        {
            object location =
                GetMemberValue(supplier, "_currentLocation");

            if (location == null)
                return "null";

            try
            {
                Component component =
                    location as Component;

                if (component != null)
                    return component.gameObject.name;
            }
            catch { }

            return location.GetType().Name;
        }

        private static string SafeBool(
            object target,
            string name)
        {
            object value =
                GetMemberValue(target, name);

            return value != null
                ? value.ToString()
                : "?";
        }

        private static string SafeFloat(
            object target,
            string name)
        {
            object value =
                GetMemberValue(target, name);

            if (value == null)
                return "?";

            try
            {
                return Convert.ToSingle(value)
                    .ToString("0.00");
            }
            catch
            {
                return value.ToString();
            }
        }

        private static string SafeInt(
            object target,
            string name)
        {
            object value =
                GetMemberValue(target, name);

            if (value == null)
                return "?";

            try
            {
                return Convert.ToInt32(value)
                    .ToString();
            }
            catch
            {
                return value.ToString();
            }
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            Type type =
                target.GetType();

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

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(target);
                    }
                }
                catch { }

                try
                {
                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                        return field.GetValue(target);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }
    }
}