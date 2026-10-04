using UnityEngine;
using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    public static class DMTEffectManager
    {
        private static GameObject _controllerObject;
        private static DMTScreenEffect _effect;

        public static bool IsActive =>
            _effect != null && _effect.IsActive;

        public static void Update()
        {
            EnsureEffectController();

            // The trip's whisper is read out of the mod's own resources the first time this runs rather
            // than on the first trip: the cost is then paid while nothing is happening, and a clip the
            // game will not take says so in the log before anybody consumes anything.
            DMTWhisper.Prime();
        }

        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            if (_controllerObject == null)
            {
                _controllerObject =
                    new GameObject("WVC_DMT_EffectController");

                UnityEngine.Object.DontDestroyOnLoad(
                    _controllerObject
                );
            }

            if (_effect == null)
            {
                _effect =
                    _controllerObject.GetComponent<DMTScreenEffect>();

                if (_effect == null)
                {
                    _effect =
                        _controllerObject.AddComponent<DMTScreenEffect>();
                }
            }

            WvcLog.Msg("[DMT Effect] Staged effect controller ready.");
        }

        public static void TriggerEffect()
        {
            EnsureEffectController();

            DMTScreenEffect.ShouldStop = false;
            DMTScreenEffect.ShouldStart = true;

            // The whisper is deliberately not started here. This call only asks for a trip; the trip
            // itself begins on the next frame, and it clears whatever ran before it on the way in - and
            // that clearing stops the whisper. Starting it here meant it was cut off a frame later, which
            // is the whole of what the player ever heard of it. It starts with the trip instead, in
            // DMTScreenEffect.StartOrRefresh.
            WvcLog.Msg("[DMT Effect] Trigger requested.");
        }

        public static void StopEffect()
        {
            DMTScreenEffect.ShouldStart = false;
            DMTScreenEffect.ShouldStop = true;

            if (_effect != null)
                _effect.StopImmediately();
        }
    }
}
