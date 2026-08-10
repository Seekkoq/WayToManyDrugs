using System;
using CustomNPCExample.NPCs;
using CustomNPCExample.Products;
using Il2CppInterop.Runtime.Injection;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(
    typeof(CustomNPCMod.MainMod),
    "Westville Connection",
    "2.0.0",
    "0.Seek"
)]

[assembly: MelonGame("TVGS", "Schedule I")]

namespace CustomNPCMod
{
    public class MainMod : MelonMod
    {
        private bool _productsRegistered;
        private bool _metadataRegistered;
        private bool _cauldronsPrepared;

        private float _retryTimer;
        private float _cauldronScanTimer;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg(
                "Westville Connection v2 initialized."
            );

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MDMAScreenEffect>();

                MelonLogger.Msg(
                    "[Westville Connection] MDMAScreenEffect registered."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Westville Connection] MDMAScreenEffect registration warning: " +
                    ex.Message
                );
            }

            // Apply production/consumption patches early.
            MDMAConsumptionPatch.ApplyPatch();
            MollyCauldronPatch.ApplyPatch();

            /*
             * Testing mode:
             * every loaded cauldron can use Safrole Oil + PMK Powder.
             */
            MollyCauldronPatch
                .TreatEveryCauldronAsMollyForTesting = true;

            MelonLogger.Msg(
                "[MollyCauldron] Initialization complete."
            );
        }

        public override void OnUpdate()
        {
            PrepareLoadedCauldrons();

            // F10: Rescan and mark currently loaded cauldrons.
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _cauldronsPrepared = false;
                _cauldronScanTimer = 0f;

                ScanAndMarkCauldrons();

                MelonLogger.Msg(
                    "[MollyCauldron] Manual cauldron rescan triggered."
                );
            }

            // F11: Print ingredient console commands.
            if (Input.GetKeyDown(KeyCode.F11))
            {
                WvcDebugItems.PrintMollyIngredientCommands(10);
            }

            // F12: Print cauldron/output state.
            if (Input.GetKeyDown(KeyCode.F12))
            {
                MollyCauldronPatch.DumpCauldronState();
            }

            // Product/effect updates.
            MDMAEffectManager.Update();
            MDMAEyeEffect.UpdateEyeEffect();
            MDMANpcLoveEyes.Update();

            // Product registration retry loop.
            _retryTimer += Time.deltaTime;

            if (_retryTimer < 0.5f)
                return;

            _retryTimer = 0f;

            if (!_productsRegistered)
            {
                bool ingredientsReady =
                    MollyIngredients.TryRegister();

                bool mdmaReady =
                    MDMA.TryRegister();

                if (ingredientsReady && mdmaReady)
                {
                    _productsRegistered = true;

                    MelonLogger.Msg(
                        "[Westville Connection] MDMA and Molly precursors registered."
                    );

                    // Products are ready: scan cauldrons again.
                    _cauldronsPrepared = false;
                    _cauldronScanTimer = 0f;
                }

                return;
            }

            if (!_metadataRegistered)
            {
                if (MDMA.TryRegisterMetadata())
                {
                    _metadataRegistered = true;

                    MelonLogger.Msg(
                        "[Westville Connection] Product Manager metadata complete."
                    );
                }
            }
        }

        private void PrepareLoadedCauldrons()
        {
            if (_cauldronsPrepared)
                return;

            _cauldronScanTimer += Time.deltaTime;

            if (_cauldronScanTimer < 2f)
                return;

            _cauldronScanTimer = 0f;

            ScanAndMarkCauldrons();
        }

        private void ScanAndMarkCauldrons()
        {
            try
            {
                Cauldron[] cauldrons =
                    UnityEngine.Object.FindObjectsOfType<Cauldron>(
                        true
                    );

                if (cauldrons == null ||
                    cauldrons.Length == 0)
                {
                    // World has not finished loading; retry later.
                    return;
                }

                int marked = 0;

                foreach (Cauldron cauldron in cauldrons)
                {
                    if (cauldron == null)
                        continue;

                    MollyCauldronPatch.MarkAsCustomMollyCauldron(
                        cauldron
                    );

                    marked++;
                }

                if (marked > 0)
                {
                    _cauldronsPrepared = true;

                    MelonLogger.Msg(
                        "[MollyCauldron] Cauldron scan complete. " +
                        $"Registered {marked} cauldron(s)."
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[MollyCauldron] Cauldron scan failed: " +
                    ex.Message
                );
            }
        }
    }
}