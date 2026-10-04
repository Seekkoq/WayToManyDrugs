using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;
using CustomNPCExample.Quests;

namespace CustomNPCExample.NPCs
{
    public static class MartyMellowsDialogue
    {
        public const string ContainerName =
            "wvc_marty_gummies_intro";

        private const string EntryNode = "ENTRY";
        private const string DetailsNode = "GUMMY_DETAILS";
        private const string StockNode = "GUMMY_STOCK";
        private const string SampleNode = "GUMMY_SAMPLE";
        private const string RecipeNode = "GUMMY_RECIPE";
        private const string DropNode = "GUMMY_DROP";

        private const string AskChoice = "marty_gummy_ask";
        private const string BrowseChoice = "marty_gummy_browse";
        private const string SampleChoice = "marty_gummy_sample";
        private const string RecipeChoice = "marty_gummy_recipe";
        private const string DropChoice = "marty_gummy_drop";
        private const string LeaveChoice = "marty_gummy_leave";
        private const string FinishChoice = "marty_gummy_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _armedDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.MartyMellowsKey);

        public static bool TryRegisterAndArm(
            NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Marty] Sample already claimed. " +
                    "Arming intro without the sample option."
                );
            }

            if (SnitchStoryManager.IsAskMartyActive)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Marty] Normal gummies dialogue skipped: " +
                    "Second Opinion investigation is active."
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
                            "You looking to get into gummies? " +
                            "Smart move. Everybody wants edibles right now. " +
                            "I can keep THC oil and gelatin moving your way.",
                            choices =>
                            {
                                choices.Add(
                                    RecipeChoice,
                                    "How do I make gummies?",
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
                                    "What do I need to make them?",
                                    DetailsNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Not right now.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            RecipeNode,
                            "Two-step cook. Cauldron then oven.\n\n" +
                            "CAULDRON:\n" +
                            "- 3x THC Oil\n" +
                            "- 5x Gelatin\n" +
                            "- 3x Sugar\n" +
                            "- 1x Gasoline (liquid slot)\n" +
                            "That makes an Unbaked Gummy Mix.\n\n" +
                            "LAB OVEN:\n" +
                            "- Drop the mix in and bake it.\n" +
                            "- Out come your finished gummies.\n\n" +
                            "Sugar's cheap at any Gas-Mart. You can also see how to make it in the phone app",
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
                                    "Remind me how to make them.",
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
                            "THC oil is the base. That's where the effect comes from. " +
                            "Gelatin gives them the chew. " +
                            "You'll also need sugar, but you can grab that from any Gas-Mart. " +
                            "Get your ratios right and people will keep coming back.",
                            choices =>
                            {
                                choices.Add(
                                    RecipeChoice,
                                    "Give me the exact steps.",
                                    RecipeNode
                                );

                                choices.Add(
                                    BrowseChoice,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Maybe later.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            StockNode,
                            "THC oil and gelatin, steady supply. " +
                            "Order through your phone whenever you're ready. " +
                            "I've also got a few test pieces pressed already " +
                            "if you want to see what the finished product looks like.",
                            choices =>
                            {
                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        SampleChoice,
                                        "Let me try a sample.",
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
                                    "How do I make them?",
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
                            "Three gummies. On the house. " +
                            "Sugar you can grab at any Gas-Mart. " +
                            "Once you've got all three, you're in business.",
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

                _armedDialogue = dialogue;

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
                        "[Marty] Gummies intro dialogue armed."
                    );
                }

                return armed;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static string BuildDropText()
        {
            try
            {
                DeadDropInstance drop =
                    DeadDropManager.Get<BehindSlopShop>();

                if (drop != null)
                {
                    return
                        "Payment goes behind the Slop Shop. " +
                        "It's marked on your map. Leave the cash,";
                }
            }
            catch (Exception)
            {

            }

            return
                "The drop's behind the Slop Shop. " +
                "Leave the cash there, grab your order later.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Marty] Sample already claimed."
                );

                return;
            }

            bool given = WvcGiveItem.TryGive(
                "gummies",
                3
            );

            if (!given)
            {


                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.MartyMellowsKey);

            if (_armedDialogue != null)
            {
                try
                {
                    TryRegisterAndArm(_armedDialogue);
                }
                catch (Exception)
                {

                }
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Marty] Gave player 3 THC gummies. Sample option removed."
            );
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.MartyMellowsKey);
            _armedDialogue = null;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Marty] Sample state reset."
            );
        }
    }
}
