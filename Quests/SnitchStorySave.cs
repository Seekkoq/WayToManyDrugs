using MelonLoader;

namespace CustomNPCExample.Quests
{
    public static class SnitchStorySave
    {
        public const int STAGE_NONE = 0;
        public const int STAGE_UNDER_RADAR = 1;
        public const int STAGE_CLEAN_UP_CREW = 2;
        public const int STAGE_LOOSE_LIPS = 3;
        public const int STAGE_ASK_MARTY = 4;
        public const int STAGE_COLLECT_EVIDENCE = 5;
        public const int STAGE_REMY_PROOF = 6;
        public const int STAGE_CONFRONT_REMY = 7;
        public const int STAGE_STORY_COMPLETE = 8;

        private static readonly MelonPreferences_Category Category =
            MelonPreferences.CreateCategory(
                "WVC_SnitchStory",
                "Westville Connection - Snitch Story"
            );

        private static readonly MelonPreferences_Entry<int>
            StoryStagePref =
                Category.CreateEntry(
                    "StoryStage",
                    STAGE_NONE
                );

        private static readonly MelonPreferences_Entry<int>
            RemyOutcomePref =
                Category.CreateEntry(
                    "RemyOutcome",
                    0
                );

        private static readonly MelonPreferences_Entry<long>
            ConfrontReadyTicksPref =
                Category.CreateEntry(
                    "ConfrontReadyUtcTicks",
                    0L
                );

        private static readonly MelonPreferences_Entry<bool>
            RemyCashGivenPref =
                Category.CreateEntry(
                    "RemyCashGiven",
                    false
                );

        private static readonly MelonPreferences_Entry<bool>
            RemyCartsGivenPref =
                Category.CreateEntry(
                    "RemyCartsGiven",
                    false
                );

        private static readonly MelonPreferences_Entry<float>
            StingChanceOverridePref =
                Category.CreateEntry(
                    "StingChanceOverride",
                    -1f
                );

        public static int Stage
        {
            get => StoryStagePref.Value;
            set
            {
                StoryStagePref.Value = value;
                Category.SaveToFile();
            }
        }

        public static int RemyOutcome
        {
            get => RemyOutcomePref.Value;
            set
            {
                RemyOutcomePref.Value = value;
                Category.SaveToFile();
            }
        }

        public static long ConfrontReadyUtcTicks
        {
            get => ConfrontReadyTicksPref.Value;
            set
            {
                ConfrontReadyTicksPref.Value = value;
                Category.SaveToFile();
            }
        }

        public static bool RemyCashGiven
        {
            get => RemyCashGivenPref.Value;
            set
            {
                RemyCashGivenPref.Value = value;
                Category.SaveToFile();
            }
        }

        public static bool RemyCartsGiven
        {
            get => RemyCartsGivenPref.Value;
            set
            {
                RemyCartsGivenPref.Value = value;
                Category.SaveToFile();
            }
        }

        public static float StingChanceOverride
        {
            get => StingChanceOverridePref.Value;
            set
            {
                StingChanceOverridePref.Value = value;
                Category.SaveToFile();
            }
        }

        public static float GetEffectiveStingChance(
            float normalChance)
        {
            return Stage == STAGE_STORY_COMPLETE &&
                   RemyOutcome == 3 &&
                   StingChanceOverride >= 0f
                ? StingChanceOverride
                : normalChance;
        }

        public static void Reset()
        {
            Stage = STAGE_NONE;
            RemyOutcome = 0;
            ConfrontReadyUtcTicks = 0L;
            RemyCashGiven = false;
            RemyCartsGiven = false;
            StingChanceOverride = -1f;
        }
    }
}