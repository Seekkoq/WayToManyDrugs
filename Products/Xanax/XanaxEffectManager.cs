using UnityEngine;

using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Owns the Xanax high and the dose ladder. Every bar consumed while the high is live raises the
    /// dose, and the dose decays back to zero once the effect has worn off.
    /// </summary>
    public static class XanaxEffectManager
    {
        private static GameObject _controllerObject;
        private static XanaxScreenEffect _effect;

        private static int _dose;
        private static float _lastDoseTime = -999f;
        private static bool _failureLogged;

        public static bool IsActive => _effect != null && _effect.IsActive;

        public static int Dose => _dose;

        public static string StageName =>
            _effect != null ? _effect.StageName : "idle";

        public static void Update()
        {
            try
            {
                EnsureEffectController();

                // Once the high has fully ended the ladder goes back to zero, so the next bar is
                // Dose 1 instead of clamping at the cap from a session that has already passed. A
                // bar taken while the curve is merely winding down is handled in TriggerEffect,
                // because that decision has to be made before the ladder is incremented.
                if (_dose > 0 && _effect != null && !_effect.IsActive)
                {
                    _dose = 0;
                    _lastDoseTime = -999f;

                    WvcLog.Msg("[Xanax Effect] High ended; the dose ladder went back to zero.");
                }
            }
            catch (System.Exception ex)
            {
                LogFailure("update", ex);
            }
        }

        /// <summary>
        /// Creates the host object and its component once. <c>GetComponent&lt;T&gt;()</c> is
        /// deliberately avoided: the interop generic MethodInfo store for it can fail during load
        /// and take the whole per-frame update chain down with it. The component is only ever added
        /// to an object this class created, so there is nothing else on it to look up.
        /// </summary>
        private static void EnsureEffectController()
        {
            if (_controllerObject != null && _effect != null)
                return;

            if (_controllerObject == null)
            {
                _controllerObject = new GameObject("WVC_Xanax_EffectController");

                UnityEngine.Object.DontDestroyOnLoad(_controllerObject);
            }

            if (_effect == null)
            {
                _effect = _controllerObject.AddComponent<XanaxScreenEffect>();


            }
        }

        private static void LogFailure(string stage, System.Exception ex)
        {
            if (_failureLogged)
                return;

            _failureLogged = true;

            MelonLoader.MelonLogger.Error(
                "[Xanax Effect] " + stage + " failed: " + ex
            );
        }

        /// <summary>
        /// One more bar. Bars taken while the previous one is still working stack into a stronger
        /// effect instead of resetting it.
        /// </summary>
        public static void TriggerEffect()
        {
            EnsureEffectController();

            bool active = _effect != null && _effect.IsActive;
            bool windingDown = active && _effect.IsWindingDown;

            // A bar eaten once the previous high is on its way out is a new high, not a top-up:
            // the ladder restarts so the curve actually plays again from Come-Up.
            if (!active ||
                windingDown ||
                UnityEngine.Time.time - _lastDoseTime > XanaxScreenEffect.DoseWindowSeconds)
            {
                _dose = 0;
            }

            _dose = Mathf.Clamp(_dose + 1, 1, XanaxScreenEffect.MaxDoses);
            _lastDoseTime = UnityEngine.Time.time;

            XanaxScreenEffect.PendingDose = _dose;
            XanaxScreenEffect.ShouldStart = true;


        }

        public static void StopEffect()
        {
            EnsureEffectController();

            _dose = 0;
            _lastDoseTime = -999f;

            XanaxScreenEffect.ShouldStart = false;
            XanaxScreenEffect.ShouldStop = true;

            if (_effect != null)
                _effect.StopImmediately();


        }
    }
}
