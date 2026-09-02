using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomNPCExample.Products.Edibles
{
    public sealed class THCGummyScreenEffect : MonoBehaviour
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
        private DepthOfField _depthOfField;

        private bool _setupOk;

        public THCGummyScreenEffect(IntPtr pointer) : base(pointer) { }

        private void Awake()
        {
            try
            {
                CreateVolume();
                _setupOk = true;
                MelonLogger.Msg("[Gummy Effect] Post-processing volume ready.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Gummy Effect] Volume setup failed: " + ex);
            }
        }

        private void Update()
        {
            if (!_setupOk) return;

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

            if (!_active) return;

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

            MelonLogger.Msg($"[Gummy Effect] Edible high started. Duration: {EffectDuration}s");
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

            MelonLogger.Msg("[Gummy Effect] Edible high ended.");
        }

        private void CreateVolume()
        {
            if (_volume != null) return;

            GameObject go = new GameObject("WVC_Gummy_PostProcess");
            UnityEngine.Object.DontDestroyOnLoad(go);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 9998f;
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
            _bloom.threshold.value = 0.72f;
            _bloom.scatter.value = 0.88f;

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
            _vignette.smoothness.value = 0.78f;

            _volume.enabled = false;
        }

        private void ApplyEffects()
        {
            float elapsed = EffectDuration - _timeRemaining;

            // Slow edible come-up / long hang
            float fadeIn = Mathf.Clamp01(elapsed / 16f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 38f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            // Lazy breathing
            float breath =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.14f) * 0.5f;

            // Occasional heavier wave
            float wave =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.05f) * 0.5f;

            _volume.weight = fade;

            // --------------------------------------------------
            // COLOR — pop, not green
            // Saturated, slightly warmer, like lights and food
            // looking extra vivid while stoned
            // --------------------------------------------------

            _colorAdjust.saturation.value = (28f + breath * 14f + wave * 8f) * fade;
            _colorAdjust.contrast.value = (8f + breath * 4f) * fade;
            _colorAdjust.postExposure.value = (0.10f + breath * 0.10f) * fade;

            // Warm cream, not green. Keeps whites/skins looking right.
            Color sober = Color.white;
            Color stoned = new Color(1.00f, 0.94f, 0.86f);
            _colorAdjust.colorFilter.value = Color.Lerp(sober, stoned, fade * 0.55f);

            _colorAdjust.hueShift.value =
                Mathf.Sin(_pulseTimer * 0.10f) * 3.5f * fade;

            // --------------------------------------------------
            // BLOOM — lights bloom, colors glow a bit
            // --------------------------------------------------
            _bloom.intensity.value = (0.85f + breath * 0.45f + wave * 0.25f) * fade;
            _bloom.tint.value = new Color(1.00f, 0.97f, 0.90f);

            // --------------------------------------------------
            // CHROMATIC — soft-focus fringe on waves
            // --------------------------------------------------
            _chromatic.intensity.value = (0.06f + wave * 0.12f) * fade;

            // --------------------------------------------------
            // LENS — heavy-headed float
            // --------------------------------------------------
            _lens.intensity.value =
                (0.035f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.09f) * 0.045f) * fade;

            // --------------------------------------------------
            // VIGNETTE — eyelids, dark brown not green
            // --------------------------------------------------
            _vignette.intensity.value = (0.32f + breath * 0.10f + wave * 0.06f) * fade;
            _vignette.color.value = new Color(0.06f, 0.05f, 0.04f);
        }
    }
}