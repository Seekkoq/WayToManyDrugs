using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MelonLoader;

namespace CustomNPCExample.NpcMemory
{
    public static class NpcMemoryManager
    {
        public const int MaxStrikes = 3;

        private static readonly Dictionary<string, NpcMemoryData> Memories =
            new Dictionary<string, NpcMemoryData>(
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        private static string SaveDirectory =>
            Path.Combine(
                GetUserDataDirectory(),
                "WayToManyDrugs",
                "NpcMemory"
            );

        private static string SavePath =>
            Path.Combine(
                SaveDirectory,
                "NpcMemory.json"
            );

        public static void Initialize()
        {
            Load();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"[WVC NPC Memory] Safe version initialized with " +
                $"{Memories.Count} NPC entries."
            );
        }

        public static int AddStrike(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return 0;

            NpcMemoryData data = GetOrCreate(npcId);

            data.Strikes = Math.Min(
                MaxStrikes,
                data.Strikes + 1
            );

            data.LastStrikeUtcTicks =
                DateTime.UtcNow.Ticks;

            return data.Strikes;
        }

        public static int GetStrikes(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return 0;

            return Memories.TryGetValue(
                npcId,
                out NpcMemoryData data
            )
                ? data.Strikes
                : 0;
        }

        public static bool HasGrudge(string npcId)
        {
            return GetStrikes(npcId) >= MaxStrikes;
        }

        public static void Reset(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            Memories.Remove(npcId);
        }

        public static void Clear()
        {
            Memories.Clear();
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SaveDirectory);

                string json = JsonSerializer.Serialize(
                    Memories,
                    JsonOptions
                );

                File.WriteAllText(
                    SavePath,
                    json
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    $"[WVC NPC Memory] Save failed: {ex}"
                );
            }
        }

        public static void Load()
        {
            try
            {
                Memories.Clear();

                if (!File.Exists(SavePath))
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC NPC Memory] No existing memory file found."
                    );

                    return;
                }

                string json =
                    File.ReadAllText(SavePath);

                Dictionary<string, NpcMemoryData> loaded =
                    JsonSerializer.Deserialize<
                        Dictionary<string, NpcMemoryData>
                    >(
                        json,
                        JsonOptions
                    );

                if (loaded == null)
                    return;

                foreach (
                    KeyValuePair<string, NpcMemoryData> pair
                    in loaded)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) ||
                        pair.Value == null)
                    {
                        continue;
                    }

                    pair.Value.Strikes = Math.Max(
                        0,
                        Math.Min(
                            MaxStrikes,
                            pair.Value.Strikes
                        )
                    );

                    Memories[pair.Key] = pair.Value;
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    $"[WVC NPC Memory] Loaded " +
                    $"{Memories.Count} NPC entries."
                );
            }
            catch (Exception ex)
            {
                Memories.Clear();

                MelonLogger.Error(
                    $"[WVC NPC Memory] Load failed: {ex}"
                );
            }
        }

        public static void RequireSample(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            NpcMemoryData data = GetOrCreate(npcId);

            data.RequiresSample = true;
        }

        public static bool RequiresSample(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            return Memories.TryGetValue(
                       npcId,
                       out NpcMemoryData data) &&
                   data != null &&
                   data.RequiresSample;
        }

        public static void CompleteSample(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            if (!Memories.TryGetValue(
                    npcId,
                    out NpcMemoryData data) ||
                data == null)
            {
                return;
            }

            data.RequiresSample = false;

            data.Strikes = 0;
        }

        private static NpcMemoryData GetOrCreate(string npcId)
        {
            if (!Memories.TryGetValue(
                    npcId,
                    out NpcMemoryData data) ||
                data == null)
            {
                data = new NpcMemoryData();
                Memories[npcId] = data;
            }

            return data;
        }

        private static string GetUserDataDirectory()
        {
#pragma warning disable CS0618
            return MelonLoader.Utils.MelonEnvironment.UserDataDirectory;
#pragma warning restore CS0618
        }
    }
}
