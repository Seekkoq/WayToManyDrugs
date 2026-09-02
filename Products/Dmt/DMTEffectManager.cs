using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class DMTEffectManager
    {
        private static GameObject _controllerObject;
        private static DMTScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_DMT_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _effect =
                _controllerObject.AddComponent<DMTScreenEffect>();

            MelonLogger.Msg("[DMT Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();
            DMTScreenEffect.ShouldStart = true;
            MelonLogger.Msg("[DMT Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();
            DMTScreenEffect.ShouldStop = true;
            MelonLogger.Msg("[DMT Effect] Forced stop requested.");
        }
    }
}