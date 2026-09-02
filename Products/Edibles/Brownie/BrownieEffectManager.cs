using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class BrownieEffectManager
    {
        private static GameObject _controllerObject;
        private static BrownieScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_Brownie_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _effect =
                _controllerObject.AddComponent<BrownieScreenEffect>();

            MelonLogger.Msg("[Brownie Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            BrownieScreenEffect.ShouldStart = true;

            MelonLogger.Msg("[Brownie Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            BrownieScreenEffect.ShouldStop = true;

            MelonLogger.Msg("[Brownie Effect] Forced stop requested.");
        }
    }
}