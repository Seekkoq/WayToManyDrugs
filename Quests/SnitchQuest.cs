using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;
using CustomNPCExample.NPCs;

namespace CustomNPCExample.Quests
{
    public sealed class SnitchQuest : Quest
    {
        protected override string Title => "The Snitch";

        protected override string Description =>
            "One of your suppliers got pinched by the feds and is talking. " +
            "Damon Trey has the details. Meet him at Bud's Bar.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry TalkToDamonEntry { get; private set; }
        public QuestEntry BurnRouteEntry { get; private set; }

        public SnitchQuest()
        {
            TalkToDamonEntry = AddEntry(
                "Talk to Damon Trey at Bud's Bar",
                new Vector3(-18.5f, -0.76f, -44.8f)
            );

            BurnRouteEntry = AddEntry(
                "Clear the compromised dead drops (0/7)"
            );

            BurnRouteEntry.SetState(
                S1API.Quests.Constants.QuestState.Inactive
            );

            TryBindDamonPOI();
        }

        public void TryBindDamonPOI()
        {
            try
            {
                if (DamonTrey.Instance != null && TalkToDamonEntry != null)
                {
                    TalkToDamonEntry.SetPOIToNPC(DamonTrey.Instance);
                }
            }
            catch (Exception)
            {

            }
        }

        public void UpdateDropCounter(int cleared, int total)
        {
            if (BurnRouteEntry == null) return;

            try
            {
                BurnRouteEntry.Title = $"Clear the compromised dead drops ({cleared}/{total})";
            }
            catch { }
        }

        public void CompleteTalkStep()
        {
            try
            {
                if (TalkToDamonEntry != null &&
                    TalkToDamonEntry.State != S1API.Quests.Constants.QuestState.Completed)
                {
                    TalkToDamonEntry.Complete();
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Step 1 complete: Talked to Damon.");
                }

                if (BurnRouteEntry != null)
                {
                    BurnRouteEntry.Begin();
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Step 2 started: Burn the Routes.");

                    SnitchDeadDrop.SetupBurnRoute(this, BurnRouteEntry);
                }

                SnitchDialogue.RestoreNormalDialogue();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] CompleteTalkStep failed: " + ex);
            }
        }
    }
}
