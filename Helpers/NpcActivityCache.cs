using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppScheduleOne.AvatarFramework.Animation;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    internal static class NpcActivityCache
    {
        private sealed class ActivityEntry
        {
            public string Kind;
            public string Guid = "";
            public string Name = "";
            public string Path = "";
            public float X;
            public float Y;
            public float Z;
        }

        private static readonly List<ActivityEntry> Entries =
            new List<ActivityEntry>();

        private static bool _scannedThisSession;

        private static string CachePath =>
            Path.Combine(
                MelonLoader.Utils.MelonEnvironment.UserDataDirectory,
                "WVC_npc_activity_cache.txt");

        public static bool Loaded => Entries.Count > 0;

        static NpcActivityCache()
        {
            LoadCache();
        }

        public static void ScanMainScene()
        {
            if (_scannedThisSession)
                return;

            _scannedThisSession = true;

            var found = new List<ActivityEntry>();

            try
            {
                var atms = UnityEngine.Object.FindObjectsOfType<ATM>();

                if (atms != null)
                {
                    foreach (var atm in atms)
                    {
                        if (atm == null)
                            continue;

                        found.Add(new ActivityEntry
                        {
                            Kind = "atm",
                            Guid = SafeGuid(atm),
                            X = atm.transform.position.x,
                            Y = atm.transform.position.y,
                            Z = atm.transform.position.z
                        });
                    }
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var machines = UnityEngine.Object.FindObjectsOfType<VendingMachine>();

                if (machines != null)
                {
                    foreach (var machine in machines)
                    {
                        if (machine == null)
                            continue;

                        found.Add(new ActivityEntry
                        {
                            Kind = "vending",
                            Guid = SafeGuid(machine),
                            X = machine.transform.position.x,
                            Y = machine.transform.position.y,
                            Z = machine.transform.position.z
                        });
                    }
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var seatSets = UnityEngine.Object.FindObjectsOfType<AvatarSeatSet>();

                if (seatSets != null)
                {
                    foreach (var seatSet in seatSets)
                    {
                        if (seatSet == null || seatSet.transform == null)
                            continue;

                        found.Add(new ActivityEntry
                        {
                            Kind = "seat",
                            Name = seatSet.gameObject.name,
                            Path = BuildTransformPath(seatSet.transform),
                            X = seatSet.transform.position.x,
                            Y = seatSet.transform.position.y,
                            Z = seatSet.transform.position.z
                        });
                    }
                }
            }
            catch (Exception)
            {

            }

            Entries.Clear();
            Entries.AddRange(found);
            SaveCache();

            int atmsFound = CountKind("atm");
            int vendingFound = CountKind("vending");
            int seatsFound = CountKind("seat");

        }

        public static string GetAtmGuid(int index)
        {
            return GetKindGuid("atm", index);
        }

        public static string GetVendingMachineGuid(int index)
        {
            return GetKindGuid("vending", index);
        }

        public static int AtmCount => CountKind("atm");
        public static int VendingMachineCount => CountKind("vending");
        public static int SeatSetCount => CountKind("seat");

        public static int StableIndex(string label, int count)
        {
            if (count <= 0)
                return -1;

            uint hash = 2166136261u;

            foreach (char c in label)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return (int)(hash % (uint)count);
        }

        public static bool TryGetSeatSet(int index, out string name, out string path)
        {
            name = "";
            path = "";

            var seats = KindEntries("seat");

            if (index < 0 || index >= seats.Count)
                return false;

            name = seats[index].Name;
            path = seats[index].Path;

            return !string.IsNullOrEmpty(name);
        }

        private static List<ActivityEntry> KindEntries(string kind)
        {
            var result = new List<ActivityEntry>();

            foreach (var entry in Entries)
            {
                if (entry.Kind == kind)
                    result.Add(entry);
            }

            return result;
        }

        private static int CountKind(string kind)
        {
            int count = 0;

            foreach (var entry in Entries)
            {
                if (entry.Kind == kind)
                    count++;
            }

            return count;
        }

        private static string GetKindGuid(string kind, int index)
        {
            var list = KindEntries(kind);

            if (index < 0 || index >= list.Count)
                return "";

            return list[index].Guid;
        }

        private static string SafeGuid(
            Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase machine)
        {
            try
            {
                var bakedProp = machine.GetType().GetProperty("BakedGUID");

                if (bakedProp != null)
                {
                    string baked = bakedProp.GetValue(machine) as string;

                    if (!string.IsNullOrEmpty(baked))
                        return baked;
                }
            }
            catch
            {
            }

            try
            {
                var guidProp = machine.GetType().GetProperty("GUID");

                if (guidProp != null)
                {
                    object value = guidProp.GetValue(machine);

                    if (value != null)
                        return value.ToString();
                }
            }
            catch
            {
            }

            return "";
        }

        private static string BuildTransformPath(Transform t)
        {
            try
            {
                var parts = new List<string>();
                Transform current = t;

                while (current != null)
                {
                    parts.Insert(0, current.name);
                    current = current.parent;
                }

                return string.Join("/", parts);
            }
            catch
            {
                return "";
            }
        }

        private static void SaveCache()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# WVC world activity cache (auto-generated)");

                foreach (var entry in Entries)
                {
                    sb.AppendLine(string.Join("|",
                        entry.Kind,
                        entry.Guid,
                        entry.Name.Replace("|", ""),
                        entry.Path.Replace("|", ""),
                        entry.X.ToString("F2"),
                        entry.Y.ToString("F2"),
                        entry.Z.ToString("F2")));
                }

                File.WriteAllText(CachePath, sb.ToString());
            }
            catch (Exception)
            {

            }
        }

        private static void LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath))
                    return;

                foreach (string line in File.ReadAllLines(CachePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split('|');

                    if (parts.Length < 7)
                        continue;

                    Entries.Add(new ActivityEntry
                    {
                        Kind = parts[0],
                        Guid = parts[1],
                        Name = parts[2],
                        Path = parts[3],
                        X = ParseFloat(parts[4]),
                        Y = ParseFloat(parts[5]),
                        Z = ParseFloat(parts[6])
                    });
                }

                if (Entries.Count > 0)
                {
                }
            }
            catch (Exception)
            {

            }
        }

        private static float ParseFloat(string input)
        {
            return float.TryParse(
                input,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float value) ? value : 0f;
        }
    }
}
