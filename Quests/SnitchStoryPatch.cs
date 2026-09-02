using System;
using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.Persistence;

namespace CustomNPCExample.Quests
{
    internal static class SnitchStorySaveCoordinator
    {
        private static int _saveDepth;

        public static void BeforeSave()
        {
            _saveDepth++;

            // If one Save overload calls another, only clean once.
            if (_saveDepth != 1)
                return;

            try
            {
                SnitchStoryManager
                    .HideTransientQuestsForSave();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Pre-save quest cleanup failed: " +
                    ex.Message
                );
            }
        }

        public static void AfterSave()
        {
            if (_saveDepth <= 0)
            {
                _saveDepth = 0;
                return;
            }

            _saveDepth--;

            // Rebuild only after the outermost save completes.
            if (_saveDepth != 0)
                return;

            try
            {
                SnitchStoryManager
                    .RebuildAfterGameSave();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Post-save quest rebuild failed: " +
                    ex.Message
                );
            }
        }
    }

    [HarmonyPatch(
        typeof(SaveManager),
        "Save",
        new Type[] { typeof(string) }
    )]
    public static class SnitchStorySaveStringPatch
    {
        public static void Prefix()
        {
            SnitchStorySaveCoordinator.BeforeSave();
        }

        public static void Postfix()
        {
            SnitchStorySaveCoordinator.AfterSave();
        }
    }

    [HarmonyPatch(
        typeof(SaveManager),
        "Save",
        new Type[] { }
    )]
    public static class SnitchStorySaveNoArgsPatch
    {
        public static void Prefix()
        {
            SnitchStorySaveCoordinator.BeforeSave();
        }

        public static void Postfix()
        {
            SnitchStorySaveCoordinator.AfterSave();
        }
    }
}