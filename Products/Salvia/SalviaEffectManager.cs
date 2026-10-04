using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class SalviaEffectManager
    {
        private static GameObject _controllerObject;
        private static SalviaScreenEffect _effect;

        public static bool IsActive =>
            _effect != null && _effect.IsActive;

        public static void Update()
        {
            EnsureEffectController();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null &&
                _effect != null)
            {
                return;
            }

            _controllerObject =
                new GameObject("WVC_Salvia_EffectController");

            UnityEngine.Object.DontDestroyOnLoad(
                _controllerObject
            );

            _effect =
                _controllerObject.AddComponent<SalviaScreenEffect>();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Salvia Effect] Effect controller ready."
            );
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            SalviaScreenEffect.ShouldStart = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Salvia Effect] Trigger requested."
            );
        }

        public static void StopEffect()
        {
            EnsureEffectController();

            SalviaScreenEffect.ShouldStop = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Salvia Effect] Forced stop requested."
            );
        }
    }
}