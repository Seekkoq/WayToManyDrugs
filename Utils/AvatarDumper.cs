using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

using NativeAvatarSettings = Il2CppScheduleOne.AvatarFramework.AvatarSettings;

namespace CustomNPCExample.Utils
{
    public static class AvatarDumper
    {
        public static void DumpAllAvatarPaths()
        {
            try
            {
                string dumpPath = Path.Combine(
                    MelonEnvironment.UserDataDirectory,
                    "AvatarPaths_Dump.txt"
                );

                var hairPaths = new SortedSet<string>();
                var facePaths = new SortedSet<string>();
                var bodyPaths = new SortedSet<string>();
                var accessoryPaths = new SortedSet<string>();

                var all = Resources.FindObjectsOfTypeAll(
                    Il2CppType.Of<NativeAvatarSettings>()
                );

                int settingsCount = 0;

                foreach (var obj in all)
                {
                    var settings = obj.TryCast<NativeAvatarSettings>();
                    if (settings == null) continue;

                    settingsCount++;

                    if (!string.IsNullOrWhiteSpace(settings.HairPath))
                        hairPaths.Add(settings.HairPath);

                    if (settings.FaceLayerSettings != null)
                    {
                        for (int i = 0; i < settings.FaceLayerSettings.Count; i++)
                        {
                            string p = settings.FaceLayerSettings[i]?.layerPath;
                            if (!string.IsNullOrWhiteSpace(p)) facePaths.Add(p);
                        }
                    }

                    if (settings.BodyLayerSettings != null)
                    {
                        for (int i = 0; i < settings.BodyLayerSettings.Count; i++)
                        {
                            string p = settings.BodyLayerSettings[i]?.layerPath;
                            if (!string.IsNullOrWhiteSpace(p)) bodyPaths.Add(p);
                        }
                    }

                    if (settings.AccessorySettings != null)
                    {
                        for (int i = 0; i < settings.AccessorySettings.Count; i++)
                        {
                            string p = settings.AccessorySettings[i]?.path;
                            if (!string.IsNullOrWhiteSpace(p)) accessoryPaths.Add(p);
                        }
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine("   SCHEDULE I - WORKING AVATAR PATH STRINGS");
                sb.AppendLine("   AvatarSettings objects scanned: " + settingsCount);
                sb.AppendLine("==================================================");
                sb.AppendLine();

                AppendSection(sb, "HAIR  (av.HairPath = \"...\")", hairPaths);
                AppendSection(sb, "FACE  (av.WithFaceLayer(\"...\", color))", facePaths);
                AppendSection(sb, "BODY  (av.WithBodyLayer(\"...\", color))", bodyPaths);
                AppendSection(sb, "ACCESSORY  (av.WithAccessoryLayer(\"...\", color))", accessoryPaths);

                File.WriteAllText(dumpPath, sb.ToString());

                MelonLogger.Msg("[WVC Dump] Hair: " + hairPaths.Count
                    + " | Face: " + facePaths.Count
                    + " | Body: " + bodyPaths.Count
                    + " | Accessory: " + accessoryPaths.Count);

                MelonLogger.Msg("[WVC Dump] Saved to: " + dumpPath);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Dump] Failed: " + ex);
            }
        }

        private static void AppendSection(
            StringBuilder sb,
            string title,
            SortedSet<string> paths)
        {
            sb.AppendLine("--- " + title + " ---");

            if (paths.Count == 0)
            {
                sb.AppendLine("(none found)");
            }
            else
            {
                foreach (string p in paths)
                    sb.AppendLine(p);
            }

            sb.AppendLine();
        }
    }
}