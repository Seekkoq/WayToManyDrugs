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

            if (_saveDepth != 1)
                return;

            try
            {
                SnitchStoryManager
                    .HideTransientQuestsForSave();
            }
            catch (Exception)
            {

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

            if (_saveDepth != 0)
                return;

            try
            {
                SnitchStoryManager
                    .RebuildAfterGameSave();
            }
            catch (Exception)
            {

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
