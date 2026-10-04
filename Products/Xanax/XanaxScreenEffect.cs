using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// The Xanax high. One bar runs a dose curve over three minutes:
    /// Come-Up -> Plateau -> Sedation -> Come-Down -> Afterglow. Every extra bar taken while the
    /// effect is still live raises the intensity and pushes the curve towards the heavy stages
    /// faster, and a big enough stack blacks the player out for a moment.
    /// </summary>
    public sealed class XanaxScreenEffect : MonoBehaviour
    {
        // ---- tuning knobs --------------------------------------------------------------------

        /// <summary>Length of one bar's dose curve, in seconds. Three minutes.</summary>
        public static float EffectDuration = 180f;

        /// <summary>Global scaling hook, mirrors <see cref="DMTScreenEffect.Intensity"/>.</summary>
        public static float Intensity = 1f;

        /// <summary>Set false to keep the extreme doses from fading the screen to black.</summary>
        public static bool EnableBlackout = true;

        /// <summary>
        /// Bloom is off by default on purpose: in URP it is a multi-pass effect with a mip chain,
        /// which is a lot of full screen work for a little glow. The come-up glow is delivered
        /// through colour grading instead, which rides along in the uber pass. Set true for the
        /// heavier look if the frame rate can take it.
        /// </summary>
        public static bool EnableBloom = false;

        /// <summary>
        /// Hard cap on stacked bars.
        /// </summary>
        public const int MaxDoses = 6;

        /// <summary>Dose that triggers the blackout.</summary>
        public const int BlackoutDose = 5;

        /// <summary>
        /// How many times one high can knock the player out. Past the blackout dose, every further
        /// bar re-arms this, so a big enough stack drops them again instead of doing nothing.
        /// </summary>
        public const int MaxBlackouts = 3;

        /// <summary>A bar taken this soon after the last one still counts as a re-dose.</summary>
        public const float DoseWindowSeconds = 180f;

        /// <summary>The whole dose curve, mirrored on PlayerMovement while sedated.</summary>
        private static readonly float[] MoveSpeeds = { 0.94f, 0.86f, 0.78f, 0.70f, 0.62f, 0.55f };
        private const float BlendRate = 0.7f;
        private const float ReDoseMinimumRemaining = 0.6f;
        private const float BlackoutFadeIn = 1.2f;
        private const float BlackoutHold = 2.5f;
        private const float BlackoutFadeOut = 1.6f;
        private const float BlackoutCooldown = 6f;
        private const float SwayInterval = 0.1f;

        /// <summary>Dose at which the eyelids start to drop on their own.</summary>
        private const int NoddingDose = 3;

        private const float BlinkFadeIn = 0.14f;
        private const float BlinkHold = 0.22f;
        private const float BlinkFadeOut = 0.34f;
        private const float NoddingMinInterval = 5f;
        private const float NoddingMaxInterval = 14f;
        private const float NoddingPeakAlpha = 0.72f;

        /// <summary>How long the fog and the missing-time blinks linger after a blackout.</summary>
        private const float ConfusionDuration = 25f;
        private const float ConfusionBlinkAlpha = 0.85f;
        private const float ConfusionMinInterval = 3.5f;
        private const float ConfusionMaxInterval = 7f;

        private static readonly string[] StageNames =
        {
            "Come-Up",
            "Plateau",
            "Sedation",
            "Come-Down",
            "Afterglow"
        };

        /// <summary>Normalised progress at which each stage ends.</summary>
        private static readonly float[] StageEnds = { 0.17f, 0.50f, 0.83f, 0.94f, 1f };

        private static readonly float[] Saturation = { -8f, -14f, -24f, -18f, -12f };
        private static readonly float[] Contrast = { -6f, -10f, -18f, -12f, -7f };
        private static readonly float[] PostExposure = { -0.05f, -0.12f, -0.35f, -0.18f, -0.09f };
        private static readonly float[] VignetteAmt = { 0.14f, 0.20f, 0.36f, 0.24f, 0.17f };
        private static readonly float[] LensAmt = { 0.02f, 0.06f, 0.14f, 0.08f, 0.04f };
        private static readonly float[] ChromaticAmt = { 0.02f, 0.05f, 0.13f, 0.07f, 0.03f };
        private static readonly float[] BloomAmt = { 0.16f, 0.26f, 0.44f, 0.30f, 0.20f };
        private static readonly float[] GrainAmt = { 0.03f, 0.05f, 0.10f, 0.15f, 0.08f };

        private static readonly Color WarmTint = new Color(1f, 0.97f, 0.90f, 1f);
        private static readonly Color CoolTint = new Color(0.93f, 0.95f, 1f, 1f);
        private static readonly Color NeutralTint = Color.white;

        // ---- state --------------------------------------------------------------------------

        public static bool ShouldStart;
        public static bool ShouldStop;

        /// <summary>Dose handed over by the manager when <see cref="ShouldStart"/> is set.</summary>
        internal static int PendingDose = 1;

        public bool IsActive => _active;

        /// <summary>
        /// True once the curve has reached its last stage: the high is on its way out, so the next
        /// bar starts a fresh ladder rather than being clamped onto the old one.
        /// </summary>
        public bool IsWindingDown => _active && _stage >= StageNames.Length - 1;

        /// <summary>Name of the stage the curve is in, for logging and for the perf probe context.</summary>
        public string StageName =>
            _stage >= 0 && _stage < StageNames.Length ? StageNames[_stage] : "idle";

        private bool _setupOk;
        private bool _active;
        private int _dose = 1;
        private float _elapsed;
        private float _timeRemaining;
        private float _timeScale = 1f;
        private int _stage = -1;
        private float _swayTimer;

        private Volume _volume;
        private VolumeProfile _profile;
        private ColorAdjustments _color;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private LensDistortion _lens;
        private Vignette _vignette;
        private FilmGrain _grain;

        private float _currentVignette;
        private float _currentFog;

        private int _blackoutState;
        private float _blackoutTimer;
        private int _blackoutCount;
        private float _blackoutCooldown;
        private float _blackoutAlpha;
        private bool _blinkActive;
        private float _blinkTimer;
        private float _blinkAlpha;
        private float _blinkPeak;
        private float _blinkCooldown;
        private float _confusionTimer;
        private bool _appliedConfused;
        private int _appliedStage = -1;
        private float _appliedIntensity;
        private float _swayAccumulator;

        public XanaxScreenEffect(IntPtr pointer)
            : base(pointer)
        {
        }

        private void Awake()
        {
            try
            {
                CreateVolume();
                _setupOk = true;


            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Xanax Effect] Volume setup failed: " + ex);
            }
        }

        private void OnDisable()
        {
            StopImmediately();
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

                // Another bar during the curve stacks onto it; a bar taken once the curve is
                // winding down (afterglow) starts a whole new three minute high instead, which is
                // what a player expects when the first one has visibly passed.
                if (_active && !IsWindingDown)
                    Redose();
                else
                    StartEffect();
            }

            if (!_active)
                return;

            float dt = Time.deltaTime;

            _elapsed += dt * _timeScale;
            _timeRemaining -= dt;
            _swayTimer += dt;

            if (_timeRemaining <= 0f)
            {
                StopEffect();
                return;
            }

            ProgressBlackout(dt);
            ProgressBlinks(dt);
            EvaluateStages(dt);
            ApplyEffectValues();
        }

        private void StartEffect()
        {
            _dose = Mathf.Clamp(PendingDose, 1, MaxDoses);
            _elapsed = 0f;
            _timeRemaining = Mathf.Max(1f, EffectDuration);
            _timeScale = 1f;
            _stage = -1;
            _swayTimer = 0f;
            _currentVignette = 0f;
            _currentFog = 0f;
            _blackoutState = 0;
            _blackoutTimer = 0f;
            _blackoutCount = 0;
            _blackoutCooldown = 0f;
            _appliedStage = -1;
            _appliedIntensity = 0f;
            _blackoutAlpha = 0f;
            _blinkActive = false;
            _blinkTimer = 0f;
            _blinkAlpha = 0f;
            _blinkCooldown = 0f;
            _confusionTimer = 0f;
            _appliedConfused = false;
            _active = true;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }


        }

        /// <summary>
        /// Another bar while the first is still working. The dose clamps at
        /// <see cref="MaxDoses"/>, the curve runs faster so the heavy stages arrive sooner, and the
        /// tail of the effect is stretched so a stack keeps going for a while.
        /// </summary>
        private void Redose()
        {
            int previous = _dose;

            _dose = Mathf.Clamp(PendingDose, 1, MaxDoses);
            _timeScale = Mathf.Clamp(1f + 0.35f * (_dose - 1), 1f, 2.2f);
            _timeRemaining = Mathf.Max(_timeRemaining, EffectDuration * ReDoseMinimumRemaining);

            // Past the blackout dose the stack is already maxed, so a further bar tops the high up
            // instead of doing nothing: the tail stretches above and the blackout is re-armed so
            // the player can be dropped up to MaxBlackouts times in one high.
            if (_dose >= BlackoutDose && _blackoutCount < MaxBlackouts)
            {
                _blackoutCooldown = BlackoutCooldown;

                if (_blackoutState >= 4)
                {
                    _blackoutState = 0;
                    _blackoutTimer = 0f;
                }
            }


        }

        private void StopEffect()
        {
            _active = false;
            _timeRemaining = 0f;
            _dose = 0;
            _timeScale = 1f;
            _swayTimer = 0f;
            _blackoutState = 0;
            _blackoutTimer = 0f;
            _blackoutCount = 0;
            _blackoutCooldown = 0f;
            _appliedStage = -1;
            _blackoutAlpha = 0f;
            _blinkActive = false;
            _blinkTimer = 0f;
            _blinkAlpha = 0f;
            _blinkCooldown = 0f;
            _confusionTimer = 0f;
            _appliedConfused = false;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }

            XanaxBlackoutOverlay.Hide();
            XanaxMovementImpairment.Clear();


        }

        public void StopImmediately()
        {
            ShouldStart = false;
            ShouldStop = false;

            if (!_active)
            {
                XanaxMovementImpairment.Clear();
                XanaxBlackoutOverlay.Hide();
                return;
            }

            StopEffect();
        }

        private float DoseIntensity()
        {
            float value = 1f + 0.45f * (Mathf.Max(1, _dose) - 1);
            float scale = Intensity;

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                scale = 1f;

            return value * scale;
        }

        private void EvaluateStages(float dt)
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(1f, EffectDuration));

            int nextStage = StageEnds.Length - 1;

            for (int i = 0; i < StageEnds.Length; i++)
            {
                if (progress < StageEnds[i])
                {
                    nextStage = i;
                    break;
                }
            }

            if (nextStage != _stage)
            {
                _stage = nextStage;


            }

            float intensity = DoseIntensity();
            int index = Mathf.Clamp(_stage, 0, StageNames.Length - 1);

            _currentVignette = Mathf.MoveTowards(
                _currentVignette,
                Mathf.Clamp(VignetteAmt[index] * intensity, 0f, 0.8f),
                dt * BlendRate * 0.4f
            );

            _currentFog = Mathf.MoveTowards(_currentFog, 1f, dt * BlendRate * 0.5f);

            if (_volume != null)
            {
                float masterTarget = Ramp(0f, 0.14f, progress) * (1f - Ramp(0.90f, 1f, progress));

                masterTarget *= Mathf.Clamp(0.75f + 0.25f * intensity, 0f, 1f);

                // Only write the weight while it is actually moving: a write here changes the
                // volume the renderer uploads, and the weight sits still for most of the high.
                if (Mathf.Abs(masterTarget - _volume.weight) > 0.002f)
                {
                    _volume.weight = Mathf.MoveTowards(_volume.weight, masterTarget, dt * 0.9f);
                }
            }
        }

        private static float Ramp(float start, float end, float value)
        {
            if (end <= start)
                return value >= end ? 1f : 0f;

            float t = Mathf.Clamp01((value - start) / (end - start));

            return t * t * (3f - 2f * t);
        }

        private void ApplyEffectValues()
        {
            if (_color == null || _grain == null)
                return;

            float intensity = DoseIntensity();
            int index = Mathf.Clamp(_stage, 0, StageNames.Length - 1);

            bool confused = _confusionTimer > 0f;

            bool stageChanged =
                index != _appliedStage ||
                Mathf.Abs(intensity - _appliedIntensity) > 0.01f ||
                confused != _appliedConfused;

            // Stage and dose only move a handful of times per high, so the static look is written
            // when they change instead of every frame: each of these is a managed -> native call
            // and writing a dozen of them per frame is what makes the effect hitch.
            if (stageChanged)
            {
                _appliedStage = index;
                _appliedIntensity = intensity;
                _appliedConfused = confused;

                // Coming round after a blackout also washes the colour out and thickens the grain
                // until the fog lifts, so the missing time reads as time that really went missing.
                float fog = confused ? 1.35f : 1f;

                _color.saturation.value = Mathf.Clamp(
                    Saturation[index] * intensity * (confused ? 0.88f : 1f),
                    -70f,
                    0f
                );
                _color.contrast.value = Mathf.Clamp(Contrast[index] * intensity, -40f, 0f);
                _color.postExposure.value = Mathf.Clamp(PostExposure[index] * intensity, -1.2f, 0f);

                Color tint =
                    index <= 1 ? WarmTint :
                    index == 2 ? NeutralTint : CoolTint;

                _color.colorFilter.value = Color.Lerp(
                    NeutralTint,
                    tint,
                    Mathf.Clamp01(0.35f + 0.18f * (Mathf.Max(1, _dose) - 1))
                );

                _vignette.smoothness.value = Mathf.Clamp(
                    0.62f + 0.06f * index + 0.05f * (intensity - 1f),
                    0.5f,
                    0.92f
                );

                _lens.intensity.value = Mathf.Clamp(LensAmt[index] * intensity, 0f, 0.4f);
                _chromatic.intensity.value = Mathf.Clamp(ChromaticAmt[index] * intensity, 0f, 0.35f);
                if (_bloom != null)
                {
                    _bloom.intensity.value = Mathf.Clamp(
                        BloomAmt[index] * intensity,
                        0f,
                        0.85f
                    );
                }
                // Grain is the "cotton wool" layer, and it only starts once the sedation does.
                if (confused || _dose >= NoddingDose)
                {
                    _grain.intensity.value = Mathf.Clamp(
                        GrainAmt[index] * intensity * fog,
                        0f,
                        0.34f
                    );
                }
                else
                {
                    _grain.intensity.value = 0f;
                }
            }

            // The vignette and the breathing sway keep animating, but at ten hertz rather than per
            // frame - the eye cannot tell on a blur this soft, and it keeps the native calls down.
            _swayAccumulator += Time.deltaTime;

            if (_swayAccumulator >= SwayInterval)
            {
                _swayAccumulator = 0f;

                float breath = Mathf.Sin(_swayTimer * 0.55f) * 0.006f * intensity;
                float drift = Mathf.Sin(_swayTimer * 0.21f) * 0.012f * intensity;

                _vignette.intensity.value = _currentVignette * _currentFog;
                _lens.scale.value = 1f + breath;
                _lens.center.value = new Vector2(0.5f + drift, 0.5f + drift * 0.5f);

                // The come-up breathes a little warmth into the frame: the fuzzy "it is all fine"
                // part of the bar, before the sedation takes the edges off. It rides on the colour
                // grading rather than bloom so it costs nothing extra to render.
                if (index == 0)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_swayTimer * 1.15f);

                    _color.postExposure.value = Mathf.Clamp(
                        PostExposure[index] * intensity + 0.045f * pulse * intensity,
                        -1.2f,
                        0.1f
                    );

                    if (_bloom != null)
                    {
                        _bloom.intensity.value = Mathf.Clamp(
                            (BloomAmt[index] + 0.14f * pulse) * intensity,
                            0f,
                            0.95f
                        );
                    }
                }
            }

            float speed = MoveSpeeds[Mathf.Clamp(Mathf.Max(1, _dose) - 1, 0, MoveSpeeds.Length - 1)];

            if (_blackoutState == 1 || _blackoutState == 2)
                speed = Mathf.Min(speed, 0.35f);

            XanaxMovementImpairment.Apply(speed);

            if (_volume != null && !_volume.enabled)
            {
                _volume.enabled = true;
                _volume.weight = 0f;

                // The volume just came back: force the static look to be rewritten next frame.
                if (_stage >= 0)
                    _appliedStage = -1;
            }
        }

        /// <summary>
        /// The nodding-off blinks. At <see cref="NoddingDose"/> and up the eyelids drop for a
        /// fraction of a second every so often, and for a while after a blackout the player loses
        /// whole seconds the same way - the cheap, very benzo-flavoured way of saying "you are not
        /// fully here". Shares the overlay with the blackout, so the strongest caller wins.
        /// </summary>
        private void ProgressBlinks(float dt)
        {
            if (_blackoutState >= 1 && _blackoutState <= 3)
            {
                _blinkActive = false;
                _blinkAlpha = 0f;

                ApplyOverlay();
                return;
            }

            if (_confusionTimer > 0f)
                _confusionTimer -= dt;

            bool confused = _confusionTimer > 0f;

            if (!_blinkActive)
            {
                _blinkCooldown -= dt;

                if (_blinkCooldown <= 0f && (confused || _dose >= NoddingDose))
                {
                    _blinkActive = true;
                    _blinkTimer = 0f;
                    _blinkAlpha = 0f;

                    if (confused)
                    {
                        _blinkPeak = ConfusionBlinkAlpha;
                        _blinkCooldown = UnityEngine.Random.Range(
                            ConfusionMinInterval,
                            ConfusionMaxInterval
                        );
                    }
                    else
                    {
                        float extra = Mathf.Clamp01(0.08f * (_dose - NoddingDose));

                        _blinkPeak = Mathf.Clamp(NoddingPeakAlpha + extra, 0.4f, 0.92f);
                        _blinkCooldown = UnityEngine.Random.Range(
                            NoddingMinInterval,
                            NoddingMaxInterval
                        ) * Mathf.Clamp(1.3f - 0.15f * (_dose - NoddingDose), 0.5f, 1.3f);
                    }
                }
            }
            else
            {
                _blinkTimer += dt;

                float t = _blinkTimer;

                if (t < BlinkFadeIn)
                {
                    _blinkAlpha = _blinkPeak * (t / BlinkFadeIn);
                }
                else if (t < BlinkFadeIn + BlinkHold)
                {
                    _blinkAlpha = _blinkPeak;
                }
                else if (t < BlinkFadeIn + BlinkHold + BlinkFadeOut)
                {
                    _blinkAlpha =
                        _blinkPeak *
                        (1f - (t - BlinkFadeIn - BlinkHold) / BlinkFadeOut);
                }
                else
                {
                    _blinkAlpha = 0f;
                    _blinkActive = false;
                }
            }

            ApplyOverlay();
        }

        private void ApplyOverlay()
        {
            float alpha = Mathf.Max(_blackoutAlpha, _blinkAlpha);

            // Nothing showing at all: skip the work entirely, and do not even build the canvas
            // until the first blink or blackout actually needs it.
            if (alpha <= 0.001f)
            {
                if (XanaxBlackoutOverlay.IsVisible)
                    XanaxBlackoutOverlay.SetAlpha(0f);

                return;
            }

            XanaxBlackoutOverlay.SetAlpha(alpha);
        }

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            GameObject go = new GameObject("WVC_Xanax_PostProcess");
            UnityEngine.Object.DontDestroyOnLoad(go);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 9998f;
            _volume.weight = 0f;
            _volume.enabled = false;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "WVC_Xanax_RuntimeProfile";
            _volume.profile = _profile;

            _color = _profile.Add<ColorAdjustments>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _lens = _profile.Add<LensDistortion>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);

            // Bloom is skipped entirely when it is off, so its passes are never even built.
            _bloom = EnableBloom ? _profile.Add<Bloom>(true) : null;

            _color.active = true;
            _chromatic.active = true;
            _lens.active = true;
            _vignette.active = true;
            _grain.active = true;

            if (_bloom != null)
            {
                _bloom.active = true;
                _bloom.intensity.overrideState = true;
                _bloom.threshold.overrideState = true;
                _bloom.scatter.overrideState = true;

                _bloom.threshold.value = 0.90f;
                _bloom.scatter.value = 0.60f;
            }

            _color.saturation.overrideState = true;
            _color.contrast.overrideState = true;
            _color.colorFilter.overrideState = true;
            _color.postExposure.overrideState = true;

            _chromatic.intensity.overrideState = true;

            _lens.intensity.overrideState = true;
            _lens.scale.overrideState = true;
            _lens.center.overrideState = true;

            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;

            _grain.intensity.overrideState = true;
            _grain.response.overrideState = true;

            _lens.center.value = new Vector2(0.5f, 0.5f);
            _lens.scale.value = 1f;

            _vignette.smoothness.value = 0.70f;

            _grain.type.value = FilmGrainLookup.Thin1;
            _grain.response.value = 0.80f;
        }

        private void ProgressBlackout(float dt)
        {
            if (!EnableBlackout || _blackoutCount >= MaxBlackouts)
                return;

            if (_blackoutCooldown > 0f)
                _blackoutCooldown -= dt;

            if (_blackoutState == 0)
            {
                if (_dose < BlackoutDose || _blackoutCooldown > 0f)
                    return;

                float progress = Mathf.Clamp01(_elapsed / Mathf.Max(1f, EffectDuration));

                if (progress < StageEnds[1])
                    return;

                _blackoutCount++;
                _blackoutState = 1;
                _blackoutTimer = 0f;


            }

            _blackoutTimer += dt;

            if (_blackoutState == 1)
            {
                _blackoutAlpha = Mathf.Clamp01(_blackoutTimer / BlackoutFadeIn);

                if (_blackoutTimer >= BlackoutFadeIn)
                {
                    _blackoutState = 2;
                    _blackoutTimer = 0f;


                }

                return;
            }

            if (_blackoutState == 2)
            {
                _blackoutAlpha = 1f;

                if (_blackoutTimer >= BlackoutHold)
                {
                    _blackoutState = 3;
                    _blackoutTimer = 0f;
                }

                return;
            }

            if (_blackoutState == 3)
            {
                _blackoutAlpha = 1f - Mathf.Clamp01(_blackoutTimer / BlackoutFadeOut);

                if (_blackoutTimer >= BlackoutFadeOut)
                {
                    _blackoutState = 4;
                    _blackoutAlpha = 0f;
                    _confusionTimer = ConfusionDuration;
                    _appliedConfused = false;


                }
            }
        }

        // Movement impairment lives in XanaxMovementImpairment, and the blackout fade lives in
        // XanaxBlackoutOverlay. Both are deliberately off this component: Il2CppInterop injects
        // every MonoBehaviour the mod adds, and exotic member types break that injection.

    }
}
