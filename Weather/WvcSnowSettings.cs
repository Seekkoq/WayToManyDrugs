using System;
using MelonLoader;

namespace CustomNPCExample.Weather
{
    /// <summary>
    /// The snow system's knobs, in one place.
    ///
    /// Three readers want the same numbers - the config file MelonLoader writes, the
    /// <c>setweather</c> console command and the rows the mod adds to the game's settings screen -
    /// so they all come through here rather than each keeping their own copy. Every setter saves
    /// and raises <see cref="Changed"/>, which is what lets a row clicked in the settings screen
    /// take effect on the frame it is clicked instead of on the next restart.
    /// </summary>
    public static class WvcSnowSettings
    {
        public const string CategoryId = "WVC_SnowWeather";

        public const int DefaultSnowFlakes = 850;
        public const int DefaultStormFlakes = 2600;
        public const int DefaultBlizzardsPerWeek = 2;

        /// <summary>Whether NPCs remark on the weather when the player walks up to them.</summary>
        public const bool DefaultWeatherDialogue = true;

        /// <summary>The range a config file or a console argument is allowed to land in.</summary>
        public const int MaxSnowFlakes = 6000;
        public const int MinSnowFlakes = 80;

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _scheduleEnabled;
        private static MelonPreferences_Entry<int> _blizzardsPerWeek;
        private static MelonPreferences_Entry<int> _snowFlakes;
        private static MelonPreferences_Entry<int> _stormFlakes;
        private static MelonPreferences_Entry<bool> _weatherDialogue;

        private static bool _initialized;

        /// <summary>Raised after any value changes, so the live effect can be brought in line at once.</summary>
        public static event Action Changed;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                _category = MelonPreferences.CreateCategory(
                    CategoryId,
                    "Westville Connection Snow"
                );

                _scheduleEnabled = _category.CreateEntry(
                    "ScheduleEnabled",
                    true,
                    "Snow schedule",
                    "When on, the mod puts snow on screen by itself on a few days each week, and a " +
                    "blizzard on the days the schedule picks as storm days. When off, no weather is " +
                    "started automatically and only the setweather command changes anything."
                );

                _blizzardsPerWeek = _category.CreateEntry(
                    "BlizzardsPerWeek",
                    DefaultBlizzardsPerWeek,
                    "Blizzards each week",
                    "How many of the week's snow days are blizzards instead of ordinary snow."
                );

                _snowFlakes = _category.CreateEntry(
                    "SnowFlakeCount",
                    DefaultSnowFlakes,
                    "Snowflakes (calm)",
                    "How many flakes ordinary snow draws. Lower is cheaper: this is the main cost " +
                    "the effect has on the frame rate."
                );

                _stormFlakes = _category.CreateEntry(
                    "StormFlakeCount",
                    DefaultStormFlakes,
                    "Snowflakes (blizzard)",
                    "How many flakes a blizzard draws. A blizzard starts at this number and the " +
                    "adaptive budget walks it down if the frames cannot carry it."
                );

                _weatherDialogue = _category.CreateEntry(
                    "WeatherDialogue",
                    DefaultWeatherDialogue,
                    "Weather talk",
                    "When on, people the player walks up to say something about the weather they " +
                    "are standing in - the rain greetings the game already has, and snow and " +
                    "blizzard lines on top of them. When off, nobody says anything about it."
                );


            }
            catch (Exception)
            {

            }
        }

        /// <summary>The snow settings category, so another part of the mod can add an entry to it.</summary>
        public static MelonPreferences_Category Category
        {
            get { return _category; }
        }

        public static bool ScheduleEnabled
        {
            get
            {
                if (_scheduleEnabled == null)
                    return true;

                return _scheduleEnabled.Value;
            }

            set
            {
                if (_scheduleEnabled == null || _scheduleEnabled.Value == value)
                    return;

                _scheduleEnabled.Value = value;



                SaveAndAnnounce();
            }
        }

        /// <summary>
        /// Whether the people the player walks up to say anything about the weather. Read fresh on
        /// every greeting, so turning it off stops the talk at once.
        /// </summary>
        public static bool WeatherDialogue
        {
            get
            {
                if (_weatherDialogue == null)
                    return DefaultWeatherDialogue;

                return _weatherDialogue.Value;
            }

            set
            {
                if (_weatherDialogue == null || _weatherDialogue.Value == value)
                    return;

                _weatherDialogue.Value = value;



                SaveAndAnnounce();
            }
        }

        public static int BlizzardsPerWeek
        {
            get
            {
                if (_blizzardsPerWeek == null)
                    return DefaultBlizzardsPerWeek;

                return Clamp(_blizzardsPerWeek.Value, 0, 7);
            }

            set
            {
                int wanted = Clamp(value, 0, 7);

                if (_blizzardsPerWeek == null || _blizzardsPerWeek.Value == wanted)
                    return;

                _blizzardsPerWeek.Value = wanted;



                SaveAndAnnounce();
            }
        }

        public static int SnowFlakes
        {
            get
            {
                if (_snowFlakes == null)
                    return DefaultSnowFlakes;

                return Clamp(_snowFlakes.Value, MinSnowFlakes, MaxSnowFlakes);
            }

            set
            {
                int wanted = Clamp(value, MinSnowFlakes, MaxSnowFlakes);

                if (_snowFlakes == null || _snowFlakes.Value == wanted)
                    return;

                _snowFlakes.Value = wanted;



                SaveAndAnnounce();
            }
        }

        public static int StormFlakes
        {
            get
            {
                if (_stormFlakes == null)
                    return DefaultStormFlakes;

                return Clamp(_stormFlakes.Value, MinSnowFlakes, MaxSnowFlakes);
            }

            set
            {
                int wanted = Clamp(value, MinSnowFlakes, MaxSnowFlakes);

                if (_stormFlakes == null || _stormFlakes.Value == wanted)
                    return;

                _stormFlakes.Value = wanted;



                SaveAndAnnounce();
            }
        }

        public static string Describe()
        {
            return "schedule=" + (ScheduleEnabled ? "on" : "off") +
                   " blizzardsPerWeek=" + BlizzardsPerWeek +
                   " calmFlakes=" + SnowFlakes +
                   " stormFlakes=" + StormFlakes +
                   " weatherTalk=" + (WeatherDialogue ? "on" : "off");
        }

        private static void SaveAndAnnounce()
        {
            try
            {
                MelonPreferences.Save();
            }
            catch (Exception)
            {

            }

            try
            {
                Changed?.Invoke();
            }
            catch (Exception)
            {

            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}
