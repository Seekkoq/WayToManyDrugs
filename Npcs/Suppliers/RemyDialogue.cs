using System;
using CustomNPCExample.Quests;
using CustomNPCExample.Utils;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class RemyDialogue
    {
        public const string ContainerName = "wvc_remy_cart_intro";

        private const string EntryNode = "ENTRY";
        private const string CartsNode = "CARTS";
        private const string HowNode = "HOW";
        private const string SampleNode = "SAMPLE";
        private const string DropNode = "DROP";
        private const string CloudNode = "CLOUD";

        private const string AskCarts = "remy_carts";
        private const string AskHow = "remy_how";
        private const string AskSample = "remy_sample";
        private const string AskDrop = "remy_drop";
        private const string AskCloud = "remy_cloud";
        private const string Leave = "remy_leave";
        private const string Finish = "remy_finish";

        private static bool _callbackHooked;
        private static NPCDialogue _activeDialogue;

        public static bool SampleClaimed =>
            SupplierSampleSaveManager.IsSampleClaimed(SupplierSampleSaveManager.RemyKey);

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            if (SnitchStoryManager.IsConfrontRemyActive)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg("[Remy] Normal cart dialogue skipped: Face to Face confrontation active.");
                return true;
            }

            try
            {
                dialogue.BuildAndRegisterContainer(ContainerName, builder =>
                {
                    builder.SetAllowExit(true);

                    builder.AddNode(
                        EntryNode,
                        "Remy. People call me Fog. I move carts.",
                        choices =>
                        {
                            choices.Add(AskCarts, "What kind?", CartsNode);
                            choices.Add(AskHow, "How do I sell them?", HowNode);
                            choices.Add(AskDrop, "Where do I pay?", DropNode);
                            choices.Add(AskCloud, "Why Fog?", CloudNode);
                            choices.Add(Leave, "Nah.", null);
                        });

                    builder.AddNode(
                        CartsNode,
                        "Ceramic THC carts. Clean stuff.",
                        choices =>
                        {
                            if (!SampleClaimed)
                                choices.Add(AskSample, "Can I try one?", SampleNode);
                            choices.Add(AskDrop, "Where's the drop?", DropNode);
                            choices.Add(Leave, "I'll think about it.", null);
                        });

                    builder.AddNode(
                        HowNode,
                        "Bag 'em or jar 'em. List them in your app. Weed people love these.",
                        choices =>
                        {
                            if (!SampleClaimed)
                                choices.Add(AskSample, "Give me one.", SampleNode);
                            choices.Add(AskDrop, "Where do I pay?", DropNode);
                            choices.Add(Leave, "Cool.", null);
                        });

                    builder.AddNode(
                        DropNode,
                        BuildDropText(),
                        choices =>
                        {
                            choices.Add(AskCarts, "What are you selling?", CartsNode);
                            if (!SampleClaimed)
                                choices.Add(AskSample, "Got a sample?", SampleNode);
                            choices.Add(Leave, "Got it.", null);
                        });

                    builder.AddNode(
                        CloudNode,
                        "Used to blow huge clouds at the arcade. Name stuck.",
                        choices =>
                        {
                            choices.Add(AskCarts, "So what's the deal?", CartsNode);
                            if (!SampleClaimed)
                                choices.Add(AskSample, "Let me try one.", SampleNode);
                            choices.Add(Leave, "Later.", null);
                        });

                    builder.AddNode(
                        SampleNode,
                        "Here. One on me. Hit me up when you want more.",
                        choices =>
                        {
                            choices.Add(Finish, "Thanks.", null);
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
                    global::CustomNPCExample.Utils.WvcLog.Msg("[Remy] Cart intro dialogue armed until sample is claimed.");
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
                DeadDropInstance drop = DeadDropManager.Get<TownHallFountain>();
                if (drop != null)
                {
                    return "Town Hall fountain. Leave the cash there. It's on your map.";
                }
            }
            catch { }

            return "Town Hall fountain downtown. Cash goes there.";
        }

        private static void OnSampleChosen()
        {
            if (SampleClaimed) return;

            bool given = WvcGiveItem.TryGive("cart", 1) || WvcGiveItem.TryGive("westvilleconnection:products/vape_cart", 1);
            if (!given) return;

            SupplierSampleSaveManager.MarkSampleClaimed(SupplierSampleSaveManager.RemyKey);
            try
            {
                if (_activeDialogue != null)
                {
                    TryRegisterAndArm(_activeDialogue);
                }
            }
            catch { }

            global::CustomNPCExample.Utils.WvcLog.Msg("[Remy] Gave player 1 Vape Cart sample. Sample option removed.");
        }

        public static void ResetSampleState()
        {
            SupplierSampleSaveManager.ResetSample(SupplierSampleSaveManager.RemyKey);
            global::CustomNPCExample.Utils.WvcLog.Msg("[Remy] Sample state reset.");
        }
    }
}
