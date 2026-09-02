using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class DamonTreyDialogue
    {
        public const string ContainerName =
            "wvc_damon_trey_dmt_intro";

        private const string EntryNode = "ENTRY";
        private const string DetailsNode = "DMT_DETAILS";
        private const string StockNode = "DMT_STOCK";
        private const string SampleNode = "DMT_SAMPLE";
        private const string RecipeNode = "DMT_RECIPE";
        private const string DropNode = "DMT_DROP";

        private const string AskChoice = "damon_dmt_ask";
        private const string BrowseChoice = "damon_dmt_browse";
        private const string SampleChoice = "damon_dmt_sample";
        private const string RecipeChoice = "damon_dmt_recipe";
        private const string DropChoice = "damon_dmt_drop";
        private const string LeaveChoice = "damon_dmt_leave";
        private const string FinishChoice = "damon_dmt_finish";

        private static bool _callbackHooked;

        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.DamonTreyKey);

        public static bool TryRegisterAndArm(
            NPCDialogue dialogue
        )
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            if (SampleClaimed)
            {
                try
                {
                    dialogue.StopOverride();
                }
                catch
                {
                }

                MelonLogger.Msg(
                    "[DamonTrey] Sample already claimed. " +
                    "Intro dialogue stays disabled."
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

                        // ============================================
                        // ENTRY
                        // ============================================
                        builder.AddNode(
                            EntryNode,

                            "Name's Damon. Damon Trey. " +
                            "Yeah, people shorten it. " +
                            "I deal in short-run crystal stock. " +
                            "Strong, expensive, and not for tourists.",

                            choices =>
                            {
                                choices.Add(
                                    RecipeChoice,
                                    "How do I make DMT?",
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

                        // ============================================
                        // RECIPE / INSTRUCTIONS
                        // ============================================
                        builder.AddNode(
                            RecipeNode,

                            "Alright, listen close. Two-step cook.\n\n" +
                            "CAULDRON:\n" +
                            "- 3x Mimosa Root Bark\n" +
                            "- 2x Caustic Base\n" +
                            "- 1x Lab Solvent\n" +
                            "- 1x Gasoline (liquid slot)\n" +
                            "That gives you Crude DMT Extract.\n\n" +
                            "Toss in a Crystalizer and you get " +
                            "Premium Crude Extract instead.\n\n" +
                            "LAB OVEN:\n" +
                            "- Drop the extract in and cook it.\n" +
                            "- Out comes 15x DMT. Premium extract " +
                            "makes premium product. You can also see how to make it in the phone app",

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

                        // ============================================
                        // DEAD DROP LOCATION
                        // ============================================
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

                        // ============================================
                        // DETAILS (flavor)
                        // ============================================
                        builder.AddNode(
                            DetailsNode,

                            "Four materials. Mimosa root bark is the base. " +
                            "Caustic base prepares the batch. Lab solvent " +
                            "handles the extraction, and crystalizer finishes " +
                            "it into something worth selling. No shortcuts.",

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

                        // ============================================
                        // STOCK
                        // ============================================
                        builder.AddNode(
                            StockNode,

                            "Mimosa root bark, caustic base, lab solvent, " +
                            "and crystalizer. Order through your phone " +
                            "whenever you're ready. I keep the supply steady, " +
                            "but I don't give out my sources. I've also got " +
                            "a sample if you want to see the finished product.",

                            choices =>
                            {
                                choices.Add(
                                    SampleChoice,
                                    "Can I see a sample?",
                                    SampleNode
                                );

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

                        // ============================================
                        // SAMPLE
                        // ============================================
                        builder.AddNode(
                            SampleNode,

                            "One sample. Don't waste it. " +
                            "If you like what you see, order the materials " +
                            "and make your own batch through the lab.",

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
                    MelonLogger.Msg(
                        "[DamonTrey] DMT dialogue armed " +
                        "until sample is claimed."
                    );
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[DamonTrey] Dialogue setup failed: " +
                    ex.Message
                );

                return false;
            }
        }

        // ================================================================
        // Dead drop location text
        // ================================================================

        private static string BuildDropText()
        {
            try
            {
                DeadDropInstance drop =
                    DeadDropManager.Get<BehindLaundromat>();

                if (drop != null)
                {
                    return
                        "You'll leave payment behind the Laundromat. " +
                        "It's marked on your map now. " +
                        "Drop the cash there";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[DamonTrey] Drop lookup failed: " +
                    ex.Message
                );
            }

            return
                "The drop's behind the Laundromat. " +
                "Leave the cash there, pick up your order later.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                MelonLogger.Msg(
                    "[DamonTrey] Sample already claimed."
                );

                return;
            }

            bool given =
                WvcGiveItem.TryGive(
                    "dmt",
                    1
                );

            if (!given)
            {
                MelonLogger.Warning(
                    "[DamonTrey] Could not give DMT sample. Can retry."
                );

                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.DamonTreyKey);

            try
            {
                _activeDialogue?.StopOverride();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[DamonTrey] Failed to clear override: " +
                    ex.Message
                );
            }

            MelonLogger.Msg(
                "[DamonTrey] Gave player 1 DMT sample. " +
                "Intro dialogue disabled."
            );
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.DamonTreyKey);

            MelonLogger.Msg(
                "[DamonTrey] Sample state reset."
            );
        }
    }
}