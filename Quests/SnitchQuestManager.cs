using System;
using System.Reflection;
using Il2CppScheduleOne.Quests;
using MelonLoader;
using UnityEngine;

using S1QuestManager = S1API.Quests.QuestManager;
using NativeQuest = Il2CppScheduleOne.Quests.Quest;

namespace CustomNPCExample.Quests
{
    public static class SnitchQuestManager
    {
        private const string SnitchQuestTitle =
            "The Snitch";

        private static bool _startedThisSession;
        private static bool _dialogueArmed;
        private static bool _talkStepDone;
        private static bool _requiredRankDetected;
        private static bool _rankDetectionLogged;

        private static SnitchQuest _activeQuest;

        private static float _timer;
        private static float _dialogueTimer;
        private static float _bootTimer;
        private static bool _bootReported;

        public static void Update()
        {
            SnitchPhoneDelivery.Update();

            if (!_bootReported)
            {
                _bootTimer += Time.deltaTime;

                if (_bootTimer >= 15f)
                {
                    _bootReported = true;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snitch] Game loaded - restoring quests."
                    );

                    SnitchHeat.PrintPatrolCount("GAME LOAD");
                    SnitchStoryManager.RestoreAfterGameLoad();

                    if (SnitchSaveManager.IsTalkActive)
                        SendDamonTextMessage();

                    if (!SnitchSaveManager.IsCompleted &&
                        HasRequiredRank())
                    {
                        TryStartQuest();
                    }
                }
            }

            SnitchHeat.Update();

            SnitchStoryManager.Update();

            if (SnitchSaveManager.IsCompleted)
                return;

            SnitchDeadDrop.Update();

            if (_startedThisSession &&
                !_talkStepDone &&
                !_dialogueArmed)
            {
                _dialogueTimer += Time.deltaTime;

                if (_dialogueTimer >= 1f)
                {
                    _dialogueTimer = 0f;

                    if (SnitchDialogue.ArmSnitchDialogue())
                        _dialogueArmed = true;

                    try
                    {
                        _activeQuest?.TryBindDamonPOI();
                    }
                    catch { }
                }
            }

            if (_startedThisSession)
                return;

            _timer += Time.deltaTime;

            if (_timer < 2f)
                return;

            _timer = 0f;

            if (TryBindExistingQuest())
            {
                _startedThisSession = true;
                return;
            }

