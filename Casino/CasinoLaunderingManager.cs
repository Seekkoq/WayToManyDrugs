using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MelonLoader;
using UnityEngine;

using CustomNPCExample.Utils;

namespace CustomNPCExample.Casino
{
    /// <summary>
    /// The money side of the casino laundry.
    ///
    /// Philip Wentworth works the cage: he takes cash off the player's hands, holds it for a few
    /// in-game days, and pays a cleaned amount into the bank (online) balance when the player comes
    /// back to claim it. Nothing here touches heat or law - the whole point of the feature is that
    /// it is quiet.
    ///
    /// Laundering is a trust relationship, not a fixed service. Each save carries its own
    /// completed-run count that climbs every time the player collects; the higher the standing,
    /// the bigger the stack Philip will take, the shorter the hold, and the smaller his cut. At the
    /// top ("Made") he takes no fee at all and pays a small VIP bonus on top. The fee and the hold
    /// are frozen into the waiting-money record when the cash is handed over, so a promotion
    /// mid-hold can never change what the player was promised. Tiers are defined once in
    /// <see cref="CasinoTiers"/>; enabling/disabling the progression and the fee are preferences.
    ///
    /// The give side uses the game's own <c>MoneyManager.ChangeCashBalance</c>; the claim side
    /// credits through the game's own changebalance command via S1API's
    /// <c>ConsoleHelper.RunOnlineBalanceCommand</c>, so the bank shows the green popup exactly as
    /// it would if the command had been typed. If that path throws, the balance is credited
    /// directly - and a failed claim never loses the waiting money, it stays claimable.
    ///
    /// Waiting money and trust are stored per save (keyed off the save id the mod already tracks in
    /// SupplierSampleSaveManager) both in PlayerPrefs and in a MelonPreferences blob, the same
    /// dual-write the supplier sample claims use. Old 3-field records (amount:day) load as fee-free
    /// full payouts so nothing already on the books is lost.
    /// </summary>
    public static class CasinoLaunderingManager
    {
        /// <summary>
        /// Baseline run size and hold (the Tier-0 / "New Face" figures). Kept as constants so old
        /// references and the phone notifier still have a sane floor; the live values come from the
        /// player's current tier via <see cref="CurrentCap"/>, <see cref="CurrentHoldDays"/> and
        /// <see cref="CurrentFeeFraction"/>.
        /// </summary>
        public const float LaunderAmount = 65000f;

        /// <summary>Baseline in-game days the money sits with Philip (Tier-0 hold).</summary>
        public const int ClaimDelayDays = 2;

        /// <summary>
        /// The trust ladder. Caps, fees, VIP bonus and hold length per rung; unlocking is driven by
        /// the number of completed (collected) runs on the current save.
        /// </summary>
        private static readonly WvcLaunderTier[] Tiers =
        {
            new WvcLaunderTier { Index = 0, Name = "New Face",  UnlockRuns = 0,  Cap = 65000f,  FeePercent = 0.08f, VipBonusPercent = 0f,    HoldDays = 2 },
            new WvcLaunderTier { Index = 1, Name = "Regular",   UnlockRuns = 3,  Cap = 125000f, FeePercent = 0.05f, VipBonusPercent = 0f,    HoldDays = 2 },
            new WvcLaunderTier { Index = 2, Name = "Connected", UnlockRuns = 8,  Cap = 200000f, FeePercent = 0.03f, VipBonusPercent = 0f,    HoldDays = 1 },
            new WvcLaunderTier { Index = 3, Name = "Made",      UnlockRuns = 15, Cap = 300000f, FeePercent = 0.0f,  VipBonusPercent = 0.02f, HoldDays = 1 },
        };

        private static readonly MelonPreferences_Category Category =
            MelonPreferences.CreateCategory(
                "WVC_Casino",
                "Westville Connection - Casino"
            );

        private static readonly MelonPreferences_Entry<bool> FeatureEntry =
            Category.CreateEntry(
                "EnableCasinoLaundering",
                true,
                "Enable casino money laundering with Philip Wentworth"
            );

