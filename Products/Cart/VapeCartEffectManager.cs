using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class VapeCartEffectManager
    {
        private static GameObject _controllerObject;
        private static VapeCartScreenEffect _effect;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            _controllerObject =
                new GameObject("WVC_Cart_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _effect =
                _controllerObject.AddComponent<VapeCartScreenEffect>();

            global::CustomNPCExample.Utils.WvcLog.Msg("[Cart Effect] Effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            VapeCartScreenEffect.ShouldStart = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[Cart Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            VapeCartScreenEffect.ShouldStop = true;

            global::CustomNPCExample.Utils.WvcLog.Msg("[Cart Effect] Forced stop requested.");
        }
    }
}
