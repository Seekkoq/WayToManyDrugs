using System;
using System.Collections.Generic;
using CustomNPCExample.Products;
using Il2CppScheduleOne.GameTime;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.SupplierPricing
{
    public sealed class PriceEntry
    {
        public string ItemId;
        public float BasePrice;
        public float MinMultiplier;
        public float MaxMultiplier;

        public float CurrentPrice { get; set; }
        public float CurrentMultiplier { get; set; } = 1f;
        public float PreviousPrice { get; set; }
    }

    public static class SupplierPricingManager
    {
        public static Action<string, float> PriceSetter;

                private static readonly List<PriceEntry> Entries =
            new List<PriceEntry>();

        private static readonly System.Random Rng =
            new System.Random();

        private static bool _initialized;
        private static int _lastDay = int.MinValue;

        public static void Initialize()
        {
            if (_initialized)
                return;

            if (PriceSetter == null)
            {


                return;
            }

            RoscoePricing.Register();
            _initialized = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"[WVC Pricing] Initialized with {Entries.Count} Roscoe item(s)."
            );

            RollNewDay("startup", notifyRoscoe: false);
        }

        public static void RegisterItem(
            string itemId,
            float basePrice,
            float minMultiplier,
            float maxMultiplier)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            foreach (PriceEntry existing in Entries)
            {
                if (string.Equals(
                        existing.ItemId,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            Entries.Add(
                new PriceEntry
                {
                    ItemId = itemId,
                    BasePrice = basePrice,
                    MinMultiplier = minMultiplier,
                    MaxMultiplier = maxMultiplier,
                    CurrentPrice = basePrice,
                    PreviousPrice = basePrice,
                    CurrentMultiplier = 1f
                }
            );
        }

        public static void Update()
        {
            if (!_initialized)
                return;

            TryDetectDayChange();
        }

        private static void TryDetectDayChange()
        {
            try
            {
                TimeManager timeManager = TimeManager.Instance;

                if (timeManager == null)
                    return;

                int currentDay = timeManager.ElapsedDays;

                if (_lastDay == int.MinValue)
                {
                    _lastDay = currentDay;
                    return;
                }

                if (currentDay == _lastDay)
                    return;

                _lastDay = currentDay;

                RollNewDay(
                    "day " + currentDay,
                    notifyRoscoe: true
                );
            }
            catch (Exception)
            {

            }
        }

        private static void RollNewDay(
            string reason,
            bool notifyRoscoe)
        {
            if (Entries.Count == 0)
                return;

            foreach (PriceEntry entry in Entries)
            {
                entry.PreviousPrice = entry.CurrentPrice;

                float multiplier = RandomRange(
                    entry.MinMultiplier,
                    entry.MaxMultiplier
                );

                entry.CurrentMultiplier = multiplier;

                entry.CurrentPrice = Mathf.Max(
                    1f,
                    Mathf.Round(entry.BasePrice * multiplier)
                );

                ApplyPrice(entry);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Pricing] " +
                    GetShortName(entry.ItemId) +
                    " $" +
                    entry.CurrentPrice.ToString("0") +
                    " (was $" +
                    entry.PreviousPrice.ToString("0") +
                    ", x" +
                    entry.CurrentMultiplier.ToString("0.00") +
                    ")"
                );
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Pricing] Rolled Roscoe prices (" + reason + ")."
            );

            if (!notifyRoscoe)
                return;

            RoscoeMarketTexts.SendMarketUpdate(
                Entries.ToArray(),
                reason
            );
        }

        private static void ApplyPrice(PriceEntry entry)
        {
            try
            {
                PriceSetter?.Invoke(
                    entry.ItemId,
                    entry.CurrentPrice
                );
            }
            catch (Exception)
            {

            }
        }

        private static float RandomRange(
            float minimum,
            float maximum)
        {
            return (float)(
                Rng.NextDouble() *
                (maximum - minimum) +
                minimum
            );
        }

        private static string GetShortName(string itemId)
        {
            if (string.Equals(
                    itemId,
                    MollyIngredients.SafroleId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Safrole";
            }

            if (string.Equals(
                    itemId,
                    MollyIngredients.PmkId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "PMK";
            }

            if (string.Equals(
                    itemId,
                    MollyIngredients.PmkRefinedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Refined PMK";
            }

            if (string.Equals(
                    itemId,
                    MollyIngredients.PmkLabGradeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Lab PMK";
            }

            return itemId;
        }

        public static float GetCurrentPrice(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0f;

            foreach (PriceEntry entry in Entries)
            {
                if (string.Equals(
                        entry.ItemId,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return entry.CurrentPrice;
                }
            }

            return 0f;
        }
    }
}
