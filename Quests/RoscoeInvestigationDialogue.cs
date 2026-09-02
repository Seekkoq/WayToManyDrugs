using System;
using MelonLoader;
using S1API.Entities;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public static class RoscoeInvestigationDialogue
    {
        public const string ContainerName =
            "wvc_roscoe_loose_lips";

        private const string EntryNode = "ENTRY";
        private const string EmptyDropNode = "EMPTY_DROP";
        private const string ScheduleNode = "SCHEDULE";
        private const string BehaviorNode = "BEHAVIOR";
        private const string ConclusionNode = "CONCLUSION";

        private const string AskEmptyDrop =
            "loose_lips_empty_drop";

        private const string AskSchedule =
            "loose_lips_schedule";

        private const string AskBehavior =
            "loose_lips_behavior";

        private const string AskConclusion =
            "loose_lips_conclusion";

        private const string FinishChoice =
            "loose_lips_finish";

        private const string LeaveChoice =
            "loose_lips_leave";

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

            if (!SnitchStoryManager.IsLooseLipsActive)
                return false;

            try
            {
                RoscoeBellweather roscoe =
                    RoscoeBellweather.Instance;

                if (roscoe == null)
                    return false;

                _dialogue = roscoe.Dialogue;

                if (_dialogue == null)
                    return false;

                // Only build the container once for each dialogue instance.
                if (!ReferenceEquals(
                        _registeredDialogue,
                        _dialogue))
                {
                    BuildContainer(_dialogue);
                    _registeredDialogue = _dialogue;
                }

                // Only hook the callback once per dialogue instance.
                if (!ReferenceEquals(
                        _callbackDialogue,
                        _dialogue))
                {
                    _dialogue.OnChoiceSelected(
                        FinishChoice,
                        OnQuestioningFinished
                    );

                    _callbackDialogue = _dialogue;
                }

                bool armed =
                    _dialogue.UseContainerOnInteract(
                        ContainerName
                    );

                if (armed)
                {
                    _armed = true;

                    MelonLogger.Msg(
                        "[WVC Snitch] Roscoe Loose Lips dialogue armed."
                    );
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed arming Roscoe investigation dialogue: " +
                    ex.Message
                );

                return false;
            }
        }

        private static void BuildContainer(
            NPCDialogue dialogue)
        {
            dialogue.BuildAndRegisterContainer(
                ContainerName,
                builder =>
                {
                    builder.SetAllowExit(true);

                    // ================================================
                    // ENTRY
                    // ================================================

                    builder.AddNode(
                        EntryNode,

                        "You look like you came here for something " +
                        "other than precursor stock. What's going on?",

                        choices =>
                        {
                            choices.Add(
                                AskEmptyDrop,
                                "One of my dead drops was empty before " +
                                "the cops got there. You hear anything?",
                                EmptyDropNode
                            );

                            choices.Add(
                                LeaveChoice,
                                "Never mind.",
                                null
                            );
                        }
                    );

                    // ================================================
                    // EMPTY DROP
                    // ================================================

                    builder.AddNode(
                        EmptyDropNode,

                        "Empty before the cops? Then it wasn't the cops.\n\n" +

                        "Feds kick doors, flip boxes, leave tape and boot " +
                        "prints everywhere. They don't clean a drop neat. " +

                        "Whoever took that package knew exactly where it " +
                        "was and exactly when to grab it.",

                        choices =>
                        {
                            choices.Add(
                                AskSchedule,
                                "Who knew the dead-drop schedule?",
                                ScheduleNode
                            );

                            choices.Add(
                                LeaveChoice,
                                "I'll come back.",
                                null
                            );
                        }
                    );

                    // ================================================
                    // SCHEDULE
                    // ================================================

                    builder.AddNode(
                        ScheduleNode,

                        "Not many people should've.\n\n" +

                        "Damon sets the drops, but Damon ain't dumb enough " +
                        "to burn his own map. Marty keeps his business away " +
                        "from the docks. Gus is suburbia — he doesn't run " +
                        "water-side.\n\n" +

                        "Only one supplier I know spends that much time " +
                        "moving product around the water.",

                        choices =>
                        {
                            choices.Add(
                                AskBehavior,
                                "Anybody been acting different lately?",
                                BehaviorNode
                            );

                            choices.Add(
                                LeaveChoice,
                                "That's enough for now.",
                                null
                            );
                        }
                    );

                    // ================================================
                    // BEHAVIOR
                    // ================================================

                    builder.AddNode(
                        BehaviorNode,

                        "Different? Yeah.\n\n" +

                        "Someone bought a new van last week. Cash. No " +
                        "financing, no questions. Been jumpy at Bud's too. " +
                        "Comes in, checks the room, leaves early.\n\n" +

                        "Used to drink like a fish. Now he barely touches " +
                        "the glass. That ain't discipline. That's nerves.",

                        choices =>
                        {
                            choices.Add(
                                AskConclusion,
                                "You think he took the package?",
                                ConclusionNode
                            );

                            choices.Add(
                                LeaveChoice,
                                "I need to think.",
                                null
                            );
                        }
                    );

                    // ================================================
                    // CONCLUSION
                    // ================================================

                    builder.AddNode(
                        ConclusionNode,

                        "I ain't saying the name out loud.\n\n" +

                        "But if a man works near the water, suddenly has " +
                        "cash, suddenly has wheels, and suddenly can't sit " +
                        "still in a bar... you don't need me to spell it out.",

                        choices =>
                        {
                            choices.Add(
                                FinishChoice,
                                "I know who you mean.",
                                null
                            );

                            choices.Add(
                                LeaveChoice,
                                "I'll look into it.",
                                null
                            );
                        }
                    );
                }
            );
        }

        private static void OnQuestioningFinished()
        {
            if (_completed)
                return;

            if (!SnitchStoryManager.IsLooseLipsActive)
                return;

            _completed = true;
            _armed = false;

            MelonLogger.Msg(
                "[WVC Snitch] Player finished questioning Roscoe."
            );

            // This changes IsLooseLipsActive to false before normal
            // supplier dialogue is restored.
            SnitchStoryManager.NotifyRoscoeQuestioned();

            RestoreNormalDialogue();
        }

        public static void RestoreNormalDialogue()
        {
            try
            {
                _dialogue?.StopOverride();
                _armed = false;

                if (RoscoeBellweather.Instance?.Dialogue != null)
                {
                    RoscoeDialogue.TryRegisterAndArm(
                        RoscoeBellweather.Instance.Dialogue
                    );
                }

                MelonLogger.Msg(
                    "[WVC Snitch] Roscoe restored to normal supplier dialogue."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed restoring Roscoe dialogue: " +
                    ex.Message
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
                if (RoscoeBellweather.Instance?.Dialogue != null)
                {
                    RoscoeDialogue.TryRegisterAndArm(
                        RoscoeBellweather.Instance.Dialogue
                    );
                }
            }
            catch { }
        }
    }
}