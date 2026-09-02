using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public sealed class AskMartyQuest : Quest
    {
        protected override string Title => "Second Opinion";

        protected override string Description =>
            "Roscoe would not name anyone, but everything he said points at the water. " +
            "Before you move, get another read.\n\n" +
            "Talk to Marty Mellows at the Slop Shop.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry TalkToMartyEntry { get; private set; }

        public static readonly Vector3 MartyFallbackPosition =
            new Vector3(-50f, 1.06f, 70f);

        public AskMartyQuest()
        {
            TalkToMartyEntry = AddEntry(
                "Talk to Marty Mellows at the Slop Shop",
                MartyFallbackPosition
            );

            TryBindMartyPOI();
        }

        public bool TryBindMartyPOI()
        {
            try
            {
                if (TalkToMartyEntry == null || MartyMellows.Instance == null)
                    return false;

                TalkToMartyEntry.SetPOIToNPC(MartyMellows.Instance);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Could not bind Second Opinion marker to Marty: " + ex.Message
                );
                return false;
            }
        }

        public void CompleteMartyQuestioning()
        {
            try
            {
                if (TalkToMartyEntry != null &&
                    TalkToMartyEntry.State != S1API.Quests.Constants.QuestState.Completed)
                {
                    TalkToMartyEntry.Complete();
                }

                Complete();

                MelonLogger.Msg(
                    "[WVC Snitch] Second Opinion completed. Marty pointed at his dead drop."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] CompleteMartyQuestioning failed: " + ex.Message
                );
            }
        }
    }
}