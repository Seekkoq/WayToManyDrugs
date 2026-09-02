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

        // Cool vapor palette
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

                MelonLogger.Msg(
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
            // If already active, extend duration (stacking hits)
            if (_active)
            {
                _timeRemaining =
                    Mathf.Min(
                        _timeRemaining + EffectDuration * 0.50f,
                        EffectDuration * 2.0f
                    );

                MelonLogger.Msg(
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

            MelonLogger.Msg(
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

            MelonLogger.Msg("[Cart Effect] Vapor buzz ended.");
        }

        // ============================================================
        // Post-processing volume
        // ============================================================

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

        // ============================================================
        // Effect application — clean, airy, foggy vapor buzz
        // ============================================================

        private void ApplyEffects()
        {
            float elapsed = EffectDuration - _timeRemaining;

            // Fast onset (vapor hits quickly), smooth fade-out
            float fadeIn = Mathf.Clamp01(elapsed / 3f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 20f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            // Light floaty breathing
            float breath =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.20f) * 0.5f;

            // Slower wave for heavier effects
            float wave =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.08f) * 0.5f;

            // Quick flutter for light-headedness
            float flutter =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.55f) * 0.5f;

            _volume.weight = fade;

            // --------------------------------------------------
            // COLOR — slightly desaturated, hazy, cool
            // Like looking through a thin vape cloud
            // --------------------------------------------------

            _colorAdjust.saturation.value =
                (-6f + breath * 10f) * fade;

            _colorAdjust.contrast.value =
                (-4f + wave * 6f) * fade;

            _colorAdjust.postExposure.value =
                (0.12f + breath * 0.08f) * fade;

            // Cool, foggy color filter
            Color sober = Color.white;
            Color hazed = Color.Lerp(VaporWhite, HazeBlue, wave * 0.35f);
            _colorAdjust.colorFilter.value =
                Color.Lerp(sober, hazed, fade * 0.45f);

            // Minimal hue shift — just a gentle drift
            _colorAdjust.hueShift.value =
                Mathf.Sin(_pulseTimer * 0.12f) * 2.5f * fade;

            // --------------------------------------------------
            // BLOOM — vapor cloud glow, cool white
            // Lights look softly diffused through haze
            // --------------------------------------------------
            _bloom.intensity.value =
                (0.90f + breath * 0.55f + wave * 0.30f) * fade;

            _bloom.tint.value =
                Color.Lerp(VaporWhite, HazeBlue, breath * 0.25f);

            // --------------------------------------------------
            // CHROMATIC — light-headed pulsing
            // Gentle fringe that throbs with breathing
            // --------------------------------------------------
            _chromatic.intensity.value =
                (0.08f + flutter * 0.10f + wave * 0.06f) * fade;

            // --------------------------------------------------
            // LENS — floating sensation
            // Subtle distortion breathing, slightly buoyant
            // --------------------------------------------------
            _lens.intensity.value =
                (Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.15f) * 0.06f +
                 wave * 0.03f) * fade;

            _lens.scale.value =
                1f - 0.02f * wave * fade;

            // --------------------------------------------------
            // VIGNETTE — hazy peripheral vision
            // Cool grey edges, lighter than edible effects
            // --------------------------------------------------
            _vignette.intensity.value =
                (0.22f + breath * 0.10f + wave * 0.05f) * fade;

            _vignette.color.value = FogGrey;

            // --------------------------------------------------
            // GRAIN — minimal, clean vapor aesthetic
            // --------------------------------------------------
            _grain.intensity.value =
                (0.03f + flutter * 0.04f) * fade;
        }
    }
}
