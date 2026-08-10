using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class RoscoeDialogue
    {
        public const string ContainerName = "wvc_roscoe_mdma_intro";

        private const string EntryNode = "ENTRY";
        private const string DetailsNode = "MDMA_DETAILS";
        private const string StockNode = "MDMA_STOCK";
        private const string SampleNode = "MDMA_SAMPLE";

        private const string AskChoice = "roscoe_mdma_ask";
        private const string BrowseChoice = "roscoe_mdma_browse";
        private const string SampleChoice = "roscoe_mdma_sample";
        private const string LeaveChoice = "roscoe_mdma_leave";
        private const string FinishChoice = "roscoe_mdma_finish";

        private static bool _sampleClaimed;
        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed => _sampleClaimed;

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            // If the sample was already claimed, do not arm the override again.
            if (_sampleClaimed)
            {
                try
                {
                    dialogue.StopOverride();
                }
                catch
                {
                }

                MelonLogger.Msg("[Roscoe] Sample already claimed. Intro dialogue stays disabled.");
                return true;
            }

            try
            {
                dialogue.BuildAndRegisterContainer(
                    ContainerName,
                    builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(
                            EntryNode,
                            "Word is you're looking to branch out. MDMA might be worth your time. I can supply the Safrole Oil and PMK Powder.",
                            choices =>
                            {
                                choices.Add(
                                    AskChoice,
                                    "What do I need to make it?",
                                    DetailsNode
                                );

                                choices.Add(
                                    BrowseChoice,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "I'm not interested right now.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            DetailsNode,
                            "Start with Safrole Oil and PMK Powder. Get your process right and you'll have something people will pay good money for. I can keep both ingredients coming.",
                            choices =>
                            {
                                choices.Add(
                                    BrowseChoice,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Maybe another time.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            StockNode,
                            "Safrole Oil and PMK Powder, steady supply. Order through your phone whenever you're ready. I've also got a few pink hearts pressed already, if you want to see what the finished product looks like.",
                            choices =>
                            {
                                choices.Add(
                                    SampleChoice,
                                    "Let me try a sample.",
                                    SampleNode
                                );

                                choices.Add(
                                    AskChoice,
                                    "Tell me about the ingredients again.",
                                    DetailsNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "That's all I needed.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            SampleNode,
                            "Three pink hearts. On the house. Don't be stupid with them. Once you've seen what sells, come order the ingredients and make your own.",
                            choices =>
                            {
                                choices.Add(
                                    FinishChoice,
                                    "Appreciate it.",
                                    null
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Maybe another time.",
                                    null
                                );
                            }
                        );
                    }
                );

                if (!_callbackHooked)
                {
                    dialogue.OnChoiceSelected(
                        SampleChoice,
                        OnSampleChosen
                    );

                    _callbackHooked = true;
                }

                bool armed = dialogue.UseContainerOnInteract(ContainerName);

                if (armed)
                {
                    MelonLogger.Msg("[Roscoe] MDMA dialogue armed until sample is claimed.");
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Roscoe] Dialogue setup failed: " + ex.Message);
                return false;
            }
        }

        private static void OnSampleChosen()
        {
            if (_sampleClaimed)
            {
                MelonLogger.Msg("[Roscoe] Sample already claimed.");
                return;
            }

            bool given = WvcGiveItem.TryGive("molly", 3);

            if (!given)
            {
                MelonLogger.Warning("[Roscoe] Could not give MDMA. Can retry.");
                return;
            }

            _sampleClaimed = true;

            try
            {
                _activeDialogue?.StopOverride();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Roscoe] Failed to clear override: " + ex.Message);
            }

            MelonLogger.Msg("[Roscoe] Gave player 3 MDMA tablets. Intro dialogue disabled.");
        }

        public static void ResetSampleState()
        {
            _sampleClaimed = false;
            MelonLogger.Msg("[Roscoe] Sample state reset.");
        }
    }
}