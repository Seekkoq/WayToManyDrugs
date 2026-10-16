using System;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// One rung of a launderer's trust ladder. Values are per contact (the casino moves bigger
    /// numbers than the pawnshop) and every figure is a plain old number so the whole ladder can
    /// be retuned without touching the flow code.
    ///
    /// <see cref="FeePercent"/> is the cut the launderer keeps (0.08 = 8%). <see cref="VipBonusPercent"/>
    /// is an extra paid on top of the hand-back at the top tier (0.02 = 2% more lands in the bank).
    /// </summary>
    public sealed class WvcLaunderTier
    {
        /// <summary>0-based rung. Higher is better standing.</summary>
        public int Index;

        /// <summary>Display name shown in dialogue ("New Face", "Made", ...).</summary>
        public string Name;

        /// <summary>Completed runs required to reach this rung.</summary>
        public int UnlockRuns;

        /// <summary>Maximum single run at this tier.</summary>
        public float Cap;

        /// <summary>Cut kept by the launderer, 0..1 (frozen into the pending record at give).</summary>
        public float FeePercent;

        /// <summary>Bonus paid on top of the hand-back at claim, 0..1 (VIP only).</summary>
        public float VipBonusPercent;

        /// <summary>In-game days the money sits before it can be collected.</summary>
        public int HoldDays;

        public bool IsVip => VipBonusPercent > 0f;
    }

    /// <summary>Pure helpers for reading a trust ladder. Kept side-effect free so both the casino
    /// and the pawnshop share exactly the same tier maths.</summary>
    public static class WvcLaunderTiers
    {
        /// <summary>The highest rung the given completed-run count has unlocked (never null for a
        /// non-empty ladder; falls back to rung 0).</summary>
        public static WvcLaunderTier Pick(WvcLaunderTier[] tiers, int completedRuns)
        {
            if (tiers == null || tiers.Length == 0)
                return null;

            WvcLaunderTier best = tiers[0];
            for (int i = 0; i < tiers.Length; i++)
            {
                if (completedRuns >= tiers[i].UnlockRuns)
                    best = tiers[i];
            }
            return best;
        }

        /// <summary>
        /// Index of the next rung not yet unlocked, or -1 when already at the top. Used to render
        /// "N more runs to Connected".
        /// </summary>
        public static int NextIndex(WvcLaunderTier[] tiers, int completedRuns)
        {
            if (tiers == null)
                return -1;

            for (int i = 0; i < tiers.Length; i++)
            {
                if (completedRuns < tiers[i].UnlockRuns)
                    return i;
            }
            return -1;
        }

        /// <summary>0..1 progress toward the next rung (1 when already maxed).</summary>
        public static float Progress(WvcLaunderTier[] tiers, int completedRuns)
        {
            if (tiers == null || tiers.Length == 0)
                return 1f;

            int next = NextIndex(tiers, completedRuns);
            if (next < 0)
                return 1f;

            WvcLaunderTier current = Pick(tiers, completedRuns);
            int floor = current != null ? current.UnlockRuns : 0;
            int need = tiers[next].UnlockRuns - floor;
            if (need <= 0)
                return 1f;

            int done = completedRuns - floor;
            if (done < 0)
                done = 0;
            if (done > need)
                done = need;

            return (float)done / need;
        }
    }
}
