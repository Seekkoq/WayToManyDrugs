using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;
using S1API.Entities.Dialogue;

namespace CustomNPCExample.NPCs
{
    public static class PillVilleDialogue
    {
        public const string ContainerName = "wvc_pill_ville_xanax_intro";

        private const string EntryNode = "ENTRY";
        private const string StockNode = "PILL_STOCK";
        private const string DropNode = "PILL_DROP";
        private const string SampleNode = "PILL_SAMPLE";

        private const string AskStock = "pill_ask_stock";
        private const string AskDrop = "pill_ask_drop";
        private const string AskSample = "pill_ask_sample";
        private const string LeaveChoice = "pill_leave";
        private const string FinishChoice = "pill_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;
        private static bool _suppressedForMeeting;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed("pill_ville");

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            try
            {
                dialogue.BuildAndRegisterContainer(ContainerName, builder =>
                {
                    builder.SetAllowExit(true);

                    builder.AddNode(EntryNode,
                        "Eleanor Scrivens. I run the pharmacy here.\n\nThe good bars never make the shelf. They come to me instead.",
                        choices =>
                        {
                            choices.Add(AskStock, "Let's see them.", StockNode);
                            choices.Add(AskDrop, "Where do I leave the money?", DropNode);
                            choices.Add(LeaveChoice, "Not today.", null);
                        });

                    builder.AddNode(StockNode,
                        "Xanax, straight out of the back. Order it on your phone and I leave it in the drop.\n\nFirst one is free if you want to try before you buy.",
                        choices =>
                        {
                            if (!SampleClaimed)
                                choices.Add(AskSample, "I'll take the free one.", SampleNode);
                            choices.Add(AskDrop, "Where's the drop?", DropNode);
                            choices.Add(LeaveChoice, "Maybe later.", null);
                        });

                    builder.AddNode(DropNode, BuildDropText(),
                        choices =>
                        {
                            choices.Add(AskStock, "Show me the stock.", StockNode);
                            if (!SampleClaimed)
                                choices.Add(AskSample, "I'll take the free one.", SampleNode);
                            choices.Add(LeaveChoice, "Understood.", null);
                        });

                    builder.AddNode(SampleNode,
                        "Here - one bar, on the house.\n\nTake half first. That's the only warning you get.",
                        choices =>
                        {
                            choices.Add(FinishChoice, "Thanks.", null);
                            choices.Add(LeaveChoice, "Not now.", null);
                        });
                });

                if (!_callbackHooked)
                {
                    dialogue.OnChoiceSelected(AskSample, OnSampleChosen);
                    _callbackHooked = true;
                }

                bool armed = dialogue.UseContainerOnInteract(ContainerName);
                if (armed)
                    WvcLog.Msg("[PillVille] Xanax dialogue armed.");

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PillVille] Dialogue setup failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildDropText()
        {
            try
            {
                if (DeadDropManager.Get<BehindMedicalPractice>() != null)
                    return "Behind the medical practice. It's marked on your map.\n\nCash in the pouch, then keep walking. Order first, pay after.";
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PillVille] Drop lookup failed: " + ex.Message);
            }

            return "Behind the medical practice, marked on your map. Leave the cash in the pouch and keep walking.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                WvcLog.Msg("[PillVille] Sample already claimed.");
                return;
            }

            if (!WvcGiveItem.TryGive("xanax", 1) &&
                !WvcGiveItem.TryGive("westvilleconnection:products/xanax_v1", 1))
            {
                MelonLogger.Warning("[PillVille] Could not give the Xanax sample. Player can retry.");
                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed("pill_ville");

            try
            {
                if (_activeDialogue != null)
                    TryRegisterAndArm(_activeDialogue);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PillVille] Failed to refresh dialogue after sample: " + ex.Message);
            }

            WvcLog.Msg("[PillVille] Gave player a Xanax bar. Sample option removed.");
        }

        public static void SuppressForMeeting()
        {
            if (_suppressedForMeeting)
                return;

            _suppressedForMeeting = true;

            try
            {
                if (_activeDialogue != null)
                    _activeDialogue.StopOverride();

                WvcLog.Msg("[PillVille] Intro dialogue suppressed for native supplier meetup.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PillVille] Could not suppress intro dialogue: " + ex.Message);
            }
        }

        public static void RestoreAfterMeeting()
        {
            if (!_suppressedForMeeting)
                return;

            _suppressedForMeeting = false;

            if (_activeDialogue == null)
                return;

            try
            {
                WvcLog.Msg("[PillVille] Intro dialogue restored after meetup. Armed=" +
                    TryRegisterAndArm(_activeDialogue));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PillVille] Could not restore intro dialogue: " + ex.Message);
            }
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample("pill_ville");
            WvcLog.Msg("[PillVille] Sample state reset.");
        }
    }
}