            if (HasRequiredRank())
                TryStartQuest();
        }

        private static void HardReset()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC DEBUG] FULL HARD RESET TO STAGE 0."
            );

            try
            {
                S1QuestManager
                    .GetQuestByName(SnitchQuestTitle)
                    ?.Cancel();
            }
            catch { }

            ResetRuntimeState();

            SnitchSaveManager.ResetSave();
            SnitchStorySave.Reset();
            SnitchStoryManager.Reset();
            SnitchDeadDrop.Reset();
            SnitchPhoneDelivery.Reset();

            try
            {
                SnitchDialogue.RestoreNormalDialogue();
            }
            catch { }

            TryStartQuest();
        }

        public static void PrepareForGameLoad()
        {
            ResetRuntimeState();
            SnitchDeadDrop.Reset();
            SnitchStoryManager.PrepareForGameLoad();
        }

        private static void ResetRuntimeState()
        {
            _startedThisSession = false;
            _dialogueArmed = false;
            _talkStepDone = false;
            _requiredRankDetected = false;
            _rankDetectionLogged = false;

            _activeQuest = null;

            _timer = 0f;
            _dialogueTimer = 0f;
            _bootTimer = 0f;
            _bootReported = false;
        }

        public static void NotifyTalkedToDamon()
        {
            if (_talkStepDone)
                return;

            try
            {
                SnitchQuest quest =
                    _activeQuest ??
                    S1QuestManager.GetQuestByName(
                        SnitchQuestTitle
                    ) as SnitchQuest;

                if (quest == null)
                    return;

                _activeQuest = quest;
                quest.CompleteTalkStep();

                _talkStepDone = true;
                _dialogueArmed = false;

                SnitchSaveManager.SetStage(
                    SnitchSaveManager.STAGE_BURN_ROUTES
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] Failed advancing after " +
                    "Damon talk: " + ex.Message
                );
            }
        }

        public static void NotifyRouteBurned()
        {
            if (SnitchSaveManager.IsCompleted)
                return;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] All seven routes burned."
            );

            TryDeepPhoneDelivery(
                "Damon Trey",
                "That's all of them. You got the product out " +
                "just in time. But there were names in those " +
                "boxes. This isn't over."
            );

            SnitchSaveManager.MarkCompleted();
            SnitchHeat.TransitionToPostQuestLockdown();

            if (SnitchStorySave.Stage ==
                SnitchStorySave.STAGE_NONE)
            {
                SnitchStoryManager.StartUnderRadarQuest();
            }

            try
            {
                _activeQuest?.Complete();
            }
            catch { }

            try
            {
                S1QuestManager
                    .GetQuestByName(SnitchQuestTitle)
                    ?.Complete();
            }
            catch { }
        }

        public static bool TryStartQuest()
        {
            if (SnitchSaveManager.IsCompleted)
                return false;

            if (_startedThisSession)
                return true;

            try
            {
                if (TryBindExistingQuest())
                {
                    _startedThisSession = true;
                    return true;
                }

                SnitchQuest quest =
                    S1QuestManager.CreateQuest<SnitchQuest>()
                    as SnitchQuest;

                if (quest == null)
                {
                    MelonLogger.Error(
                        "[WVC Snitch] CreateQuest<SnitchQuest> " +
                        "returned null."
                    );

                    return false;
                }

                _activeQuest = quest;

                quest.Begin();

                RestoreActiveQuestRuntime();

                _startedThisSession = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Snitch] The Snitch started."
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] TryStartQuest failed: " +
                    ex.Message
                );

                return false;
            }
        }

        public static void ForceStartSnitch()
        {
            if (SnitchSaveManager.IsCompleted)
            {


                return;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] DEBUG: Bypassing rank requirement."
            );

            _requiredRankDetected = true;

            TryStartQuest();
        }

        private static bool TryBindExistingQuest()
        {
            try
            {
                SnitchQuest existing =
                    S1QuestManager.GetQuestByName(
                        SnitchQuestTitle
                    ) as SnitchQuest;

                if (existing == null)
                    return false;

                _activeQuest = existing;
                RestoreActiveQuestRuntime();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Snitch] Existing The Snitch quest restored."
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RestoreActiveQuestRuntime()
        {
            if (_activeQuest == null)
                return;

            int stage = SnitchSaveManager.Stage;

            if (stage ==
                SnitchSaveManager.STAGE_NOT_STARTED)
            {
                SendDamonTextMessage();

                SnitchSaveManager.SetStage(
                    SnitchSaveManager
                        .STAGE_TALK_TO_DAMON
                );

                stage =
                    SnitchSaveManager
                        .STAGE_TALK_TO_DAMON;
            }

            if (stage ==
                SnitchSaveManager.STAGE_TALK_TO_DAMON)
            {
                SendDamonTextMessage();

                try
                {
                    _talkStepDone =
                        _activeQuest.TalkToDamonEntry != null &&
                        _activeQuest.TalkToDamonEntry.State ==
                        S1API.Quests.Constants
                            .QuestState.Completed;
                }
                catch
                {
                    _talkStepDone = false;
                }

                if (_talkStepDone)
                {
                    SnitchSaveManager.SetStage(
                        SnitchSaveManager
                            .STAGE_BURN_ROUTES
                    );

                    stage =
                        SnitchSaveManager
                            .STAGE_BURN_ROUTES;
                }
            }

            if (stage ==
                SnitchSaveManager.STAGE_BURN_ROUTES)
            {
                _talkStepDone = true;
                _dialogueArmed = false;

                try
                {
                    if (_activeQuest.TalkToDamonEntry != null &&
                        _activeQuest.TalkToDamonEntry.State !=
                        S1API.Quests.Constants
                            .QuestState.Completed)
                    {
                        _activeQuest
                            .TalkToDamonEntry
                            .Complete();
                    }
                }
                catch { }

                try
                {
                    _activeQuest.BurnRouteEntry?.Begin();
                }
                catch { }

                try
                {
                    SnitchDialogue.RestoreNormalDialogue();
                }
                catch { }

                SnitchDeadDrop.ResumeFromSave(
                    _activeQuest,
                    _activeQuest.BurnRouteEntry
                );
            }
        }

        private static string GetQuestTitle(object quest)
        {
            if (quest == null)
                return string.Empty;

            string[] memberNames =
            {
                "Title",
                "title",
                "QuestTitle",
                "questTitle",
                "Name",
                "name",
                "ID",
                "Id",
                "id",
                "StaticID",
                "staticID"
            };

            for (int i = 0; i < memberNames.Length; i++)
            {
                object value =
                    GetMemberValue(
                        quest,
                        memberNames[i]
                    );

                string text = value as string;

                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            return string.Empty;
        }

        public static void DumpNativeQuestStates()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg(
                "=========== WVC QUEST/RANK DUMP ==========="
            );

            string rankName;
            int rankLevel;

            bool rankFound =
                TryGetPlayerRank(
                    out rankName,
                    out rankLevel
                );

            bool requiredRankMet = false;

            try
            {
                requiredRankMet =
                    IsEnforcerTwoOrHigher(
                        rankName,
                        rankLevel
                    );
            }
            catch { }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Rank found=" +
                rankFound +
                " | rankName='" +
                (rankName ?? "?") +
                "' | rankLevel=" +
                rankLevel +
                " | Enforcer2OrHigher=" +
                requiredRankMet
            );

            try
            {
                var snitchQuest =
                    S1QuestManager.GetQuestByName(
                        SnitchQuestTitle
                    );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Snitch] The Snitch quest=" +
                    (snitchQuest != null
                        ? "FOUND"
                        : "NOT FOUND")
                );
            }
            catch (Exception)
            {

            }

            try
            {
                var quests =
                    NativeQuest.Quests;

                if (quests == null)
                {

                }
                else
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snitch] Native quest count=" +
                        quests.Count
                    );

                    for (int i = 0; i < quests.Count; i++)
                    {
                        NativeQuest quest =
                            quests[i];

                        if (quest == null)
                            continue;

                        string title =
                            GetQuestTitle(quest);

                        string state =
                            GetQuestStateText(quest);

                        bool completed =
                            IsQuestCompleted(quest);

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[" +
                            i +
                            "] title='" +
                            title +
                            "' | state=" +
                            state +
                            " | completed=" +
                            completed
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Snitch] Native quest dump failed: " +
                    ex.Message
                );
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] RequiredRankDetected=" +
                _requiredRankDetected +
                " | SnitchStarted=" +
                _startedThisSession +
                " | SnitchSaveCompleted=" +
                SnitchSaveManager.IsCompleted +
                " | CurrentStage=" +
                SnitchSaveManager.Stage
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "==========================================="
            );
        }

        private static string GetQuestStateText(object quest)
        {
            if (quest == null)
                return "?";

            try
            {
                object state =
                    GetMemberValue(quest, "State") ??
                    GetMemberValue(quest, "state") ??
                    GetMemberValue(quest, "QuestState") ??
                    GetMemberValue(quest, "questState");

                return state != null
                    ? state.ToString()
                    : "?";
            }
            catch
            {
                return "?";
            }
        }

        private static bool IsQuestCompleted(object quest)
        {
            if (quest == null)
                return false;

            try
            {
                object state =
                    GetMemberValue(quest, "State") ??
                    GetMemberValue(quest, "state") ??
                    GetMemberValue(quest, "QuestState") ??
                    GetMemberValue(quest, "questState");

                if (state != null)
                {
                    string stateName = state.ToString();

                    if (!string.IsNullOrEmpty(stateName))
                    {
                        string normalized =
                            stateName.Replace(" ", "")
                                     .Replace("_", "")
                                     .Replace("-", "")
                                     .ToLowerInvariant();

                        if (normalized == "complete" ||
                            normalized == "completed" ||
                            normalized == "finished" ||
                            normalized == "done")
                        {
                            return true;
                        }
                    }

                    try
                    {
                        int value = Convert.ToInt32(state);

                        if (value == 2)
                            return true;
                    }
                    catch { }
                }

                object completed =
                    GetMemberValue(quest, "IsCompleted") ??
                    GetMemberValue(quest, "isCompleted") ??
                    GetMemberValue(quest, "Completed") ??
                    GetMemberValue(quest, "IsComplete");

                return completed is bool && (bool)completed;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRequiredRank()
        {
            if (_requiredRankDetected)
                return true;

            try
            {
                string rankName;
                int rankLevel;

                if (!TryGetPlayerRank(
                        out rankName,
                        out rankLevel))
                {
                    return false;
                }

                bool isEnforcer =
                    !string.IsNullOrEmpty(rankName) &&
                    rankName.IndexOf(
                        "enforcer",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0;

                bool isEnforcerTwo =
                    IsEnforcerTwoOrHigher(
                        rankName,
                        rankLevel
                    );

                if (!isEnforcer ||
                    !isEnforcerTwo)
                {
                    return false;
                }

                _requiredRankDetected = true;

                if (!_rankDetectionLogged)
                {
                    _rankDetectionLogged = true;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snitch] Required rank detected: '" +
                        rankName +
                        "' level=" +
                        rankLevel +
                        ". Starting The Snitch."
                    );
                }

                return true;
            }
            catch (Exception)
            {
                if (!_rankDetectionLogged)
                {
                    _rankDetectionLogged = true;


                }

                return false;
            }
        }

        private static bool IsEnforcerTwoOrHigher(
    string rankName,
    int rankLevel)
        {
            if (string.IsNullOrEmpty(rankName))
                return false;

            string normalized =
                rankName.Replace(" ", "")
                        .Replace("_", "")
                        .Replace("-", "")
                        .Replace(".", "")
                        .ToLowerInvariant();

            bool isEnforcer =
                normalized.Contains("enforcer");

            if (!isEnforcer)
                return false;

            if (rankLevel >= 2)
                return true;

            return normalized.Contains("enforcer2") ||
                   normalized.Contains("enforcerii");
        }

        private static bool TryGetPlayerRank(
    out string rankName,
    out int rankLevel)
        {
            rankName = string.Empty;
            rankLevel = 0;

            try
            {
                Type levelManagerType =
                    typeof(NativeQuest).Assembly.GetType(
                        "Il2CppScheduleOne.Levelling.LevelManager"
                    );

                if (levelManagerType != null)
                {
                    object levelManager =
                        GetStaticMemberFromHierarchy(
                            levelManagerType,
                            "Instance"
                        ) ??
                        GetStaticMemberFromHierarchy(
                            levelManagerType,
                            "_instance"
                        );

                    if (levelManager != null)
                    {
                        object rank =
                            GetMemberValue(levelManager, "Rank") ??
                            GetMemberValue(levelManager, "rank") ??
                            GetMemberValue(levelManager, "_rank") ??
                            GetMemberValue(levelManager, "CurrentRank") ??
                            GetMemberValue(levelManager, "currentRank") ??
                            GetMemberValue(levelManager, "_currentRank");

                        object tier =
                            GetMemberValue(levelManager, "Tier") ??
                            GetMemberValue(levelManager, "tier") ??
                            GetMemberValue(levelManager, "_tier") ??
                            GetMemberValue(levelManager, "RankTier") ??
                            GetMemberValue(levelManager, "rankTier") ??
                            GetMemberValue(levelManager, "_rankTier");

                        if (rank != null)
                        {
                            rankName = rank.ToString();

                            TryReadNestedRank(
                                rank,
                                ref rankName,
                                ref rankLevel
                            );
                        }

                        if (tier != null)
                        {
                            try
                            {
                                rankLevel =
                                    Convert.ToInt32(tier);
                            }
                            catch { }
                        }

                        if (!string.IsNullOrEmpty(rankName) ||
                            rankLevel > 0)
                        {
                            return true;
                        }

                        if (TryReadRankFromObject(
                                levelManager,
                                ref rankName,
                                ref rankLevel))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            object player = null;

            try
            {
                player =
                    Il2CppScheduleOne.PlayerScripts
                        .Player.Local;
            }
            catch { }

            if (player == null)
                return false;

            if (TryReadRankFromObject(
                    player,
                    ref rankName,
                    ref rankLevel))
            {
                return true;
            }

            string[] childMembers =
            {
        "PlayerData",
        "playerData",
        "_playerData",
        "Progression",
        "progression",
        "_progression",
        "Levelling",
        "levelling",
        "_levelling",
        "LevelManager",
        "levelManager",
        "_levelManager",
        "SaveData",
        "saveData",
        "_saveData"
    };

            for (int i = 0; i < childMembers.Length; i++)
            {
                object child =
                    GetMemberValue(
                        player,
                        childMembers[i]
                    );

                if (child == null)
                    continue;

                if (TryReadRankFromObject(
                        child,
                        ref rankName,
                        ref rankLevel))
                {
                    return true;
                }
            }

            return !string.IsNullOrEmpty(rankName) ||
                   rankLevel > 0;
        }

        private static object GetStaticMemberFromHierarchy(
    Type type,
    string name)
        {
            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.FlattenHierarchy
                        );

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(null);
                    }
                }
                catch { }

                try
                {
                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.FlattenHierarchy
                        );

                    if (field != null)
                        return field.GetValue(null);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }

        private static bool TryReadRankFromObject(
            object source,
            ref string rankName,
            ref int rankLevel)
        {
            if (source == null)
                return false;

            bool found = false;

            string[] rankMembers =
            {
        "Rank",
        "rank",
        "_rank",
        "RankName",
        "rankName",
        "_rankName",
        "CurrentRank",
        "currentRank",
        "_currentRank",
        "RankData",
        "rankData",
        "_rankData"
    };

            for (int i = 0; i < rankMembers.Length; i++)
            {
                object value =
                    GetMemberValue(
                        source,
                        rankMembers[i]
                    );

                if (value == null)
                    continue;

                found = true;

                string text = value as string;

                if (string.IsNullOrEmpty(text))
                    text = value.ToString();

                if (!string.IsNullOrEmpty(text) &&
                    text != value.GetType().FullName)
                {
                    rankName = text;
                }

                TryReadNestedRank(
                    value,
                    ref rankName,
                    ref rankLevel
                );
            }

            string[] levelMembers =
            {
        "RankLevel",
        "rankLevel",
        "_rankLevel",
        "RankTier",
        "rankTier",
        "_rankTier",
        "Level",
        "level",
        "_level",
        "Tier",
        "tier",
        "_tier"
    };

            for (int i = 0; i < levelMembers.Length; i++)
            {
                object value =
                    GetMemberValue(
                        source,
                        levelMembers[i]
                    );

                if (value == null)
                    continue;

                try
                {
                    rankLevel =
                        Mathf.Max(
                            rankLevel,
                            Convert.ToInt32(value)
                        );

                    found = true;
                }
                catch { }
            }

            return found;
        }

        private static void TryReadNestedRank(
            object rankObject,
            ref string rankName,
            ref int rankLevel)
        {
            if (rankObject == null)
                return;

            string[] nameMembers =
            {
        "Name",
        "name",
        "Title",
        "title",
        "RankName",
        "rankName",
        "DisplayName",
        "displayName"
    };

            for (int i = 0; i < nameMembers.Length; i++)
            {
                object value =
                    GetMemberValue(
                        rankObject,
                        nameMembers[i]
                    );

                string text = value as string;

                if (!string.IsNullOrEmpty(text))
                {
                    rankName = text;
                    break;
                }
            }

            string[] levelMembers =
            {
        "Level",
        "level",
        "Tier",
        "tier",
        "RankLevel",
        "rankLevel",
        "Value",
        "value"
    };

            for (int i = 0; i < levelMembers.Length; i++)
            {
                object value =
                    GetMemberValue(
                        rankObject,
                        levelMembers[i]
                    );

                if (value == null)
                    continue;

                try
                {
                    rankLevel =
                        Mathf.Max(
                            rankLevel,
                            Convert.ToInt32(value)
                        );

                    break;
                }
                catch { }
            }
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(target);
                    }
                }
                catch { }

                try
                {
                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                        return field.GetValue(target);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }

        private static int GetCollectionCount(object collection)
        {
            if (collection == null)
                return 0;

            try
            {
                if (collection is Array)
                    return ((Array)collection).Length;
            }
            catch { }

            try
            {
                if (collection is System.Collections.IList)
                {
                    return
                        ((System.Collections.IList)collection)
                            .Count;
                }
            }
            catch { }

            try
            {
                object count =
                    GetMemberValue(collection, "Count") ??
                    GetMemberValue(collection, "Length");

                if (count != null)
                    return Convert.ToInt32(count);
            }
            catch { }

            return 0;
        }

        private static object GetCollectionItem(
            object collection,
            int index)
        {
            if (collection == null || index < 0)
                return null;

            try
            {
                if (collection is Array)
                {
                    Array array = (Array)collection;

                    return index < array.Length
                        ? array.GetValue(index)
                        : null;
                }
            }
            catch { }

            try
            {
                if (collection is System.Collections.IList)
                {
                    System.Collections.IList list =
                        (System.Collections.IList)collection;

                    return index < list.Count
                        ? list[index]
                        : null;
                }
            }
            catch { }

            try
            {
                MethodInfo[] methods =
                    collection.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "get_Item")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType !=
                        typeof(int))
                    {
                        continue;
                    }

                    return method.Invoke(
                        collection,
                        new object[] { index }
                    );
                }
            }
            catch { }

            return null;
        }

        private static void SendDamonTextMessage()
        {
            DamonSnitchMessage.Send();
        }

        public static bool TryDeepPhoneDelivery(
            string sender,
            string message)
        {
            return SnitchPhoneDelivery.Queue(
                null,
                sender,
                message
            );
        }
    }
}
