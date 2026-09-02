using System;
using CustomNPCExample.Products;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class GusDialogue
    {
        public const string ContainerName = "wvc_gus_baking_intro";

        private const string EntryNode   = "ENTRY";
        private const string SuppliesNode = "SUPPLIES";
        private const string HowNode      = "HOW";
        private const string RecipeNode   = "RECIPE";
        private const string SampleNode   = "SAMPLE";
        private const string DropNode     = "DROP";
        private const string StoryNode    = "STORY";

        private const string AskSupplies = "gus_supplies";
        private const string AskHow      = "gus_how";
        private const string AskRecipe   = "gus_recipe";
        private const string AskSample   = "gus_sample";
        private const string AskDrop     = "gus_drop";
        private const string AskStory    = "gus_story";
        private const string Leave       = "gus_leave";
        private const string Finish      = "gus_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.GusKey);

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            if (SampleClaimed)
            {
                try { dialogue.StopOverride(); } catch { }

                MelonLogger.Msg("[Gus] Sample already claimed. Intro off.");
                return true;
            }

            try
            {
                dialogue.BuildAndRegisterContainer(ContainerName, builder =>
                {
                    builder.SetAllowExit(true);

                    // ENTRY
                    builder.AddNode(
                        EntryNode,
                        "Name's Gus. I bake. And I supply the folks who... " +
                        "bake other things.",
                        choices =>
                        {
                            choices.Add(AskSupplies, "What do you sell?",        SuppliesNode);
                            choices.Add(AskRecipe,   "How do I make brownies?",  RecipeNode);
                            choices.Add(AskHow,      "What do I do with it?",    HowNode);
                            choices.Add(AskDrop,     "Where do I pay?",          DropNode);
                            choices.Add(AskStory,    "Why baking?",              StoryNode);
                            choices.Add(Leave,       "Not interested.",          null);
                        });

                    // SUPPLIES
                    builder.AddNode(
                        SuppliesNode,
                        "Baker's cocoa, a special butter, and a leavening mix. " +
                        "Everything you need to whip up a strong batch of brownies.",
                        choices =>
                        {
                            choices.Add(AskRecipe, "Walk me through the recipe.", RecipeNode);
                            choices.Add(AskSample, "Can I get a taster?",         SampleNode);
                            choices.Add(AskDrop,   "Where's the drop?",           DropNode);
                            choices.Add(Leave,     "I'll consider it.",           null);
                        });

                    // HOW
                    builder.AddNode(
                        HowNode,
                        "Combine them right and you get a brownie that hits harder " +
                        "than anything you'd buy in a store. Package it and sell it.",
                        choices =>
                        {
                            choices.Add(AskRecipe, "Give me the exact steps.",  RecipeNode);
                            choices.Add(AskSample, "Hook me up.",               SampleNode);
                            choices.Add(AskDrop,   "Where do I pay?",           DropNode);
                            choices.Add(Leave,     "Got it.",                   null);
                        });

                    // RECIPE
                    builder.AddNode(
                        RecipeNode,
                        "Two steps. Chemistry station first, then the oven.\n\n" +
                        "CHEMISTRY STATION — makes Unbaked Brownie Mix:\n" +
                        "- 3x Baker's Cocoa\n" +
                        "- 1x Infused Butter\n" +
                        "- 1x Leavening Mix\n\n" +
                        "LAB OVEN — bakes the mix:\n" +
                        "- 1x Unbaked Brownie Mix\n" +
                        "→ Produces a batch of Brownies.\n\n" +
                        "Order the ingredients through your phone. " +
                        "Payment goes behind the casino.",
                        choices =>
                        {
                            choices.Add(AskSupplies, "What do I need to order?", SuppliesNode);
                            choices.Add(AskSample,   "Got a sample?",            SampleNode);
                            choices.Add(AskDrop,     "Where's the drop?",        DropNode);
                            choices.Add(Leave,       "Got it, thanks.",          null);
                        });

                    // DROP
                    builder.AddNode(
                        DropNode,
                        BuildDropText(),
                        choices =>
                        {
                            choices.Add(AskSupplies, "What are you selling again?", SuppliesNode);
                            choices.Add(AskSample, "Got a sample?", SampleNode);
                            choices.Add(Leave, "Understood.", null);
                        });

                    // STORY
                    builder.AddNode(
                        StoryNode,
                        "Ran a real bakery once. Taxman took it. " +
                        "Now I bake for a different kind of customer.",
                        choices =>
                        {
                            choices.Add(AskSupplies, "So what's on offer?", SuppliesNode);
                            choices.Add(AskSample, "Let me try some.", SampleNode);
                            choices.Add(Leave, "Rough.", null);
                        });

                    // SAMPLE
                    builder.AddNode(
                        SampleNode,
                        "Here. A little cocoa on the house. " +
                        "Come back when you're ready to bake for real.",
                        choices =>
                        {
                            choices.Add(Finish, "Appreciated.", null);
                            choices.Add(Leave, "Maybe later.", null);
                        });
                });

                if (!_callbackHooked)
                {
                    dialogue.OnChoiceSelected(AskSample, OnSampleChosen);
                    dialogue.OnChoiceSelected(Finish, OnSampleChosen);
                    _callbackHooked = true;
                }

                bool armed = dialogue.UseContainerOnInteract(ContainerName);

                if (armed)
                {
                    MelonLogger.Msg(
                        "[Gus] Baking intro dialogue armed until sample is claimed."
                    );
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Gus] Dialogue setup failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildDropText()
        {
            try
            {
                DeadDropInstance drop =
                    DeadDropManager.Get<BehindCasino>();

                if (drop != null)
                {
                    return
                        "Behind the casino. Crate against the wall. " +
                        "Cash goes there. It's marked on your map.";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Gus] Drop lookup failed: " + ex.Message);
            }

            return "Behind the casino. Leave the cash in the crate.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                MelonLogger.Msg("[Gus] Sample already claimed.");
                return;
            }

            bool given =
                WvcGiveItem.TryGive("cocoa", 1) ||
                WvcGiveItem.TryGive(BrownieIngredients.CocoaProductId, 1);

            if (!given)
            {
                MelonLogger.Warning(
                    "[Gus] Could not give cocoa sample. Player can retry."
                );
                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.GusKey);

            try { _activeDialogue?.StopOverride(); }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Gus] Failed to clear override: " + ex.Message);
            }

            MelonLogger.Msg(
                "[Gus] Gave player 1 Baker's Cocoa sample. Intro dialogue disabled."
            );
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.GusKey);
            MelonLogger.Msg("[Gus] Sample state reset.");
        }
    }
}