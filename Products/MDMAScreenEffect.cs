using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomNPCExample.Products
{
    public sealed class MDMAScreenEffect : MonoBehaviour
    {
        public static float EffectDuration = 180f;
        public static bool ShouldStart;
        public static bool ShouldStop;

        private float _timeRemaining;
        private float _pulseTimer;
        private bool _active;

        private Volume _volume;
        private VolumeProfile _profile;

        private ColorAdjustments _colorAdjust;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private LensDistortion _lens;
        private Vignette _vignette;

        private bool _setupOk;

        public MDMAScreenEffect(IntPtr pointer)
            : base(pointer)
        {
        }

        private void Awake()
        {
            try
            {
                CreateVolume();
                _setupOk = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[MDMA Effect] Post-processing volume ready."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[MDMA Effect] Volume setup failed: " + ex
                );
            }
        }

        private void Update()
        {
            if (!_setupOk)
                return;

            if (ShouldStop)
            {
                ShouldStop = false;
                StopEffect();
                return;
            }

            if (ShouldStart)
            {
                ShouldStart = false;
                StartEffect();
            }

            if (!_active)
                return;

            _timeRemaining -= Time.deltaTime;
            _pulseTimer += Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                StopEffect();
                return;
            }

            ApplyEffects();
        }

        private void StartEffect()
        {
            _timeRemaining = EffectDuration;
            _pulseTimer = 0f;
            _active = true;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"[MDMA Effect] Euphoria started. Duration: {EffectDuration}s"
            );
        }

        private void StopEffect()
        {
            _active = false;
            _timeRemaining = 0f;
            _pulseTimer = 0f;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }

            MDMAEyeEffect.StopEyeEffect();
            MDMANpcLoveEyes.Stop();

            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Effect] Euphoria ended.");
        }

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            GameObject go = new GameObject("WVC_MDMA_PostProcess");
            UnityEngine.Object.DontDestroyOnLoad(go);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 9999f;
            _volume.weight = 0f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _profile;

            _colorAdjust = _profile.Add<ColorAdjustments>(true);
            _bloom = _profile.Add<Bloom>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _lens = _profile.Add<LensDistortion>(true);
            _vignette = _profile.Add<Vignette>(true);

            _colorAdjust.active = true;
            _colorAdjust.saturation.overrideState = true;
            _colorAdjust.contrast.overrideState = true;
            _colorAdjust.colorFilter.overrideState = true;
            _colorAdjust.postExposure.overrideState = true;
            _colorAdjust.hueShift.overrideState = true;

            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            _bloom.scatter.overrideState = true;
            _bloom.tint.overrideState = true;
            _bloom.threshold.value = 0.8f;
            _bloom.scatter.value = 0.8f;

            _chromatic.active = true;
            _chromatic.intensity.overrideState = true;

            _lens.active = true;
            _lens.intensity.overrideState = true;
            _lens.scale.overrideState = true;
            _lens.scale.value = 1f;

            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.color.overrideState = true;
            _vignette.smoothness.value = 0.9f;

            _volume.enabled = false;
        }

        private void ApplyEffects()
        {
            float elapsed = EffectDuration - _timeRemaining;

            float fadeIn = Mathf.Clamp01(elapsed / 5f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 25f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            float pulse =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.65f) * 0.5f;

            _volume.weight = fade;

            _colorAdjust.saturation.value = (35f + pulse * 15f) * fade;
            _colorAdjust.contrast.value = 12f * fade;
            _colorAdjust.postExposure.value = (0.15f + pulse * 0.15f) * fade;

            _colorAdjust.colorFilter.value = Color.Lerp(
                Color.white,
                new Color(1.0f, 0.68f, 0.85f),
                fade
            );

            _colorAdjust.hueShift.value =
                Mathf.Sin(_pulseTimer * 0.35f) * 7f * fade;

            _bloom.intensity.value = (1.5f + pulse * 1.0f) * fade;
            _bloom.tint.value = new Color(1.0f, 0.55f, 0.80f);

            _chromatic.intensity.value = (0.20f + pulse * 0.40f) * fade;

            _lens.intensity.value =
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.30f) * 0.16f * fade;

            _vignette.intensity.value = (0.28f + pulse * 0.12f) * fade;
            _vignette.color.value = new Color(0.45f, 0.05f, 0.55f);
        }
    }
}