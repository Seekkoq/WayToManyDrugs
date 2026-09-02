using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    /// <summary>
    /// Persists supplier sample claims per save file so players can only
    /// receive a free sample once per save.
    /// </summary>
    public static class SupplierSampleSaveManager
    {
        public const string RoscoeKey       = "roscoe";
        public const string GusKey          = "gus";
        public const string DamonTreyKey    = "damon_trey";
        public const string MartyMellowsKey = "marty_mellows";
        public const string RemyKey         = "remy";
        public const string StellaVanceKey  = "stella_vance";

        private static readonly MelonPreferences_Category Category =
            MelonPreferences.CreateCategory(
                "WVC_SupplierSamples",
                "Westville Connection - Supplier Sample Claims"
            );

        private static readonly MelonPreferences_Entry<string> ClaimedDataPref =
            Category.CreateEntry(
                "ClaimedData_v1",
                ""
            );

        // Memory cache: SaveId -> Set of claimed supplier keys
        private static readonly Dictionary<string, HashSet<string>> Cache =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static string _activeSaveId = "default_save";
        private static bool _loaded;

        public static string ActiveSaveId
        {
            get => _activeSaveId;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _activeSaveId = SanitizeKey(value);
            }
        }

        public static bool IsSampleClaimed(string supplierKey)
        {
            if (string.IsNullOrEmpty(supplierKey))
                return false;

            EnsureLoaded();

            string saveId = _activeSaveId;

            // 1. Check in-memory cache for active save
            if (Cache.TryGetValue(saveId, out var set) && set.Contains(supplierKey))
                return true;

            // 2. Check in-memory cache for default save fallback
            if (Cache.TryGetValue("default_save", out var defaultSet) && defaultSet.Contains(supplierKey))
                return true;

            // 3. Check PlayerPrefs
            string prefKey = GetPlayerPrefKey(saveId, supplierKey);
            if (PlayerPrefs.GetInt(prefKey, 0) == 1)
            {
                AddToCache(saveId, supplierKey);
                return true;
            }

            string defaultPrefKey = GetPlayerPrefKey("default_save", supplierKey);
            if (PlayerPrefs.GetInt(defaultPrefKey, 0) == 1)
            {
                AddToCache(saveId, supplierKey);
                return true;
            }

            return false;
        }

        public static void MarkSampleClaimed(string supplierKey)
        {
            if (string.IsNullOrEmpty(supplierKey))
                return;

            EnsureLoaded();

            string saveId = _activeSaveId;

            AddToCache(saveId, supplierKey);
            AddToCache("default_save", supplierKey);

            // Save to PlayerPrefs
            try
            {
                PlayerPrefs.SetInt(GetPlayerPrefKey(saveId, supplierKey), 1);
                PlayerPrefs.SetInt(GetPlayerPrefKey("default_save", supplierKey), 1);
                PlayerPrefs.Save();
            }
            catch { }

            // Save to MelonPreferences file
            SavePreferences();

            MelonLogger.Msg(
                "[WVC Samples] Sample marked as claimed for supplier '" +
                supplierKey + "' in save '" + saveId + "'."
            );
        }

        public static void ResetSample(string supplierKey)
        {
            if (string.IsNullOrEmpty(supplierKey))
                return;

            EnsureLoaded();

            string saveId = _activeSaveId;

            if (Cache.TryGetValue(saveId, out var set))
                set.Remove(supplierKey);

            if (Cache.TryGetValue("default_save", out var defaultSet))
                defaultSet.Remove(supplierKey);

            try
            {
                PlayerPrefs.DeleteKey(GetPlayerPrefKey(saveId, supplierKey));
                PlayerPrefs.DeleteKey(GetPlayerPrefKey("default_save", supplierKey));
                PlayerPrefs.Save();
            }
            catch { }

            SavePreferences();

            MelonLogger.Msg(
                "[WVC Samples] Sample claim reset for supplier '" +
                supplierKey + "' in save '" + saveId + "'."
            );
        }

        public static void ResetAllSamplesForCurrentSave()
        {
            EnsureLoaded();

            string saveId = _activeSaveId;
            if (Cache.TryGetValue(saveId, out var set))
            {
                foreach (var key in set)
                {
                    try
                    {
                        PlayerPrefs.DeleteKey(GetPlayerPrefKey(saveId, key));
                    }
                    catch { }
                }
                set.Clear();
            }

            try { PlayerPrefs.Save(); } catch { }
            SavePreferences();

            MelonLogger.Msg(
                "[WVC Samples] All sample claims reset for save '" + saveId + "'."
            );
        }

        private static string GetPlayerPrefKey(string saveId, string supplierKey)
        {
            return "WVC_SampleClaimed_" + saveId + "_" + supplierKey;
        }

        private static string SanitizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "default_save";

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                raw = raw.Replace(invalid[i], '_');

            return raw.Replace(" ", "_").Replace(":", "_").Replace("/", "_").Replace("\\", "_");
        }

        // ============================================================
        // Storage & Serialization
        // ============================================================

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            try
            {
                string raw = ClaimedDataPref.Value;
                if (!string.IsNullOrEmpty(raw))
                {
                    // Format: saveId:key1,key2;saveId2:key1
                    string[] entries = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < entries.Length; i++)
                    {
                        string[] parts = entries[i].Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            string sId = parts[0].Trim();
                            string[] keys = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            if (!Cache.TryGetValue(sId, out var set))
                            {
                                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                Cache[sId] = set;
                            }

                            for (int k = 0; k < keys.Length; k++)
                                set.Add(keys[k].Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Samples] Error loading sample claims: " + ex.Message);
            }
        }

        private static void SavePreferences()
        {
            try
            {
                List<string> list = new List<string>();
                foreach (var pair in Cache)
                {
                    if (pair.Value.Count > 0)
                    {
                        list.Add(pair.Key + ":" + string.Join(",", pair.Value));
                    }
                }

                ClaimedDataPref.Value = string.Join(";", list);
                Category.SaveToFile();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WVC Samples] Error saving sample claims: " + ex.Message);
            }
        }

        private static void AddToCache(string saveId, string supplierKey)
        {
            if (!Cache.TryGetValue(saveId, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Cache[saveId] = set;
            }

            set.Add(supplierKey);
        }
    }

    // ============================================================
    // Save tracking patch
    // ============================================================

    [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { typeof(string) })]
    public static class SupplierSampleSaveNamePatch
    {
        public static void Prefix(string __0)
        {
            if (!string.IsNullOrEmpty(__0))
            {
                SupplierSampleSaveManager.ActiveSaveId = __0;
            }
        }
    }
}
