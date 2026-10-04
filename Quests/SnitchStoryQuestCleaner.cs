using System;
using MelonLoader;
using NativeQuest = Il2CppScheduleOne.Quests.Quest;

namespace CustomNPCExample.Quests
{
    public static class SnitchStoryQuestCleaner
    {
        public static int CancelAllLoadedStoryQuests()
        {
            int cancelled = 0;

            try
            {
                var quests = NativeQuest.Quests;
                if (quests == null) return 0;

                for (int i = quests.Count - 1; i >= 0; i--)
                {
                    var quest = quests[i];
                    if (quest == null) continue;

                    string title = "";
                    try { title = quest.Title ?? quest.title ?? quest.name ?? ""; } catch { }

                    if (!IsTransientStoryQuest(title))
                        continue;

                    try
                    {
                        quest.Cancel();
                        cancelled++;
                    }
                    catch { }
                }
            }
            catch (Exception)
            {

            }

            if (cancelled > 0)
                global::CustomNPCExample.Utils.WvcLog.Msg($"[WVC Snitch] Cancelled {cancelled} transient story quest instance(s).");

            return cancelled;
        }

        public static bool IsTransientStoryQuest(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            return title.Equals("Under the Radar", StringComparison.OrdinalIgnoreCase) ||
                   title.Equals("Clean Up Crew", StringComparison.OrdinalIgnoreCase) ||
                   title.Equals("Loose Lips", StringComparison.OrdinalIgnoreCase) ||
                   title.Equals("Second Opinion", StringComparison.OrdinalIgnoreCase) ||
                   title.Equals("Hard Evidence", StringComparison.OrdinalIgnoreCase) ||
                   title.Equals("Face to Face", StringComparison.OrdinalIgnoreCase);
        }
    }
}