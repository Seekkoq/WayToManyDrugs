using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;

namespace CustomNPCExample.Quests
{
    public sealed class HardEvidenceQuest : Quest
    {
        protected override string Title => "Hard Evidence";

        protected override string Description =>
            "Marty revealed that Remy stashed a suspicious vape cart in the dead drop behind the Slop Shop.\n\n" +
            "Search the dead drop behind the Slop Shop and recover the evidence.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry CollectEvidenceEntry { get; private set; }

        public static readonly Vector3 SlopShopDropFallback = new Vector3(-58.2f, 1.1f, 68.4f);

        public HardEvidenceQuest()
        {
            CollectEvidenceEntry = AddEntry(
                "Recover the stashed cart from the Slop Shop dead drop",
                SlopShopDropFallback
            );
        }

        public void SetDropPosition(Vector3 position)
        {
            try
            {
                if (CollectEvidenceEntry != null && position != Vector3.zero)
                {
                    CollectEvidenceEntry.POIPosition = position;
                }
            }
            catch { }
        }

        public void BeginCollectStep()
        {
            try
            {
                CollectEvidenceEntry?.Begin();
            }
            catch { }
        }

        public void CompleteCollectStep()
        {
            try
            {
                if (CollectEvidenceEntry != null)
                {
                    CollectEvidenceEntry.Title = "Recover the stashed cart from the Slop Shop dead drop (Collected)";
                    if (CollectEvidenceEntry.State != S1API.Quests.Constants.QuestState.Completed)
                    {
                        CollectEvidenceEntry.Complete();
                    }
                }

                Complete();
                MelonLogger.Msg("[WVC Snitch] Hard Evidence completed: Cart recovered.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] CompleteCollectStep failed: " + ex.Message);
            }
        }
    }
}