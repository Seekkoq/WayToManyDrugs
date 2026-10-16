using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Texts the player when money waiting at Philip's cage or Mick's counter becomes collectible.
    /// Checked every few seconds from the mod's main update loop; the "already told" flag is
    /// stored per save next to the waiting money, so each handover texts exactly once.
    ///
    /// The text is sent through S1API's NPC wrapper (S1API.Entities.NPC.Get(id).SendTextMessage),
    /// not the raw native NPC.SendTextMessage. The native method silently drops the message when
    /// the NPC has no messaging conversation, and a vanilla shop NPC such as Philip or Mick has
    /// none by default. S1API's wrapper calls EnsureMessageConversationReady first - creating the
    /// phone thread - and then posts the message, which is exactly how the offshore-sales feature
    /// reaches Declan over the phone.
    /// </summary>
    public static class WvcClaimReadyNotifier
    {
        private const float Interval = 5f;
        private static float _timer;

        /// <summary>NPC ids whose "not loaded yet" deferral has already been logged (anti-spam).</summary>
        private static readonly HashSet<string> DeferredLogged = new HashSet<string>();

        public static void Tick()
        {
            try
            {
                _timer += Time.unscaledDeltaTime;
                if (_timer < Interval)
                    return;

                _timer = 0f;

                Check(
                    CustomNPCExample.Casino.PhilipWentworthBootstrap.PhilipNpcId,
                    CustomNPCExample.Casino.CasinoLaunderingManager.FeatureEnabled,
                    CustomNPCExample.Casino.CasinoLaunderingManager.HasPending,
                    CustomNPCExample.Casino.CasinoLaunderingManager.IsClaimReady,
                    CustomNPCExample.Casino.CasinoLaunderingManager.ClaimReadyNotified,
                    CustomNPCExample.Casino.CasinoLaunderingManager.MarkClaimReadyNotified,
                    "Philip Wentworth: The cage's books are square - your $65,000 is clean and " +
                    "ready. Come by the cage and collect it.");

                Check(
                    CustomNPCExample.Pawnshop.MickLubbinBootstrap.MickNpcId,
                    CustomNPCExample.Pawnshop.PawnshopLaunderingManager.FeatureEnabled,
                    CustomNPCExample.Pawnshop.PawnshopLaunderingManager.HasPending,
                    CustomNPCExample.Pawnshop.PawnshopLaunderingManager.IsClaimReady,
                    CustomNPCExample.Pawnshop.PawnshopLaunderingManager.ClaimReadyNotified,
                    CustomNPCExample.Pawnshop.PawnshopLaunderingManager.MarkClaimReadyNotified,
                    "Mick Lubbin: Your twenty's been on the books a day and it's sitting in the " +
                    "till, clean. Come collect it.");
            }
            catch (Exception ex)
            {
                WvcLog.Always("[WVC Claim] Ready check failed: " + ex.Message);
            }
        }

        private static void Check(
            string npcId,
            bool featureOn,
            bool hasPending,
            bool ready,
            bool alreadyTold,
            Action markTold,
            string body)
        {
            if (!featureOn || !hasPending || !ready || alreadyTold)
                return;

            // Reach the NPC through S1API's wrapper so SendTextMessage creates the phone messaging
            // thread first. The raw native call drops the message when the NPC has no conversation.
            S1API.Entities.NPC npc;
            try
            {
                npc = S1API.Entities.NPC.Get(npcId);
            }
            catch (Exception ex)
            {
                WvcLog.Always("[WVC Claim] S1API lookup failed for " + npcId + ": " + ex.Message);
                return;
            }

            if (npc == null)
            {
                // The NPC has not spawned yet - try again on the next tick. Logged once per id.
                if (DeferredLogged.Add(npcId))
                    WvcLog.Always("[WVC Claim] " + npcId + " is not loaded yet - ready text deferred.");
                return;
            }

            try
            {
                npc.SendTextMessage(body);
                markTold();
                WvcLog.Always("[WVC Claim] Ready text sent to the player from " + npcId + ".");
            }
            catch (Exception ex)
            {
                WvcLog.Always("[WVC Claim] Could not send the ready text: " + ex.Message);
            }
        }
    }
}
