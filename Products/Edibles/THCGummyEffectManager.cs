using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    public static class THCGummyEffectManager
    {
        private static GameObject _controllerObject;
        private static THCGummyScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_Gummy_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _effect =
                _controllerObject.AddComponent<THCGummyScreenEffect>();

            global::CustomNPCExample.Utils.WvcLog.Msg("[Gummy Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            THCGummyScreenEffect.ShouldStart = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[Gummy Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            THCGummyScreenEffect.ShouldStop = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[Gummy Effect] Forced stop requested.");
        }
    }
}