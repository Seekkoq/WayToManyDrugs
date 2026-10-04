using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.StationFramework;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Chemistry
{
    public static class ChemistryStationPatch
    {
        private static readonly HarmonyLib.Harmony Harmony =
            new HarmonyLib.Harmony("westvilleconnection.chemistry");

        private static readonly HashSet<MethodInfo> PatchedMethods =
            new HashSet<MethodInfo>();

        private static readonly HashSet<int> ReadyLoggedStations =
            new HashSet<int>();

        private static readonly HashSet<int> RecipeInjectedStations =
            new HashSet<int>();

        private static readonly HashSet<int> _injectionLookupWarned =
            new HashSet<int>();

        private static Il2CppScheduleOne.StationFramework.StationRecipe _brownieRecipe;

        private const float CheckInterval = 0.25f;

        private const float RescanIntervalTracking = 30f;
        private const float RescanIntervalEmpty = 20f;

        private const float PostLoadScanDelay = 3f;

        private static readonly Dictionary<int, ChemistryStation> Stations =
            new Dictionary<int, ChemistryStation>();

        private static readonly List<int> StationIds = new List<int>();

        private static int _cursor;
        private static float _checkTimer;
        private static float _rescanTimer;
        private static float _requestedScanAt = -1f;

        private static bool _applied;
        private static bool _updateConfirmed;

        public static int TrackedStationCount => StationIds.Count;

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                PatchCookMethods();
                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Chemistry] Recipe-injection patches applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Chemistry] Patch setup failed: " + ex);
            }
        }

        private static void PatchCookMethods()
        {
            var finalize = HarmonyLib.AccessTools.Method(
                typeof(Il2CppScheduleOne.ObjectScripts.ChemistryStation),
                "FinalizeOperation");

            if (finalize != null)
            {
                AddPostfix(finalize, nameof(FinalizeOperation_Postfix));
            }

            PatchIngredientFilters();

            var beginTask = HarmonyLib.AccessTools.Method(
                typeof(Il2CppScheduleOne.UI.Stations.ChemistryStationInterface),
                "BeginTask");
            if (beginTask != null)
                AddPrefix(beginTask, nameof(BeginTask_Prefix));
        }

        public static bool BeginTask_Prefix(
            Il2CppScheduleOne.UI.Stations.ChemistryStationInterface __instance)
        {
            try
            {
                var entry = __instance.selectedRecipe;
                var recipe = entry != null ? entry.Recipe : null;

                if (recipe == null || !IsCustomRecipe(recipe))
                    return true;

                var station = __instance.ChemistryStation;

                if (station == null)
                    return true;

                return StartCookManually(station, recipe);
            }
            catch (Exception)
            {

                return true;
            }
        }

        private static bool IsCustomRecipe(StationRecipe recipe)
        {
            try
            {
                if (recipe == null)
                    return false;

                if (recipe == _brownieRecipe)
                    return true;

                string id = recipe.RecipeID;

                return id != null && id.IndexOf(
                    "westvilleconnection", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool StartCookManually(
            ChemistryStation station,
            StationRecipe recipe)
        {
            try
            {
                if (station.CurrentCookOperation != null)
                {
                    return false;
                }


                var op = new ChemistryCookOperation(
                    recipe,
                    Il2CppScheduleOne.ItemFramework.EQuality.Standard,
                    recipe.FinalLiquidColor,
                    1f,
                    0);

                station.SendCookOperation(op);

                if (station.CurrentCookOperation == null)
                {


                    station.CurrentCookOperation = op;
                }


                return false;
            }
            catch (Exception)
            {

                return true;
            }
        }

        private static void PatchIngredientFilters()
        {
            MethodInfo[] methods = typeof(ItemSlot).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName)
                    continue;

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    AddPrefix(method, nameof(ItemFilter_Prefix));
                }

            }
        }

        private static void PatchSlotMethods()
        {
            MethodInfo[] methods = typeof(ItemSlot).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method == null || method.IsSpecialName)
                    continue;

                if (method.Name == "DoesItemMatchHardFilters" ||
                    method.Name == "DoesItemMatchPlayerFilters")
                {
                    AddPrefix(method, nameof(ItemFilter_Prefix));
                }
                else if (method.Name == "GetCapacityForItem")
                {
                    AddPrefix(method, nameof(Capacity_Prefix));
                }
            }
        }

        private static void AddPrefix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                prefix: new HarmonyLib.HarmonyMethod(typeof(ChemistryStationPatch), handler));

            PatchedMethods.Add(method);
        }

        private static void AddPostfix(MethodInfo method, string handler)
        {
            if (method == null || PatchedMethods.Contains(method))
                return;

            Harmony.Patch(
                method,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ChemistryStationPatch), handler));

            PatchedMethods.Add(method);
        }

        public static void RequestScan(float delaySeconds)
        {
            _requestedScanAt = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
        }

        public static void Update()
        {
            if (!_applied)
                return;

            if (!_updateConfirmed)
            {
                _updateConfirmed = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Chemistry] Update running. Waiting for stations...");

                if (_requestedScanAt < 0f)
                    RequestScan(PostLoadScanDelay);
            }

            float dt = Time.deltaTime;

            if (_requestedScanAt >= 0f && Time.unscaledTime >= _requestedScanAt)
            {
                _requestedScanAt = -1f;
                _rescanTimer = 0f;
                FullScan("scene load");
            }

            _rescanTimer += dt;
            float rescanEvery = StationIds.Count == 0 ? RescanIntervalEmpty : RescanIntervalTracking;

            if (_rescanTimer >= rescanEvery)
            {
                _rescanTimer = 0f;
                FullScan("periodic");
            }

            _checkTimer += dt;

            if (_checkTimer < CheckInterval)
                return;

            _checkTimer = 0f;
            CheckNextStation();
        }

        private static void FullScan(string reason)
        {
            try
            {
                ChemistryStation[] found = UnityEngine.Object.FindObjectsOfType<ChemistryStation>();

                int before = StationIds.Count;

                if (found != null)
                {
                    for (int i = 0; i < found.Length; i++)
                        RegisterStation(found[i], reason);
                }

                if (StationIds.Count != before || StationIds.Count == 0)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Chemistry] Scan (" + reason + ") found " +
                        (found != null ? found.Length : 0) +
                        " station(s); tracking " + StationIds.Count + ".");
                }
            }
            catch (Exception)
            {

            }
        }

        private static void RegisterStation(ChemistryStation station, string source)
        {
            if (!IsAlive(station))
                return;

            try
            {
                int id = station.GetInstanceID();

                if (Stations.ContainsKey(id))
                    return;

                Stations.Add(id, station);
                StationIds.Add(id);

                TryInjectBrownieRecipe(station, id);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Chemistry] Tracking chemistry station #" + id +
                    " (" + source + "). Total: " + StationIds.Count);
            }
            catch
            {
            }
        }

        private static bool IsAlive(ChemistryStation station)
        {
            try
            {
                return station != null;
            }
            catch
            {
                return false;
            }
        }

        private static void CheckNextStation()
        {
            int attempts = StationIds.Count;

            while (attempts-- > 0 && StationIds.Count > 0)
            {
                if (_cursor >= StationIds.Count)
                    _cursor = 0;

                int id = StationIds[_cursor];

                if (!Stations.TryGetValue(id, out ChemistryStation station) || !IsAlive(station))
                {
                    Stations.Remove(id);
                    StationIds.RemoveAt(_cursor);
                    ReadyLoggedStations.Remove(id);
                    continue;
                }

                _cursor++;
                CheckStationForBrownies(station);
                return;
            }
        }

        private static void CheckStationForBrownies(ChemistryStation station)
        {
            if (station == null)
                return;

            try
            {
                if (!RecipeInjectedStations.Contains(station.GetInstanceID()))
                    TryInjectBrownieRecipe(station, station.GetInstanceID());
            }
            catch (Exception)
            {

            }
        }

        private static bool SetOutputSlotItem(ItemSlot slot, ItemInstance item)
        {
            if (slot == null || item == null)
                return false;

            try
            {
                slot.SetStoredItem(item, false);
                return true;
            }
            catch
            {
            }

            try
            {
                MethodInfo[] methods = slot.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "SetStoredItem")
                        continue;

                    if (method.GetParameters().Length != 2)
                        continue;

                    method.Invoke(slot, new object[] { item, false });
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        private static ItemInstance CreateBrownieMixInstance(int quantity)
        {
            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(UnbakedBrownieMix.ItemId);

                if (wrapper == null)
                {

                    return null;
                }

                ItemDefinition rawDefinition = GetMember(wrapper, "S1ItemDefinition") as ItemDefinition;

                if (rawDefinition == null)
                {

                    return null;
                }

                try
                {
                    return rawDefinition.GetDefaultInstance(quantity);
                }
                catch
                {
                }

                MethodInfo[] methods = rawDefinition.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "GetDefaultInstance")
                        continue;

                    if (method.GetParameters().Length != 1)
                        continue;

                    if (method.Invoke(rawDefinition, new object[] { quantity }) is ItemInstance item)
                        return item;
                }

                return null;
            }
            catch (Exception)
            {

                return null;
            }
        }

        private const string BrownieRecipeTitle = "Unbaked Brownie Mix";

        public static void FinalizeOperation_Postfix(ChemistryStation __instance)
        {
            try
            {
                if (__instance == null || _brownieRecipe == null)
                    return;

                if (GetSelectedRecipe(__instance) != _brownieRecipe)
                    return;

                ItemSlot output = __instance.OutputSlot;
                bool hasOutput = output != null && output.ItemInstance != null;

                if (hasOutput)
                    return;

                ItemInstance mix = CreateBrownieMixInstance(1);

                if (mix == null || !SetOutputSlotItem(output, mix))
                {

                    return;
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Chemistry] Placed Unbaked Brownie Mix in the output slot.");
            }
            catch (Exception)
            {

            }
        }

        private static StationRecipe GetSelectedRecipe(ChemistryStation station)
        {
            object config =
                GetMember(station, "_stationConfiguration") ??
                GetMember(station, "_stationConfiguration_k__BackingField") ??
                GetMember(station, "stationConfiguration") ??
                GetMember(station, "StationConfiguration") ??
                GetMember(station, "_configuration") ??
                GetMember(station, "Configuration");

            if (config == null)
                return null;

            object recipeField =
                GetMember(config, "Recipe") ??
                GetMember(config, "recipe") ??
                GetMember(config, "_recipe") ??
                GetMember(config, "RecipeField") ??
                GetMember(config, "_recipeField");

            return GetMember(recipeField, "SelectedRecipe") as StationRecipe;
        }

        private static void TryInjectBrownieRecipe(ChemistryStation station, int id)
        {
            try
            {
                if (RecipeInjectedStations.Contains(id))
                    return;

                object config =
                    GetMember(station, "_stationConfiguration") ??
                    GetMember(station, "_stationConfiguration_k__BackingField") ??
                    GetMember(station, "stationConfiguration") ??
                    GetMember(station, "StationConfiguration") ??
                    GetMember(station, "_configuration") ??
                    GetMember(station, "Configuration");

                if (config == null)
                {
                    if (_injectionLookupWarned.Add(id))
                        { }
                    return;
                }

                object recipeField =
                    GetMember(config, "Recipe") ??
                    GetMember(config, "recipe") ??
                    GetMember(config, "_recipe") ??
                    GetMember(config, "RecipeField") ??
                    GetMember(config, "_recipeField");

                if (recipeField == null)
                {
                    if (_injectionLookupWarned.Add(id))
                        { }
                    return;
                }

                var options =
                    GetMember(recipeField, "Options") as
                    Il2CppSystem.Collections.Generic.List<StationRecipe>;

                if (options == null)
                {
                    if (_injectionLookupWarned.Add(id))
                        { }
                    return;
                }

                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i] != null && options[i].RecipeTitle == BrownieRecipeTitle)
                    {
                        RecipeInjectedStations.Add(id);
                        return;
                    }
                }

                StationRecipe brownie = GetOrCreateBrownieRecipe(options);
                if (brownie == null)
                    return;

                options.Add(brownie);

                bool present = false;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i] != null && options[i].RecipeTitle == BrownieRecipeTitle)
                    {
                        present = true;
                        break;
                    }
                }

                if (!present)
                    return;

                RecipeInjectedStations.Add(id);

            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Chemistry] Recipe injection failed: " + ex.Message);
            }
        }

        private static StationRecipe GetOrCreateBrownieRecipe(
            Il2CppSystem.Collections.Generic.List<StationRecipe> options)
        {
            if (_brownieRecipe != null)
                return _brownieRecipe;

            StationRecipe template = null;

            if (options != null && options.Count > 0)
                template = options[options.Count - 1];

            if (template == null)
            {
                try
                {
                    var all = UnityEngine.Object.FindObjectsOfType<StationRecipe>();
                    if (all != null && all.Length > 0)
                        template = all[0];
                }
                catch
                {
                }
            }

            if (template == null)
                return null;

            StationRecipe clone = null;

            try
            {
                clone = UnityEngine.Object.Instantiate(template);
            }
            catch
            {
            }

            if (clone == null)
            {
                try
                {
                    foreach (var ctor in typeof(StationRecipe).GetConstructors())
                    {
                        var ps = ctor.GetParameters();
                        if (ps.Length == 5 &&
                            ps[0].ParameterType == typeof(string) &&
                            ps[1].ParameterType == typeof(Il2CppScheduleOne.ItemFramework.EQuality) &&
                            ps[2].ParameterType == typeof(Color) &&
                            ps[3].ParameterType == typeof(float) &&
                            ps[4].ParameterType == typeof(int))
                        {
                            clone = ctor.Invoke(new object[]
                            {
                                "wvc_brownie_recipe",
                                Il2CppScheduleOne.ItemFramework.EQuality.Standard,
                                new Color(0.55f, 0.35f, 0.18f, 1f),
                                1f,
                                1
                            }) as StationRecipe;
                            break;
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            if (clone == null)
                return null;

            if (!ConfigureBrownieRecipe(template, clone))
                return null;

            _brownieRecipe = clone;

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Chemistry] Brownie recipe created.");

            return _brownieRecipe;
        }

        private static bool ConfigureBrownieRecipe(StationRecipe template, StationRecipe clone)
        {
            try
            {
                var qualityMethod = template.QualityCalculationMethod;
                var cookTemp = template.CookTemperature;
                var tolerance = template.CookTemperatureTolerance;

                clone.RecipeTitle = BrownieRecipeTitle;
                clone.Unlocked = true;
                clone.IsDiscovered = true;
                clone.CookTime_Mins = 270;
                clone.FinalLiquidColor = new Color(0.55f, 0.35f, 0.18f, 1f);
                clone.CookTemperature = cookTemp;
                clone.CookTemperatureTolerance = tolerance;
                clone.QualityCalculationMethod = qualityMethod;

                ItemDefinition mix = GetRawDefinitionById(UnbakedBrownieMix.ItemId);
                ItemDefinition cocoa = GetRawDefinitionById(BrownieIngredients.CocoaProductId);
                ItemDefinition butter = GetRawDefinitionById(BrownieIngredients.ButterProductId);
                ItemDefinition leaven = GetRawDefinitionById(BrownieIngredients.LeavenProductId);

                if (mix == null || cocoa == null || butter == null || leaven == null)
                {

                    return false;
                }

                var ingredients =
                    new Il2CppSystem.Collections.Generic.List<StationRecipe.IngredientQuantity>();

                ingredients.Add(CreateIngredientQuantity(
                    cocoa, BrownieChemistryRecipe.CocoaRequired));
                ingredients.Add(CreateIngredientQuantity(
                    butter, BrownieChemistryRecipe.ButterRequired));
                ingredients.Add(CreateIngredientQuantity(
                    leaven, BrownieChemistryRecipe.LeavenRequired));

                clone.Ingredients = ingredients;

                var product = new StationRecipe.ItemQuantity();
                product.Item = mix;
                product.Quantity = BrownieChemistryRecipe.OutputBatchSize;
                clone.Product = product;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Chemistry] Brownie recipe configured with " +
                    ingredients.Count + " vanilla ingredient(s) (from the meth recipe).");

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        private static StationRecipe.IngredientQuantity CreateIngredientQuantity(
            ItemDefinition item,
            int qty)
        {
            var entry = new StationRecipe.IngredientQuantity();

            var variants =
                new Il2CppSystem.Collections.Generic.List<ItemDefinition>();

            if (item != null)
                variants.Add(item);

            entry.Items = variants;
            entry.Quantity = qty;

            return entry;
        }

        private static StationRecipe.ItemQuantity CreateItemQuantity(
            StationRecipe template,
            ItemDefinition item,
            int qty)
        {
            try
            {
                var product = template != null ? template.Product : null;

                if (product != null)
                {
                    product.Item = item;
                    product.Quantity = qty;
                    return product;
                }
            }
            catch
            {
            }

            var created = new StationRecipe.ItemQuantity();
            created.Item = item;
            created.Quantity = qty;

            return created;
        }

        private static ItemDefinition GetRawDefinitionById(string itemId)
        {
            try
            {
                var wrapper = S1API.Items.ItemManager.GetDefinition(itemId);
                return GetMember(wrapper, "S1ItemDefinition") as ItemDefinition;
            }
            catch
            {
                return null;
            }
        }

        public static bool ItemFilter_Prefix(ItemSlot __instance, ItemInstance item, ref bool __result)
        {
            try
            {
                if (__instance == null || item == null)
                    return true;

                if (!IsChemistryInputSlot(__instance))
                    return true;

                if (!BrownieChemistryRecipe.IsBrownieIngredient(item))
                    return true;

                __result = true;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool Capacity_Prefix(ItemSlot __instance, ItemInstance item, bool checkPlayerFilters, ref int __result)
        {
            try
            {
                if (__instance == null || item == null)
                    return true;

                if (!IsChemistryInputSlot(__instance))
                    return true;

                if (!BrownieChemistryRecipe.IsBrownieIngredient(item))
                    return true;

                __result = Math.Max(0, 20 - __instance.Quantity);
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsChemistryInputSlot(ItemSlot slot)
        {
            try
            {
                if (slot == null || slot.SlotOwner == null)
                    return false;

                Il2CppObjectBase ownerBase = slot.SlotOwner as Il2CppObjectBase;
                ChemistryStation station = ownerBase?.TryCast<ChemistryStation>();

                if (station == null)
                    return false;

                RegisterStation(station, "slot interaction");

                if (station.OutputSlot != null && station.OutputSlot.Pointer == slot.Pointer)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object GetMember(object target, string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo p = type.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (p != null)
                        return p.GetValue(target);

                    FieldInfo f = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null)
                        return f.GetValue(target);
                }
                catch
                {
                }

                type = type.BaseType;
            }

            return null;
        }

        [HarmonyLib.HarmonyPatch(
            typeof(Il2CppScheduleOne.UI.Stations.ChemistryStationInterface),
            "Open")]
        public static class PotUiOpenDiagnostic
        {
            [HarmonyLib.HarmonyPostfix]
            private static void Postfix(
                Il2CppScheduleOne.UI.Stations.ChemistryStationInterface __instance,
                ChemistryStation station)
            {
                try
                {
                    var recipes = __instance.Recipes;

                    int count = recipes != null ? recipes.Count : -1;

                    bool hasBrownie = false;

                    if (recipes != null)
                    {
                        for (int i = 0; i < recipes.Count; i++)
                        {
                            if (recipes[i] != null &&
                                recipes[i].RecipeTitle == BrownieRecipeTitle)
                            {
                                hasBrownie = true;
                                break;
                            }
                        }
                    }


                    bool entryExists = false;

                    var entries = __instance.recipeEntries;

                    if (entries != null && _brownieRecipe != null)
                    {
                        for (int i = 0; i < entries.Count; i++)
                        {
                            if (entries[i] != null &&
                                entries[i].Recipe != null &&
                                entries[i].Recipe.RecipeTitle == BrownieRecipeTitle)
                            {
                                entryExists = true;
                                break;
                            }
                        }
                    }

                    if (_brownieRecipe != null && !entryExists)
                    {
                        if (recipes != null && !hasBrownie)
                        {
                            recipes.Add(_brownieRecipe);
                            __instance.Recipes = recipes;
                        }

                        var prefab = __instance.RecipeEntryPrefab;
                        var container = __instance.RecipeSelectionContainer;

                        Il2CppScheduleOne.UI.Stations.StationRecipeEntry refEntry = null;

                        if (entries != null)
                        {
                            for (int i = 0; i < entries.Count; i++)
                            {
                                var existing = entries[i];

                                if (existing != null && existing.gameObject != null)
                                {
                                    refEntry = existing;
                                    break;
                                }
                            }
                        }

                        if (prefab != null &&
                            (refEntry != null || container != null))
                        {
                            UnityEngine.Transform parent =
                                refEntry != null && refEntry.transform.parent != null
                                    ? refEntry.transform.parent
                                    : container.transform;

                            var entryGo = UnityEngine.Object.Instantiate(
                                prefab.gameObject, parent);

                            var entry =
                                entryGo.GetComponent<Il2CppScheduleOne.UI.Stations.StationRecipeEntry>();

                            entry.AssignRecipe(_brownieRecipe);

                            var srcRect = refEntry != null
                                ? refEntry.gameObject.GetComponent<UnityEngine.RectTransform>()
                                : null;

                            var dstRect = entryGo.GetComponent<UnityEngine.RectTransform>();

                            if (srcRect != null && dstRect != null)
                            {
                                dstRect.anchorMin = srcRect.anchorMin;
                                dstRect.anchorMax = srcRect.anchorMax;
                                dstRect.pivot = srcRect.pivot;
                                dstRect.sizeDelta = srcRect.sizeDelta;
                                dstRect.localScale = srcRect.localScale;
                                dstRect.anchoredPosition = srcRect.anchoredPosition +
                                    new Vector2(0f, -(srcRect.rect.height + 6f));
                            }

                            if (entries != null)
                            {
                                entries.Add(entry);
                                __instance.recipeEntries = entries;
                            }

                            var button = entry.Button;

                            if (button != null)
                            {
                                var selectedEntry = entry;

                                button.onClick.AddListener(
                                    new System.Action(() =>
                                    {
                                        try
                                        {
                                            __instance.SetSelectedRecipe(selectedEntry);
                                        }
                                        catch
                                        {
                                        }
                                    }));
                            }

                        }
                    }
                    else if (_brownieRecipe != null && entryExists && !hasBrownie && recipes != null)
                    {
                        recipes.Add(_brownieRecipe);
                        __instance.Recipes = recipes;
                    }
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
