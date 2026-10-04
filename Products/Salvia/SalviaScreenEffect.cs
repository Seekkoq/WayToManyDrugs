using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomNPCExample.Products
{
    public sealed class SalviaScreenEffect : MonoBehaviour
    {
        public static float EffectDuration = 65f;

        public static bool ShouldStart;
        public static bool ShouldStop;

        public bool IsActive => _active;

        private bool _active;
        private bool _setupOk;

        private float _timeRemaining;
        private float _tripTime;
        private float _seed;

        private GameObject _volumeObject;
        private Volume _volume;
        private VolumeProfile _profile;

        private ColorAdjustments _colorAdjust;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private LensDistortion _lens;
        private Vignette _vignette;
        private FilmGrain _grain;
        private ChannelMixer _channelMixer;
        private WhiteBalance _whiteBalance;
        private SplitToning _splitToning;

        public SalviaScreenEffect(IntPtr pointer)
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
                    "[Salvia Effect] Custom post-processing ready."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Salvia Effect] Setup failed: " + ex
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
            _tripTime += Time.deltaTime;

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
            _tripTime = 0f;
            _seed = UnityEngine.Random.Range(0f, 1000f);
            _active = true;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Salvia Effect] Reality fracture started. Duration=" +
                EffectDuration.ToString("0") + "s"
            );
        }

        private void StopEffect()
        {
            _active = false;
            _timeRemaining = 0f;
            _tripTime = 0f;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Salvia Effect] Reality fracture ended."
            );
        }

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            _volumeObject =
                new GameObject("WVC_Salvia_PostProcess");

            UnityEngine.Object.DontDestroyOnLoad(
                _volumeObject
            );

            _volume =
                _volumeObject.AddComponent<Volume>();

            _volume.isGlobal = true;
            _volume.priority = 10050f;
            _volume.weight = 0f;
            _volume.enabled = false;

            _profile =
                ScriptableObject.CreateInstance<VolumeProfile>();

            _volume.profile = _profile;

            _colorAdjust =
                _profile.Add<ColorAdjustments>(true);

            _bloom =
                _profile.Add<Bloom>(true);

            _chromatic =
                _profile.Add<ChromaticAberration>(true);

            _lens =
                _profile.Add<LensDistortion>(true);

            _vignette =
                _profile.Add<Vignette>(true);

            _grain =
                _profile.Add<FilmGrain>(true);

            _channelMixer =
                _profile.Add<ChannelMixer>(true);

            _whiteBalance =
                _profile.Add<WhiteBalance>(true);

            _splitToning =
                _profile.Add<SplitToning>(true);

            SetupOverrides();
        }

        private void SetupOverrides()
        {
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
            _bloom.threshold.value = 0.75f;
            _bloom.scatter.value = 0.80f;

            _chromatic.active = true;
            _chromatic.intensity.overrideState = true;

            _lens.active = true;
            _lens.intensity.overrideState = true;
            _lens.center.overrideState = true;
            _lens.scale.overrideState = true;
            _lens.xMultiplier.overrideState = true;
            _lens.yMultiplier.overrideState = true;

            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.color.overrideState = true;

            _grain.active = true;
            _grain.intensity.overrideState = true;
            _grain.response.overrideState = true;
            _grain.type.overrideState = true;
            _grain.response.value = 0.85f;

            _channelMixer.active = true;
            _channelMixer.redOutRedIn.overrideState = true;
            _channelMixer.redOutGreenIn.overrideState = true;
            _channelMixer.redOutBlueIn.overrideState = true;
            _channelMixer.greenOutRedIn.overrideState = true;
            _channelMixer.greenOutGreenIn.overrideState = true;
            _channelMixer.greenOutBlueIn.overrideState = true;
            _channelMixer.blueOutRedIn.overrideState = true;
            _channelMixer.blueOutGreenIn.overrideState = true;
            _channelMixer.blueOutBlueIn.overrideState = true;

            _whiteBalance.active = true;
            _whiteBalance.temperature.overrideState = true;
            _whiteBalance.tint.overrideState = true;

            _splitToning.active = true;
            _splitToning.shadows.overrideState = true;
            _splitToning.highlights.overrideState = true;
            _splitToning.balance.overrideState = true;
        }

        private void ApplyEffects()
        {
            float elapsed =
                EffectDuration - _timeRemaining;

            float fadeIn =
                Mathf.Clamp01(elapsed / 1.5f);

            float fadeOut =
                Mathf.Clamp01(_timeRemaining / 14f);

            float fade =
                Mathf.Min(fadeIn, fadeOut);

            float earlyIntensity =
                Mathf.Lerp(
                    1f,
                    0.62f,
                    Mathf.Clamp01((elapsed - 18f) / 22f)
                );

            float k = fade * earlyIntensity;

            float t = _tripTime;

            float slowX =
                Mathf.PerlinNoise(
                    _seed + t * 0.24f,
                    13.7f
                ) * 2f - 1f;

            float slowY =
                Mathf.PerlinNoise(
                    87.2f,
                    _seed + t * 0.31f
                ) * 2f - 1f;

            float fast =
                Mathf.PerlinNoise(
                    _seed + t * 2.4f,
                    55.5f
                ) * 2f - 1f;

            float pulseA =
                0.5f +
                Mathf.Sin(t * Mathf.PI * 2f * 0.72f) *
                0.5f;

            float pulseB =
                0.5f +
                Mathf.Sin(t * Mathf.PI * 2f * 1.83f) *
                0.5f;

            _volume.weight = fade;

            Color filter =
                Color.Lerp(
                    new Color(0.44f, 0.88f, 0.48f),
                    new Color(0.78f, 0.30f, 0.95f),
                    pulseA
                );

            _colorAdjust.saturation.value =
                (-10f + pulseB * 85f) * k;

            _colorAdjust.contrast.value =
                (24f + pulseA * 22f) * k;

            _colorAdjust.postExposure.value =
                (-0.15f + pulseB * 0.45f) * k;

            _colorAdjust.hueShift.value =
                (
                    slowX * 95f +
                    fast * 24f
                ) * k;

            _colorAdjust.colorFilter.value =
                Color.Lerp(
                    Color.white,
                    filter,
                    0.82f * k
                );

            _bloom.intensity.value =
                (1.2f + pulseA * 2.2f) * k;

            _bloom.tint.value =
                Color.Lerp(
                    new Color(0.35f, 1f, 0.48f),
                    new Color(1f, 0.18f, 0.82f),
                    pulseB
                );

            _chromatic.intensity.value =
                Mathf.Clamp01(
                    0.38f +
                    Mathf.Abs(fast) * 0.62f
                ) * k;

            _lens.intensity.value =
                (
                    slowY * 0.56f +
                    fast * 0.18f
                ) * k;

            _lens.center.value =
                new Vector2(
                    0.5f + slowX * 0.20f,
                    0.5f + slowY * 0.18f
                );

            _lens.scale.value =
                Mathf.Lerp(
                    1f,
                    1.13f + pulseB * 0.06f,
                    k
                );

            _lens.xMultiplier.value =
                Mathf.Lerp(
                    1f,
                    0.64f + Mathf.Abs(slowX) * 0.42f,
                    k
                );

            _lens.yMultiplier.value =
                Mathf.Lerp(
                    1f,
                    0.64f + Mathf.Abs(slowY) * 0.42f,
                    k
                );

            _vignette.intensity.value =
                (0.38f + pulseA * 0.30f) * k;

            _vignette.smoothness.value =
                0.78f;

            _vignette.color.value =
                Color.Lerp(
                    new Color(0.02f, 0.18f, 0.05f),
                    new Color(0.23f, 0.01f, 0.36f),
                    pulseB
                );

            _grain.intensity.value =
                (0.20f + Mathf.Abs(fast) * 0.35f) * k;

            float channelWarp =
                (
                    slowX * 34f +
                    fast * 17f
                ) * k;

            _channelMixer.redOutRedIn.value = 100f;
            _channelMixer.redOutGreenIn.value = channelWarp;
            _channelMixer.redOutBlueIn.value = -channelWarp * 0.45f;

            _channelMixer.greenOutRedIn.value = -channelWarp * 0.25f;
            _channelMixer.greenOutGreenIn.value = 100f;
            _channelMixer.greenOutBlueIn.value = channelWarp * 0.65f;

            _channelMixer.blueOutRedIn.value = channelWarp * 0.72f;
            _channelMixer.blueOutGreenIn.value = -channelWarp * 0.18f;
            _channelMixer.blueOutBlueIn.value = 100f;

            _whiteBalance.temperature.value =
                (slowX * 42f) * k;

            _whiteBalance.tint.value =
                (slowY * 55f) * k;

            _splitToning.shadows.value =
                Color.Lerp(
                    new Color(0.02f, 0.24f, 0.06f),
                    new Color(0.30f, 0.01f, 0.45f),
                    pulseA
                );

            _splitToning.highlights.value =
                Color.Lerp(
                    new Color(0.70f, 1f, 0.32f),
                    new Color(1f, 0.30f, 0.86f),
                    pulseB
                );

            _splitToning.balance.value =
                slowX * 38f * k;
        }
    }
}
