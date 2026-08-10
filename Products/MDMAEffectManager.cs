using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class MDMAEffectManager
    {
        private static GameObject _controllerObject;
        private static MDMAScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
            WatchTestKey();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_MDMA_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(
                _controllerObject
            );

            _effect =
                _controllerObject.AddComponent<MDMAScreenEffect>();

            MelonLogger.Msg("[MDMA Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            MDMAScreenEffect.ShouldStart = true;

            MelonLogger.Msg("[MDMA Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            MDMAScreenEffect.ShouldStop = true;

            MelonLogger.Msg("[MDMA Effect] Forced stop requested.");
        }

        private static void WatchTestKey()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                MelonLogger.Msg("[MDMA Effect] Debug F8 key pressed.");
                TriggerEffect();
            }
        }
    }
}