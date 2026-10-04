using System;
using HarmonyLib;
using Il2CppScheduleOne.NPCs.Schedules;
using MelonLoader;

namespace CustomNPCExample.SchedulePatches
{
    [HarmonyPatch(typeof(NPCEvent_StayInBuilding), "OnActiveTick")]
    public static class StayInBuildingSafePatch
    {
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            return null;
        }
    }

    [HarmonyPatch(typeof(NPCEvent_StayInBuilding), "LateStarted")]
    public static class StayInBuildingLateStartedSafePatch
    {
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {

            }

            return null;
        }
    }
}
