using CustomNPCExample.Admin;
using CustomNPCExample.Law;
using CustomNPCExample.NpcMemory;
using CustomNPCExample.NPCs;
using CustomNPCExample.Products;
using CustomNPCExample.Products.Cauldrons;
using CustomNPCExample.Products.Chemistry;
using CustomNPCExample.Products.Edibles;
using CustomNPCExample.Products.Ovens;
using CustomNPCExample.QoL;
using CustomNPCExample.Quests;
using CustomNPCExample.SupplierPricing;
using CustomNPCExample.UI;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using System;
using UnityEngine;
using HarmonyInstance = HarmonyLib.Harmony;

[assembly: MelonInfo(
    typeof(CustomNPCMod.MainMod),
    "WayToManyDrugs",
    "3.7.0",
    "0.Seek"
)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace CustomNPCMod
{
    public class MainMod : MelonMod
    {
        private bool _mollyIngredientsRegistered;
        private bool _gummyIngredientsRegistered;
        private bool _gummyMixRegistered;
        private bool _gummyMixVisualApplied;
        private bool _sugarRegistered;
        private bool _brownieMixRegistered;

        private bool _mdmaRegistered;
        private bool _gummiesRegistered;
        private bool _dmtRegistered;
        private bool _cartRegistered;

        private bool _mdmaMetadataRegistered;
        private bool _gummiesMetadataRegistered;
        private bool _dmtMetadataRegistered;
        private bool _cartMetadataRegistered;
        private bool _brownieRegistered;
        private bool _brownieMetadataRegistered;
        private bool _brownieIngredientsRegistered;

        private bool _cookieIngredientsRegistered;
        private bool _cookieDoughRegistered;
        private bool _cookieRegistered;
        private bool _cookieMetadataRegistered;

        private bool _sugarInShops;

        private bool _salviaRegistered;
        private bool _salviaSeedRegistered;
        private bool _salviaMetadataRegistered;

        private bool _xanaxRegistered;
        private bool _xanaxMetadataRegistered;
        private bool _xanaxPowderRegistered;

        private float _retryTimer;
        private float _shopTimer;
        private bool _allRegistered;

        private float _activityScanTimer = -1f;

        private int _gummyVisualAttempts;
        private const int MaxGummyVisualAttempts = 20;

        private int _shopAttempts;
        private const int MaxShopAttempts = 40;

        private HarmonyInstance _snitchStoryHarmony;

        public override void OnInitializeMelon()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Westville Connection 3.7.0 initializing.");

            RegisterIl2CppTypes();

            _snitchStoryHarmony = new HarmonyInstance("wvc.snitch.story");
            _snitchStoryHarmony.PatchAll();

            SupplierPricingManager.PriceSetter = (itemId, price) =>
            {
                MollyIngredients.SetPrice(itemId, price);
            };

            NpcMemoryManager.Initialize();

            VapeCartQualityPatch.ApplyPatch();
            MDMAConsumptionPatch.ApplyPatch();
            CauldronPatch.ApplyPatch();
            CauldronPatch.TreatEveryCauldronAsCustom = true;
            LabOvenPatch.ApplyPatch();
            LabOvenVisuals.ApplyPatch();
            UndercoverStingPatch.ApplyPatch();
            DMTLabOvenPatch.ApplyPatch();
            ChemistryStationPatch.ApplyPatch();
            BrownieOvenPatch.ApplyPatch();
            CustomNPCExample.NPCs.RoscoeBulkMeetupManager.ApplyPatch();
            CustomNPCExample.NPCs.SalViahBulkMeetupManager.ApplyPatch();
            CustomNPCExample.NPCs.DamonTreyBulkMeetupManager.ApplyPatch();
            CustomNPCExample.NPCs.PillVilleBulkMeetupManager.ApplyPatch();
            CustomNPCExample.Products.XanaxBrickPressPatch.ApplyPatch();

            // The snow command takes the console's own 'setweather' word out of the way and answers
            // it through S1API, so this has to be in place before the console wakes up.
            CustomNPCExample.Weather.WvcConsoleCommands.ApplyPatch();

            // The game's own per-frame exceptions cost more than any feature in here: one of them was
            // throwing once a frame for a whole session. Installed first, so nothing else has to run
            // with that overhead in place.
            CustomNPCExample.Qol.WvcExceptionGuards.ApplyPatch();

            CustomNPCExample.Utils.WvcLog.Initialize();

            FlashlightBoost.Initialize(_snitchStoryHarmony);
            WvcRecipesPhoneApp.Initialize();

            // The snow system's settings first: the schedule and the settings screen both read them.
            CustomNPCExample.Weather.WvcSnowSettings.Initialize();

            // The weather talk hooks are patched by hand: a hook the game no longer offers must not
            // be able to take the rest of the mod's start-up with it.
            CustomNPCExample.Weather.WvcWeatherChatterPatches.ApplyPatch(_snitchStoryHarmony);

            CustomNPCExample.UI.WvcSettingsTab.Initialize(_snitchStoryHarmony);

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Initialization complete.");
        }

        public override void OnApplicationQuit()
        {
            NpcMemoryManager.Save();
        }

        private static void RegisterIl2CppTypes()
        {
            RegisterIl2CppType<VapeCartUseBehaviour>("VapeCartUseBehaviour");
            RegisterIl2CppType<THCGummyScreenEffect>("THCGummyScreenEffect");
            RegisterIl2CppType<DMTScreenEffect>("DMTScreenEffect");
            RegisterIl2CppType<MDMAScreenEffect>("MDMAScreenEffect");
            RegisterIl2CppType<BrownieScreenEffect>("BrownieScreenEffect");
            RegisterIl2CppType<THCCookieScreenEffect>("THCCookieScreenEffect");
            RegisterIl2CppType<VapeCartScreenEffect>("VapeCartScreenEffect");
            RegisterIl2CppType<SalviaScreenEffect>(
    "SalviaScreenEffect"
);
            // Registered here like every other runtime screen effect: without this the component
            // type is unknown to the il2cpp domain and AddComponent<T> throws a
            // TypeInitializationException from the interop's generic method store.
            RegisterIl2CppType<XanaxScreenEffect>("XanaxScreenEffect");
        }

        private static void RegisterIl2CppType<T>(string typeName)
            where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<T>();
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] " + typeName + " registered.");
            }
            catch (Exception)
            {

            }
        }

        private static void RegisterTypeSafely<T>(string name) where T : class
        {
            try
            {
                if (!ClassInjector.IsTypeRegisteredInIl2Cpp<T>())
                {
                    ClassInjector.RegisterTypeInIl2Cpp<T>();
                    global::CustomNPCExample.Utils.WvcLog.Msg($"[WVC] {name} registered in IL2CPP.");
                }
            }
            catch (Exception)
            {

            }
        }

        private void CheckAllRegistered()
        {
            bool gummyVisualDone =
                _gummyMixVisualApplied ||
                _gummyVisualAttempts >= MaxGummyVisualAttempts;

            if (_mollyIngredientsRegistered &&
                _gummyIngredientsRegistered &&
                _gummyMixRegistered &&
                _sugarRegistered &&
                _mdmaRegistered &&
                _gummiesRegistered &&
                _dmtRegistered &&
                _cartRegistered &&
                _brownieRegistered &&
                _cookieRegistered &&
                _salviaRegistered &&
                _salviaSeedRegistered &&
                _mdmaMetadataRegistered &&
                _gummiesMetadataRegistered &&
                _dmtMetadataRegistered &&
                _cartMetadataRegistered &&
                _brownieMetadataRegistered &&
                _cookieMetadataRegistered &&
                _salviaMetadataRegistered &&
                _xanaxRegistered &&
                _xanaxMetadataRegistered &&
                _xanaxPowderRegistered &&
                gummyVisualDone)
            {
                _allRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] All registration complete. Retry loop stopped.");
            }
        }

        public override void OnUpdate()
        {
            AdminMenu.Update();

            CustomNPCExample.Utils.WvcIconGuard.Update();

            CustomNPCExample.Qol.WvcExceptionGuards.Update();

            if (_mollyIngredientsRegistered)
            {
                SupplierPricingManager.Initialize();
                SupplierPricingManager.Update();
            }

            if (!_allRegistered)
            {
                _retryTimer += Time.deltaTime;

                if (_retryTimer >= 1.0f)
                {
                    _retryTimer = 0f;

                    RegisterIngredients();
                    RegisterGummyMixVisual();
                    RegisterProducts();
                    RegisterMetadata();
                    CheckAllRegistered();
                }
            }

            UpdateShops();

            FlashlightBoost.OnUpdate();

            ChemistryStationPatch.Update();

            MDMAEffectManager.Update();

            MDMAEyeEffect.UpdateEyeEffect();

            MDMANpcLoveEyes.Update();

            THCGummyEffectManager.Update();

            DMTEffectManager.Update();

            BrownieEffectManager.Update();

            THCCookieEffectManager.Update();

            VapeCartEffectManager.Update();

            SalviaEffectManager.Update();

            XanaxEffectManager.Update();

            CustomNPCExample.NPCs.RoscoeBulkMeetupManager.Update();
            CustomNPCExample.NPCs.SalViahBulkMeetupManager.Update();
            CustomNPCExample.NPCs.DamonTreyBulkMeetupManager.Update();
            CustomNPCExample.NPCs.PillVilleBulkMeetupManager.Update();

            CustomNPCExample.NPCs.OffshoreSalesManager.Update();
            CustomNPCExample.NPCs.OffshoreHeat.Update();

            DMT.UpdateIconRepair();
            THCGummies.UpdateIconRepair();
            MDMA.UpdateIconRepair();
            Brownie.UpdateIconRepair();
            THCCookie.UpdateIconRepair();
            CustomNPCExample.Products.Xanax.UpdateIconRepair();
            CustomNPCExample.Products.XanaxPowder.UpdateIconRepair();

            CustomNPCExample.Weather.WvcSnowWeather.Update();

            CustomNPCExample.UI.WvcSettingsTab.Tick();

            MainMenuWatermark.Update();

            CustomNPCExample.UI.WvcMinimap.Update();

            SnitchQuestManager.Update();

            if (_activityScanTimer > 0f)
            {
                _activityScanTimer -= Time.unscaledDeltaTime;

                if (_activityScanTimer <= 0f)
                {
                    CustomNPCExample.NPCs.NpcActivityCache.ScanMainScene();
                }
            }

        }

        public override void OnGUI()
        {
            // The watermark's own visibility check already runs every frame from OnUpdate, and OnGUI runs
            // more than once per frame on its own, so calling it from both made its timer run down at
            // several times the rate it was written for.
            AdminMenu.Draw();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ChemistryStationPatch.RequestScan(3f);

            if (sceneName == "Main")
                _activityScanTimer = 5f;
        }

        private void RegisterIngredients()
        {
            if (!_mollyIngredientsRegistered && MollyIngredients.TryRegister())
            {
                _mollyIngredientsRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Safrole Oil and PMK Powder registered.");
            }

            if (!_gummyIngredientsRegistered && GummyIngredients.TryRegister())
            {
                _gummyIngredientsRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] THC Oil and Gelatin registered.");
            }

            if (!_gummyMixRegistered && UnbakedGummyMix.TryRegister())
            {
                _gummyMixRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Unbaked Gummy Mix registered.");
            }

            if (!_sugarRegistered && Sugar.TryRegister())
            {
                _sugarRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Infused Sugar registered.");
            }

            if (!_brownieIngredientsRegistered && BrownieIngredients.TryRegister())
            {
                _brownieIngredientsRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Brownie ingredients registered.");
            }

            if (_brownieIngredientsRegistered &&
                !_brownieMixRegistered &&
                UnbakedBrownieMix.TryRegister())
            {
                _brownieMixRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Unbaked Brownie Mix registered.");
            }

            if (!_cookieIngredientsRegistered && CookieIngredients.TryRegister())
            {
                _cookieIngredientsRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Cookie ingredients registered.");
            }

            // The powder is a plain stock item (not a product), so it registers with the rest of
            // the ingredients.
            if (!_xanaxPowderRegistered && XanaxPowder.TryRegister())
            {
                _xanaxPowderRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Xanax Powder registered.");
            }

            // The press keeps the name the game gives it ("Brick Press"), so nothing here renames it.

            if (_cookieIngredientsRegistered && !_cookieDoughRegistered && UnbakedCookieDough.TryRegister())
            {
                _cookieDoughRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Unbaked Cookie Dough registered.");
            }
        }

        private void RegisterGummyMixVisual()
        {
            if (!_gummyMixRegistered ||
                _gummyMixVisualApplied ||
                _gummyVisualAttempts >= MaxGummyVisualAttempts)
            {
                return;
            }

            _gummyVisualAttempts++;

            try
            {
                if (UnbakedGummyMixVisual.Apply())
                {
                    _gummyMixVisualApplied = true;
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Unbaked Gummy Mix visual applied.");
                }
            }
            catch (Exception)
            {

            }
        }

        private void RegisterProducts()
        {
            if (_mollyIngredientsRegistered &&
                !_mdmaRegistered &&
                MDMA.TryRegister())
            {
                _mdmaRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] MDMA registered.");
            }

            if (_gummyIngredientsRegistered &&
                !_gummiesRegistered &&
                THCGummies.TryRegister())
            {
                _gummiesRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] THC Gummies registered.");
            }

            if (!_cartRegistered &&
                VapeCartProduct.TryRegister())
            {
                _cartRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Vape Cart product registered and discovered.");
            }

            if (!Salvia.IsBuilt)
            {
                Salvia.TryEnsureBuilt();
            }

            if (!_dmtRegistered)
            {
                DMTIngredients.TryRegister();
                DMTIntermediates.TryRegister();

                if (DMT.TryRegister())
                {
                    _dmtRegistered = true;
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] DMT registered.");
                }
            }

            if (!Salvia.IsBuilt)
            {
                Salvia.TryEnsureBuilt();
            }

            if (Salvia.IsBuilt &&
                !_salviaSeedRegistered &&
                SalviaSeed.TryRegister())
            {
                _salviaSeedRegistered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Salvia seed registered.");
            }

            if (!_salviaRegistered &&
                Salvia.TryRegister())
            {
                _salviaRegistered = true;

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Salvia registered.");
            }

            if (!_brownieRegistered &&
                Brownie.TryRegister())
            {
                _brownieRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Brownie registered and discovered.");
            }

            if (!_cookieRegistered && THCCookie.TryRegister())
            {
                _cookieRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] THC Cookie registered.");
            }

            if (!_xanaxRegistered && Xanax.TryRegister())
            {
                _xanaxRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Xanax registered.");
            }
        }

        private void RegisterMetadata()
        {
            if (_mdmaRegistered &&
                !_mdmaMetadataRegistered &&
                MDMA.TryRegisterMetadata())
            {
                _mdmaMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] MDMA metadata complete.");
            }

            if (_gummiesRegistered &&
                !_gummiesMetadataRegistered &&
                THCGummies.TryRegisterMetadata())
            {
                _gummiesMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Gummies metadata complete.");
            }

            if (_dmtRegistered &&
                !_dmtMetadataRegistered &&
                DMT.TryRegisterMetadata())
            {
                _dmtMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] DMT metadata complete.");
            }

            if (_cartRegistered &&
                !_cartMetadataRegistered &&
                VapeCartProduct.TryRegisterMetadata())
            {
                _cartMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Vape Cart metadata complete.");
            }

            if (_brownieRegistered &&
                !_brownieMetadataRegistered &&
                Brownie.TryRegisterMetadata())
            {
                _brownieMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Brownie metadata complete.");
            }

            if (_cookieRegistered && !_cookieMetadataRegistered && THCCookie.TryRegisterMetadata())
            {
                _cookieMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Cookie metadata complete.");
            }

            if (_salviaRegistered &&
                !_salviaMetadataRegistered &&
                Salvia.TryRegisterMetadata())
            {
                _salviaMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Salvia metadata complete.");
            }

            if (_xanaxRegistered &&
                !_xanaxMetadataRegistered &&
                Xanax.TryRegisterMetadata())
            {
                _xanaxMetadataRegistered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC] Xanax metadata complete.");
            }
        }

        private void UpdateShops()
        {
            if (!_sugarRegistered ||
                _sugarInShops ||
                _shopAttempts >= MaxShopAttempts)
            {
                return;
            }

            _shopTimer += Time.deltaTime;

            if (_shopTimer >= 3f)
            {
                _shopTimer = 0f;
                _shopAttempts++;

                if (Sugar.TryAddToGasMarts())
                {
                    _sugarInShops = true;
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Sugar] Shop injection succeeded.");
                }
            }
        }

    }
}
