using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    public static class THCCookieEffectManager
    {
        private static GameObject _controllerObject;
        private static THCCookieScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_Cookie_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _effect =
                _controllerObject.AddComponent<THCCookieScreenEffect>();

            MelonLogger.Msg("[Cookie Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            THCCookieScreenEffect.ShouldStart = true;

            MelonLogger.Msg("[Cookie Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            THCCookieScreenEffect.ShouldStop = true;

            MelonLogger.Msg("[Cookie Effect] Forced stop requested.");
        }
    }
}
