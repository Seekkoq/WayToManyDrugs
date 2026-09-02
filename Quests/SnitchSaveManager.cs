using System;
using System.Collections.Generic;
using MelonLoader;

namespace CustomNPCExample.Quests
{
    public static class SnitchSaveManager
    {
        public const int STAGE_NOT_STARTED = 0;
        public const int STAGE_TALK_TO_DAMON = 1;
        public const int STAGE_BURN_ROUTES = 2;
        public const int STAGE_COMPLETED = 3;

        private static readonly MelonPreferences_Category Category =
            MelonPreferences.CreateCategory(
                "WVC_Snitch",
                "Westville Connection - The Snitch"
            );

        private static readonly MelonPreferences_Entry<int> StagePref =
            Category.CreateEntry(
                "Stage",
                STAGE_NOT_STARTED
            );

        private static readonly MelonPreferences_Entry<string>
            CompromisedDropsPref =
                Category.CreateEntry(
                    "CompromisedDrops",
                    ""
                );

        private static readonly MelonPreferences_Entry<string>
            ClearedDropsPref =
                Category.CreateEntry(
                    "ClearedDrops",
                    ""
                );

        public static int Stage
        {
            get => StagePref.Value;
            set
            {
                StagePref.Value = value;
                Category.SaveToFile();
            }
        }

        public static List<string> CompromisedDrops
        {
            get => ParseList(CompromisedDropsPref.Value);
            set
            {
                CompromisedDropsPref.Value =
                    value == null
                        ? ""
                        : string.Join("|", value);

                Category.SaveToFile();
            }
        }

        public static List<string> ClearedDrops
        {
            get => ParseList(ClearedDropsPref.Value);
            set
            {
                ClearedDropsPref.Value =
                    value == null
                        ? ""
                        : string.Join("|", value);

                Category.SaveToFile();
            }
        }

        public static bool IsCompleted =>
            Stage == STAGE_COMPLETED;

        public static bool IsBurnRouteActive =>
            Stage == STAGE_BURN_ROUTES;

        public static bool IsTalkActive =>
            Stage == STAGE_TALK_TO_DAMON;

        public static bool IsNotStarted =>
            Stage == STAGE_NOT_STARTED;

        public static void SetStage(int stage)
        {
            Stage = stage;
        }

        public static void RecordRouteSetup(
            List<string> dropNames)
        {
            Stage = STAGE_BURN_ROUTES;

            CompromisedDrops =
                dropNames == null
                    ? new List<string>()
                    : new List<string>(dropNames);

            ClearedDrops = new List<string>();
        }

        public static void RecordDropCleared(
            string dropName)
        {
            if (string.IsNullOrEmpty(dropName))
                return;

            List<string> cleared = ClearedDrops;

            for (int i = 0; i < cleared.Count; i++)
            {
                if (string.Equals(
                        cleared[i],
                        dropName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            cleared.Add(dropName);
            ClearedDrops = cleared;
        }

        public static bool IsDropCleared(
            string dropName)
        {
            if (string.IsNullOrEmpty(dropName))
                return false;

            List<string> cleared = ClearedDrops;

            for (int i = 0; i < cleared.Count; i++)
            {
                if (string.Equals(
                        cleared[i],
                        dropName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static void MarkCompleted()
        {
            Stage = STAGE_COMPLETED;
        }

        public static void ResetSave()
        {
            Stage = STAGE_NOT_STARTED;
            CompromisedDrops = new List<string>();
            ClearedDrops = new List<string>();
        }

        private static List<string> ParseList(
            string value)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(value))
                return result;

            string[] values = value.Split('|');

            for (int i = 0; i < values.Length; i++)
            {
                string item = values[i];

                if (string.IsNullOrWhiteSpace(item))
                    continue;

                bool exists = false;

                for (int j = 0; j < result.Count; j++)
                {
                    if (string.Equals(
                            result[j],
                            item,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    result.Add(item);
            }

            return result;
        }
    }
}