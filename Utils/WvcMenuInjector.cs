using System;
using System.Collections;
using System.Reflection;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.NPCs;
using DialogueController = Il2CppScheduleOne.Dialogue.DialogueController;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Injects our laundering option into a vanilla NPC's own interact menu, so it appears
    /// ALONGSIDE the NPC's vanilla options (pawn/buy) instead of replacing them. On 0.4.7 the
    /// vanilla menu is the controller's public Choices property (List&lt;DialogueChoice&gt;); each
    /// DialogueChoice has a settable ChoiceText and a Conversation that opens when picked. We
    /// construct a DialogueChoice directly (il2cpp object_new via its public parameterless ctor)
    /// and add it to Choices, so vanilla's own Interacted builds a menu showing both.
    ///
    /// The running game types DialogueChoice.Conversation as Conversation; the stale
    /// Assembly-CSharp this project compiles against types it as DialogueContainer, a type that
    /// NO LONGER EXISTS at runtime (so any compiled setter binding throws TypeLoadException the
    /// moment the method is JIT'd). We therefore set Conversation by name via reflection, which
    /// resolves the property against the running assembly and accepts the live container object
    /// directly. We locate our built container by reading the handler's dialogueContainers via
    /// reflection (that member's accessor was also renamed). Everything else (new
    /// DialogueChoice(), Choices, primitive setters) uses compiled types that il2cpp resolves by
    /// name at runtime regardless of the managed type annotation.
    /// </summary>
    internal static class WvcMenuInjector
    {
        /// <summary>
        /// Adds (or refreshes) a menu option on <paramref name="controller"/> whose text is
        /// <paramref name="choiceText"/> and whose conversation is the container named
        /// <paramref name="containerName"/> on <paramref name="npc"/>'s handler. Idempotent.
        /// </summary>
        public static bool AddOrRefresh(
            DialogueController controller,
            NPC npc,
            string containerName,
            string choiceText,
            Action<string> log)
        {
            try
            {
                if (controller == null || npc == null ||
                    string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(choiceText))
                {
                    log?.Invoke("inject: bad args (controller/npc/container/text).");
                    return false;
                }

                var handler = npc.GetComponentInChildren<DialogueHandler>(true);
                if (handler == null)
                {
                    log?.Invoke("inject: no DialogueHandler on npc.");
                    return false;
                }

                object container = FindContainerOnHandler(handler, containerName, log);
                if (container == null)
                {
                    log?.Invoke("inject: container '" + containerName + "' not found on handler.");
                    return false;
                }

                var choices = controller.Choices;
                if (choices == null)
                {
                    log?.Invoke("inject: controller.Choices is null.");
                    return false;
                }

                // Diagnostic: dump the vanilla menu we are about to add to, so an empty/foreign
                // Choices list is visible in the log (tells us whether this NPC's interact is even
                // driven by DialogueChoice menus).
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("inject: vanilla Choices count=").Append(choices.Count).Append(" [");
                    for (int k = 0; k < choices.Count; k++)
                    {
                        if (k > 0) sb.Append(" | ");
                        sb.Append(choices[k] != null ? choices[k].ChoiceText : "<null>");
                    }
                    sb.Append("]");
                    log?.Invoke(sb.ToString());
                }
                catch (Exception)
                {
                }

                for (int i = choices.Count - 1; i >= 0; i--)
                {
                    var existing = choices[i];
                    if (existing == null) continue;
                    if (existing.ChoiceText == choiceText)
                        choices.RemoveAt(i);
                }

                var choice = new DialogueController.DialogueChoice();
                choice.ChoiceText = choiceText;
                choice.Enabled = true;
                choice.Priority = 0;
                // The running game types Conversation as Conversation (not the stale
                // DialogueContainer), so a compiled setter binding would throw TypeLoadException
                // at JIT time. Set the property by name via reflection, which resolves against the
                // running assembly and accepts the live container object straight through.
                var convoProp = choice.GetType().GetProperty(
                    "Conversation", BindingFlags.Public | BindingFlags.Instance);
                if (convoProp == null || !convoProp.CanWrite)
                {
                    log?.Invoke("inject: no writable Conversation property on DialogueChoice.");
                    return false;
                }
                convoProp.SetValue(choice, container);

                // Prefer the game's own AddDialogueChoice (public virtual) so the controller
                // records the choice the way vanilla expects (priority insertion + refresh);
                // fall back to a direct list add if that native call is unavailable.
                try
                {
                    controller.AddDialogueChoice(choice, 0);
                    log?.Invoke("inject: added menu option '" + choiceText +
                                "' via AddDialogueChoice (choices now " + choices.Count + ").");
                }
                catch (Exception)
                {
                    choices.Add(choice);
                    log?.Invoke("inject: added menu option '" + choiceText +
                                "' via Choices.Add (choices now " + choices.Count + ").");
                }
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke("inject: threw " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        /// <summary>Removes any menu option we previously added (same text), leaving vanilla intact.</summary>
        public static void Remove(DialogueController controller, string choiceText, Action<string> log)
        {
            try
            {
                if (controller == null || string.IsNullOrEmpty(choiceText))
                    return;
                var choices = controller.Choices;
                if (choices == null)
                    return;
                for (int i = choices.Count - 1; i >= 0; i--)
                {
                    var existing = choices[i];
                    if (existing == null) continue;
                    if (existing.ChoiceText == choiceText)
                        choices.RemoveAt(i);
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("inject remove: " + ex.Message);
            }
        }

        /// <summary>
        /// Finds the built container by name on the handler's dialogueContainers list. On 0.4.7
        /// that list is an Il2CppSystem.Collections.Generic.List (it does NOT implement managed
        /// System.Collections.IList), so a cast to IList fails and misses the container even when
        /// it is present. We instead enumerate via reflection through Count + the int indexer,
        /// which both the Il2Cpp and managed list types expose.
        /// </summary>
        private static object FindContainerOnHandler(
            DialogueHandler handler, string containerName, Action<string> log)
        {
            var list = GetMemberValue(handler, "dialogueContainers")
                    ?? GetMemberValue(handler, "DialogueContainers");
            if (list == null)
            {
                log?.Invoke("inject: handler has no readable dialogueContainers member.");
                return null;
            }

            int count = GetIntMember(list, "Count");
            var indexer = list.GetType().GetProperty(
                "Item", BindingFlags.Public | BindingFlags.Instance);
            if (count < 0 || indexer == null)
            {
                log?.Invoke("inject: could not enumerate container list (count=" + count +
                            ", indexer=" + (indexer != null) + ").");
                return null;
            }

            var names = new System.Text.StringBuilder();
            object found = null;
            for (int i = 0; i < count; i++)
            {
                object item;
                try { item = indexer.GetValue(list, new object[] { i }); }
                catch { continue; }
                if (item == null) continue;
                var name = GetMemberValue(item, "name") as string;
                if (names.Length > 0) names.Append(", ");
                names.Append(name ?? "<null>");
                if (found == null && name == containerName)
                    found = item;
            }

            if (found == null)
                log?.Invoke("inject: container '" + containerName + "' not found; handler lists [" +
                            names + "].");
            return found;
        }

        private static int GetIntMember(object target, string member)
        {
            var v = GetMemberValue(target, member);
            return v is int i ? i : -1;
        }

        // Reflection helper (field OR property, public OR non-public).
        private static object GetMemberValue(object target, string member)
        {
            if (target == null) return null;
            var t = target.GetType();
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var p = t.GetProperty(member, F);
            if (p != null && p.CanRead)
            {
                try { return p.GetValue(target); } catch { }
            }
            var f = t.GetField(member, F);
            if (f != null)
            {
                try { return f.GetValue(target); } catch { }
            }
            return null;
        }
    }
}

