using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class RoscoeDialogue
    {
        public const string ContainerName =
            "wvc_roscoe_mdma_intro";

        private const string EntryNode = "ENTRY";
        private const string DetailsNode = "MDMA_DETAILS";
        private const string StockNode = "MDMA_STOCK";
        private const string SampleNode = "MDMA_SAMPLE";
        private const string RecipeNode = "MDMA_RECIPE";
        private const string DropNode = "MDMA_DROP";

        private const string AskChoice = "roscoe_mdma_ask";
        private const string BrowseChoice = "roscoe_mdma_browse";
        private const string SampleChoice = "roscoe_mdma_sample";
        private const string RecipeChoice = "roscoe_mdma_recipe";
        private const string DropChoice = "roscoe_mdma_drop";
        private const string LeaveChoice = "roscoe_mdma_leave";
        private const string FinishChoice = "roscoe_mdma_finish";

        private static bool _callbackHooked;
        private static bool _suppressedForMeeting;

        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.RoscoeKey);

        public static bool TryRegisterAndArm(
            NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Sample already claimed. " +
                    "Arming intro without the sample option."
                );
            }

            if (_suppressedForMeeting)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Intro dialogue not armed: " +
                    "native meetup is active."
                );

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
                            "Roscoe Bellweather. I move precursor stock. " +
                            "Safrole oil and PMK, three grades. " +
                            "You handle the cook, I keep you supplied.",
                            choices =>
                            {
                                choices.Add(
                                    RecipeChoice,
                                    "How do I make MDMA?",
                                    RecipeNode
                                );

                                choices.Add(
                                    BrowseChoice,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    DropChoice,
                                    "Where's the drop?",
                                    DropNode
                                );

                                choices.Add(
                                    AskChoice,
                                    "What does it take to make?",
                                    DetailsNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Not interested.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            RecipeNode,
                            "Simple cook. One cauldron run.\n\n" +
                            "CAULDRON:\n" +
                            "- 2x Safrole Oil\n" +
                            "- 3x PMK Powder\n" +
                            "- 1x Gasoline (liquid slot)\n\n" +
                            "That gives you a batch of MDMA.\n\n" +
                            "The PMK grade sets your quality:\n" +
                            "- Standard PMK -> Standard\n" +
                            "- Refined PMK -> Premium\n" +
                            "- Lab-Grade PMK -> Heavenly. You can also see how to make it in the phone app",
                            choices =>
                            {
                                choices.Add(
                                    BrowseChoice,
                                    "Show me the stock.",
                                    StockNode
                                );

                                choices.Add(
                                    DropChoice,
                                    "Where's the drop?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Got it, thanks.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            DropNode,
                            BuildDropText(),
                            choices =>
                            {
                                choices.Add(
                                    BrowseChoice,
                                    "Let me order something.",
                                    StockNode
                                );

                                choices.Add(
                                    RecipeChoice,
                                    "Remind me how to cook it.",
                                    RecipeNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Appreciate it.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            DetailsNode,
                            "Two things drive it. Safrole oil is the base. " +
                            "PMK does the heavy lifting, and the grade you " +
                            "run decides how clean the batch comes out.",
                            choices =>
                            {
                                choices.Add(
                                    RecipeChoice,
                                    "Give me the exact steps.",
                                    RecipeNode
                                );

                                choices.Add(
                                    BrowseChoice,
                                    "Show me the stock.",
                                    StockNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "I'll come back later.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            StockNode,
                            "Safrole oil, and PMK in standard, refined, " +
                            "and lab-grade. Leave payment at the drop or " +
                            "hit me up for bulk orders. I've got a sample too " +
                            "if you want to see the finished product.",
                            choices =>
                            {
                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        SampleChoice,
                                        "Can I see a sample?",
                                        SampleNode
                                    );
                                }

                                choices.Add(
                                    DropChoice,
                                    "Where's the drop?",
                                    DropNode
                                );

                                choices.Add(
                                    RecipeChoice,
                                    "How do I cook it?",
                                    RecipeNode
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
                            "Here's a taste of the finished product. " +
                            "Like it, order the precursors and run your " +
                            "own batch through the cauldron.",
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

                bool armed =
                    dialogue.UseContainerOnInteract(
                        ContainerName
                    );

                if (armed)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Roscoe] MDMA dialogue armed " +
                        "until sample is claimed."
                    );
                }

                return armed;
            }
            catch (Exception)
            {


                return false;
            }
        }

        public static void SuppressForMeeting()
        {
            if (_suppressedForMeeting)
                return;

            _suppressedForMeeting = true;

            try
            {
                _activeDialogue?.StopOverride();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Intro dialogue suppressed for " +
                    "native supplier meetup."
                );
            }
            catch (Exception)
            {

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
                bool armed = TryRegisterAndArm(_activeDialogue);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Intro dialogue restored after meetup. " +
                    "Armed=" + armed
                );
            }
            catch (Exception)
            {

            }
        }

        private static string BuildDropText()
        {
            try
            {
                DeadDropInstance drop =
                    DeadDropManager.Get<GreyDocksBuilding>();

                if (drop != null)
                {
                    return
                        "Payment goes to the grey building down at the " +
                        "docks. It's marked on your map. Leave the cash, " +
                        "I collect it.";
                }
            }
            catch (Exception)
            {

            }

            return
                "The drop's the grey building at the docks. " +
                "Leave the cash, pick up your order later.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Sample already claimed."
                );

                return;
            }

            bool given =
                WvcGiveItem.TryGive("mdma", 1);

            if (!given)
            {


                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.RoscoeKey);

            try
            {
                if (_activeDialogue != null)
                {
                    TryRegisterAndArm(_activeDialogue);
                }
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Roscoe] Gave player 1 MDMA sample. Sample option removed."
            );
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.RoscoeKey);
            _suppressedForMeeting = false;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Roscoe] Sample state reset."
            );
        }
    }
}
