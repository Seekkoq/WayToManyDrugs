using System;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Quests;
using MelonLoader;

using HarmonyInstance = HarmonyLib.Harmony;
using HarmonyPatchMethod = HarmonyLib.HarmonyMethod;

namespace CustomNPCExample.Law
{
    public static class UndercoverStingPatch
    {
        private static HarmonyInstance _harmony;
        private static bool _patched;

        public static void ApplyPatch()
        {
            if (_patched)
                return;

            try
            {
                _harmony = new HarmonyInstance("westvilleconnection.sting");

                var offerContract = AccessTools.Method(
                    typeof(Customer),
                    "OfferContract",
                    new Type[] { typeof(ContractInfo) }
                );

                if (offerContract != null)
                {
                    _harmony.Patch(
                        offerContract,
                        postfix: new HarmonyPatchMethod(
                            typeof(UndercoverStingPatch),
                            nameof(OfferContractPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Sting] Patched OfferContract.");
                }
                else
                {
                    MelonLogger.Error("[WVC Sting] OfferContract not found.");
                }

                var processHandover = AccessTools.Method(
                    typeof(Customer),
                    "ProcessHandover"
                );

                if (processHandover != null)
                {
                    _harmony.Patch(
                        processHandover,
                        postfix: new HarmonyPatchMethod(
                            typeof(UndercoverStingPatch),
                            nameof(ProcessHandoverPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Sting] Patched ProcessHandover.");
                }
                else
                {
                    MelonLogger.Error("[WVC Sting] ProcessHandover not found.");
                }

                _patched = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Sting] Sting system active.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Sting] Patch failed: " + ex);
            }
        }

        private static void OfferContractPostfix(Customer __instance)
        {
            UndercoverStingManager.RollForContract(__instance);
        }

        private static void ProcessHandoverPostfix(Customer __instance)
        {
            if (__instance == null || __instance.NPC == null)
                return;

            if (!UndercoverStingManager.IsSting(__instance))
                return;

            UndercoverStingManager.Clear(__instance);

            UndercoverBust.Trigger(__instance.NPC.FullName);

            UndercoverPoliceReveal.StartReveal(
                __instance.NPC.gameObject.transform.position
            );
        }
    }
}