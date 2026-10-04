using System;
using CustomNPCExample.Products;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    /// <summary>
    /// The intro conversation for Dr. Eleanor "Pill" Scrivens, the corrupt pharmacist at
    /// Pillville. Four short nodes: who she is, the stock, where the money goes, and the free
    /// sample. Nothing in here assumes a shop interior - Pillville is a closed storefront, so
    /// she does her talking on the pavement.
    /// </summary>
    public static class PillVilleDialogue
    {
        public const string ContainerName =
            "wvc_pill_ville_xanax_intro";

        private const string EntryNode = "ENTRY";
        private const string StockNode = "PILL_STOCK";
        private const string DropNode = "PILL_DROP";
        private const string SampleNode = "PILL_SAMPLE";
        private const string RecipeNode = "PILL_RECIPE";

        private const string AskStock = "pill_ask_stock";
        private const string AskDrop = "pill_ask_drop";
        private const string AskSample = "pill_ask_sample";
        private const string AskRecipe = "pill_ask_recipe";
        private const string LeaveChoice = "pill_leave";
        private const string FinishChoice = "pill_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;
        private static bool _suppressedForMeeting;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.PillVilleKey);

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            try
            {
                dialogue.BuildAndRegisterContainer(
                    ContainerName,
                    builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(
                            EntryNode,
                            "Eleanor Scrivens. I run the pharmacy here.\n\n" +
                            "The good bars never make the shelf. They come to me instead.",
                            choices =>
                            {
                                choices.Add(
                                    AskRecipe,
                                    "How do I make the bars?",
                                    RecipeNode
                                );

                                choices.Add(
                                    AskStock,
                                    "Let's see them.",
                                    StockNode
                                );

                                choices.Add(
                                    AskDrop,
                                    "Where do I leave the money?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Not today.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            RecipeNode,
                            "Two steps, and the pressing is yours.\n\n" +
                            "ORDER — Xanax Powder:\n" +
                            "- I sell you the powder loose. Order it on your phone and " +
                            "collect it from the drop.\n\n" +
                            "BRICK PRESS — Xanax bars:\n" +
                            "- Load the powder into the press by hand\n" +
                            "- Press BEGIN\n" +
                            "→ One bar comes out per unit of powder.\n\n" +
                            "Order the powder through your phone. You can also see how " +
                            "to make it in the phone app.",
                            choices =>
                            {
                                choices.Add(
                                    AskStock,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    AskDrop,
                                    "Where's the drop?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Got it.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            StockNode,
                            "Xanax, straight out of the back. Order it on your phone and " +
                            "I leave it in the drop.\n\n" +
                            "First one is free if you want to try before you buy.",
                            choices =>
                            {
                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        AskSample,
                                        "I'll take the free one.",
                                        SampleNode
                                    );
                                }

                                choices.Add(
                                    AskDrop,
                                    "Where's the drop?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Maybe later.",
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
                                    AskStock,
                                    "Show me the stock.",
                                    StockNode
                                );

                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        AskSample,
                                        "I'll take the free one.",
                                        SampleNode
                                    );
                                }

                                choices.Add(
                                    LeaveChoice,
                                    "Understood.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            SampleNode,
                            "Here - one bar, on the house.\n\n" +
                            "Take half first. That's the only warning you get.",
                            choices =>
                            {
                                choices.Add(
                                    FinishChoice,
                                    "Thanks.",
                                    null
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Not now.",
                                    null
                                );
                            }
                        );
                    }
                );

                if (!_callbackHooked)
                {
                    dialogue.OnChoiceSelected(
                        AskSample,
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
                        "[PillVille] Xanax dialogue armed."
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
                    DeadDropManager.Get<BehindMedicalPractice>();

                if (drop != null)
                {
                    return
                        "Behind the medical practice. It's marked on your map.\n\n" +
                        "Cash in the pouch, then keep walking. Order first, pay after.";
                }
            }
            catch (Exception)
            {

            }

            return
                "Behind the medical practice, marked on your map. " +
                "Leave the cash in the pouch and keep walking.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[PillVille] Sample already claimed."
                );
                return;
            }

            bool barGiven =
                WvcGiveItem.TryGive("xanax", 1) ||
                WvcGiveItem.TryGive(Xanax.ProductId, 1);

            if (!barGiven)
            {

                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(
                SupplierSampleSaveManager.PillVilleKey
            );

            try
            {
                if (_activeDialogue != null)
                    TryRegisterAndArm(_activeDialogue);
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[PillVille] Gave player a Xanax bar. Sample option removed."
            );
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
                    "[PillVille] Intro dialogue suppressed for native supplier meetup."
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
                    "[PillVille] Intro dialogue restored after meetup. Armed=" + armed
                );
            }
            catch (Exception)
            {

            }
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(
                SupplierSampleSaveManager.PillVilleKey
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[PillVille] Sample state reset."
            );
        }
    }
}
