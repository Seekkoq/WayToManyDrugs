using System;
using System.IO;
using System.Text.Json;
using Il2CppScheduleOne.GameTime;
using MelonLoader;
using S1API.Leveling;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public enum OffshoreHeatTier
    {
        Calm,
        Watchful,
        Hot,
        Boiling
    }

    public static class OffshoreHeat
    {
        public const float MaxHeat = 100f;

        private const float HeatPerRunBase = 8f;
        private const float HeatPerRunPerUnit = 0.08f;
        private const float HeatPerRunCap = 12f;
        private const float HeatOnSeizure = 5f;
        private const float HeatCooledPerDay = 15f;

        private const float WatchfulThreshold = 25f;
        private const float HotThreshold = 50f;
        private const float BoilingThreshold = 75f;
        private const float CooldownFromBoiling = 75f;

        private const float CalmSeizureChance = 0.04f;
        private const float WatchfulSeizureChance = 0.08f;
        private const float HotSeizureChance = 0.14f;
        private const float BoilingSeizureChance = 0.22f;

        private const float HotCooldownMultiplier = 1.5f;
        private const float HotPayoutBonus = 0.10f;
        private const float BoilingPayoutBonus = 0.15f;

        private static float _heat = 0f;
        private static int _lastKnownDay = int.MinValue;
        private static bool _loaded;
        private static OffshoreHeatTier _lastAnnouncedTier = OffshoreHeatTier.Calm;
        private static bool _dayDetectWarned;
        private static bool _introDealUsed;
        private static bool _runNowUsed;
        private static bool _shotcallerMessaged;
        private static bool _rankEventsHooked;
        private static TimeManager _timeManagerCache;
        private static readonly System.Random Rng = new System.Random();

        public static float Heat => _heat;

        public static OffshoreHeatTier Tier
        {
            get
            {
                if (_heat >= BoilingThreshold) return OffshoreHeatTier.Boiling;
                if (_heat >= HotThreshold) return OffshoreHeatTier.Hot;
                if (_heat >= WatchfulThreshold) return OffshoreHeatTier.Watchful;
                return OffshoreHeatTier.Calm;
            }
        }

        public static float CurrentSeizureChance
        {
            get
            {
                switch (Tier)
                {
                    case OffshoreHeatTier.Boiling: return BoilingSeizureChance;
                    case OffshoreHeatTier.Hot: return HotSeizureChance;
                    case OffshoreHeatTier.Watchful: return WatchfulSeizureChance;
                    default: return CalmSeizureChance;
                }
            }
        }

        public static float CooldownMultiplier =>
            (Tier == OffshoreHeatTier.Hot || Tier == OffshoreHeatTier.Boiling)
                ? HotCooldownMultiplier
                : 1f;

        public static float PayoutBonus
        {
            get
            {
                switch (Tier)
                {
                    case OffshoreHeatTier.Boiling: return BoilingPayoutBonus;
                    case OffshoreHeatTier.Hot: return HotPayoutBonus;
                    default: return 0f;
                }
            }
        }

        public static bool CanSail => _heat < CooldownFromBoiling;

        /// <summary>True once the one-time intro deal has been started this save.</summary>
        public static bool IntroDealUsed => _introDealUsed;

        /// <summary>True when the player has reached the ShotCaller rank.</summary>
        public static bool IsShotCaller
        {
            get
            {
                try
                {
                    return LevelManager.Exists &&
                           LevelManager.Rank >= Rank.ShotCaller;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Flags the one-time intro deal as used (persisted with the heat save).</summary>
        public static void MarkIntroDealUsed()
        {
            if (_introDealUsed)
                return;

            _introDealUsed = true;
            Save();
        }

        /// <summary>
        /// True once the player has asked Declan to run one up on the spot and he took it.
        ///
        /// The ask is a one-time favour: it gets a run moving without waiting on his own clock, so
        /// being able to lean on it whenever the lane went quiet would make his regular texts
        /// pointless. Refusals do not spend it - only a run he actually agreed to.
        ///
        /// Stored with the heat save, which is the offshore system's own file: the heat, the intro
        /// deal and this all persist together for the install rather than per character slot, the
        /// way the rest of the offshore state already does.
        /// </summary>
        public static bool RunNowUsed => _runNowUsed;

        /// <summary>Flags the run-on-demand favour as used (persisted with the heat save).</summary>
        public static void MarkRunNowUsed()
        {
            if (_runNowUsed)
                return;

            _runNowUsed = true;
            Save();
        }

        private static void HookRankEvents()
        {
            if (_rankEventsHooked)
                return;

            _rankEventsHooked = true;

            try
            {
                LevelManager.OnRankUp += OnRankUpHandler;
            }
            catch (Exception)
            {

            }
        }

        private static void OnRankUpHandler(S1API.Leveling.FullRank before, S1API.Leveling.FullRank after)
        {
            try
            {
                if (after.Rank < Rank.ShotCaller || _shotcallerMessaged)
                    return;

                _shotcallerMessaged = true;
                Save();

                string[] messages = new[]
                {
                    "Declan: Well, well - Shot Caller now, are we? That changes things. The overseas line is officially open. When a buyer comes knocking, I'll text you. Keep that product coming.",
                    "Declan: Shot Caller! Look at you. I only sail for names that carry weight, and yours does now. The offshore route is yours - I'll text you when there's a manifest ready.",
                    "Declan: Word around the docks is you made Shot Caller. About time. The overseas buyers only deal with players who've earned it - watch your phone."
                };
                OffshoreSalesManager.SendDeclanMessage(messages[Rng.Next(messages.Length)]);
            }
            catch (Exception)
            {

            }
        }

        public static string TierName
        {
            get
            {
                switch (Tier)
                {
                    case OffshoreHeatTier.Boiling: return "Boiling";
                    case OffshoreHeatTier.Hot: return "Hot";
                    case OffshoreHeatTier.Watchful: return "Watchful";
                    default: return "Calm";
                }
            }
        }

        public static void Update()
        {
            if (!_loaded)
                Load();

            HookRankEvents();
            DetectDayChange();
        }

        private static void DetectDayChange()
        {
            try
            {
                if (_timeManagerCache == null)
                    _timeManagerCache = TimeManager.Instance;

                TimeManager timeManager = _timeManagerCache;
                if (timeManager == null)
                    return;

                int currentDay = timeManager.ElapsedDays;

                if (_lastKnownDay == int.MinValue)
                {
                    _lastKnownDay = currentDay;
                    return;
                }

                if (currentDay <= _lastKnownDay)
                    return;

                int daysPassed = currentDay - _lastKnownDay;
                _lastKnownDay = currentDay;

                if (_heat <= 0f)
                    return;

                float cooled = HeatCooledPerDay * daysPassed;
                float previousHeat = _heat;
                _heat = Math.Max(0f, _heat - cooled);


                Save();

                if (Tier != _lastAnnouncedTier)
                {
                    AnnounceTierChange();
                }
                else
                {
                    string[] coolingNudges = new[]
                    {
                        "Declan: Quiet day on the water. The heat on our lane is dropping - keep laying low and it'll keep falling.",
                        "Declan: Patrols eased off a little overnight. Not clear yet, but we're headed the right way.",
                        "Declan: Another calm-ish day. The inspectors are getting bored. Keep your nose clean a while longer."
                    };
                    OffshoreSalesManager.SendDeclanMessage(coolingNudges[Rng.Next(coolingNudges.Length)]);
                }
            }
            catch (Exception)
            {
                if (!_dayDetectWarned)
                {
                    _dayDetectWarned = true;

                }
            }
        }

        public static void RegisterSuccessfulRun(int orderedUnits)
        {
            float gain = Math.Min(
                HeatPerRunCap,
                HeatPerRunBase + orderedUnits * HeatPerRunPerUnit);

            AddHeat(gain, $"successful run ({orderedUnits} units)");
        }

        public static void RegisterSeizure()
        {
            AddHeat(HeatOnSeizure, "shipment seizure");
        }

        private static void AddHeat(float amount, string reason)
        {
            float previousHeat = _heat;
            _heat = Math.Min(MaxHeat, _heat + amount);

            if (_heat <= previousHeat)
                return;

            Save();

            AnnounceTierChange();
        }

        private static void AnnounceTierChange()
        {
            OffshoreHeatTier newTier = Tier;
            if (newTier == _lastAnnouncedTier)
                return;

            string message = PickTierChangeMessage(_lastAnnouncedTier, newTier);
            _lastAnnouncedTier = newTier;

            if (!string.IsNullOrEmpty(message))
                OffshoreSalesManager.SendDeclanMessage(message);
        }

        private static string PickTierChangeMessage(OffshoreHeatTier oldTier, OffshoreHeatTier newTier)
        {
            string[] pool;

            if (newTier > oldTier)
            {
                switch (newTier)
                {
                    case OffshoreHeatTier.Watchful:
                        pool = new[]
                        {
                            "Declan: Word to the wise - our last few runs have gotten people talking. Harbor patrol is paying extra attention to the lane now. Watch yourself.",
                            "Declan: The buyers won't stop raving about our product. Good for business, bad for staying invisible. Customs is starting to ask questions at the docks.",
                            "Declan: We've been busy, and busy leaves footprints. A customs officer was sniffing around my slip this morning. Keep your head down for a while.",
                            "Declan: Word's out that the Night Wave has been running hot cargo. There are eyes on the water now. I'll pace the next orders accordingly."
                        };
                        break;

                    case OffshoreHeatTier.Hot:
                        pool = new[]
                        {
                            "Declan: Bad news, friend. That syndicate war in the east has every port on high alert. Inspectors are X-raying half the containers out there.",
                            "Declan: A patrol boat shadowed me halfway to the shipping lane yesterday. We're officially on their radar. I'm spacing the runs out - and the syndicate knows it, so they've authorized a danger premium on payouts.",
                            "Declan: The harbor master got a tip-off. Someone's been talking. I've got friends covering for me, but every run from here is a gamble with worse odds.",
                            "Declan: This is about as hot as I like the water. Seizure risk is real now - if a run goes bad, the cargo's gone. No insurance on this route."
                        };
                        break;

                    case OffshoreHeatTier.Boiling:
                        pool = new[]
                        {
                            "Declan: That's it - I'm pulling the Night Wave from service. Naval patrols, drone overflights, undercover customs agents in every terminal. We don't sail until this blows over.",
                            "Declan: The coast guard just seized a freighter two piers down. Two piers! I'm not risking the hold until the heat drops. Sit tight and stay quiet.",
                            "Declan: We pushed it too far. There's a task force on the water with our vessel description on a wanted sheet. No more runs - not for you, not for anyone - until I say the word.",
                            "Declan: Dock's swarming. Inspectors, dogs, the lot. I've never seen it this bad. We wait. A smart captain knows when to stay in port."
                        };
                        break;

                    default:
                        return null;
                }
            }
            else
            {
                switch (newTier)
                {
                    case OffshoreHeatTier.Hot:
                        pool = new[]
                        {
                            "Declan: Patrols are thinning out. Not out of the woods yet, but the danger pay stays on the table for as long as we're sailing warm waters.",
                            "Declan: Things are calming down out there. Give it a bit longer and we'll be back in business - premium rates, since we'll still be sailing angry waters."
                        };
                        break;

                    case OffshoreHeatTier.Watchful:
                        pool = new[]
                        {
                            "Declan: Heat's dropping. The inspectors moved on to some poor smuggler up the coast. We're back to regular runs - still keep your wits about you.",
                            "Declan: Quiet again, mostly. The lane's breathable. I'll line up the next order before long."
                        };
                        break;

                    case OffshoreHeatTier.Calm:
                        pool = new[]
                        {
                            "Declan: Clean waters again. Far as anyone remembers, the Night Wave is just a fishing trawler. Business as usual, friend.",
                            "Declan: Coast guard's back to waving us through. All clear - let's get back to making money."
                        };
                        break;

                    default:
                        return null;
                }
            }

            return pool[Rng.Next(pool.Length)];
        }

        public static string GetStatusLine()
        {
            string[] pool;

            switch (Tier)
            {
                case OffshoreHeatTier.Calm:
                    pool = new[]
                    {
                        "Lane heat is calm - the coast guard hasn't looked our way in days.",
                        "Waters are quiet. Nobody's watching the Night Wave right now."
                    };
                    break;

                case OffshoreHeatTier.Watchful:
                    pool = new[]
                    {
                        "Lane heat is watchful - harbor patrol is a bit jumpy lately, but nothing we can't handle.",
                        "There's some heat on the lane. Eyes are on us, but loosely."
                    };
                    break;

                case OffshoreHeatTier.Hot:
                    pool = new[]
                    {
                        "Lane heat is hot - the coast guard is actively sniffing around. Every run from here is a gamble, but the syndicate pays a danger premium for it.",
                        "It's hot out there. Inspectors at every terminal. Runs still sail, but with real risk - and premium pay to match."
                    };
                    break;

                case OffshoreHeatTier.Boiling:
                    pool = new[]
                    {
                        "Lane heat is boiling - ports are crawling with inspectors and I'm not sailing until it cools off.",
                        "It's boiling out there. No ship leaves this slip until the task force stands down."
                    };
                    break;

                default:
                    return "";
            }

            return pool[Rng.Next(pool.Length)];
        }

        public static string GetRefusalMessage()
        {
            string[] pool = new[]
            {
                "Declan: Are you mad? The ports are crawling. No ship leaves this slip until the heat cools down. Sit tight - I'll text you.",
                "Declan: Not a chance. There's a task force out there with our vessel description on a wanted sheet. We wait for cooler waters.",
                "Declan: I'd rather keep my boat and my freedom, thanks. When the inspectors clear out, you'll be the first to know. Until then - no sailing.",
                "Declan: Look outside, friend. Drones over the harbor. Nobody's going anywhere. I'll text you when it's safe to run again."
            };

            return pool[Rng.Next(pool.Length)];
        }

        public static string GetOrderFlavorLine()
        {
            if (Tier != OffshoreHeatTier.Hot)
                return null;

            string[] pool = new[]
            {
                "Fair warning - waters are hot right now, so the syndicate added a danger premium to your cut. If we make it, we eat well.",
                "Heads up: the lane is hot as blazes. Payout carries a danger premium to match the risk. Your call if you'd rather wait for it to cool.",
                "One more thing - coast guard is restless, so this run sails with a danger bonus. Hot waters, hot pay."
            };

            return pool[Rng.Next(pool.Length)];
        }

        private sealed class HeatSaveData
        {
            public float Heat { get; set; }
            public int LastDay { get; set; }
            public bool IntroDealUsed { get; set; }
            public bool ShotcallerMessaged { get; set; }
            public bool RunNowUsed { get; set; }
        }

        private static string SaveDirectory =>
            Path.Combine(
                MelonLoader.Utils.MelonEnvironment.UserDataDirectory,
                "WayToManyDrugs",
                "Offshore");

        private static string SavePath =>
            Path.Combine(SaveDirectory, "OffshoreHeat.json");

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(SaveDirectory);

                var data = new HeatSaveData
                {
                    Heat = _heat,
                    LastDay = _lastKnownDay == int.MinValue ? 0 : _lastKnownDay,
                    IntroDealUsed = _introDealUsed,
                    ShotcallerMessaged = _shotcallerMessaged,
                    RunNowUsed = _runNowUsed
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
            }
            catch (Exception)
            {

            }
        }

        private static void Load()
        {
            _loaded = true;

            try
            {
                if (!File.Exists(SavePath))
                {
                    return;
                }

                string json = File.ReadAllText(SavePath);
                var data = JsonSerializer.Deserialize<HeatSaveData>(json);
                if (data == null)
                    return;

                _heat = Math.Clamp(data.Heat, 0f, MaxHeat);
                _lastKnownDay = data.LastDay > 0 ? data.LastDay : int.MinValue;
                _introDealUsed = data.IntroDealUsed;
                _shotcallerMessaged = data.ShotcallerMessaged;
                _runNowUsed = data.RunNowUsed;

                // Loading a save from before this system: if the player already
                // outranks the gate, stay quiet and let auto orders flow.
                if (!_shotcallerMessaged && IsShotCaller)
                    _shotcallerMessaged = true;

                _lastAnnouncedTier = Tier;

            }
            catch (Exception)
            {

            }
        }

    }
}
