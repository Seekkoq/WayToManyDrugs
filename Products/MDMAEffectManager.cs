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

            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            MDMAScreenEffect.ShouldStart = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            MDMAScreenEffect.ShouldStop = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Effect] Forced stop requested.");
        }
    }
}