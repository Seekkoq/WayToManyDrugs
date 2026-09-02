using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;
using S1API.DeadDrops;
using S1API.Quests;
using UnityEngine;

using S1DeadDrop = S1API.DeadDrops.DeadDropInstance;
using NativeQuest = Il2CppScheduleOne.Quests.Quest;

namespace CustomNPCExample.Quests
{
    public static class SnitchDeadDrop
    {
        public const int RequiredDrops = 7;

        private static readonly string[] DeadDropTypeNames =
        {
            "BehindAutoShop",
            "BehindBank",
            "BehindCrimsonCanary",
            "BehindFireStation",
            "BehindGasMart",
            "BehindGroceryStore",
            "BehindLaundromat",
            "BehindMedicalPractice",
            "BehindMotelOffice",
            "BehindRandysBaitAndTackle",
            "BehindSlopShop",
            "BehindSupermarket",
            "BehindThompsonConstruction",
            "BehindTopTattoo",
            "BrownApartmentBlock",
            "CentralCanal",
            "Gazebo",
            "GreyDocksBuilding",
            "NorthArcadeWall",
            "PawnShopWestWall",
            "SkatePark",
            "TacoTicklersExteriorWall",
            "UnderWestBridge"
        };

        private static readonly string[] EvidenceProductIds =
        {
            "westvilleconnection:products/mdma_v3",
            "westvilleconnection:products/dmt"
        };

        private sealed class TrackedDrop
        {
            public S1DeadDrop Drop;
            public string Name;
            public bool Cleared;

            // Only populated while initially planting.
            public object MdmaSlot;
            public object DmtSlot;
        }

        private static readonly List<TrackedDrop>
            ActiveDrops = new List<TrackedDrop>();

        private static readonly System.Random Random =
            new System.Random();

        private static SnitchQuest _quest;
        private static QuestEntry _entry;

        private static bool _setup;
        private static bool _setupRunning;
        private static bool _allCleared;

        private static float _scanTimer;
        private static int _operationGeneration;

        public static void Reset()
        {
            _operationGeneration++;

            ActiveDrops.Clear();

            _quest = null;
            _entry = null;

            _setup = false;
            _setupRunning = false;
            _allCleared = false;
            _scanTimer = 0f;
        }

        public static void SetupBurnRoute(
            SnitchQuest quest,
            QuestEntry entry)
        {
            if (_setup || _setupRunning)
                return;

            _quest = quest;
            _entry = entry;

            ActiveDrops.Clear();

            _allCleared = false;
            _setupRunning = true;

            int generation = ++_operationGeneration;

            MelonCoroutines.Start(
                SetupCoroutine(generation)
            );
        }

        public static void ResumeFromSave(
            SnitchQuest quest,
            QuestEntry entry)
        {
            if (_setup || _setupRunning)
                return;

            _quest = quest;
            _entry = entry;

            List<string> savedDrops =
                SnitchSaveManager.CompromisedDrops;

            if (savedDrops == null ||
                savedDrops.Count == 0)
            {
                SetupBurnRoute(quest, entry);
                return;
            }

            if (savedDrops.Count != RequiredDrops)
            {
                MelonLogger.Warning(
                    "[WVC Snitch] Saved route contained " +
                    savedDrops.Count +
                    " drops instead of seven. Creating a new route."
                );

                SetupBurnRoute(quest, entry);
                return;
            }

            ActiveDrops.Clear();
            _allCleared = false;
            _setupRunning = true;

            int generation = ++_operationGeneration;

            MelonCoroutines.Start(
                ResumeCoroutine(
                    generation,
                    savedDrops
                )
            );
        }

        private static IEnumerator ResumeCoroutine(
            int generation,
            List<string> savedDrops)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (generation != _operationGeneration)
                    yield break;

                ActiveDrops.Clear();

                bool allResolved = true;

                for (int i = 0; i < savedDrops.Count; i++)
                {
                    string name = savedDrops[i];
                    S1DeadDrop drop = TryGetDrop(name);

                    if (drop == null)
                    {
                        allResolved = false;
                        break;
                    }

                    ActiveDrops.Add(new TrackedDrop
                    {
                        Drop = drop,
                        Name = name,
                        Cleared =
                            SnitchSaveManager
                                .IsDropCleared(name)
                    });
                }

