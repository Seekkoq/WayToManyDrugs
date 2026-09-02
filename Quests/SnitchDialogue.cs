using System;
using MelonLoader;
using S1API.Entities;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public static class SnitchDialogue
    {
        public const string ContainerName = "wvc_damon_snitch_arc";

        private const string EntryNode = "ENTRY";
        private const string WhoNode = "WHO";
        private const string HowNode = "HOW";
        private const string PlanNode = "PLAN";

        private const string AskWho = "snitch_who";
        private const string AskHow = "snitch_how";
        private const string AskPlan = "snitch_plan";
        private const string Finish = "snitch_finish";
        private const string Leave = "snitch_leave";

        private static bool _armed;
        private static bool _callbackHooked;
        private static NPCDialogue _dialogue;

        public static bool IsArmed => _armed;

        /// <summary>
        /// Swaps Damon to snitch-arc dialogue.
        /// </summary>
        public static bool ArmSnitchDialogue()
        {
            if (_armed)
                return true;

            try
            {
                if (DamonTrey.Instance == null)
                    return false;

                _dialogue = DamonTrey.Instance.Dialogue;

                if (_dialogue == null)
                    return false;

                _dialogue.BuildAndRegisterContainer(ContainerName, builder =>
                {
                    builder.SetAllowExit(true);

                    builder.AddNode(
                        EntryNode,
                        "You made it. Look, it's bad. One of ours got picked up two nights ago " +
                        "and he's already talking. Cops are everywhere.",
                        choices =>
                        {
                            choices.Add(AskWho, "Who got caught?", WhoNode);
                            choices.Add(AskHow, "How'd they get him?", HowNode);
                            choices.Add(AskPlan, "What do we do?", PlanNode);
                            choices.Add(Leave, "I'll handle it.", null);
                        });

                    builder.AddNode(
                        WhoNode,
                        "I ain't saying names over a table. But he knew the drop spots. " +
                        "He knew your routes. That's all you need to know.",
                        choices =>
                        {
                            choices.Add(AskHow, "How'd they get him?", HowNode);
                            choices.Add(AskPlan, "So what's the plan?", PlanNode);
                            choices.Add(Finish, "Understood.", null);
                        });

                    builder.AddNode(
                        HowNode,
                        "Sloppy handoff. Undercover posing as a buyer. " +
                        "They had him on camera for a week before they moved in.",
                        choices =>
                        {
                            choices.Add(AskWho, "Who was it?", WhoNode);
                            choices.Add(AskPlan, "What now?", PlanNode);
                            choices.Add(Finish, "Got it.", null);
                        });

                    builder.AddNode(
                        PlanNode,
                        "You lay low. Don't sell for a bit. Let the patrols thin out. " +
                        "I'll reach out when it's clear. Don't do nothing stupid.",
                        choices =>
                        {
                            choices.Add(Finish, "Alright. I'll wait it out.", null);
                            choices.Add(Leave, "We'll see.", null);
                        });
                });

                if (!_callbackHooked)
                {
                    _dialogue.OnChoiceSelected(Finish, OnConversationFinished);
                    _callbackHooked = true;
                }

                bool ok = _dialogue.UseContainerOnInteract(ContainerName);

                if (ok)
                {
                    _armed = true;
                    MelonLogger.Msg("[WVC Snitch] Damon snitch-arc dialogue armed.");
                }

                return ok;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Snitch] Dialogue arm failed: " + ex.Message);
                return false;
            }
        }

        private static void OnConversationFinished()
        {
            if (!_armed)
                return;

            MelonLogger.Msg("[WVC Snitch] Player finished snitch conversation with Damon.");
            SnitchQuestManager.NotifyTalkedToDamon();
        }

        /// <summary>
        /// Restores Damon's normal supplier dialogue.
        /// </summary>
        public static void RestoreNormalDialogue()
        {
            if (!_armed)
                return;

            try
            {
                _dialogue?.StopOverride();
                _armed = false;

                MelonLogger.Msg("[WVC Snitch] Damon restored to normal supplier dialogue.");

                // Re-arm his supplier intro if sample not yet claimed
                if (DamonTrey.Instance?.Dialogue != null)
                {
                    // Assuming DamonDialogue is your existing supplier dialogue class
                    // If your class name differs, change this line
                    try
                    {
                        DamonTreyDialogue.TryRegisterAndArm(DamonTrey.Instance.Dialogue);
                    }
                    catch
                    {
                        // Supplier dialogue may already be claimed/disabled — that's fine
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Snitch] Restore dialogue failed: " + ex.Message);
            }
        }
    }
}