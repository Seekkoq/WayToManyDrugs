using System;
using MelonLoader;
using S1API.Entities.Schedule;

namespace CustomNPCExample.NPCs
{
    internal static class NpcScheduleMixins
    {
        public static void AtmStop(
            PrefabScheduleBuilder plan, int startTime, string label)
        {
            try
            {
                plan.UseATM(startTime, null, label);
            }
            catch (Exception)
            {

            }
        }

        public static void VendingStop(
            PrefabScheduleBuilder plan, int startTime, string label)
        {
            try
            {
                plan.UseVendingMachine(startTime, null, label);
            }
            catch (Exception)
            {

            }
        }

        public static void SitStop(
            PrefabScheduleBuilder plan,
            int startTime,
            int durationMinutes,
            string label)
        {
            try
            {
                if (!NpcActivityCache.Loaded || NpcActivityCache.SeatSetCount == 0)
                    return;

                int index = NpcActivityCache.StableIndex(
                    label, NpcActivityCache.SeatSetCount);

                if (!NpcActivityCache.TryGetSeatSet(index, out string name, out string path))
                    return;

                plan.SitAtSeatSet(name, startTime, durationMinutes, false, label, path);
            }
            catch (Exception)
            {

            }
        }
    }
}
