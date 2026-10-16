using System;

using NativeNpc = Il2CppScheduleOne.NPCs.NPC;
using NativeController = Il2CppScheduleOne.Dialogue.DialogueController;
using NativeHandler = Il2CppScheduleOne.Dialogue.DialogueHandler;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Retired. The casino and pawnshop laundering dialogues used to merge their nodes into a
    /// vanilla NPC's <c>DialogueContainer</c> through these helpers. Game build 0.4.7 replaced
    /// that container with a new conversation model, so the low-level node/choice surgery here
    /// no longer applies and the removed <c>DialogueContainer</c> type throws at runtime.
    ///
    /// The features now arm their own container through S1API's <c>NPCDialogue</c> abstraction
    /// (<c>BuildAndRegisterContainer</c> + <c>UseContainerOnInteract</c>), so these helpers are
    /// kept only as inert lookups that touch no removed type.
    /// </summary>
    public static class WvcDialogueChoice
    {
        /// <summary>The first DialogueController under the NPC, or null.</summary>
        public static NativeController FindController(NativeNpc npc)
        {
            try
            {
                var components = npc?.GetComponentsInChildren<NativeController>(true);
                if (components == null)
                    return null;

                foreach (NativeController controller in components)
                {
                    if (controller != null)
                        return controller;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>The NPC's dialogue handler, or null when it has none yet.</summary>
        public static NativeHandler FindHandler(NativeNpc npc)
        {
            try
            {
                var handlers = npc?.GetComponentsInChildren<NativeHandler>(true);
                if (handlers == null)
                    return null;

                foreach (NativeHandler handler in handlers)
                {
                    if (handler != null)
                        return handler;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
