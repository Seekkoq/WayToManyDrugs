using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class StellaVanceDialogue
    {
        public const string ContainerName = "wvc_stella_cookie_intro";

        // Node keys
        private const string EntryNode  = "ENTRY";
        private const string RecipeNode = "RECIPE";
        private const string StockNode  = "STOCK";
        private const string SampleNode = "SAMPLE";
        private const string DropNode   = "DROP";
        private const string TipsNode   = "TIPS";

        // Choice keys
        private const string AskRecipe  = "stella_recipe";
        private const string AskStock   = "stella_stock";
        private const string AskSample  = "stella_sample";
        private const string AskDrop    = "stella_drop";
        private const string AskTips    = "stella_tips";
        private const string Leave      = "stella_leave";
        private const string Finish     = "stella_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.StellaVanceKey);

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            if (SampleClaimed)
            {
                try { dialogue.StopOverride(); } catch { }
                MelonLogger.Msg("[Stella] Sample already claimed. Intro dialogue disabled.");
                return true;
            }

            try
            {
                dialogue.BuildAndRegisterContainer(ContainerName, builder =>
                {
                    builder.SetAllowExit(true);

                    builder.AddNode(
                        EntryNode,
                        "Stella Vance. I bake — the kind people actually want. " +
                        "I've got the chips and the flour. You do the mixing, " +
                        "I keep you stocked.",
                        choices =>
                        {
                            choices.Add(AskRecipe, "How do I make the cookies?", RecipeNode);
                            choices.Add(AskStock,  "What are you selling?",       StockNode);
                            choices.Add(AskDrop,   "Where do I pay you?",          DropNode);
                            choices.Add(AskSample, "Got a sample?",               SampleNode);
                            choices.Add(Leave,     "Maybe another time.",         null);
                        });

                    builder.AddNode(
                        RecipeNode,
                        "Two steps. Cauldron first, then the oven.\n\n" +
                        "CAULDRON — makes Unbaked Cookie Dough:\n" +
                        "- 3x Butterscotch Chips\n" +
                        "- 2x Cannabis Flour\n\n" +
                        "LAB OVEN — bakes the dough:\n" +
                        "- 1x Unbaked Cookie Dough\n" +
                        "→ Produces a batch of THC Cookies.\n\n" +
                        "Order the ingredients through your phone. You can also see how to make it in the phone app",
                        choices =>
                        {
                            choices.Add(AskStock,  "Let me see what you've got.", StockNode);
                            choices.Add(AskTips,   "Any tips on selling them?",   TipsNode);
                            choices.Add(AskDrop,   "Where's the drop?",           DropNode);
                            choices.Add(Leave,     "Got it, thanks.",             null);
                        });

                    builder.AddNode(
                        StockNode,
                        "Butterscotch chips for binding, cannabis flour for the kick. " +
                        "Order both through your phone. " +
                        "I've got a finished cookie here if you want to see what you're working towards.",
                        choices =>
                        {
                            choices.Add(AskSample, "I'll take that sample.",   SampleNode);
                            choices.Add(AskRecipe, "Walk me through the cook.", RecipeNode);
                            choices.Add(AskDrop,   "Payment details?",          DropNode);
                            choices.Add(Leave,     "That's enough for now.",   null);
                        });

                    builder.AddNode(
                        DropNode,
                        BuildDropText(),
                        choices =>
                        {
                            choices.Add(AskRecipe, "Remind me how to make these.", RecipeNode);
                            choices.Add(AskStock,  "What do you carry?",            StockNode);
                            choices.Add(Leave,     "Appreciated.",                  null);
                        });

                    builder.AddNode(
                        TipsNode,
                        "Bag them up nice. Edibles go to people who don't want to smoke — " +
                        "different crowd, steadier money. " +
                        "Pair them with gummies if you want volume.",
                        choices =>
                        {
                            choices.Add(AskStock,  "What do I need to order?",     StockNode);
                            choices.Add(AskRecipe, "Give me the recipe again.",    RecipeNode);
                            choices.Add(Leave,     "Good to know.",                null);
                        });

                    builder.AddNode(
                        SampleNode,
                        "Here — one fresh batch. " +
                        "You'll want more once you see how they move. " +
                        "Order the ingredients through the app.",
                        choices =>
                        {
                            choices.Add(Finish, "Thanks, Stella.", null);
                            choices.Add(Leave,  "Maybe later.",    null);
                        });
                });

                if (!_callbackHooked)
                {
                    dialogue.OnChoiceSelected(AskSample, OnSampleChosen);
                    dialogue.OnChoiceSelected(Finish,    OnSampleChosen);
                    _callbackHooked = true;
                }

                bool armed = dialogue.UseContainerOnInteract(ContainerName);
                if (armed)
                    MelonLogger.Msg("[Stella] Cookie intro dialogue armed.");

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Stella] Dialogue setup failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildDropText()
        {
            try
            {
                DeadDropInstance drop = DeadDropManager.Get<BehindAutoShop>();
                if (drop != null)
                    return "Near the auto shop — it's on your map. " +
                           "Leave the cash in the crate before pickup. I check it twice.";
            }
            catch { }

            return "Near the auto shop. Leave what you owe in the crate, " +
                   "I'll collect it and queue your order.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed) return;

            bool given = WvcGiveItem.TryGive("cookie", 1)
                      || WvcGiveItem.TryGive("westvilleconnection:products/cookie_v1", 1);

            if (!given) return;

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.StellaVanceKey);
            try { _activeDialogue?.StopOverride(); } catch { }

            MelonLogger.Msg("[Stella] Gave player 1 THC Cookie sample. Intro dialogue disabled.");
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.StellaVanceKey);
            MelonLogger.Msg("[Stella] Sample state reset.");
        }
    }
}
