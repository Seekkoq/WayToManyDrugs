using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;

using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using S1QuestManager = S1API.Quests.QuestManager;
using CustomNPCExample.Utils;

namespace CustomNPCExample.Quests
{
    public static class SnitchStoryManager
    {
        private const string UnderRadarTitle = "Under the Radar";
        private const string CleanUpCrewTitle = "Clean Up Crew";
        private const string LooseLipsTitle = "Loose Lips";
        private const string AskMartyTitle = "Second Opinion";
        private const string HardEvidenceTitle = "Hard Evidence";
        private const string ConfrontRemyTitle = "Face to Face";

        private const float InspectRadius = 7f;
        private const string EvidenceCartId = "westvilleconnection:products/vape_cart_premium";

        private static UnderRadarQuest _underRadarQuest;
        private static CleanUpCrewQuest _cleanUpQuest;
        private static LooseLipsQuest _looseLipsQuest;
        private static AskMartyQuest _askMartyQuest;
        private static HardEvidenceQuest _hardEvidenceQuest;

        private static bool _evidencePlanting;
        private static int _evidencePlantGeneration;

        private static int _confrontDelayGeneration;

        private static ConfrontRemyQuest _confrontRemyQuest;
        private static bool _confrontRemyStarted;
        private static bool _remyConfronted;
        private static bool _remyDialogueArmed;
        private static float _remyArmTimer;
        private static bool _confrontDelayRunning;

        private static bool _sleepWatching;
        private static bool _sleepHooked;
        private static float _sleepCheckTimer;
        private static int _lastHour = -1;
        private static int _lastDay = -1;

        private static bool _docksInspected;
        private static Vector3 _greyDocksPosition = Vector3.zero;
        private static float _proximityTimer;

        private static bool _looseLipsStarted;
        private static bool _roscoeQuestioned;
        private static bool _roscoeDialogueArmed;
        private static float _roscoeArmTimer;

        private static bool _askMartyStarted;
        private static bool _martyQuestioned;
        private static bool _martyDialogueArmed;
        private static float _martyArmTimer;

        private static bool _evidenceStarted;
        private static bool _evidenceCollected;
        private static bool _evidencePlanted;
        private static Vector3 _slopShopDropPosition = Vector3.zero;
        private static float _evidenceCheckTimer;

        private static bool _loadRestoreDone;
        private static bool _saveCleanupActive;

        public static bool IsLooseLipsActive =>
            SnitchStorySave.Stage == SnitchStorySave.STAGE_LOOSE_LIPS && !_roscoeQuestioned;

        public static bool IsAskMartyActive =>
            SnitchStorySave.Stage == SnitchStorySave.STAGE_ASK_MARTY && !_martyQuestioned;

        public static bool IsEvidenceActive =>
            SnitchStorySave.Stage == SnitchStorySave.STAGE_COLLECT_EVIDENCE && !_evidenceCollected;

        public static bool IsConfrontRemyActive =>
    SnitchStorySave.Stage == SnitchStorySave.STAGE_CONFRONT_REMY && !_remyConfronted;

        public static void Update()
        {
            UpdateSleepDetection();
            UpdateDocksInspection();
            UpdateRoscoeInvestigation();
            UpdateMartyInvestigation();
            UpdateEvidenceCollection();
            UpdateRemyConfrontation();
        }

        public static void RestoreAfterGameLoad()
        {
            if (_loadRestoreDone) return;
            _loadRestoreDone = true;

            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();
            SnitchStoryQuestCleaner.CancelAllLoadedStoryQuests();
            RebuildVisibleQuestForCurrentStage();

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Post-Snitch quest UI restored for stage " + SnitchStorySave.Stage + ".");
        }

        public static void HideTransientQuestsForSave()
        {
            if (_saveCleanupActive) return;
            _saveCleanupActive = true;

            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();
            SnitchStoryQuestCleaner.CancelAllLoadedStoryQuests();

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Transient story quests hidden before save.");
        }

        public static void RebuildAfterGameSave()
        {
            if (!_saveCleanupActive) return;
            _saveCleanupActive = false;
            RebuildVisibleQuestForCurrentStage();

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Transient story quest rebuilt after save.");
        }

        public static void RepairVisibleQuest()
        {
            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();
            SnitchStoryQuestCleaner.CancelAllLoadedStoryQuests();
            _saveCleanupActive = false;
            RebuildVisibleQuestForCurrentStage();

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible story quest repaired.");
        }

