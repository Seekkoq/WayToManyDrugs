using System;
using MelonLoader;
using S1API.Entities;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public static class MartyInvestigationDialogue
    {
        public const string ContainerName = "wvc_marty_second_opinion";

        private const string EntryNode = "ENTRY";
        private const string EmptyDropNode = "EMPTY_DROP";
        private const string WaterNode = "WATER";
        private const string BehaviorNode = "BEHAVIOR";
        private const string StashNode = "STASH";

        private const string AskEmpty = "marty_inv_empty";
        private const string AskWater = "marty_inv_water";
        private const string AskBehavior = "marty_inv_behavior";
        private const string AskStash = "marty_inv_stash";
        private const string FinishChoice = "marty_inv_finish";
        private const string LeaveChoice = "marty_inv_leave";

        private static bool _armed;
        private static bool _completed;

        private static NPCDialogue _dialogue;
        private static NPCDialogue _registeredDialogue;
        private static NPCDialogue _callbackDialogue;

        public static bool IsArmed => _armed;
        public static bool IsCompleted => _completed;

        public static bool ArmInvestigationDialogue()
        {
            if (_completed)
                return true;

            if (_armed)
                return true;

            if (!SnitchStoryManager.IsAskMartyActive)
                return false;

            try
            {
                MartyMellows marty = MartyMellows.Instance;
                if (marty == null)
                    return false;

                _dialogue = marty.Dialogue;
                if (_dialogue == null)
                    return false;

                if (!ReferenceEquals(_registeredDialogue, _dialogue))
                {
                    BuildContainer(_dialogue);
                    _registeredDialogue = _dialogue;
                }

                if (!ReferenceEquals(_callbackDialogue, _dialogue))
                {
                    _dialogue.OnChoiceSelected(FinishChoice, OnQuestioningFinished);
                    _callbackDialogue = _dialogue;
                }

                bool armed = _dialogue.UseContainerOnInteract(ContainerName);

                if (armed)
                {
                    _armed = true;
                    MelonLogger.Msg("[WVC Snitch] Marty Second Opinion dialogue armed.");
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed arming Marty investigation dialogue: " + ex.Message
                );
                return false;
            }
        }

        private static void BuildContainer(NPCDialogue dialogue)
        {
            dialogue.BuildAndRegisterContainer(
                ContainerName,
                builder =>
                {
                    builder.SetAllowExit(true);

                    builder.AddNode(
                        EntryNode,
                        "You look rattled. This about the heat, " +
                        "or you actually here for oil?",
                        choices =>
                        {
                            choices.Add(
                                AskEmpty,
                                "Someone cleaned out one of my dead drops " +
                                "before the cops got there.",
                                EmptyDropNode
                            );
                            choices.Add(LeaveChoice, "Never mind.", null);
                        }
                    );

                    builder.AddNode(
                        EmptyDropNode,
                        "That's not a raid. Raids leave a mess.\n\n" +
                        "That's somebody who had the calendar. " +
                        "I don't run the docks, man. That's not my side of town.",
                        choices =>
                        {
                            choices.Add(
                                AskWater,
                                "Roscoe said it was somebody who works near the water.",
                                WaterNode
                            );
                            choices.Add(LeaveChoice, "I'll come back.", null);
                        }
                    );

                    builder.AddNode(
                        WaterNode,
                        "Water-side? Yeah, I don't go down there.\n\n" +
                        "There's a guy who does. Carts. Always rolling through. " +
                        "He's been in here lately flashing more cash than a " +
                        "kitchen supplier should have.",
                        choices =>
                        {
                            choices.Add(
                                AskBehavior,
                                "He been acting different?",
                                BehaviorNode
                            );
                            choices.Add(LeaveChoice, "That's enough for now.", null);
                        }
                    );

                    builder.AddNode(
                        BehaviorNode,
                        "Used to sit and talk product. Now he checks the door " +
                        "every thirty seconds.\n\n" +
                        "Bought himself a van. Paid the lot in cash. " +
                        "I asked where the money came from and he changed the subject.",
                        choices =>
                        {
                            choices.Add(
                                AskStash,
                                "Did he leave anything behind?",
                                StashNode
                            );
                            choices.Add(LeaveChoice, "I need to think.", null);
                        }
                    );

                    builder.AddNode(
                        StashNode,
                        "Funny you ask.\n\n" +
                        "Two nights back he came through here in a hurry. " +
                        "Stuffed something in my drop out back and left without " +
                        "saying a word. Never came back for it.\n\n" +
                        "I ain't touching it. Behind the Slop Shop. " +
                        "You want answers? Go look.",
                        choices =>
                        {
                            choices.Add(
                                FinishChoice,
                                "I'll go check it out.",
                                null
                            );
                            choices.Add(LeaveChoice, "Later.", null);
                        }
                    );
                }
            );
        }

        private static void OnQuestioningFinished()
        {
            if (_completed)
                return;

            if (!SnitchStoryManager.IsAskMartyActive)
                return;

            _completed = true;
            _armed = false;

            MelonLogger.Msg("[WVC Snitch] Player finished questioning Marty.");

            SnitchStoryManager.NotifyMartyQuestioned();
            RestoreNormalDialogue();
        }

        public static void RestoreNormalDialogue()
        {
            try
            {
                _dialogue?.StopOverride();
                _armed = false;

                if (MartyMellows.Instance?.Dialogue != null)
                {
                    MartyMellowsDialogue.TryRegisterAndArm(
                        MartyMellows.Instance.Dialogue
                    );
                }

                MelonLogger.Msg(
                    "[WVC Snitch] Marty restored to normal supplier dialogue."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed restoring Marty dialogue: " + ex.Message
                );
            }
        }

        public static void Reset()
        {
            try
            {
                if (_armed)
                    _dialogue?.StopOverride();
            }
            catch { }

            _armed = false;
            _completed = false;
            _dialogue = null;

            // Preserve _registeredDialogue and _callbackDialogue to avoid
            // registering duplicate callbacks on the same dialogue object.

            try
            {
                if (MartyMellows.Instance?.Dialogue != null)
                {
                    MartyMellowsDialogue.TryRegisterAndArm(
                        MartyMellows.Instance.Dialogue
                    );
                }
            }
            catch { }
        }       
    }
}