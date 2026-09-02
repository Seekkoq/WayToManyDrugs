    using CustomNPCExample.UI;
    using CustomNPCExample.Law;
    using CustomNPCExample.NPCs;
    using CustomNPCExample.Products;
    using CustomNPCExample.Products.Cauldrons;
    using CustomNPCExample.Products.Chemistry;
    using CustomNPCExample.Products.Edibles;
    using CustomNPCExample.Products.Ovens;
    using CustomNPCExample.Quests;
using Il2CppInterop.Runtime.Injection;
    using MelonLoader;
    using System;
    using UnityEngine;
using CustomNPCExample.QoL;
using HarmonyInstance = HarmonyLib.Harmony;

    [ assembly: MelonInfo(
        typeof(CustomNPCMod.MainMod),
        "WayToManyDrugs",
        "3.5.0",
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

            private float _retryTimer;
            private float _shopTimer;
            private bool _allRegistered;

            private int _gummyVisualAttempts;
            private const int MaxGummyVisualAttempts = 20;

            private int _shopAttempts;
            private const int MaxShopAttempts = 40;

            private HarmonyInstance _snitchStoryHarmony;

            public override void OnInitializeMelon()
            {
                MelonLogger.Msg("[WVC] Westville Connection 3.4.1 initializing.");

                RegisterIl2CppTypes();

                // Single Harmony instance for attribute-based patches,
                // including the Snitch story save patches.
                _snitchStoryHarmony = new HarmonyInstance("wvc.snitch.story");
                _snitchStoryHarmony.PatchAll();

                // Manual patch installers.
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
                CookieOvenPatch.ApplyPatch();
                CustomNPCExample.NPCs.RoscoeBulkMeetupManager.ApplyPatch();

            FlashlightBoost.Initialize(_snitchStoryHarmony);
            WvcRecipesPhoneApp.Initialize();

            MelonLogger.Msg("[WVC] Initialization complete.");
            }

            private static void RegisterIl2CppTypes()
            {
                RegisterIl2CppType<VapeCartUseBehaviour>(
                    "VapeCartUseBehaviour");

                RegisterIl2CppType<THCGummyScreenEffect>(
                    "THCGummyScreenEffect");

                RegisterIl2CppType<DMTScreenEffect>(
                    "DMTScreenEffect");

                RegisterIl2CppType<MDMAScreenEffect>(
                    "MDMAScreenEffect");

                RegisterIl2CppType<BrownieScreenEffect>(
                    "BrownieScreenEffect");

                RegisterIl2CppType<THCCookieScreenEffect>(
                    "THCCookieScreenEffect");

                RegisterIl2CppType<VapeCartScreenEffect>(
                    "VapeCartScreenEffect");
            }

            private static void RegisterIl2CppType<T>(string typeName)
                where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
            {
                try
                {
                    ClassInjector.RegisterTypeInIl2Cpp<T>();

                    MelonLogger.Msg(
                        "[WVC] " + typeName + " registered.");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[WVC] " + typeName +
                        " registration warning: " +
                        ex.Message);
                }
            }

            private static void RegisterTypeSafely<T>(string name) where T : class
            {
                try
                {
                    if (!ClassInjector.IsTypeRegisteredInIl2Cpp<T>())
                    {
                        ClassInjector.RegisterTypeInIl2Cpp<T>();
                        MelonLogger.Msg($"[WVC] {name} registered in IL2CPP.");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[WVC] {name} registration warning: {ex.Message}");
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
                    _mdmaMetadataRegistered &&
                    _gummiesMetadataRegistered &&
                    _dmtMetadataRegistered &&
                    _cartMetadataRegistered &&
                    _brownieMetadataRegistered &&
                    gummyVisualDone)
                {
                    _allRegistered = true;

                    MelonLogger.Msg(
                        "[WVC] All registration complete. Retry loop stopped."
                    );
                }
            }

            public override void OnUpdate()
            {
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
                HandleDebugKeys();

            FlashlightBoost.OnUpdate();

                MDMAEffectManager.Update();
                MDMAEyeEffect.UpdateEyeEffect();
                MDMANpcLoveEyes.Update();
                THCGummyEffectManager.Update();
                DMTEffectManager.Update();
                BrownieEffectManager.Update();
                THCCookieEffectManager.Update();
                VapeCartEffectManager.Update();
                ChemistryStationPatch.Update();
                CustomNPCExample.Weather.WvcSnowWeather.Update();
                CustomNPCExample.NPCs.RoscoeBulkMeetupManager.Update();
                MainMenuWatermark.Update();

                SnitchQuestManager.Update();
            }

            public override void OnGUI()
            {
                CustomNPCExample.UI.MainMenuWatermark.Update();
            }

            private void RegisterIngredients()
            {
                if (!_mollyIngredientsRegistered && MollyIngredients.TryRegister())
                {
                    _mollyIngredientsRegistered = true;
                    MelonLogger.Msg("[WVC] Safrole Oil and PMK Powder registered.");
                }

                if (!_gummyIngredientsRegistered && GummyIngredients.TryRegister())
                {
                    _gummyIngredientsRegistered = true;
                    MelonLogger.Msg("[WVC] THC Oil and Gelatin registered.");
                }

                if (!_gummyMixRegistered && UnbakedGummyMix.TryRegister())
                {
                    _gummyMixRegistered = true;
                    MelonLogger.Msg("[WVC] Unbaked Gummy Mix registered.");
                }

                if (!_sugarRegistered && Sugar.TryRegister())
                {
                    _sugarRegistered = true;
                    MelonLogger.Msg("[WVC] Infused Sugar registered.");
                }

                if (!_brownieIngredientsRegistered && BrownieIngredients.TryRegister())
                {
                    _brownieIngredientsRegistered = true;
                    MelonLogger.Msg("[WVC] Brownie ingredients registered.");
                }

                if (_brownieIngredientsRegistered &&
                    !_brownieMixRegistered &&
                    UnbakedBrownieMix.TryRegister())
                {
                    _brownieMixRegistered = true;
                    MelonLogger.Msg("[WVC] Unbaked Brownie Mix registered.");
                }

                if (!_cookieIngredientsRegistered && CookieIngredients.TryRegister())
                {
                    _cookieIngredientsRegistered = true;
                    MelonLogger.Msg("[WVC] Cookie ingredients registered.");
                }

                if (_cookieIngredientsRegistered && !_cookieDoughRegistered && UnbakedCookieDough.TryRegister())
                {
                    _cookieDoughRegistered = true;
                    MelonLogger.Msg("[WVC] Unbaked Cookie Dough registered.");
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
                        MelonLogger.Msg("[WVC] Unbaked Gummy Mix visual applied.");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        $"[WVC] Gummy Mix visual attempt {_gummyVisualAttempts} failed: {ex.Message}"
                    );
                }
            }

            private void RegisterProducts()
            {
                if (_mollyIngredientsRegistered &&
                    !_mdmaRegistered &&
                    MDMA.TryRegister())
                {
                    _mdmaRegistered = true;
                    MelonLogger.Msg("[WVC] MDMA registered.");
                }

                if (_gummyIngredientsRegistered &&
                    !_gummiesRegistered &&
                    THCGummies.TryRegister())
                {
                    _gummiesRegistered = true;
                    MelonLogger.Msg("[WVC] THC Gummies registered.");
                }

                if (!_cartRegistered &&
                    VapeCartProduct.TryRegister())
                {
                    _cartRegistered = true;
                    MelonLogger.Msg("[WVC] Vape Cart product registered and discovered.");
                }

                DMTIngredients.TryRegister();
                DMTIntermediates.TryRegister();

                if (!_dmtRegistered &&
                    DMT.TryRegister())
                {
                    _dmtRegistered = true;
                    MelonLogger.Msg("[WVC] DMT registered.");
                }

                if (!_brownieRegistered &&
                    Brownie.TryRegister())
                {
                    _brownieRegistered = true;
                    MelonLogger.Msg("[WVC] Brownie registered and discovered.");
                }

                if (!_cookieRegistered && THCCookie.TryRegister())
                {
                    _cookieRegistered = true;
                    MelonLogger.Msg("[WVC] THC Cookie registered.");
                }
            }

            private void RegisterMetadata()
            {
                if (_mdmaRegistered &&
                    !_mdmaMetadataRegistered &&
                    MDMA.TryRegisterMetadata())
                {
                    _mdmaMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] MDMA metadata complete.");
                }

                if (_gummiesRegistered &&
                    !_gummiesMetadataRegistered &&
                    THCGummies.TryRegisterMetadata())
                {
                    _gummiesMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] Gummies metadata complete.");
                }

                if (_dmtRegistered &&
                    !_dmtMetadataRegistered &&
                    DMT.TryRegisterMetadata())
                {
                    _dmtMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] DMT metadata complete.");
                }

                if (_cartRegistered &&
                    !_cartMetadataRegistered &&
                    VapeCartProduct.TryRegisterMetadata())
                {
                    _cartMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] Vape Cart metadata complete.");
                }

                if (_brownieRegistered &&
                    !_brownieMetadataRegistered &&
                    Brownie.TryRegisterMetadata())
                {
                    _brownieMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] Brownie metadata complete.");
                }

                if (_cookieRegistered && !_cookieMetadataRegistered && THCCookie.TryRegisterMetadata())
                {
                    _cookieMetadataRegistered = true;
                    MelonLogger.Msg("[WVC] Cookie metadata complete.");
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
                        MelonLogger.Msg("[WVC Sugar] Shop injection succeeded.");
                    }
                }
            }

            private void HandleDebugKeys()
            {
            }
        }
    }