        private static readonly MelonPreferences_Entry<bool> DialogueEntry =
            Category.CreateEntry(
                "EnablePhilipDialogue",
                true,
                "Append the casino laundry to Philip Wentworth's dialogue (off removes it)"
            );

        private static readonly MelonPreferences_Entry<bool> ProgressionEntry =
            Category.CreateEntry(
                "EnableTrustProgression",
                true,
                "Trust tiers: bigger caps, shorter holds and lower fees as you launder more"
            );

        private static readonly MelonPreferences_Entry<bool> FeeEntry =
            Category.CreateEntry(
                "EnableFees",
                true,
                "Charge Philip's cut (off = always 1:1, but trust tiers still raise the cap)"
            );

        private static readonly MelonPreferences_Entry<string> StateEntry =
            Category.CreateEntry(
                "LaunderState_v1",
                "",
                "Legacy last-run day from 3.8.0 (kept so old config files load; unused)"
            );

        private static readonly MelonPreferences_Entry<string> PendingEntry =
            Category.CreateEntry(
                "PendingState_v2",
                "",
                "Per-save waiting money: saveId:amount:day:holdDays:payout (internal)"
            );

        private static readonly MelonPreferences_Entry<string> TrustEntry =
            Category.CreateEntry(
                "TrustState_v2",
                "",
                "Per-save completed launder runs (internal)"
            );

        /// <summary>One save's money waiting to be claimed.</summary>
        private sealed class PendingInfo
        {
            public float Amount;

            public int Day;

            /// <summary>In-game days this run sits (frozen at give from the then-current tier).</summary>
            public int HoldDays = ClaimDelayDays;

            /// <summary>Bank credit paid out at claim (frozen at give, after fee and VIP bonus).</summary>
            public float Payout;
        }

        private static readonly Dictionary<string, PendingInfo> PendingCache =
            new Dictionary<string, PendingInfo>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, int> TrustCache =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static bool _stateLoaded;

        // ---------------------------------------------------------------------
        // Trust tiers
        // ---------------------------------------------------------------------

        /// <summary>Completed (collected) runs on the active save. Drives the tier.</summary>
        public static int CompletedRuns
        {
            get
            {
                EnsureStateLoaded();
                TrustCache.TryGetValue(ActiveSaveId, out int runs);
                return runs < 0 ? 0 : runs;
            }
        }

        /// <summary>Whether trust progression is on. When off, everyone sits at Tier 0.</summary>
        public static bool ProgressionEnabled =>
            ProgressionEntry == null || ProgressionEntry.Value;

        /// <summary>Whether Philip takes a cut. When off, payouts are 1:1 regardless of tier.</summary>
        public static bool FeesEnabled =>
            FeeEntry == null || FeeEntry.Value;

        /// <summary>The player's current rung for the active save (falls back to Tier 0).</summary>
        public static WvcLaunderTier CurrentTier
        {
            get
            {
                var tier = WvcLaunderTiers.Pick(Tiers, ProgressionEnabled ? CompletedRuns : 0);
                return tier ?? Tiers[0];
            }
        }

        /// <summary>Maximum single run the player can move right now.</summary>
        public static float CurrentCap => CurrentTier.Cap;

        /// <summary>Hold length (in-game days) for a run handed over right now.</summary>
        public static int CurrentHoldDays => CurrentTier.HoldDays;

        /// <summary>Fraction kept by Philip, 0..1 (0 when fees are off). 0.08 = 8%.</summary>
        public static float CurrentFeeFraction
        {
            get
            {
                if (!FeesEnabled)
                    return 0f;
                return CurrentTier.FeePercent;
            }
        }

        /// <summary>VIP bonus fraction paid on top at claim, 0..1 (0 when fees are off).</summary>
        public static float CurrentVipBonusFraction
        {
            get
            {
                if (!FeesEnabled)
                    return 0f;
                return CurrentTier.VipBonusPercent;
            }
        }

        /// <summary>Display name of the current rung ("New Face" ... "Made").</summary>
        public static string CurrentTierName => CurrentTier.Name;

