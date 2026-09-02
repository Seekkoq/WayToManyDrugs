using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public sealed class LooseLipsQuest : Quest
    {
        protected override string Title => "Loose Lips";

        protected override string Description =>
            "The Grey Docks dead drop was emptied before the police arrived. " +
            "Someone inside the supply chain knew its location.\n\n" +
            "Question your suppliers. Start with Roscoe at Bud's Bar.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon =>
            SnitchQuestIcon.Get();

        public QuestEntry QuestionRoscoeEntry
        {
            get;
            private set;
        }

        // Fallback until the NPC marker can bind.
        public static readonly Vector3 RoscoeFallbackPosition =
            new Vector3(-72f, -0.76f, -58f);

        public LooseLipsQuest()
        {
            QuestionRoscoeEntry = AddEntry(
                "Question Roscoe (0/1)",
                RoscoeFallbackPosition
            );

            TryBindRoscoePOI();
        }

        public bool TryBindRoscoePOI()
        {
            try
            {
                if (QuestionRoscoeEntry == null ||
                    RoscoeBellweather.Instance == null)
                {
                    return false;
                }

                QuestionRoscoeEntry.SetPOIToNPC(
                    RoscoeBellweather.Instance
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Could not bind Loose Lips marker to Roscoe: " +
                    ex.Message
                );

                return false;
            }
        }

        public void CompleteRoscoeQuestioning()
        {
            try
            {
                if (QuestionRoscoeEntry != null)
                {
                    QuestionRoscoeEntry.Title =
                        "Question Roscoe (1/1)";

                    if (QuestionRoscoeEntry.State !=
                        S1API.Quests.Constants.QuestState.Completed)
                    {
                        QuestionRoscoeEntry.Complete();
                    }
                }

                Complete();

                MelonLogger.Msg(
                    "[WVC Snitch] Loose Lips completed. " +
                    "Roscoe's clues point toward Remy."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] Could not complete Roscoe questioning: " +
                    ex.Message
                );
            }
        }
    }
}