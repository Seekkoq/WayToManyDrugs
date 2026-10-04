using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomNPCExample.Products
{
    public sealed class BrownieScreenEffect : MonoBehaviour
    {
        public static bool ShouldStart;
        public static bool ShouldStop;

        public static float OnsetDelay = 8f;
        public static float EffectDuration = 150f;

        public static float MinimumAudioPitchMultiplier = 0.62f;

        public static float MinimumFovMultiplier = 0.62f;

        private bool _setupOk;
        private bool _pendingOnset;
        private bool _active;

        private float _onsetTimer;
        private float _timeRemaining;
        private float _effectAge;
        private float _pulseTimer;
        private float _audioScanTimer;

        private Volume _volume;
        private VolumeProfile _profile;

        private ColorAdjustments _colorAdjust;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private LensDistortion _lens;
        private Vignette _vignette;
        private FilmGrain _grain;
        private WhiteBalance _whiteBalance;

        private readonly Dictionary<int, float> _originalAudioPitches =
            new Dictionary<int, float>();

        private readonly Dictionary<int, float> _originalCameraFovs =
            new Dictionary<int, float>();

        private static readonly Color WarmBrown =
            new Color(0.55f, 0.28f, 0.10f);

        private static readonly Color Golden =
            new Color(1.00f, 0.72f, 0.25f);

        private static readonly Color DeepVignette =
            new Color(0.10f, 0.04f, 0.01f);

        public BrownieScreenEffect(IntPtr pointer)
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
                    "[Brownie Effect] Warm edible post-processing ready."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Brownie Effect] Setup failed: " + ex
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
                StartOrExtendEffect();
            }

            if (_pendingOnset)
            {
                _onsetTimer -= Time.deltaTime;

                if (_onsetTimer <= 0f)
                {
                    BeginActiveEffect();
                }

                return;
            }

            if (!_active)
                return;

            _timeRemaining -= Time.deltaTime;
            _effectAge += Time.deltaTime;
            _pulseTimer += Time.deltaTime;
            _audioScanTimer += Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                StopEffect();
                return;
            }

            ApplyVisualEffects();
            ApplyCameraZoom();
            ApplyAudioSlowdown();
        }

        private void OnDestroy()
        {
            RestoreAudio();
            RestoreCameras();
        }

        private void StartOrExtendEffect()
        {
            if (_active)
            {
                _timeRemaining =
                    Mathf.Min(
                        _timeRemaining + EffectDuration * 0.65f,
                        EffectDuration * 2.0f
                    );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Brownie Effect] Effect extended. Remaining: " +
                    _timeRemaining.ToString("0.0") + "s"
                );

                return;
            }

            if (_pendingOnset)
            {
                _onsetTimer =
                    Mathf.Min(
                        _onsetTimer + 4f,
                        OnsetDelay + 6f
                    );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Brownie Effect] Another brownie eaten. Onset delayed slightly."
                );

                return;
            }

            _pendingOnset = true;
            _onsetTimer = OnsetDelay;
            _timeRemaining = EffectDuration;
            _effectAge = 0f;
            _pulseTimer = 0f;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Brownie Effect] Brownie eaten. Effect starts in " +
                OnsetDelay + "s."
            );
        }

        private void BeginActiveEffect()
        {
            _pendingOnset = false;
            _active = true;

            _timeRemaining = EffectDuration;
            _effectAge = 0f;
            _pulseTimer = 0f;
            _audioScanTimer = 999f;

            CacheCurrentCameras();

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Brownie Effect] Edible high started. Duration: " +
                EffectDuration + "s"
            );
        }

        private void StopEffect()
        {
            _pendingOnset = false;
            _active = false;

            _onsetTimer = 0f;
            _timeRemaining = 0f;
            _effectAge = 0f;
            _pulseTimer = 0f;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }

            RestoreAudio();
            RestoreCameras();

            global::CustomNPCExample.Utils.WvcLog.Msg("[Brownie Effect] Edible high ended.");
        }

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            GameObject go =
                new GameObject("WVC_Brownie_PostProcess");

            UnityEngine.Object.DontDestroyOnLoad(go);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 9500f;
            _volume.weight = 0f;

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

            _whiteBalance =
                _profile.Add<WhiteBalance>(true);

            _colorAdjust.active = true;
            _colorAdjust.saturation.overrideState = true;
            _colorAdjust.contrast.overrideState = true;
            _colorAdjust.postExposure.overrideState = true;
            _colorAdjust.colorFilter.overrideState = true;
            _colorAdjust.hueShift.overrideState = true;

            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            _bloom.scatter.overrideState = true;
            _bloom.tint.overrideState = true;

            _chromatic.active = true;
            _chromatic.intensity.overrideState = true;

            _lens.active = true;
            _lens.intensity.overrideState = true;
            _lens.scale.overrideState = true;

            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.color.overrideState = true;

            _grain.active = true;
            _grain.intensity.overrideState = true;
            _grain.response.overrideState = true;
            _grain.type.overrideState = true;

            _whiteBalance.active = true;
            _whiteBalance.temperature.overrideState = true;
            _whiteBalance.tint.overrideState = true;

            _bloom.threshold.value = 0.78f;
            _bloom.scatter.value = 0.65f;

            _vignette.smoothness.value = 0.88f;
            _vignette.color.value = DeepVignette;

            _grain.type.value = FilmGrainLookup.Thin1;
            _grain.response.value = 0.80f;

            _volume.enabled = false;
        }

        private void ApplyVisualEffects()
        {
            float fadeIn =
                Mathf.Clamp01(_effectAge / 14f);

            float fadeOut =
                Mathf.Clamp01(_timeRemaining / 20f);

            float fade =
                Mathf.Min(fadeIn, fadeOut);

            float pulseSlow =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.18f) * 0.5f;

            float pulseMedium =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.42f) * 0.5f;

            float breathing =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.11f) * 0.5f;

            if (_volume != null)
                _volume.weight = fade;

            _colorAdjust.saturation.value =
                (18f + pulseSlow * 16f) * fade;

            _colorAdjust.contrast.value =
                (10f + pulseMedium * 10f) * fade;

            _colorAdjust.postExposure.value =
                (0.03f + pulseSlow * 0.11f) * fade;

            _colorAdjust.hueShift.value =
                Mathf.Sin(_pulseTimer * 0.18f) * 10f * fade;

            Color filter =
                Color.Lerp(
                    WarmBrown,
                    Golden,
                    pulseSlow * 0.45f
                );

            _colorAdjust.colorFilter.value =
                Color.Lerp(
                    Color.white,
                    filter,
                    fade * 0.42f
                );

            _bloom.intensity.value =
                (0.35f + pulseSlow * 0.55f) * fade;

            _bloom.tint.value =
                Color.Lerp(
                    WarmBrown,
                    Golden,
                    pulseMedium
                );

            _chromatic.intensity.value =
                (0.05f + pulseMedium * 0.08f) * fade;

            _lens.intensity.value =
                (-0.18f - breathing * 0.10f) * fade;

            _lens.scale.value =
                1f - 0.045f * fade * breathing;

            _vignette.intensity.value =
                (0.36f + breathing * 0.24f) * fade;

            _vignette.smoothness.value =
                0.82f + breathing * 0.12f;

            _grain.intensity.value =
                (0.05f + pulseMedium * 0.06f) * fade;

            _whiteBalance.temperature.value =
                Mathf.Lerp(
                    8f,
                    35f,
                    pulseSlow
                ) * fade;

            _whiteBalance.tint.value =
                Mathf.Sin(_pulseTimer * 0.25f) * 6f * fade;
        }

        private void CacheCurrentCameras()
        {
            try
            {
                Camera[] cameras =
                    Camera.allCameras;

                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera camera = cameras[i];

                    if (!IsGameplayCamera(camera))
                        continue;

                    int id = camera.GetInstanceID();

                    if (!_originalCameraFovs.ContainsKey(id))
                    {
                        _originalCameraFovs[id] =
                            camera.fieldOfView;
                    }
                }
            }
            catch
            {
            }
        }

        private void ApplyCameraZoom()
        {
            float fadeIn =
                Mathf.Clamp01(_effectAge / 12f);

            float fadeOut =
                Mathf.Clamp01(_timeRemaining / 16f);

            float fade =
                Mathf.Min(fadeIn, fadeOut);

            float pulse =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.13f) * 0.5f;

            float zoomMultiplier =
                Mathf.Lerp(
                    1f,
                    MinimumFovMultiplier,
                    fade * (0.88f + pulse * 0.12f)
                );

            try
            {
                Camera[] cameras =
                    Camera.allCameras;

                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera camera = cameras[i];

                    if (!IsGameplayCamera(camera))
                        continue;

                    int id = camera.GetInstanceID();

                    if (!_originalCameraFovs.ContainsKey(id))
                    {
                        _originalCameraFovs[id] =
                            camera.fieldOfView;
                    }

                    float originalFov =
                        _originalCameraFovs[id];

                    float targetFov =
                        originalFov * zoomMultiplier;

                    camera.fieldOfView =
                        Mathf.Lerp(
                            camera.fieldOfView,
                            targetFov,
                            Time.deltaTime * 4.5f
                        );
                }
            }
            catch
            {
            }
        }

        private static bool IsGameplayCamera(Camera camera)
        {
            if (camera == null ||
                !camera.enabled ||
                camera.orthographic)
            {
                return false;
            }

            string name =
                camera.gameObject.name.ToLowerInvariant();

            if (name.Contains("icon") ||
                name.Contains("ui") ||
                name.Contains("preview") ||
                name.Contains("render") ||
                name.Contains("thumbnail"))
            {
                return false;
            }

            return true;
        }

        private void RestoreCameras()
        {
            try
            {
                Camera[] cameras =
                    Camera.allCameras;

                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera camera = cameras[i];

                    if (camera == null)
                        continue;

                    int id =
                        camera.GetInstanceID();

                    if (_originalCameraFovs.TryGetValue(
                            id,
                            out float fov))
                    {
                        camera.fieldOfView = fov;
                    }
                }
            }
            catch
            {
            }

            _originalCameraFovs.Clear();
        }

        private void ApplyAudioSlowdown()
        {
            if (_audioScanTimer < 0.35f)
                return;

            _audioScanTimer = 0f;

            float fadeIn =
                Mathf.Clamp01(_effectAge / 10f);

            float fadeOut =
                Mathf.Clamp01(_timeRemaining / 14f);

            float fade =
                Mathf.Min(fadeIn, fadeOut);

            float wobble =
                0.5f +
                Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.09f) * 0.5f;

            float pitchMultiplier =
                Mathf.Lerp(
                    1f,
                    MinimumAudioPitchMultiplier,
                    fade
                );

            pitchMultiplier *=
                1f - wobble * 0.035f * fade;

            try
            {
                AudioSource[] sources =
                    UnityEngine.Object.FindObjectsOfType<AudioSource>();

                for (int i = 0; i < sources.Length; i++)
                {
                    AudioSource source =
                        sources[i];

                    if (source == null)
                        continue;

                    if (!source.enabled)
                        continue;

                    int id =
                        source.GetInstanceID();

                    if (!_originalAudioPitches.ContainsKey(id))
                    {
                        _originalAudioPitches[id] =
                            source.pitch;
                    }

                    float originalPitch =
                        _originalAudioPitches[id];

                    float targetPitch =
                        originalPitch * pitchMultiplier;

                    source.pitch =
                        Mathf.Lerp(
                            source.pitch,
                            targetPitch,
                            0.65f
                        );
                }
            }
            catch
            {
            }
        }

        private void RestoreAudio()
        {
            try
            {
                AudioSource[] sources =
                    UnityEngine.Object.FindObjectsOfType<AudioSource>();

                for (int i = 0; i < sources.Length; i++)
                {
                    AudioSource source =
                        sources[i];

                    if (source == null)
                        continue;

                    int id =
                        source.GetInstanceID();

                    if (_originalAudioPitches.TryGetValue(
                            id,
                            out float originalPitch))
                    {
                        source.pitch = originalPitch;
                    }
                }
            }
            catch
            {
            }

            _originalAudioPitches.Clear();
        }
    }
}
