using System;
using MelonLoader;
using S1API.Entities;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public static class RemyConfrontationDialogue
    {
        public const string ContainerName =
            "wvc_remy_confrontation";

        private const string EntryNode = "ENTRY";
        private const string PanicNode = "PANIC";
        private const string ExtortNode = "EXTORT";

        private const string ShowProofChoice =
            "remy_show_proof";

        private const string ExtortChoice =
            "remy_choice_extort";

        private const string LeaveChoice =
            "remy_leave";

        private const string ExitChoice =
            "remy_exit";

        private static bool _armed;
        private static bool _completed;

        private static NPCDialogue _dialogue;
        private static NPCDialogue _registeredDialogue;
        private static NPCDialogue _callbackDialogue;

        public static bool IsArmed => _armed;
        public static bool IsCompleted => _completed;

        public static bool ArmConfrontationDialogue()
        {
            if (_completed)
                return true;

            if (_armed)
                return true;

            if (!SnitchStoryManager.IsConfrontRemyActive)
                return false;

            try
            {
                RemyFogarty remy = RemyFogarty.Instance;

                if (remy == null)
                    return false;

                _dialogue = remy.Dialogue;

                if (_dialogue == null)
                    return false;

                if (!ReferenceEquals(
                        _registeredDialogue,
                        _dialogue))
                {
                    BuildContainer(_dialogue);
                    _registeredDialogue = _dialogue;
                }

                if (!ReferenceEquals(
                        _callbackDialogue,
                        _dialogue))
                {
                    _dialogue.OnChoiceSelected(
                        ExtortChoice,
                        OnExtortSelected
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
                        "[WVC Snitch] Remy confrontation dialogue armed. " +
                        "Extort is the only ending."
                    );
                }

                return armed;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed arming Remy confrontation: " +
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

                    builder.AddNode(
                        EntryNode,

                        "What do you want, man? I'm trying to relax.",

                        choices =>
                        {
                            choices.Add(
                                ShowProofChoice,

                                "Explain why this cart has a police evidence batch stamp on it.",

                                PanicNode
                            );

                            choices.Add(
                                LeaveChoice,
                                "We'll talk later.",
                                null
                            );
                        }
                    );

                    builder.AddNode(
                        PanicNode,

                        "What?! Where did you get that?!\n\n" +
                        "Look, you don't understand. The feds cornered me " +
                        "at the docks. They said if I didn't feed them names, " +
                        "I was going away for ten years.\n\n" +
                        "I didn't want to do it. I swear. Please don't tell Damon.",

                        choices =>
                        {
                            choices.Add(
                                ExtortChoice,

                                "[Extort Him] Pay me $2,500 and give me three carts, " +
                                "or Damon gets this evidence.",

                                ExtortNode
                            );
                        }
                    );

                    builder.AddNode(
                        ExtortNode,

                        "Fine! Take the money and the carts. " +
                        "Just keep Damon away from me. " +
                        "You never saw me, understand?",

                        choices =>
                        {
                            choices.Add(
                                ExitChoice,
                                "Pleasure doing business.",
                                null
                            );
                        }
                    );
                }
            );
        }

        private static void OnExtortSelected()
        {
            if (_completed ||
                !SnitchStoryManager.IsConfrontRemyActive)
            {
                return;
            }

            _completed = true;
            _armed = false;

            MelonLogger.Msg(
                "[WVC Snitch] Player chose to extort Remy."
            );

            SnitchStoryManager.NotifyRemyConfronted(3);
            RestoreNormalDialogue();
        }

        public static void RestoreNormalDialogue()
        {
            try
            {
                _dialogue?.StopOverride();
                _armed = false;

                if (RemyFogarty.Instance?.Dialogue != null)
                {
                    RemyDialogue.TryRegisterAndArm(
                        RemyFogarty.Instance.Dialogue
                    );
                }

                MelonLogger.Msg(
                    "[WVC Snitch] Remy restored to normal dialogue."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Failed restoring Remy dialogue: " +
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

            // Do not null _registeredDialogue or _callbackDialogue.
            // Their callbacks remain registered on the old dialogue object.
            // A new dialogue instance will fail ReferenceEquals and register
            // itself normally.

            try
            {
                if (RemyFogarty.Instance?.Dialogue != null)
                {
                    RemyDialogue.TryRegisterAndArm(
                        RemyFogarty.Instance.Dialogue
                    );
                }
            }
            catch { }
        }
    }
}