        public static void RebuildVisibleQuestForCurrentStage()
        {
            if (!SnitchSaveManager.IsCompleted) return;

            switch (SnitchStorySave.Stage)
            {
                case SnitchStorySave.STAGE_NONE:
                    return;

                case SnitchStorySave.STAGE_UNDER_RADAR:
                    EnsureUnderRadarQuest();
                    return;

                case SnitchStorySave.STAGE_CLEAN_UP_CREW:
                    EnsureCleanUpCrewQuest();
                    return;

                case SnitchStorySave.STAGE_LOOSE_LIPS:
                    EnsureLooseLipsQuest();
                    return;

                case SnitchStorySave.STAGE_ASK_MARTY:
                    EnsureAskMartyQuest();
                    return;

                case SnitchStorySave.STAGE_COLLECT_EVIDENCE:
                    EnsureHardEvidenceQuest();
                    return;

                case SnitchStorySave.STAGE_REMY_PROOF:
                    StartConfrontationDelay();
                    return;

                case SnitchStorySave.STAGE_CONFRONT_REMY:
                    EnsureConfrontRemyQuest();
                    return;

                case SnitchStorySave.STAGE_STORY_COMPLETE:
                    GiveRemyRewardsOnce();
                    return;
            }
        }

        public static void StartUnderRadarQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_UNDER_RADAR) return;
            MoveToStage(SnitchStorySave.STAGE_UNDER_RADAR);
        }

        public static void StartCleanUpCrewQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_CLEAN_UP_CREW) return;
            MoveToStage(SnitchStorySave.STAGE_CLEAN_UP_CREW);
        }

        public static void StartLooseLipsQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_LOOSE_LIPS) return;
            MoveToStage(SnitchStorySave.STAGE_LOOSE_LIPS);

            SnitchQuestManager.TryDeepPhoneDelivery(
                "Damon Trey",
                "Start with Roscoe. He sees everybody who walks through Bud's. Ask who's been acting nervous or spending new money."
            );
        }

        public static void StartAskMartyQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_ASK_MARTY) return;
            MoveToStage(SnitchStorySave.STAGE_ASK_MARTY);
        }

        public static void StartHardEvidenceQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_COLLECT_EVIDENCE) return;
            MoveToStage(SnitchStorySave.STAGE_COLLECT_EVIDENCE);

            SnitchQuestManager.TryDeepPhoneDelivery(
                "Damon Trey",
                "Marty says Remy left something in the drop behind the Slop Shop. Get there before he comes back for it."
            );
        }

        private static void MoveToStage(int newStage)
        {
            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();
            SnitchStoryQuestCleaner.CancelAllLoadedStoryQuests();
            SnitchStorySave.Stage = newStage;
            RebuildVisibleQuestForCurrentStage();
        }

        private static void EnsureUnderRadarQuest()
        {
            try
            {
                _underRadarQuest = S1QuestManager.GetQuestByName(UnderRadarTitle) as UnderRadarQuest;
                if (_underRadarQuest == null)
                {
                    _underRadarQuest = S1QuestManager.CreateQuest<UnderRadarQuest>() as UnderRadarQuest;
                    _underRadarQuest?.Begin();
                }
                if (_underRadarQuest == null) return;

                _sleepWatching = true;
                _sleepCheckTimer = 0f;
                _lastHour = -1;
                _lastDay = -1;

                TryHookSleepEvents();
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Under the Radar.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureUnderRadarQuest failed: " + ex.Message);
            }
        }

        private static void TryHookSleepEvents()
        {
            if (_sleepHooked) return;
            try
            {
                var time = Il2CppScheduleOne.DevUtilities.NetworkSingleton<Il2CppScheduleOne.GameTime.TimeManager>.Instance;
                if (time == null) return;
                time.onSleepEnd += new Action(OnNativeSleepEnd);
                _sleepHooked = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Hooked TimeManager.onSleepEnd.");
            }
            catch (Exception)
            {

            }
        }

        private static void OnNativeSleepEnd()
        {
            if (SnitchStorySave.Stage != SnitchStorySave.STAGE_UNDER_RADAR) return;
            CompleteUnderRadarFromSleep();
        }

        public static void DebugCompleteSleep()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch DEBUG] Forcing sleep objective complete.");
            CompleteUnderRadarFromSleep();
        }

        private static void CompleteUnderRadarFromSleep()
        {
            if (SnitchStorySave.Stage != SnitchStorySave.STAGE_UNDER_RADAR) return;

            _sleepWatching = false;
            _lastHour = -1;
            _lastDay = -1;

            try { _underRadarQuest?.CompleteSleepStep(); } catch { }
            _underRadarQuest = null;

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Player woke up. Starting Clean Up Crew.");
            StartCleanUpCrewQuest();
        }

        private static void UpdateSleepDetection()
        {
            if (!_sleepWatching || SnitchStorySave.Stage != SnitchStorySave.STAGE_UNDER_RADAR) return;
            TryHookSleepEvents();

            _sleepCheckTimer += Time.deltaTime;
            if (_sleepCheckTimer < 0.5f) return;
            _sleepCheckTimer = 0f;

            try
            {
                var time = Il2CppScheduleOne.DevUtilities.NetworkSingleton<Il2CppScheduleOne.GameTime.TimeManager>.Instance;
                if (time == null) return;

                int currentTime = time.CurrentTime;
                int currentDay = 0;
                try
                {
                    object elapsed = GetObjectMember(time, "ElapsedDays") ?? GetObjectMember(time, "DaysSinceStart") ?? GetObjectMember(time, "Day");
                    if (elapsed != null) currentDay = Convert.ToInt32(elapsed);
                }
                catch { }

                if (_lastHour < 0)
                {
                    _lastHour = currentTime;
                    _lastDay = currentDay;
                    return;
                }

                bool dayChanged = currentDay > _lastDay;
                int delta = currentTime - _lastHour;
                if (delta < 0) delta += 1440;
                bool bigTimeJump = delta >= 300;

                _lastHour = currentTime;
                _lastDay = currentDay;

                if (dayChanged || bigTimeJump)
                    CompleteUnderRadarFromSleep();
            }
            catch { }
        }

        private static void EnsureCleanUpCrewQuest()
        {
            try
            {
                _cleanUpQuest = S1QuestManager.GetQuestByName(CleanUpCrewTitle) as CleanUpCrewQuest;
                if (_cleanUpQuest == null)
                {
                    _cleanUpQuest = S1QuestManager.CreateQuest<CleanUpCrewQuest>() as CleanUpCrewQuest;
                    _cleanUpQuest?.Begin();
                }
                if (_cleanUpQuest == null) return;

                _docksInspected = false;
                _proximityTimer = 0f;
                _greyDocksPosition = ResolveDeadDropPosition<GreyDocksBuilding>(CleanUpCrewQuest.GreyDocksFallbackCoords);

                _cleanUpQuest.SetInspectPosition(_greyDocksPosition);
                _cleanUpQuest.BeginInspectStep();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Clean Up Crew.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureCleanUpCrewQuest failed: " + ex.Message);
            }
        }

        private static void UpdateDocksInspection()
        {
            if (_cleanUpQuest == null || _docksInspected || SnitchStorySave.Stage != SnitchStorySave.STAGE_CLEAN_UP_CREW)
                return;

            _proximityTimer += Time.deltaTime;
            if (_proximityTimer < 0.75f) return;
            _proximityTimer = 0f;

            try
            {
                if (NativePlayer.Local == null) return;
                Vector3 target = _greyDocksPosition != Vector3.zero ? _greyDocksPosition : CleanUpCrewQuest.GreyDocksFallbackCoords;
                float distance = Vector3.Distance(NativePlayer.Local.transform.position, target);

                if (distance <= InspectRadius)
                    TriggerEmptyDropFound();
            }
            catch { }
        }

        public static void TriggerEmptyDropFound()
        {
            if (_docksInspected || SnitchStorySave.Stage != SnitchStorySave.STAGE_CLEAN_UP_CREW) return;
            _docksInspected = true;

            try { _cleanUpQuest?.CompleteInspectStep(); } catch { }
            _cleanUpQuest = null;

            SnitchQuestManager.TryDeepPhoneDelivery("Damon Trey", "That box was empty before the cops touched it. Feds don't clean up that neat. Somebody inside knew the drop.");
            StartLooseLipsQuest();
        }

        public static void NotifyEmptyDropFound() { TriggerEmptyDropFound(); }

        private static void EnsureLooseLipsQuest()
        {
            try
            {
                _looseLipsQuest = S1QuestManager.GetQuestByName(LooseLipsTitle) as LooseLipsQuest;
                if (_looseLipsQuest == null)
                {
                    _looseLipsQuest = S1QuestManager.CreateQuest<LooseLipsQuest>() as LooseLipsQuest;
                    _looseLipsQuest?.Begin();
                }
                if (_looseLipsQuest == null) return;

                _looseLipsStarted = true;
                _roscoeQuestioned = false;
                _roscoeDialogueArmed = false;
                _roscoeArmTimer = 0f;

                _looseLipsQuest.TryBindRoscoePOI();
                _roscoeDialogueArmed = RoscoeInvestigationDialogue.ArmInvestigationDialogue();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Loose Lips.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureLooseLipsQuest failed: " + ex.Message);
            }
        }

        private static void UpdateRoscoeInvestigation()
        {
            if (!IsLooseLipsActive || !_looseLipsStarted) return;

            _roscoeArmTimer += Time.deltaTime;
            if (_roscoeArmTimer < 1f) return;
            _roscoeArmTimer = 0f;

            try { _looseLipsQuest?.TryBindRoscoePOI(); } catch { }

            if (!_roscoeDialogueArmed)
                _roscoeDialogueArmed = RoscoeInvestigationDialogue.ArmInvestigationDialogue();
        }

        public static void NotifyRoscoeQuestioned()
        {
            if (_roscoeQuestioned || SnitchStorySave.Stage != SnitchStorySave.STAGE_LOOSE_LIPS) return;

            _roscoeQuestioned = true;
            _roscoeDialogueArmed = false;

            try { _looseLipsQuest?.CompleteRoscoeQuestioning(); } catch { }
            _looseLipsQuest = null;

            SnitchQuestManager.TryDeepPhoneDelivery("Damon Trey", "Roscoe never says names, but that was enough. Don't kick the door yet. Get a second opinion. Marty hears everything at the Slop Shop.");
            StartAskMartyQuest();
        }

        private static void EnsureAskMartyQuest()
        {
            try
            {
                _askMartyQuest = S1QuestManager.GetQuestByName(AskMartyTitle) as AskMartyQuest;
                if (_askMartyQuest == null)
                {
                    _askMartyQuest = S1QuestManager.CreateQuest<AskMartyQuest>() as AskMartyQuest;
                    _askMartyQuest?.Begin();
                }
                if (_askMartyQuest == null) return;

                _askMartyStarted = true;
                _martyQuestioned = false;
                _martyDialogueArmed = false;
                _martyArmTimer = 0f;

                _askMartyQuest.TryBindMartyPOI();
                _martyDialogueArmed = MartyInvestigationDialogue.ArmInvestigationDialogue();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Second Opinion.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureAskMartyQuest failed: " + ex.Message);
            }
        }

        private static void UpdateMartyInvestigation()
        {
            if (!IsAskMartyActive || !_askMartyStarted) return;

            _martyArmTimer += Time.deltaTime;
            if (_martyArmTimer < 1f) return;
            _martyArmTimer = 0f;

            try { _askMartyQuest?.TryBindMartyPOI(); } catch { }

            if (!_martyDialogueArmed)
                _martyDialogueArmed = MartyInvestigationDialogue.ArmInvestigationDialogue();
        }

        public static void NotifyMartyQuestioned()
        {
            if (_martyQuestioned || SnitchStorySave.Stage != SnitchStorySave.STAGE_ASK_MARTY) return;

            _martyQuestioned = true;
            _martyDialogueArmed = false;

            try { _askMartyQuest?.CompleteMartyQuestioning(); } catch { }
            _askMartyQuest = null;

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Marty questioned. Starting Hard Evidence.");
            StartHardEvidenceQuest();
        }

        private static void EnsureHardEvidenceQuest()
        {
            try
            {
                _hardEvidenceQuest = S1QuestManager.GetQuestByName(HardEvidenceTitle) as HardEvidenceQuest;
                if (_hardEvidenceQuest == null)
                {
                    _hardEvidenceQuest = S1QuestManager.CreateQuest<HardEvidenceQuest>() as HardEvidenceQuest;
                    _hardEvidenceQuest?.Begin();
                }
                if (_hardEvidenceQuest == null) return;

                _evidenceStarted = true;
                _evidenceCollected = false;
                _evidenceCheckTimer = 0f;

                _slopShopDropPosition = ResolveDeadDropPosition<BehindSlopShop>(HardEvidenceQuest.SlopShopDropFallback);
                _hardEvidenceQuest.SetDropPosition(_slopShopDropPosition);
                _hardEvidenceQuest.BeginCollectStep();

                StartEvidencePlanting();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Hard Evidence.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureHardEvidenceQuest failed: " + ex.Message);
            }
        }

        private static void StartEvidencePlanting()
        {
            if (_evidencePlanted || _evidencePlanting)
                return;

            _evidencePlanting = true;

            int generation = ++_evidencePlantGeneration;

            MelonCoroutines.Start(
                PlantEvidenceCart(generation)
            );
        }

        private static void CancelEvidencePlanting()
        {
            _evidencePlantGeneration++;
            _evidencePlanting = false;
        }

        private static IEnumerator PlantEvidenceCart(
            int generation)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                if (generation != _evidencePlantGeneration ||
                    SnitchStorySave.Stage !=
                    SnitchStorySave.STAGE_COLLECT_EVIDENCE)
                {
                    yield break;
                }

                if (TryPlantCartInSlopShopDrop())
                {
                    if (generation == _evidencePlantGeneration)
                    {
                        _evidencePlanted = true;
                        _evidencePlanting = false;
                    }

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Snitch] Evidence cart planted in " +
                        "the Slop Shop dead drop."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (generation == _evidencePlantGeneration)
                _evidencePlanting = false;


        }

        private static bool TryPlantCartInSlopShopDrop()
        {
            try
            {
                DeadDropInstance drop = DeadDropManager.Get<BehindSlopShop>();
                if (drop == null) return false;

                object storage = GetObjectMember(drop, "Storage") ?? GetObjectMember(drop, "storage") ?? drop;
                if (storage == null) return false;

                if (HasCartInStorage(storage))
                    return true;

                return AddCartToStorage(storage);
            }
            catch { return false; }
        }

        private static bool AddCartToStorage(object storage)
        {
            try
            {
                var wrapper = S1API.Items.ItemManager.GetDefinition(EvidenceCartId);
                if (wrapper == null) return false;

                object raw = GetObjectMember(wrapper, "S1ItemDefinition") ?? GetObjectMember(wrapper, "ItemDefinition") ?? wrapper;
                var def = raw as Il2CppScheduleOne.ItemFramework.StorableItemDefinition;
                if (def == null) return false;

                var inst = def.GetDefaultInstance(1);
                if (inst == null) return false;

                object slots = GetSlotsCollection(storage);
                if (slots == null) return false;

                int count = GetCollectionCount(slots);
                for (int i = 0; i < count; i++)
                {
                    object slot = GetCollectionItem(slots, i);
                    if (slot == null) continue;
                    if (GetSlotItem(slot) != null) continue;
                    if (InvokeSetStoredItem(slot, inst, false))
                        return GetSlotItem(slot) != null;
                }
            }
            catch { }
            return false;
        }

        private static bool HasCartInStorage(
    object storage)
        {
            try
            {
                object slots = GetSlotsCollection(storage);

                if (slots == null)
                    return false;

                int count = GetCollectionCount(slots);

                for (int i = 0; i < count; i++)
                {
                    object slot =
                        GetCollectionItem(slots, i);

                    if (slot == null)
                        continue;

                    object item = GetSlotItem(slot);

                    if (item == null)
                        continue;

                    string id = GetItemId(item);

                    if (string.Equals(
                            id,
                            EvidenceCartId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static void UpdateEvidenceCollection()
        {
            if (!IsEvidenceActive || !_evidenceStarted || !_evidencePlanted) return;

            _evidenceCheckTimer += Time.deltaTime;
            if (_evidenceCheckTimer < 0.5f) return;
            _evidenceCheckTimer = 0f;

            try
            {
                DeadDropInstance drop = DeadDropManager.Get<BehindSlopShop>();
                if (drop == null) return;

                object storage = GetObjectMember(drop, "Storage") ?? GetObjectMember(drop, "storage") ?? drop;
                if (storage == null) return;

                if (!HasCartInStorage(storage))
                    NotifyCartRetrieved();
            }
            catch { }
        }

        public static void NotifyCartRetrieved()
        {
            if (_evidenceCollected ||
                SnitchStorySave.Stage !=
                SnitchStorySave.STAGE_COLLECT_EVIDENCE)
            {
                return;
            }

            _evidenceCollected = true;
            CancelEvidencePlanting();

            try
            {
                _hardEvidenceQuest?.CompleteCollectStep();
            }
            catch { }

            _hardEvidenceQuest = null;

            SnitchStorySave.ConfrontReadyUtcTicks =
                DateTime.UtcNow.AddSeconds(10).Ticks;

            SnitchStorySave.Stage =
                SnitchStorySave.STAGE_REMY_PROOF;

            SnitchStoryQuestCleaner
                .CancelAllLoadedStoryQuests();

            SnitchQuestManager.TryDeepPhoneDelivery(
                "Damon Trey",
                "You got it? Good. Sit tight and don't say a " +
                "word to anybody. I need to make a call."
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Evidence collected. " +
                "Waiting for Damon's call."
            );

            StartConfrontationDelay();
        }

        private static void StartConfrontationDelay()
        {
            if (_confrontDelayRunning)
                return;

            if (SnitchStorySave.ConfrontReadyUtcTicks <= 0L)
            {
                SnitchStorySave.ConfrontReadyUtcTicks =
                    DateTime.UtcNow.AddSeconds(10).Ticks;
            }

            _confrontDelayRunning = true;

            int generation = ++_confrontDelayGeneration;

            MelonCoroutines.Start(
                DelayedConfrontationStart(generation)
            );
        }

        private static void CancelConfrontationDelay()
        {
            _confrontDelayGeneration++;
            _confrontDelayRunning = false;
        }

        private static IEnumerator DelayedConfrontationStart(
            int generation)
        {
            long target =
                SnitchStorySave.ConfrontReadyUtcTicks;

            double remaining =
                target <= 0L
                    ? 0d
                    : TimeSpan.FromTicks(
                        target - DateTime.UtcNow.Ticks
                    ).TotalSeconds;

            if (remaining > 0d)
            {
                yield return new WaitForSeconds(
                    (float)remaining
                );
            }

            if (generation != _confrontDelayGeneration)
                yield break;

            _confrontDelayRunning = false;

            if (SnitchStorySave.Stage !=
                SnitchStorySave.STAGE_REMY_PROOF)
            {
                yield break;
            }

            SnitchStorySave.ConfrontReadyUtcTicks = 0L;

            SnitchQuestManager.TryDeepPhoneDelivery(
                "Damon Trey",
                "I made the call. Nobody touches him until " +
                "you look him in the eye. Find Remy. Make him " +
                "explain that batch stamp."
            );

            StartConfrontRemyQuest();
        }

        public static void StartConfrontRemyQuest()
        {
            if (SnitchStorySave.Stage > SnitchStorySave.STAGE_CONFRONT_REMY)
                return;

            if (SnitchStorySave.Stage == SnitchStorySave.STAGE_CONFRONT_REMY &&
                _confrontRemyQuest != null)
                return;

            MoveToStage(SnitchStorySave.STAGE_CONFRONT_REMY);
        }

        private static void EnsureConfrontRemyQuest()
        {
            try
            {
                _confrontRemyQuest = S1QuestManager.GetQuestByName(ConfrontRemyTitle) as ConfrontRemyQuest;
                if (_confrontRemyQuest == null)
                {
                    _confrontRemyQuest = S1QuestManager.CreateQuest<ConfrontRemyQuest>() as ConfrontRemyQuest;
                    _confrontRemyQuest?.Begin();
                }
                if (_confrontRemyQuest == null)
                {

                    return;
                }

                _confrontRemyStarted = true;
                _remyConfronted = false;
                _remyDialogueArmed = false;
                _remyArmTimer = 0f;

                _confrontRemyQuest.TryBindRemyPOI();
                _remyDialogueArmed = RemyConfrontationDialogue.ArmConfrontationDialogue();

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Visible quest active: Face to Face. Confront Remy.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Snitch] EnsureConfrontRemyQuest failed: " + ex.Message);
            }
        }

        private static void UpdateRemyConfrontation()
        {
            if (!IsConfrontRemyActive || !_confrontRemyStarted)
                return;

            _remyArmTimer += Time.deltaTime;
            if (_remyArmTimer < 1f) return;
            _remyArmTimer = 0f;

            try { _confrontRemyQuest?.TryBindRemyPOI(); } catch { }

            if (!_remyDialogueArmed)
            {
                _remyDialogueArmed = RemyConfrontationDialogue.ArmConfrontationDialogue();
                if (_remyDialogueArmed)
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch] Remy confrontation interaction is ready.");
            }
        }

        public static void NotifyRemyConfronted(
    int choiceOutcome)
        {
            if (_remyConfronted ||
                SnitchStorySave.Stage !=
                SnitchStorySave.STAGE_CONFRONT_REMY)
            {
                return;
            }

            if (choiceOutcome != 3)
                choiceOutcome = 3;

            _remyConfronted = true;
            _remyDialogueArmed = false;

            try
            {
                _confrontRemyQuest?.CompleteConfrontation();
            }
            catch { }

            _confrontRemyQuest = null;

            SnitchStorySave.RemyOutcome = 3;
            SnitchStorySave.StingChanceOverride = 0.08f;
            SnitchStorySave.Stage =
                SnitchStorySave.STAGE_STORY_COMPLETE;

            SnitchStoryQuestCleaner
                .CancelAllLoadedStoryQuests();

            GiveRemyRewardsOnce();

            SnitchQuestManager.TryDeepPhoneDelivery(
                "Damon Trey",
                "You shook him down? Cold move. Money's money, " +
                "but keep your eyes open. The heat has cooled off. " +
                "Sting chance is back to 8%."
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch] Remy extorted. " +
                "Story arc completed."
            );
        }

        private static void GiveRemyRewardsOnce()
        {
            if (!SnitchStorySave.RemyCashGiven)
            {
                if (TryGiveReward("cash", 2500))
                    SnitchStorySave.RemyCashGiven = true;
            }

            if (!SnitchStorySave.RemyCartsGiven)
            {
                if (TryGiveReward(
                        "westvilleconnection:products/vape_cart",
                        3))
                {
                    SnitchStorySave.RemyCartsGiven = true;
                }
            }
        }

        private static bool TryGiveReward(
            string itemId,
            int quantity)
        {
            try
            {
                MethodInfo[] methods =
                    typeof(CustomNPCExample.Utils.WvcGiveItem)
                        .GetMethods(
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic
                        );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "TryGive")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 2 ||
                        parameters[0].ParameterType !=
                        typeof(string))
                    {
                        continue;
                    }

                    object convertedQuantity =
                        Convert.ChangeType(
                            quantity,
                            parameters[1].ParameterType
                        );

                    object result = method.Invoke(
                        null,
                        new[]
                        {
                    (object)itemId,
                    convertedQuantity
                        }
                    );

                    if (method.ReturnType == typeof(bool))
                        return result is bool && (bool)result;

                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        public static void DebugCompleteRemyConfrontation(
            int ignoredChoice)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Snitch DEBUG] Forcing extort-only Remy outcome."
            );

            if (SnitchStorySave.Stage ==
                SnitchStorySave.STAGE_REMY_PROOF)
            {
                StartConfrontRemyQuest();
            }

            NotifyRemyConfronted(3);
        }

        public static void DebugForceStage(int stage)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch DEBUG] Forcing story stage to " + stage);
            MoveToStage(stage);
        }

        public static void DebugCompleteDocks() { TriggerEmptyDropFound(); }
        public static void DebugCompleteRoscoe() { NotifyRoscoeQuestioned(); }
        public static void DebugCompleteMarty() { NotifyMartyQuestioned(); }
        public static void DebugCompleteEvidence() { NotifyCartRetrieved(); }

        public static void DebugPrintState()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("==================================================");
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Snitch DEBUG] Story State");
            global::CustomNPCExample.Utils.WvcLog.Msg("  Snitch completed: " + SnitchSaveManager.IsCompleted);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Story stage: " + SnitchStorySave.Stage);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Sleep watching: " + _sleepWatching);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Docks inspected: " + _docksInspected);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Roscoe questioned: " + _roscoeQuestioned);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Marty questioned: " + _martyQuestioned);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Evidence planted: " + _evidencePlanted);
            global::CustomNPCExample.Utils.WvcLog.Msg("  Evidence collected: " + _evidenceCollected);
            global::CustomNPCExample.Utils.WvcLog.Msg("==================================================");
        }

        private static void StopDialogueOverrideForCleanup()
        {
            try { if (_roscoeDialogueArmed) RoscoeInvestigationDialogue.RestoreNormalDialogue(); } catch { }
            _roscoeDialogueArmed = false;

            try { if (_martyDialogueArmed) MartyInvestigationDialogue.RestoreNormalDialogue(); } catch { }
            _martyDialogueArmed = false;

            try { if (_remyDialogueArmed) RemyConfrontationDialogue.RestoreNormalDialogue(); } catch { }
            _remyDialogueArmed = false;
        }

        public static void PrepareForGameLoad()
        {
            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();

            _loadRestoreDone = false;
            _saveCleanupActive = false;

            _evidencePlanted = false;
            _evidencePlanting = false;

            _confrontDelayRunning = false;

            RoscoeInvestigationDialogue.Reset();
            MartyInvestigationDialogue.Reset();
            RemyConfrontationDialogue.Reset();
        }

        private static void CleanupLocalReferences()
        {

            CancelEvidencePlanting();
            CancelConfrontationDelay();

            _underRadarQuest = null;
            _cleanUpQuest = null;
            _looseLipsQuest = null;
            _askMartyQuest = null;
            _hardEvidenceQuest = null;

            _sleepWatching = false;
            _docksInspected = false;
            _looseLipsStarted = false;
            _roscoeQuestioned = false;
            _roscoeDialogueArmed = false;
            _askMartyStarted = false;
            _martyQuestioned = false;
            _martyDialogueArmed = false;
            _evidenceStarted = false;
            _evidenceCollected = false;

            _confrontRemyQuest = null;
            _confrontRemyStarted = false;
            _remyConfronted = false;
            _remyDialogueArmed = false;
            _remyArmTimer = 0f;

            _sleepCheckTimer = 0f;
            _proximityTimer = 0f;
            _roscoeArmTimer = 0f;
            _martyArmTimer = 0f;
            _evidenceCheckTimer = 0f;
            _lastHour = -1;
            _lastDay = -1;
            _greyDocksPosition = Vector3.zero;
            _slopShopDropPosition = Vector3.zero;
        }

        public static void Reset()
        {
            StopDialogueOverrideForCleanup();
            CleanupLocalReferences();
            SnitchStoryQuestCleaner.CancelAllLoadedStoryQuests();

            _loadRestoreDone = false;
            _saveCleanupActive = false;
            _evidencePlanted = false;

            SnitchStorySave.Reset();
            RoscoeInvestigationDialogue.Reset();
            MartyInvestigationDialogue.Reset();
            RemyConfrontationDialogue.Reset();

            CancelEvidencePlanting();
            CancelConfrontationDelay();

            _evidencePlanted = false;
            _evidencePlanting = false;
            _confrontDelayRunning = false;

            SnitchStorySave.Reset();
        }

        private static Vector3 ResolveDeadDropPosition<T>(Vector3 fallback) where T : class
        {
            try
            {
                var method = typeof(DeadDropManager).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (method == null) return fallback;

                object drop = method.MakeGenericMethod(typeof(T)).Invoke(null, null);
                if (drop != null)
                {
                    Vector3 pos = GetDropPosition(drop);
                    if (pos != Vector3.zero) return pos;
                }
            }
            catch { }
            return fallback;
        }

        private static Vector3 GetDropPosition(object drop)
        {
            if (drop == null) return Vector3.zero;

            object position = GetObjectMember(drop, "Position") ?? GetObjectMember(drop, "position");
            if (position is Vector3 v) return v;

            object transform = GetObjectMember(drop, "Transform") ?? GetObjectMember(drop, "transform");
            if (transform is Transform t && t != null) return t.position;

            object gameObject = GetObjectMember(drop, "gameObject");
            if (gameObject is GameObject go && go != null) return go.transform.position;

            return Vector3.zero;
        }

        private static object GetSlotsCollection(object storage)
        {
            if (storage == null) return null;

            object native = GetObjectMember(storage, "StorageEntity") ?? GetObjectMember(storage, "NativeStorage") ?? GetObjectMember(storage, "Storage");
            if (native != null)
            {
                object ns = GetObjectMember(native, "ItemSlots") ?? GetObjectMember(native, "Slots");
                if (ns != null) return ns;
            }

            return GetObjectMember(storage, "ItemSlots") ?? GetObjectMember(storage, "Slots");
        }

        private static int GetCollectionCount(object c)
        {
            if (c == null) return 0;
            object v = GetObjectMember(c, "Count") ?? GetObjectMember(c, "Length");
            return v != null ? Convert.ToInt32(v) : 0;
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

                if (collection is System.Collections.IList)
                {
                    var list =
                        (System.Collections.IList)collection;

                    return index < list.Count
                        ? list[index]
                        : null;
                }
            }
            catch { }

            try
            {
                MethodInfo getter =
                    collection.GetType().GetMethod(
                        "get_Item",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new[] { typeof(int) },
                        null
                    );

                return getter?.Invoke(
                    collection,
                    new object[] { index }
                );
            }
            catch
            {
                return null;
            }
        }

        private static object UnwrapSlot(object slot)
        {
            if (slot == null) return null;
            if (FindSetStoredItem(slot) != null) return slot;

            string[] members = { "S1ItemSlot", "NativeSlot", "ItemSlot", "Slot", "Native", "Raw", "_slot" };
            for (int i = 0; i < members.Length; i++)
            {
                object inner = GetObjectMember(slot, members[i]);
                if (inner != null && inner != slot && FindSetStoredItem(inner) != null)
                    return inner;
            }
            return slot;
        }

        private static MethodInfo FindSetStoredItem(object target)
        {
            if (target == null) return null;

            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (m.Name != "SetStoredItem") continue;

                var p = m.GetParameters();
                if ((p.Length == 2 || p.Length == 1) && p[0].ParameterType.Name.IndexOf("ItemInstance", StringComparison.OrdinalIgnoreCase) >= 0)
                    return m;
            }
            return null;
        }

        private static bool InvokeSetStoredItem(object slot, object itemInstance, bool flag)
        {
            object native = UnwrapSlot(slot);
            if (native == null) return false;

            var method = FindSetStoredItem(native);
            if (method == null) return false;

            try
            {
                var p = method.GetParameters();
                if (p.Length == 2)
                    method.Invoke(native, new object[] { itemInstance, flag });
                else
                    method.Invoke(native, new object[] { itemInstance });
                return true;
            }
            catch { return false; }
        }

        private static object GetSlotItem(object slot)
        {
            if (slot == null) return null;

            object native = UnwrapSlot(slot);
            object v = GetObjectMember(native, "ItemInstance");
            if (v != null) return v;

            v = GetObjectMember(native, "StoredItem");
            if (v != null) return v;

            v = GetObjectMember(slot, "ItemInstance");
            if (v != null) return v;

            return GetObjectMember(slot, "StoredItem");
        }

        private static string GetItemId(object item)
        {
            if (item == null) return string.Empty;

            object def = GetObjectMember(item, "Definition") ?? GetObjectMember(item, "definition") ?? GetObjectMember(item, "ItemDefinition");
            string id = string.Empty;

            if (def != null)
                id = GetObjectMember(def, "ID")?.ToString() ?? GetObjectMember(def, "Id")?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(id))
                id = GetObjectMember(item, "ID")?.ToString() ?? GetObjectMember(item, "Id")?.ToString() ?? string.Empty;

            return id;
        }

        private static object GetObjectMember(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (property != null) return property.GetValue(target);

                    FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(target);
                }
                catch { }
                type = type.BaseType;
            }
            return null;
        }
    }
}
