using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomNPCExample.Products
{
    public sealed class VapeCartScreenEffect : MonoBehaviour
    {
        public static float EffectDuration = 90f;
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
        private FilmGrain _grain;

        private bool _setupOk;

        private static readonly Color VaporWhite =
            new Color(0.94f, 0.96f, 1.00f);

        private static readonly Color HazeBlue =
            new Color(0.72f, 0.80f, 0.92f);

        private static readonly Color FogGrey =
            new Color(0.42f, 0.46f, 0.54f);

        public VapeCartScreenEffect(IntPtr pointer)
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
                    "[Cart Effect] Vapor buzz post-processing ready."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Cart Effect] Volume setup failed: " + ex
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
            if (_active)
            {
                _timeRemaining =
                    Mathf.Min(
                        _timeRemaining + EffectDuration * 0.50f,
                        EffectDuration * 2.0f
                    );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Cart Effect] Hit stacked. Remaining: " +
                    _timeRemaining.ToString("0.0") + "s"
                );

                return;
            }

            _timeRemaining = EffectDuration;
            _pulseTimer = 0f;
            _active = true;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"[Cart Effect] Vapor buzz started. Duration: {EffectDuration}s"
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

            global::CustomNPCExample.Utils.WvcLog.Msg("[Cart Effect] Vapor buzz ended.");
        }

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            GameObject go = new GameObject("WVC_Cart_PostProcess");
            UnityEngine.Object.DontDestroyOnLoad(go);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 9480f;
            _volume.weight = 0f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _profile;

            _colorAdjust = _profile.Add<ColorAdjustments>(true);
            _bloom = _profile.Add<Bloom>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _lens = _profile.Add<LensDistortion>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);

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
            _bloom.threshold.value = 0.70f;
            _bloom.scatter.value = 0.82f;

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
            _vignette.smoothness.value = 0.82f;

            _grain.active = true;
            _grain.intensity.overrideState = true;
            _grain.response.overrideState = true;
            _grain.type.overrideState = true;
            _grain.type.value = FilmGrainLookup.Thin2;
            _grain.response.value = 0.70f;

            _volume.enabled = false;
        }

        private void ApplyEffects()
        {
            float elapsed = EffectDuration - _timeRemaining;

            float fadeIn = Mathf.Clamp01(elapsed / 4f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 16f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            float drift =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.045f) * 0.5f;

            float swell =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.09f + 1.7f) * 0.5f;

            _volume.weight = fade;

            _colorAdjust.saturation.value =
                (-22f + drift * 16f) * fade;

            _colorAdjust.contrast.value =
                (-6f + swell * 8f) * fade;

            _colorAdjust.postExposure.value =
                (0.10f + drift * 0.16f) * fade;

            Color sober = Color.white;
            Color clouded = Color.Lerp(VaporWhite, HazeBlue, drift * 0.55f);
            _colorAdjust.colorFilter.value =
                Color.Lerp(sober, clouded, fade * 0.60f);

            _colorAdjust.hueShift.value = 0f;

            _bloom.intensity.value =
                (0.80f + drift * 1.30f) * fade;
            _bloom.tint.value =
                Color.Lerp(VaporWhite, HazeBlue, 0.35f + drift * 0.25f);

            _chromatic.intensity.value =
                (0.05f + drift * 0.20f) * fade;

            float exhale =
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.06f);

            _lens.intensity.value =
                (0.03f + exhale * 0.08f) * fade;

            _lens.scale.value =
                1f + exhale * 0.035f * fade;

            _vignette.intensity.value =
                (0.22f + drift * 0.22f) * fade;
            _vignette.color.value = FogGrey;

            _grain.intensity.value =
                (0.03f + swell * 0.04f) * fade;
        }
    }
}
