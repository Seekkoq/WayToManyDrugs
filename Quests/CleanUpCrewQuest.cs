using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;

namespace CustomNPCExample.Quests
{
    public sealed class CleanUpCrewQuest : Quest
    {
        protected override string Title => "Clean Up Crew";

        protected override string Description =>
            "The heat cooled down overnight, but Damon says one package never made it back. " +
            "Check the Grey Docks dead drop and find out what happened.";

        protected override bool AutoBegin => false;

        protected override Sprite QuestIcon => SnitchQuestIcon.Get();

        public QuestEntry InspectDropEntry { get; private set; }

        public static readonly Vector3 BudsBarCoords =
            new Vector3(-18.5f, -0.76f, -44.8f);

        public static readonly Vector3 GreyDocksFallbackCoords =
            new Vector3(-85.4f, 1.2f, -145.8f);

        public CleanUpCrewQuest()
        {
            InspectDropEntry = AddEntry(
                "Inspect the missing Grey Docks dead drop",
                GreyDocksFallbackCoords
            );
        }

        public void BeginInspectStep()
        {
            try
            {
                InspectDropEntry?.Begin();
            }
            catch { }
        }

        public void SetInspectPosition(Vector3 position)
        {
            try
            {
                if (InspectDropEntry != null && position != Vector3.zero)
                {
                    InspectDropEntry.POIPosition = position;
                }
            }
            catch { }
        }

        public void CompleteInspectStep()
        {
            try
            {
                InspectDropEntry.Title =
                    "Inspect the missing Grey Docks dead drop (empty)";

                InspectDropEntry.Complete();
            }
            catch { }

            try
            {
                Complete();
            }
            catch { }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Clean Up Crew completed: Grey Docks dead drop was empty."
            );
        }
    }
}
