using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public sealed class ConfrontRemyQuest : Quest
    {
        protected override string Title => "Face to Face";

        protected override string Description =>
            "The recovered vape cart has a police evidence batch number stamped on the casing.\n\n" +
            "Find Remy Fogarty and confront him with the proof.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry ConfrontRemyEntry { get; private set; }

        public static readonly Vector3 RemyFallbackPosition = new Vector3(-12.4f, -0.76f, 42.8f);

        public ConfrontRemyQuest()
        {
            ConfrontRemyEntry = AddEntry(
                "Confront Remy Fogarty with the evidence",
                RemyFallbackPosition
            );

            TryBindRemyPOI();
        }

        public bool TryBindRemyPOI()
        {
            try
            {
                if (ConfrontRemyEntry == null || RemyFogarty.Instance == null)
                    return false;

                ConfrontRemyEntry.SetPOIToNPC(RemyFogarty.Instance);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Snitch] Could not bind Face to Face marker to Remy: " + ex.Message);
                return false;
            }
        }

        public void CompleteConfrontation()
        {
            try
            {
                if (ConfrontRemyEntry != null && ConfrontRemyEntry.State != S1API.Quests.Constants.QuestState.Completed)
                {
                    ConfrontRemyEntry.Complete();
                }

                Complete();
                MelonLogger.Msg("[WVC Snitch] Face to Face completed! Remy confrontation resolved.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] CompleteConfrontation failed: " + ex.Message);
            }
        }
    }
}