        /// <summary>
        /// Human line describing progress toward the next rung (or a max-tier line). Rendered into
        /// the dialogue so the player can see how close the next unlock is.
        /// </summary>
        public static string StandingLine
        {
            get
            {
                if (!ProgressionEnabled)
                    return "Standing: not tracked (trust progression is off).";

                int runs = CompletedRuns;
                int next = WvcLaunderTiers.NextIndex(Tiers, runs);
                if (next < 0)
                {
                    return "Standing: " + CurrentTier.Name +
                           " - top of the book. Nothing left to unlock.";
                }

                int need = Tiers[next].UnlockRuns - runs;
                return "Standing: " + CurrentTier.Name + " (" + runs + " run(s)) - " +
                       need + " more to " + Tiers[next].Name + ".";
            }
        }

        /// <summary>
        /// Credits the claim, then bumps trust. Called only after the money actually lands, so a
        /// failed claim never advances the tier.
        /// </summary>
        private static void RegisterCompletedRun()
        {
            try
            {
                string saveId = ActiveSaveId;
                EnsureStateLoaded();
                TrustCache.TryGetValue(saveId, out int runs);
                if (runs < 0)
                    runs = 0;
                TrustCache[saveId] = runs + 1;
                SaveState();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Forces the preference entries into existence during mod start-up so the category exists
        /// before anything reads it. Safe to call more than once.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Config: feature=" + (FeatureEntry.Value ? "on" : "off") +
                    " dialogue=" + (DialogueEntry.Value ? "on" : "off"));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>Master switch for the feature (MelonPreferences, default on).</summary>
        public static bool FeatureEnabled => FeatureEntry == null || FeatureEntry.Value;

        /// <summary>The dialogue kill switch: off removes the laundry from Philip's dialogue.</summary>
        public static bool DialogueOverrideEnabled => DialogueEntry != null && DialogueEntry.Value;

        /// <summary>Persists the dialogue kill switch.</summary>
        public static void SetDialogueOverrideEnabled(bool enabled)
        {
            try
            {
                DialogueEntry.Value = enabled;
                Category.SaveToFile();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] Could not save the dialogue setting: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Time
        // ---------------------------------------------------------------------

        /// <summary>
        /// The current in-game day, or -1 when the time manager is not available (main menu).
        /// </summary>
        public static int CurrentDay
        {
            get
            {
                try
                {
                    var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
                    return timeManager != null ? timeManager.ElapsedDays : -1;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }

        // ---------------------------------------------------------------------
        // Waiting money (per save)
        // ---------------------------------------------------------------------

        private static PendingInfo GetPending()
        {
            string saveId = ActiveSaveId;
            EnsureStateLoaded();

            if (PendingCache.TryGetValue(saveId, out PendingInfo pending))
                return pending;

            // PlayerPrefs fallback (same dual-write as the supplier sample claims).
            try
            {
                string raw = PlayerPrefs.GetString(PendingKey(saveId), string.Empty);
                if (!string.IsNullOrEmpty(raw))
                {
                    // v2: amount:day:holdDays:payout   (4 fields)
                    // v1: amount:day                   (2 fields)
                    string[] parts = raw.Split(new[] { ':' }, 4);
                    if (parts.Length >= 2 &&
                        float.TryParse(parts[0], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float amount) &&
                        int.TryParse(parts[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int day))
                    {
                        int hold = ClaimDelayDays;
                        float payout = amount; // legacy: no fee charged, full hand-back

                        if (parts.Length >= 4)
                        {
                            if (int.TryParse(parts[2], NumberStyles.Integer,
                                    CultureInfo.InvariantCulture, out int h) && h > 0)
                            {
                                hold = h;
                            }

                            if (float.TryParse(parts[3], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float p) && p > 0f)
                            {
                                payout = p;
                            }
                        }

                        pending = new PendingInfo
                        {
                            Amount = amount,
                            Day = day,
                            HoldDays = hold,
                            Payout = payout
                        };
                        PendingCache[saveId] = pending;
                        return pending;
                    }
                }
            }
            catch (Exception)
            {
            }

            // Fallback to the default-save key. ActiveSaveId is only set by the SaveManager.Save
            // Harmony prefix, so it stays "default_save" until the first save of a session - which
            // in a normal play session happens on sleep. Money handed over before that first save
            // is stored under "default_save"; look there too (mirroring
            // SupplierSampleSaveManager.IsSampleClaimed) so it is still visible after the id
            // flips - otherwise a first give + sleep reads as "nothing on the books".
            if (!string.Equals(saveId, DefaultSaveKey, StringComparison.Ordinal))
            {
                if (PendingCache.TryGetValue(DefaultSaveKey, out pending))
                    return pending;

                try
                {
                    string rawDefault = PlayerPrefs.GetString(PendingKey(DefaultSaveKey), string.Empty);
                    if (!string.IsNullOrEmpty(rawDefault))
                    {
                        string[] partsDefault = rawDefault.Split(new[] { ':' }, 2);
                        if (partsDefault.Length == 2 &&
                            float.TryParse(partsDefault[0], NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float amountDefault) &&
                            int.TryParse(partsDefault[1], NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out int dayDefault))
                        {
                            pending = new PendingInfo { Amount = amountDefault, Day = dayDefault };
                            PendingCache[DefaultSaveKey] = pending;
                            return pending;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        /// <summary>True while money for this save is waiting to be claimed.</summary>
        public static bool HasPending => GetPending() != null;

        /// <summary>The waiting amount, 0 when nothing is waiting.</summary>
        public static float PendingAmount => GetPending()?.Amount ?? 0f;

        /// <summary>The in-game day the money was handed over, or -1.</summary>
        public static int PendingDay => GetPending()?.Day ?? -1;

        /// <summary>True once the waiting money is old enough to be collected.</summary>
        public static bool IsClaimReady
        {
            get
            {
                PendingInfo pending = GetPending();
                if (pending == null)
                    return false;

                int day = CurrentDay;
                if (day < 0)
                    return false;

                int hold = pending.HoldDays > 0 ? pending.HoldDays : ClaimDelayDays;
                return (day - pending.Day) >= hold;
            }
        }

        /// <summary>Whole days left before the waiting money can be collected, 0 when ready. Uses
        /// the hold frozen into the record at give time.</summary>
        public static int ClaimDaysRemaining
        {
            get
            {
                PendingInfo pending = GetPending();
                if (pending == null)
                    return 0;

                int hold = pending.HoldDays > 0 ? pending.HoldDays : ClaimDelayDays;

                int day = CurrentDay;
                if (day < 0)
                    return hold;

                int remaining = hold - (day - pending.Day);
                return remaining < 0 ? 0 : remaining;
            }
        }

        /// <summary>Bank credit the pending run will pay out at claim (frozen at give).</summary>
        public static float PendingPayout
        {
            get
            {
                PendingInfo pending = GetPending();
                return pending == null ? 0f : pending.Payout;
            }
        }

        /// <summary>The hold frozen into the pending record at give time (falls back to the
        /// Tier-0 baseline for legacy 3-field records).</summary>
        public static int PendingHoldDays
        {
            get
            {
                PendingInfo pending = GetPending();
                if (pending == null)
                    return ClaimDelayDays;
                return pending.HoldDays > 0 ? pending.HoldDays : ClaimDelayDays;
            }
        }

        private static void SetPending(float amount, int day, int holdDays, float payout)
        {
            string saveId = ActiveSaveId;
            EnsureStateLoaded();

            if (holdDays <= 0)
                holdDays = ClaimDelayDays;

            var pending = new PendingInfo
            {
                Amount = amount,
                Day = day,
                HoldDays = holdDays,
                Payout = payout
            };
            PendingCache[saveId] = pending;

            try
            {
                PlayerPrefs.SetString(
                    PendingKey(saveId),
                    amount.ToString("0", CultureInfo.InvariantCulture) + ":" +
                    day.ToString(CultureInfo.InvariantCulture) + ":" +
                    holdDays.ToString(CultureInfo.InvariantCulture) + ":" +
                    payout.ToString("0", CultureInfo.InvariantCulture));
                PlayerPrefs.DeleteKey(ReadyKey(saveId));
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
            }

            SaveState();
        }

        private static void ClearPendingCacheEntry(string saveId)
        {
            EnsureStateLoaded();
            PendingCache.Remove(saveId);

            try
            {
                PlayerPrefs.DeleteKey(PendingKey(saveId));
                PlayerPrefs.DeleteKey(ReadyKey(saveId));

                // Also clear the default-save key: a pending handed over before the first save of a
                // session lives there (ActiveSaveId is still "default_save") and is returned by the
                // GetPending fallback, so it must be cleared on claim too or it could be collected
                // twice. Deleting a missing key / removing a missing entry is a no-op.
                if (!string.Equals(saveId, DefaultSaveKey, StringComparison.Ordinal))
                {
                    PendingCache.Remove(DefaultSaveKey);
                    PlayerPrefs.DeleteKey(PendingKey(DefaultSaveKey));
                    PlayerPrefs.DeleteKey(ReadyKey(DefaultSaveKey));
                }

                PlayerPrefs.Save();
            }
            catch (Exception)
            {
            }

            SaveState();
        }

        /// <summary>True once the ready text for the waiting money has been sent.</summary>
        public static bool ClaimReadyNotified
        {
            get
            {
                try
                {
                    return PlayerPrefs.GetInt(ReadyKey(ActiveSaveId), 0) == 1;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>Records that the player has been texted about the waiting money.</summary>
        public static void MarkClaimReadyNotified()
        {
            try
            {
                PlayerPrefs.SetInt(ReadyKey(ActiveSaveId), 1);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
            }
        }

        private static string ReadyKey(string saveId)
        {
            return "WVC_CasinoReady_" + SanitizeKey(saveId);
        }

        // ---------------------------------------------------------------------
        // Give and claim
        // ---------------------------------------------------------------------

        /// <summary>
        /// Checks whether handing over <paramref name="amount"/> would go through right now and,
        /// when it would not, says why. <paramref name="force"/> skips only the waiting-money
        /// check (debug use) - the cash always has to exist, so debug use cannot conjure money
        /// out of nothing.
        /// </summary>
        public static bool CanGive(float amount, bool force, out string reason)
        {
            reason = null;

            if (!FeatureEnabled)
            {
                reason = "casino laundering is turned off (EnableCasinoLaundering)";
                return false;
            }

            if (FindMoney() == null)
            {
                reason = "the money manager is not available right now";
                return false;
            }

            if (CurrentDay < 0)
            {
                reason = "the day counter is not available right now";
                return false;
            }

            if (!force && HasPending)
            {
                reason =
                    "there is already $" + PendingAmount.ToString("N0") +
                    " waiting on the books - collect it first";
                return false;
            }

            float missing = CashShortfall(amount);
            if (missing > 0f)
            {
                reason = "you are $" + missing.ToString("N0") + " short of $" + amount.ToString("N0");
                return false;
            }

            return true;
        }

        /// <summary>Checks whether the waiting money can be collected right now.</summary>
        public static bool CanClaim(bool force, out string reason)
        {
            reason = null;

            if (!FeatureEnabled)
            {
                reason = "casino laundering is turned off (EnableCasinoLaundering)";
                return false;
            }

            if (FindMoney() == null)
            {
                reason = "the money manager is not available right now";
                return false;
            }

            PendingInfo pending = GetPending();
            if (pending == null)
            {
                reason = "nothing of yours is waiting on the books";
                return false;
            }

            if (!force && !IsClaimReady)
            {
                reason =
                    "the money needs " + ClaimDelayDays + " full in-game days on the books - " +
                    ClaimDaysRemaining + " day(s) left";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Hands <paramref name="amount"/> cash over to Philip and records it as waiting for this
        /// save. On any failure the reason comes back in <paramref name="reason"/> and no money
        /// is taken.
        /// </summary>
        public static bool TryGive(float amount, bool force, out string reason)
        {
            try
            {
                amount = MathF.Round(amount);

                if (amount <= 0f)
                {
                    reason = "the amount has to be positive";
                    return false;
                }

                if (!CanGive(amount, force, out reason))
                    return false;

                var money = FindMoney();
                if (money == null)
                {
                    reason = "the money manager is not available right now";
                    return false;
                }

                // Freeze this run's hold and net payout from the player's standing right now, so a
                // promotion (or a settings change) mid-hold can never alter what was promised.
                WvcLaunderTier tier = CurrentTier;
                int holdDays = tier.HoldDays > 0 ? tier.HoldDays : ClaimDelayDays;
                float fee = FeesEnabled ? tier.FeePercent : 0f;
                float vip = FeesEnabled ? tier.VipBonusPercent : 0f;
                float payout = amount * (1f - fee) * (1f + vip);

                // Cash out first; if the pending record cannot be written the cash goes straight
                // back, so a failed give can never eat the player's money.
                money.ChangeCashBalance(-amount, true, false);

                try
                {
                    SetPending(amount, CurrentDay, holdDays, payout);
                }
                catch (Exception ex)
                {
                    try
                    {
                        money.ChangeCashBalance(amount, true, false);
                    }
                    catch (Exception)
                    {
                    }

                    reason =
                        "the books could not record the handover and the cash was put back (" +
                        ex.Message + ")";
                    return false;
                }

                money.PlayCashSound();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Handed over $" + amount.ToString("N0") +
                    " - claimable in " + holdDays + " in-game days (day " + PendingDay +
                    "), pays $" + payout.ToString("N0") + ".");

                CasinoLaunderingDialogue.RebuildIfArmed();

                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] TryGive exception: " + ex);
                reason = "something went wrong - see the MelonLoader log";
                return false;
            }
        }

        /// <summary>
        /// Pays the waiting money into the bank. On failure the money stays waiting, so nothing
        /// is ever lost.
        /// </summary>
        public static bool TryClaim(bool force, out string reason)
        {
            try
            {
                if (!CanClaim(force, out reason))
                    return false;

                PendingInfo pending = GetPending();
                var money = FindMoney();
                if (pending == null || money == null)
                {
                    reason = "the money manager is not available right now";
                    return false;
                }

                int amount = (int)MathF.Round(pending.Amount);

                // Pay the frozen payout (what was promised at hand-over, after Philip's cut and any
                // VIP bonus), not the raw hand-back, so the tier at claim time can short-change nobody.
                int pay = pending.Payout > 0f
                    ? (int)MathF.Round(pending.Payout)
                    : amount;

                if (!TryCreditBank(money, pay, out string bankError))
                {
                    reason =
                        "the bank credit failed and the money is still waiting (" + bankError + ")";
                    return false;
                }

                money.PlayCashSound();
                ClearPendingCacheEntry(ActiveSaveId);

                // The money landed - this run counts toward the next trust rung.
                RegisterCompletedRun();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Claimed $" + pay.ToString("N0") + " (from $" + amount.ToString("N0") +
                    " handed over) -> bank. Standing now " + CurrentTierName +
                    " (" + CompletedRuns + " run(s)).");

                CasinoLaunderingDialogue.RebuildIfArmed();

                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] TryClaim exception: " + ex);
                reason = "something went wrong - see the MelonLoader log";
                return false;
            }
        }

        /// <summary>Clears the waiting money for the active save.</summary>
        public static void ClearPending()
        {
            try
            {
                ClearPendingCacheEntry(ActiveSaveId);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Waiting money cleared for save '" + ActiveSaveId + "'.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Casino] Could not clear the waiting money: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Money
        // ---------------------------------------------------------------------

        /// <summary>Cash in hand (HUD balance), 0 when no money manager exists.</summary>
        public static float GetCashBalance()
        {
            var money = FindMoney();
            try
            {
                return money != null ? money.cashBalance : 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        /// <summary>Bank (online) balance, 0 when no money manager exists.</summary>
        public static float GetBankBalance()
        {
            var money = FindMoney();
            try
            {
                return money != null ? money.onlineBalance : 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        /// <summary>How much short of <paramref name="amount"/> the player's cash is, 0 when covered.</summary>
        public static float CashShortfall(float amount)
        {
            float missing = amount - GetCashBalance();
            return missing > 0f ? missing : 0f;
        }

        /// <summary>
        /// Credits the bank through the game's own changebalance command (which adds the amount
        /// and shows the bank popup), falling back to a direct balance write if the command path
        /// throws.
        /// </summary>
        private static bool TryCreditBank(
            Il2CppScheduleOne.Money.MoneyManager money,
            int amount,
            out string error)
        {
            error = null;

            try
            {
                S1API.Console.ConsoleHelper.RunOnlineBalanceCommand(amount);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            try
            {
                money.onlineBalance = money.onlineBalance + amount;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                if (error == null)
                    error = ex.Message;

                return false;
            }
        }

        private static Il2CppScheduleOne.Money.MoneyManager FindMoney()
        {
            try
            {
                return UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.Money.MoneyManager>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ---------------------------------------------------------------------
        // Persistence (per save, dual-write like the supplier sample claims)
        // ---------------------------------------------------------------------

        private const string DefaultSaveKey = "default_save";

        private static string ActiveSaveId
        {
            get
            {
                try
                {
                    return CustomNPCExample.NPCs.SupplierSampleSaveManager.ActiveSaveId;
                }
                catch (Exception)
                {
                    return DefaultSaveKey;
                }
            }
        }

        private static void EnsureStateLoaded()
        {
            if (_stateLoaded)
                return;

            _stateLoaded = true;

            try
            {
                string raw = PendingEntry.Value;

                if (string.IsNullOrEmpty(raw))
                    return;

                string[] entries = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < entries.Length; i++)
                {
                    // v2: saveId:amount:day:holdDays:payout  (5 fields)
                    // v1: saveId:amount:day                  (3 fields)
                    string[] parts = entries[i].Split(new[] { ':' }, 5);
                    if (parts.Length < 3)
                        continue;

                    if (!float.TryParse(parts[1], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float amount) ||
                        !int.TryParse(parts[2], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int day))
                    {
                        continue;
                    }

                    int hold = ClaimDelayDays;
                    float payout = amount; // legacy record: no fee was charged, full hand-back

                    if (parts.Length >= 5)
                    {
                        if (int.TryParse(parts[3], NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out int h) && h > 0)
                        {
                            hold = h;
                        }

                        if (float.TryParse(parts[4], NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float p) && p > 0f)
                        {
                            payout = p;
                        }
                    }

                    PendingCache[parts[0].Trim()] = new PendingInfo
                    {
                        Amount = amount,
                        Day = day,
                        HoldDays = hold,
                        Payout = payout
                    };
                }
            }
            catch (Exception)
            {
            }

            // Trust: saveId:runs pairs, semicolon-separated.
            try
            {
                string rawTrust = TrustEntry.Value;
                if (string.IsNullOrEmpty(rawTrust))
                    return;

                string[] trustEntries = rawTrust.Split(
                    new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < trustEntries.Length; i++)
                {
                    string[] parts = trustEntries[i].Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                        continue;

                    if (int.TryParse(parts[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int runs) && runs >= 0)
                    {
                        TrustCache[parts[0].Trim()] = runs;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static void SaveState()
        {
            try
            {
                List<string> list = new List<string>();
                foreach (var pair in PendingCache)
                {
                    list.Add(
                        pair.Key + ":" +
                        pair.Value.Amount.ToString("0", CultureInfo.InvariantCulture) + ":" +
                        pair.Value.Day.ToString(CultureInfo.InvariantCulture) + ":" +
                        (pair.Value.HoldDays > 0 ? pair.Value.HoldDays : ClaimDelayDays)
                            .ToString(CultureInfo.InvariantCulture) + ":" +
                        pair.Value.Payout.ToString("0", CultureInfo.InvariantCulture));
                }

                PendingEntry.Value = string.Join(";", list);
            }
            catch (Exception)
            {
            }

            try
            {
                List<string> trust = new List<string>();
                foreach (var pair in TrustCache)
                {
                    trust.Add(
                        pair.Key + ":" +
                        pair.Value.ToString(CultureInfo.InvariantCulture));
                }

                TrustEntry.Value = string.Join(";", trust);
                Category.SaveToFile();
            }
            catch (Exception)
            {
            }
        }

        private static string PendingKey(string saveId)
        {
            return "WVC_CasinoPending_" + SanitizeKey(saveId);
        }

        private static string SanitizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return DefaultSaveKey;

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                raw = raw.Replace(invalid[i], '_');

            return raw.Replace(" ", "_").Replace(":", "_").Replace("/", "_").Replace("\\", "_");
        }
    }
}
