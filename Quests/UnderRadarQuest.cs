using System;
using S1API.Quests;
using UnityEngine;
using MelonLoader;

namespace CustomNPCExample.Quests
{
    public sealed class UnderRadarQuest : Quest
    {
        protected override string Title => "Under the Radar";

        protected override string Description =>
            "The police are searching the city after the dead-drop operation. " +
            "Lay low and sleep until morning.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry SleepEntry { get; private set; }

        public UnderRadarQuest()
        {
            SleepEntry = AddEntry(
                "Go home and sleep until morning"
            );

            MelonLogger.Msg(
                "[WVC Snitch] Under the Radar quest created."
            );
        }

        public void CompleteSleepStep()
        {
            try
            {
                if (SleepEntry != null &&
                    SleepEntry.State !=
                    S1API.Quests.Constants.QuestState.Completed)
                {
                    SleepEntry.Complete();
                }

                Complete();

                MelonLogger.Msg(
                    "[WVC Snitch] Under the Radar completed."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] Failed completing Under the Radar: " +
                    ex.Message
                );
            }
        }
    }
}