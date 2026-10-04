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
                global::CustomNPCExample.Utils.WvcLog.Msg("[Gummy Effect] Post-processing volume ready.");
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

            global::CustomNPCExample.Utils.WvcLog.Msg($"[Gummy Effect] Edible high started. Duration: {EffectDuration}s");
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

            global::CustomNPCExample.Utils.WvcLog.Msg("[Gummy Effect] Edible high ended.");
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

            float fadeIn = Mathf.Clamp01(elapsed / 16f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 38f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            float breath =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.14f) * 0.5f;

            float wave =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.05f) * 0.5f;

            float wobble =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.27f) * 0.5f;

            _volume.weight = fade;

            _colorAdjust.saturation.value =
                (34f + breath * 16f + wobble * 10f) * fade;
            _colorAdjust.contrast.value = (6f + wave * 5f) * fade;
            _colorAdjust.postExposure.value =
                (0.08f + breath * 0.12f) * fade;

            Color sober = Color.white;
            Color candy = new Color(0.88f, 1.00f, 0.94f);
            _colorAdjust.colorFilter.value =
                Color.Lerp(sober, new Color(0.90f, 1.00f, 0.93f), fade * 0.50f);

            _colorAdjust.hueShift.value =
                Mathf.Sin(_pulseTimer * 0.13f) * 6f * fade;

            _bloom.intensity.value =
                (1.00f + breath * 0.55f + wobble * 0.35f) * fade;
            _bloom.tint.value = new Color(0.88f, 1.00f, 0.94f);

            _chromatic.intensity.value =
                (0.08f + wobble * 0.16f) * fade;

            _lens.intensity.value =
                (0.05f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.11f) * 0.06f) * fade;

            _lens.scale.value =
                1f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.06f) * 0.015f * fade;

            _vignette.intensity.value =
                (0.30f + breath * 0.12f + wobble * 0.05f) * fade;
            _vignette.color.value = new Color(0.04f, 0.06f, 0.05f);
        }
    }
}
