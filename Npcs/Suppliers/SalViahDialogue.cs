using System;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class SalViahDialogue
    {
        public const string ContainerName =
            "wvc_sal_viah_salvia_intro";

        private const string EntryNode = "ENTRY";
        private const string AboutNode = "SALVIA_ABOUT";
        private const string GrowNode = "SALVIA_GROW";
        private const string StockNode = "SALVIA_STOCK";
        private const string DropNode = "SALVIA_DROP";
        private const string SampleNode = "SALVIA_SAMPLE";

        private const string AskAbout = "sal_ask_about";
        private const string AskGrow = "sal_ask_grow";
        private const string BrowseStock = "sal_browse_stock";
        private const string AskDrop = "sal_ask_drop";
        private const string AskSample = "sal_ask_sample";
        private const string LeaveChoice = "sal_leave";
        private const string FinishChoice = "sal_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;
        private static bool _suppressedForMeeting;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.SalViahKey);

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
                            "Greetings, traveler of the planes... Or are you just looking for green? " +
                            "The name is Sal. Sal Viah. Keeper of the Diviner's Sage. " +
                            "I cultivate pure Salvia cuttings and dried leaf — the sacred foliage that bends the grid of reality.",
                            choices =>
                            {
                                choices.Add(
                                    AskAbout,
                                    "What exactly is Salvia?",
                                    AboutNode
                                );

                                choices.Add(
                                    AskGrow,
                                    "How do I grow it?",
                                    GrowNode
                                );

                                choices.Add(
                                    BrowseStock,
                                    "Let me see what you've got.",
                                    StockNode
                                );

                                choices.Add(
                                    AskDrop,
                                    "Where is your payment drop?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Not right now.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            AboutNode,
                            "Salvia Divinorum isn't just weed, my friend. It doesn't invite you to relax — " +
                            "it unzips the 3D tapestry of space and time for a couple minutes, " +
                            "shows you the cosmic machinery, then snaps you right back into your shoes.\n\n" +
                            "Some meet the Lady of the Leaves, some turn into furniture. " +
                            "Bag it or jar it up, and your clients will never forget where they got it.",
                            choices =>
                            {
                                choices.Add(
                                    AskGrow,
                                    "How do I cultivate the cuttings?",
                                    GrowNode
                                );

                                choices.Add(
                                    BrowseStock,
                                    "Show me your stock.",
                                    StockNode
                                );

                                choices.Add(
                                    AskDrop,
                                    "Where's the drop spot?",
                                    DropNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Fascinating. Later.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            GrowNode,
                            "Growing Salvia is sacred botany:\n\n" +
                            "1. POTTING:\n" +
                            "- Take a healthy Salvia cutting and pot it in good moist soil under your lights.\n\n" +
                            "2. GROWTH & CARE:\n" +
                            "- Keep it watered and tended. Watch the broad leaves flourish into that rich electric sage green.\n\n" +
                            "3. HARVEST & PACKAGING:\n" +
                            "- Pluck the mature leaves directly from the branches.\n" +
                            "- Package them in baggies, jars, or press them into dense bricks.\n\n" +
                            "You can distribute the pure leaf or experiment with blends in your mixer.",
                            choices =>
                            {
                                choices.Add(
                                    BrowseStock,
                                    "Show me your stock.",
                                    StockNode
                                );

                                choices.Add(
                                    AskDrop,
                                    "Where do I leave payment?",
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
                            StockNode,
                            "I supply fresh, viable cuttings ready for your pots. " +
                            "Order through your phone whenever your compass points green.\n\n" +
                            "If your third eye is feeling curious, I can spare a starter cutting on the house.",
                            choices =>
                            {
                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        AskSample,
                                        "I'll take that cutting.",
                                        SampleNode
                                    );
                                }

                                choices.Add(
                                    AskDrop,
                                    "Where do I drop cash?",
                                    DropNode
                                );

                                choices.Add(
                                    AskGrow,
                                    "Remind me how to grow them.",
                                    GrowNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "That's all for now.",
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
                                    BrowseStock,
                                    "Let me see your stock.",
                                    StockNode
                                );

                                if (!SampleClaimed)
                                {
                                    choices.Add(
                                        AskSample,
                                        "Got a sample?",
                                        SampleNode
                                    );
                                }

                                choices.Add(
                                    AskGrow,
                                    "How do I grow it?",
                                    GrowNode
                                );

                                choices.Add(
                                    LeaveChoice,
                                    "Understood.",
                                    null
                                );
                            }
                        );

                        builder.AddNode(
                            SampleNode,
                            "Take this cutting. Pot it under your lights and see what the universe is hiding behind the curtain.\n\n" +
                            "Hit me up on your phone when you need bulk cuttings.",
                            choices =>
                            {
                                choices.Add(
                                    FinishChoice,
                                    "Appreciate the wisdom, Sal.",
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
                        "[SalViah] Salvia dialogue armed until sample is claimed."
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
                    DeadDropManager.Get<Gazebo>();

                if (drop != null)
                {
                    return
                        "Payment goes to the Gazebo in the park. " +
                        "It's marked on your map. Leave the paper by the wooden pillars, " +
                        "and my runners will take care of the rest.";
                }
            }
            catch (Exception)
            {

            }

            return
                "The drop's at the Gazebo in the park. " +
                "Leave the cash tucked into the drop spot, and pick up your order.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[SalViah] Sample already claimed."
                );
                return;
            }

            bool cuttingGiven =
                WvcGiveItem.TryGive("salviacutting", 1) ||
                WvcGiveItem.TryGive("salviaseed", 1);

            if (!cuttingGiven)
            {

                return;
            }

            SupplierSampleSaveManager.MarkSampleClaimed(
                SupplierSampleSaveManager.SalViahKey
            );

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
                "[SalViah] Gave player Salvia sample (leaf & cutting). Sample option removed."
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
                    "[SalViah] Intro dialogue suppressed for native supplier meetup."
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
                    "[SalViah] Intro dialogue restored after meetup. Armed=" + armed
                );
            }
            catch (Exception)
            {

            }
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(
                SupplierSampleSaveManager.SalViahKey
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[SalViah] Sample state reset."
            );
        }
    }
}
