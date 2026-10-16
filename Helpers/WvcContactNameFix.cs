using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    /// <summary>
    /// Game build 0.4.7f12 replaced MSGConversation's public "contactName" member with a
    /// private "_contactName" backing field (the same rename family as Sendables ->
    /// _sendables that OffshoreSalesManager already works around). S1API v3.2.1 still
    /// writes the old "contactName" member by reflection and fails silently, so the phone
    /// falls back to the game's default contact label. For custom NPCs that label is
    /// mis-wired: Roscoe's thread shows up as "Damon Trey", or leaks an internal
    /// identifier like "s1api.custom...". We re-apply each custom NPC's real name to
    /// whichever member actually exists, right after S1API finishes setting it up (a
    /// postfix on S1API's base NPC.OnCreated, which every custom NPC reaches through its
    /// own base.OnCreated() call).
    /// </summary>
    internal static class WvcContactNameFix
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static HarmonyLib.Harmony _harmony;

        public static void ApplyPatch()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("wvc.npc.contactname");

                MethodInfo target =
                    AccessTools.Method(typeof(S1API.Entities.NPC), "OnCreated");

                if (target == null)
                {
                    MelonLogger.Warning(
                        "[ContactName] S1API NPC.OnCreated not found; message names may stay wrong.");
                    return;
                }

                _harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(WvcContactNameFix), nameof(Postfix))));

                global::CustomNPCExample.Utils.WvcLog.Msg("[ContactName] Patch applied.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[ContactName] ApplyPatch failed: " + ex.Message);
            }
        }

        private static void Postfix(S1API.Entities.NPC __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // Only our own custom NPCs - never vanilla or another mod's.
                if (__instance.GetType().Namespace != "CustomNPCExample.NPCs")
                    return;

                // The conversation may not be constructed yet on the first pass; retry briefly.
                if (!Apply(__instance))
                    MelonCoroutines.Start(RetryAsync(__instance));
            }
            catch (System.Exception)
            {
            }
        }

        private static IEnumerator RetryAsync(S1API.Entities.NPC npc)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (npc != null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (npc == null)
                    yield break;
                if (Apply(npc))
                    yield break;
            }
        }

        /// <summary>
        /// Writes the NPC's display name onto the game conversation's contact-name member
        /// (public "contactName" pre-0.4.7, private "_contactName" in 0.4.7). Returns true
        /// once the name has been applied.
        /// </summary>
        private static bool Apply(S1API.Entities.NPC npc)
        {
            try
            {
                var gameNpc =
                    npc.gameObject.GetComponent<Il2CppScheduleOne.NPCs.NPC>()
                    ?? npc.gameObject.GetComponentInChildren<Il2CppScheduleOne.NPCs.NPC>(true);

                if (gameNpc == null)
                    return false;

                var conv = gameNpc.MSGConversation;
                if (conv == null)
                    return false;

                string display = BuildDisplayName(npc);
                if (string.IsNullOrEmpty(display))
                    return false;

                // 0.4.7 dropped MSGConversation.contactName (S1API still writes it and fails
                // silently) for a computed ContactName that renders _sender._name - the sender's
                // MessageContactInfo carrying the display name, npc id and portrait. Build a
                // correct contact record and assign it to the conversation's _sender.
                string npcId = gameNpc.ID;
                if (string.IsNullOrEmpty(npcId))
                    npcId = display;

                Sprite icon = null;
                try { icon = conv.GetSenderIcon(); } catch { }

                var info = new Il2CppScheduleOne.Messaging.MessageContactInfo(
                    display, npcId, icon, false, true);

                var t = conv.GetType();

                // Preferred: the native setter, which writes game state correctly.
                var setter = t.GetMethod("set__sender", MemberFlags);
                if (setter != null)
                {
                    setter.Invoke(conv, new object[] { info });
                    return true;
                }

                // Fallback: the backing field, for interop shapes exposing it directly.
                var field = t.GetField("_sender", MemberFlags);
                if (field != null)
                {
                    field.SetValue(conv, info);
                    return true;
                }

                return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static string BuildDisplayName(S1API.Entities.NPC npc)
        {
            // FirstName/LastName are the identities we set via WithIdentity, so they are
            // authoritative here (unlike the game's FullName, which routes through the
            // broken contact path).
            string first = npc.FirstName ?? string.Empty;
            string last = npc.LastName ?? string.Empty;
            string full = (first + " " + last).Trim();
            return string.IsNullOrEmpty(full) ? null : full;
        }
    }
}