                if (allResolved &&
                    ActiveDrops.Count ==
                    RequiredDrops)
                {
                    _setup = true;
                    _setupRunning = false;

                    UpdateQuestCounter();
                    BindPoiToNextDrop();
                    SuppressTrackedDeadDropQuests();

                    SnitchHeat.Begin();

                    int cleared = GetClearedCount();

                    MelonLogger.Msg(
                        "[WVC Snitch] Resumed burn route: " +
                        cleared + "/" + RequiredDrops +
                        " cleared."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (generation == _operationGeneration)
            {
                _setupRunning = false;
                ActiveDrops.Clear();

                MelonLogger.Error(
                    "[WVC Snitch] Could not resolve all " +
                    "saved compromised dead drops."
                );
            }
        }

        private static IEnumerator SetupCoroutine(
            int generation)
        {
            yield return new WaitForSeconds(2f);

            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (generation != _operationGeneration)
                    yield break;

                if (_allCleared ||
                    SnitchSaveManager.IsCompleted)
                {
                    _setupRunning = false;
                    yield break;
                }

                if (TrySelectDrops())
                {
                    _setup = true;
                    _setupRunning = false;

                    SnitchHeat.Begin();

                    UpdateQuestCounter();
                    BindPoiToNextDrop();
                    SuppressTrackedDeadDropQuests();

                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (generation == _operationGeneration)
            {
                _setupRunning = false;

                MelonLogger.Error(
                    "[WVC Snitch] Failed to set up " +
                    "seven compromised drops."
                );
            }
        }

        private static bool TrySelectDrops()
        {
            RollbackSelection();

            try
            {
                var shuffled =
                    new List<string>(DeadDropTypeNames);

                for (int i = shuffled.Count - 1;
                     i > 0;
                     i--)
                {
                    int j = Random.Next(i + 1);

                    string temp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = temp;
                }

                for (int i = 0;
                     i < shuffled.Count;
                     i++)
                {
                    if (ActiveDrops.Count >= RequiredDrops)
                        break;

                    string typeName = shuffled[i];
                    S1DeadDrop drop = TryGetDrop(typeName);

                    if (drop == null)
                        continue;

                    object storage = GetDropStorage(drop);

                    if (storage == null)
                        continue;

                    object mdmaSlot;
                    object dmtSlot;

                    if (!TryPlantEvidenceBundle(
                            storage,
                            out mdmaSlot,
                            out dmtSlot))
                    {
                        continue;
                    }

                    ActiveDrops.Add(new TrackedDrop
                    {
                        Drop = drop,
                        Name = typeName,
                        Cleared = false,
                        MdmaSlot = mdmaSlot,
                        DmtSlot = dmtSlot
                    });

                    MelonLogger.Msg(
                        "[WVC Snitch] Compromised drop #" +
                        ActiveDrops.Count + ": " +
                        typeName
                    );
                }

                if (ActiveDrops.Count != RequiredDrops)
                {
                    RollbackSelection();
                    return false;
                }

                var names = new List<string>();

                for (int i = 0;
                     i < ActiveDrops.Count;
                     i++)
                {
                    names.Add(ActiveDrops[i].Name);
                }

                SnitchSaveManager.RecordRouteSetup(names);
                return true;
            }
            catch (Exception ex)
            {
                RollbackSelection();

                MelonLogger.Error(
                    "[WVC Snitch] Drop selection failed: " +
                    ex.Message
                );

                return false;
            }
        }

        private static bool TryPlantEvidenceBundle(
            object storage,
            out object mdmaSlot,
            out object dmtSlot)
        {
            mdmaSlot = null;
            dmtSlot = null;

            object slots = GetSlotsCollection(storage);

            if (slots == null)
                return false;

            var emptySlots = new List<object>();

            int count = GetCollectionCount(slots);

            for (int i = 0; i < count; i++)
            {
                object slot =
                    GetCollectionItem(slots, i);

                if (slot == null ||
                    GetActualSlotItem(slot) != null ||
                    RawFindSetStoredItem(
                        UnwrapSlot(slot)) == null)
                {
                    continue;
                }

                emptySlots.Add(slot);

                if (emptySlots.Count >= 2)
                    break;
            }

            if (emptySlots.Count < 2)
                return false;

            object mdma =
                CreateItemInstance(
                    EvidenceProductIds[0],
                    1
                );

            object dmt =
                CreateItemInstance(
                    EvidenceProductIds[1],
                    1
                );

            if (mdma == null || dmt == null)
                return false;

            if (!InvokeSetStoredItem(
                    emptySlots[0],
                    mdma,
                    false))
            {
                return false;
            }

            if (!SlotContainsExactEvidence(
                    emptySlots[0],
                    EvidenceProductIds[0]))
            {
                ClearSlotIfEvidence(
                    emptySlots[0],
                    EvidenceProductIds[0]
                );

                return false;
            }

            if (!InvokeSetStoredItem(
                    emptySlots[1],
                    dmt,
                    false))
            {
                ClearSlotIfEvidence(
                    emptySlots[0],
                    EvidenceProductIds[0]
                );

                return false;
            }

            if (!SlotContainsExactEvidence(
                    emptySlots[1],
                    EvidenceProductIds[1]))
            {
                ClearSlotIfEvidence(
                    emptySlots[0],
                    EvidenceProductIds[0]
                );

                ClearSlotIfEvidence(
                    emptySlots[1],
                    EvidenceProductIds[1]
                );

                return false;
            }

            mdmaSlot = emptySlots[0];
            dmtSlot = emptySlots[1];

            return true;
        }

        private static object CreateItemInstance(
            string itemId,
            int quantity)
        {
            try
            {
                var wrapper =
                    S1API.Items.ItemManager
                        .GetDefinition(itemId);

                if (wrapper == null)
                    return null;

                object raw =
                    GetMember(
                        wrapper,
                        "S1ItemDefinition") ??
                    GetMember(
                        wrapper,
                        "ItemDefinition") ??
                    GetMember(
                        wrapper,
                        "Definition") ??
                    wrapper;

                var definition =
                    raw as Il2CppScheduleOne.ItemFramework
                        .StorableItemDefinition;

                return definition
                    ?.GetDefaultInstance(quantity);
            }
            catch
            {
                return null;
            }
        }

        private static void RollbackSelection()
        {
            for (int i = 0;
                 i < ActiveDrops.Count;
                 i++)
            {
                TrackedDrop tracked =
                    ActiveDrops[i];

                ClearSlotIfEvidence(
                    tracked.MdmaSlot,
                    EvidenceProductIds[0]
                );

                ClearSlotIfEvidence(
                    tracked.DmtSlot,
                    EvidenceProductIds[1]
                );
            }

            ActiveDrops.Clear();
        }

        private static void ClearSlotIfEvidence(
            object slot,
            string expectedId)
        {
            if (slot == null)
                return;

            object item = GetActualSlotItem(slot);

            if (item == null)
                return;

            if (string.Equals(
                    GetItemId(item),
                    expectedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                InvokeSetStoredItem(
                    slot,
                    null,
                    false
                );
            }
        }

        public static void Update()
        {
            if (!_setup || _allCleared)
                return;

            _scanTimer += Time.deltaTime;

            if (_scanTimer < 0.5f)
                return;

            _scanTimer = 0f;

            // No longer runs every frame.
            SuppressTrackedDeadDropQuests();

            bool changed = false;
            int clearedCount = 0;

            for (int i = 0;
                 i < ActiveDrops.Count;
                 i++)
            {
                TrackedDrop tracked =
                    ActiveDrops[i];

                if (tracked.Cleared)
                {
                    clearedCount++;
                    continue;
                }

                object storage =
                    GetDropStorage(tracked.Drop);

                if (storage == null)
                    continue;

                if (!HasEvidenceInStorage(storage))
                {
                    tracked.Cleared = true;
                    clearedCount++;
                    changed = true;

                    SnitchSaveManager
                        .RecordDropCleared(
                            tracked.Name
                        );
                }
            }

            if (changed)
            {
                BindPoiToNextDrop();

                MelonLogger.Msg(
                    "[WVC Snitch] Progress: " +
                    clearedCount + "/" +
                    RequiredDrops
                );
            }

            UpdateQuestCounter();

            if (clearedCount >= RequiredDrops &&
                ActiveDrops.Count == RequiredDrops)
            {
                _allCleared = true;

                try
                {
                    _entry?.Complete();
                }
                catch { }

                SnitchQuestManager.NotifyRouteBurned();
            }
        }

        public static void DebugCompleteAllDrops()
        {
            if (SnitchSaveManager.IsCompleted)
                return;

            if (ActiveDrops.Count > 0)
            {
                for (int i = 0;
                     i < ActiveDrops.Count;
                     i++)
                {
                    TrackedDrop tracked =
                        ActiveDrops[i];

                    RemoveAllQuestEvidence(
                        GetDropStorage(tracked.Drop)
                    );

                    tracked.Cleared = true;

                    SnitchSaveManager
                        .RecordDropCleared(
                            tracked.Name
                        );
                }
            }
            else
            {
                List<string> names =
                    SnitchSaveManager
                        .CompromisedDrops;

                for (int i = 0;
                     i < names.Count;
                     i++)
                {
                    S1DeadDrop drop =
                        TryGetDrop(names[i]);

                    if (drop != null)
                    {
                        RemoveAllQuestEvidence(
                            GetDropStorage(drop)
                        );
                    }

                    SnitchSaveManager
                        .RecordDropCleared(
                            names[i]
                        );
                }
            }

            _setup = true;
            _setupRunning = false;
            _allCleared = true;

            if (_entry != null)
            {
                _entry.Title =
                    "Clear the compromised dead drops (7/7)";
            }

            try
            {
                _quest?.UpdateDropCounter(7, 7);
                _entry?.Complete();
            }
            catch { }

            MelonLogger.Msg(
                "[WVC Snitch] DEBUG: 7/7 drops completed."
            );

            SnitchQuestManager.NotifyRouteBurned();
        }

        private static void RemoveAllQuestEvidence(
            object storage)
        {
            object slots =
                GetSlotsCollection(storage);

            if (slots == null)
                return;

            int count = GetCollectionCount(slots);

            for (int i = 0; i < count; i++)
            {
                object slot =
                    GetCollectionItem(slots, i);

                object item =
                    GetActualSlotItem(slot);

                if (item == null)
                    continue;

                if (IsEvidence(GetItemId(item)))
                {
                    InvokeSetStoredItem(
                        slot,
                        null,
                        false
                    );
                }
            }
        }

        private static bool HasEvidenceInStorage(
            object storage)
        {
            object slots =
                GetSlotsCollection(storage);

            if (slots == null)
                return false;

            int count = GetCollectionCount(slots);

            for (int i = 0; i < count; i++)
            {
                object slot =
                    GetCollectionItem(slots, i);

                object item =
                    GetActualSlotItem(slot);

                if (item == null)
                    continue;

                object quantityValue =
                    GetMember(item, "Quantity") ??
                    GetMember(slot, "Quantity");

                int quantity = 1;

                try
                {
                    if (quantityValue != null)
                    {
                        quantity =
                            Convert.ToInt32(
                                quantityValue
                            );
                    }
                }
                catch { }

                if (quantity > 0 &&
                    IsEvidence(GetItemId(item)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SlotContainsExactEvidence(
            object slot,
            string expectedId)
        {
            object item =
                GetActualSlotItem(slot);

            return item != null &&
                   string.Equals(
                       GetItemId(item),
                       expectedId,
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        private static bool IsEvidence(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0;
                 i < EvidenceProductIds.Length;
                 i++)
            {
                if (string.Equals(
                        id,
                        EvidenceProductIds[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void BindPoiToNextDrop()
        {
            if (_entry == null)
                return;

            for (int i = 0;
                 i < ActiveDrops.Count;
                 i++)
            {
                if (ActiveDrops[i].Cleared)
                    continue;

                Vector3 position =
                    GetDropPosition(
                        ActiveDrops[i].Drop
                    );

                if (position != Vector3.zero)
                {
                    _entry.POIPosition = position;
                    return;
                }
            }
        }

        private static int GetClearedCount()
        {
            int cleared = 0;

            for (int i = 0;
                 i < ActiveDrops.Count;
                 i++)
            {
                if (ActiveDrops[i].Cleared)
                    cleared++;
            }

            return cleared;
        }

        private static void UpdateQuestCounter()
        {
            try
            {
                int cleared = GetClearedCount();

                if (_entry != null)
                {
                    _entry.Title =
                        "Clear the compromised dead drops (" +
                        cleared + "/" + RequiredDrops + ")";
                }

                _quest?.UpdateDropCounter(
                    cleared,
                    RequiredDrops
                );
            }
            catch { }
        }

        private static void SuppressTrackedDeadDropQuests()
        {
            try
            {
                var quests = NativeQuest.Quests;

                if (quests == null)
                    return;

                for (int i = quests.Count - 1;
                     i >= 0;
                     i--)
                {
                    var quest = quests[i];

                    if (quest == null)
                        continue;

                    string title = "";

                    try
                    {
                        title =
                            quest.Title ??
                            quest.title ??
                            quest.name ??
                            "";
                    }
                    catch { }

                    if (string.IsNullOrEmpty(title))
                        continue;

                    if (title.IndexOf(
                            "The Snitch",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        title.IndexOf(
                            "Finishing",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    if (!IsQuestForTrackedDrop(title))
                        continue;

                    try
                    {
                        quest.Cancel();
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool IsQuestForTrackedDrop(
            string title)
        {
            string normalizedTitle =
                NormalizeTitle(title);

            if (string.IsNullOrEmpty(normalizedTitle))
                return false;

            for (int i = 0;
                 i < ActiveDrops.Count;
                 i++)
            {
                string fullName =
                    NormalizeTitle(
                        ActiveDrops[i].Name
                    );

                string shortName =
                    NormalizeDropName(
                        ActiveDrops[i].Name
                    );

                if ((!string.IsNullOrEmpty(fullName) &&
                     normalizedTitle.Contains(fullName)) ||
                    (!string.IsNullOrEmpty(shortName) &&
                     shortName.Length >= 5 &&
                     normalizedTitle.Contains(shortName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeDropName(
            string value)
        {
            string result = NormalizeTitle(value);

            result = result.Replace("behind", "");
            result = result.Replace("building", "");
            result = result.Replace("exterior", "");
            result = result.Replace("wall", "");
            result = result.Replace("under", "");

            return result;
        }

        private static string NormalizeTitle(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var builder = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                {
                    builder.Append(
                        char.ToLowerInvariant(value[i])
                    );
                }
            }

            return builder.ToString();
        }

        private static object UnwrapSlot(
            object slot)
        {
            if (slot == null)
                return null;

            if (RawFindSetStoredItem(slot) != null)
                return slot;

            string[] members =
            {
                "S1ItemSlot",
                "NativeSlot",
                "ItemSlot",
                "Slot",
                "Native",
                "Raw",
                "_slot",
                "_itemSlot"
            };

            for (int i = 0;
                 i < members.Length;
                 i++)
            {
                object inner =
                    GetMember(slot, members[i]);

                if (inner != null &&
                    !ReferenceEquals(inner, slot) &&
                    RawFindSetStoredItem(inner) != null)
                {
                    return inner;
                }
            }

            return slot;
        }

        private static MethodInfo RawFindSetStoredItem(
            object target)
        {
            if (target == null)
                return null;

            MethodInfo[] methods;

            try
            {
                methods = target.GetType().GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );
            }
            catch
            {
                return null;
            }

            for (int i = 0;
                 i < methods.Length;
                 i++)
            {
                MethodInfo method = methods[i];

                if (method.Name != "SetStoredItem")
                    continue;

                ParameterInfo[] parameters =
                    method.GetParameters();

                if ((parameters.Length == 1 ||
                     parameters.Length == 2) &&
                    parameters[0].ParameterType.Name.IndexOf(
                        "ItemInstance",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool InvokeSetStoredItem(
            object slot,
            object item,
            bool flag)
        {
            object native = UnwrapSlot(slot);

            if (native == null)
                return false;

            MethodInfo method =
                RawFindSetStoredItem(native);

            if (method == null)
                return false;

            try
            {
                ParameterInfo[] parameters =
                    method.GetParameters();

                if (parameters.Length == 2)
                {
                    method.Invoke(
                        native,
                        new[] { item, (object)flag }
                    );
                }
                else
                {
                    method.Invoke(
                        native,
                        new[] { item }
                    );
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object GetActualSlotItem(
            object slot)
        {
            if (slot == null)
                return null;

            object native = UnwrapSlot(slot);

            object value =
                GetMember(native, "ItemInstance") ??
                GetMember(native, "StoredItem") ??
                GetMember(slot, "ItemInstance") ??
                GetMember(slot, "StoredItem");

            return value;
        }

        private static string GetItemId(object item)
        {
            if (item == null)
                return "";

            object definition =
                GetMember(item, "Definition") ??
                GetMember(item, "definition") ??
                GetMember(item, "ItemDefinition") ??
                GetMember(
                    item,
                    "StorableItemDefinition"
                );

            string id = "";

            if (definition != null)
            {
                id =
                    GetMember(definition, "ID")
                        ?.ToString() ??
                    GetMember(definition, "Id")
                        ?.ToString() ??
                    "";
            }

            if (string.IsNullOrEmpty(id))
            {
                id =
                    GetMember(item, "ID")
                        ?.ToString() ??
                    GetMember(item, "Id")
                        ?.ToString() ??
                    "";
            }

            return id;
        }

        private static object GetSlotsCollection(
            object storage)
        {
            if (storage == null)
                return null;

            object native =
                GetMember(storage, "StorageEntity") ??
                GetMember(storage, "NativeStorage") ??
                GetMember(storage, "Storage");

            if (native != null &&
                !ReferenceEquals(native, storage))
            {
                object slots =
                    GetMember(native, "ItemSlots") ??
                    GetMember(native, "Slots");

                if (slots != null)
                    return slots;
            }

            return
                GetMember(storage, "ItemSlots") ??
                GetMember(storage, "Slots");
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

                if (collection is IList)
                {
                    IList list = (IList)collection;

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

        private static int GetCollectionCount(
            object collection)
        {
            if (collection == null)
                return 0;

            object value =
                GetMember(collection, "Count") ??
                GetMember(collection, "Length");

            try
            {
                return value == null
                    ? 0
                    : Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static object GetMember(
            object target,
            string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(
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

                    FieldInfo field = type.GetField(
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

        private static S1DeadDrop TryGetDrop(
            string typeName)
        {
            try
            {
                Type nativeType =
                    FindDeadDropType(typeName);

                if (nativeType == null)
                    return null;

                MethodInfo getMethod =
                    typeof(DeadDropManager).GetMethod(
                        "Get",
                        BindingFlags.Public |
                        BindingFlags.Static
                    );

                if (getMethod == null)
                    return null;

                return getMethod
                    .MakeGenericMethod(nativeType)
                    .Invoke(null, null)
                    as S1DeadDrop;
            }
            catch
            {
                return null;
            }
        }

        private static Type FindDeadDropType(
            string simpleName)
        {
            string[] candidates =
            {
                "S1API.DeadDrops.Native." + simpleName,
                "S1API.DeadDrops." + simpleName
            };

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int a = 0;
                 a < assemblies.Length;
                 a++)
            {
                for (int c = 0;
                     c < candidates.Length;
                     c++)
                {
                    try
                    {
                        Type type =
                            assemblies[a].GetType(
                                candidates[c],
                                false
                            );

                        if (type != null)
                            return type;
                    }
                    catch { }
                }

                Type[] types;

                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int i = 0;
                     i < types.Length;
                     i++)
                {
                    if (types[i] != null &&
                        types[i].Name == simpleName)
                    {
                        return types[i];
                    }
                }
            }

            return null;
        }

        private static Vector3 GetDropPosition(
            S1DeadDrop drop)
        {
            if (drop == null)
                return Vector3.zero;

            object position =
                GetMember(drop, "Position") ??
                GetMember(drop, "position");

            if (position is Vector3)
                return (Vector3)position;

            object transform =
                GetMember(drop, "Transform") ??
                GetMember(drop, "transform");

            if (transform is Transform)
                return ((Transform)transform).position;

            object gameObject =
                GetMember(drop, "gameObject");

            if (gameObject is GameObject)
            {
                return ((GameObject)gameObject)
                    .transform.position;
            }

            return Vector3.zero;
        }

        private static object GetDropStorage(
            S1DeadDrop drop)
        {
            if (drop == null)
                return null;

            return
                GetMember(drop, "Storage") ??
                GetMember(drop, "storage") ??
                GetMember(drop, "StorageEntity") ??
                drop;
        }
    }
}