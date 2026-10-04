using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using NativeNPC = Il2CppScheduleOne.NPCs.NPC;
using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    public sealed class DMTScreenEffect : MonoBehaviour
    {
        public static float EffectDuration = 90f;

        /// <summary>
        /// How hard the trip runs, as a multiplier over everything it does: the post-processing, the
        /// surface patterns and their opacity. It is allowed past 1 so it can be turned up.
        /// </summary>
        public static float Intensity = 1.3f;

        /// <summary>
        /// How long the trip takes to come up, in seconds.
        ///
        /// This is the fade-in, and it is deliberately its own number rather than a slice of the
        /// trip's length: a long trip should not crawl in for half a minute, and a short one should
        /// not snap on. Keeping it in seconds means the way the trip arrives is the same whatever
        /// <see cref="EffectDuration"/> is set to. It is a quick fade - a couple of seconds - so it
        /// is felt as the trip coming up rather than as a wait.
        /// </summary>
        public static float OnsetSeconds = 2.5f;

        public static bool WorldPatternsEnabled = true;
        public static bool ReducedMotion = false;

        public static bool ShouldStart;
        public static bool ShouldStop;

        private DMTEffectController _controller;

        public bool IsActive =>
            _controller != null && _controller.IsActive;

        public DMTScreenEffect(IntPtr pointer) : base(pointer)
        {
        }

        private void Awake()
        {
            EnsureController();
        }

        private void Update()
        {
            EnsureController();
            _controller.Tick();
        }

        private void OnDisable()
        {
            StopImmediately();
        }

        private void OnDestroy()
        {
            if (_controller == null)
                return;

            _controller.Dispose();
            _controller = null;
        }

        public void StopImmediately()
        {
            ShouldStart = false;
            ShouldStop = false;

            // The trip is over, so its noise goes with it.
            DMTWhisper.Stop();

            if (_controller != null)
                _controller.Stop();
        }

        private void EnsureController()
        {
            if (_controller != null)
                return;

            _controller = new DMTEffectController(gameObject);
            _controller.Initialize();
        }
    }

    internal sealed class DMTEffectController
    {
        private const int PatternPoolSize = 40;

        /// <summary>
        /// The size of the square each surface shape is drawn into. Every shape is soft, a few pixels of
        /// edge either way, so this stays cheap to build and to keep in memory even with a shape count
        /// this size.
        /// </summary>
        private const int PatternTextureSize = 160;

        /// <summary>
        /// How many surface shapes there are. The pool rotates through all of them at random, so this is
        /// the number of different things a wall can be covered with during one trip. They are different
        /// kinds of figure - outlined, filled, structural, organic - rather than variations on one.
        /// </summary>
        private const int PatternVariantCount = 28;
        private const float PatternMaxDistance = 40f;

        private readonly GameObject _owner;
        private readonly System.Random _random = new System.Random();

        private readonly DmtSurfacePattern[] _patterns =
            new DmtSurfacePattern[PatternPoolSize];

        private static readonly string[] StageNames =
        {
            "Onset",
            "Ascent",
            "Peak",
            "Recovery",
            "Afterglow"
        };

        private static readonly Color Gold =
            new Color(1f, 0.76f, 0.28f, 1f);

        private static readonly Color Teal =
            new Color(0.12f, 0.92f, 0.82f, 1f);

        private static readonly Color Violet =
            new Color(0.65f, 0.33f, 1f, 1f);

        private static readonly Color Rose =
            new Color(1f, 0.32f, 0.66f, 1f);

        private static readonly Color Emerald =
            new Color(0.20f, 1f, 0.45f, 1f);

        private static readonly Color Azure =
            new Color(0.25f, 0.55f, 1f, 1f);

        private static readonly Color Magenta =
            new Color(1f, 0.15f, 0.85f, 1f);

        private static readonly Color Amber =
            new Color(1f, 0.55f, 0.10f, 1f);

        private static readonly Color Cyan =
            new Color(0.30f, 1f, 1f, 1f);

        private static readonly Color Lime =
            new Color(0.75f, 1f, 0.20f, 1f);

        private static readonly Color Indigo =
            new Color(0.35f, 0.20f, 1f, 1f);

        private static readonly Color Coral =
            new Color(1f, 0.42f, 0.30f, 1f);

        /// <summary>
        /// The colours a surface pattern can come out as. Wide on purpose: four colours on rotation
        /// read as one effect, where a palette this size keeps the walls changing through the trip.
        /// </summary>
        private static readonly Color[] PatternColors =
        {
            Gold, Teal, Violet, Rose, Emerald, Azure, Magenta,
            Amber, Cyan, Lime, Indigo, Coral
        };

        private bool _ready;
        private bool _active;
        private bool _disposed;
        private bool _patternsReady;
        private bool _patternsFailed;

        private float _duration;
        private float _elapsed;
        private float _animationTime;
        private float _masterBlend;
        private float _peakBlend;
        private float _patternBlend;
        private float _spawnTimer;

        /// <summary>
        /// How many patterns have been placed this trip. Each one is put wherever a ray through the view
        /// lands, and the aim of that ray is walked through the screen in a low-discrepancy sequence
        /// rather than picked at random: random aims repeat and clump, so the patterns bunched up in one
        /// corner of whatever was in front of the player, while this visits every part of the view in turn
        /// and leaves them spread across the room.
        /// </summary>
        private int _spawnCounter;

        private int _stage = -1;
        private int _patternLayer;

        private NativePlayer _player;
        private Camera _camera;

        private GameObject _volumeObject;
        private Volume _volume;
        private VolumeProfile _profile;

        private ColorAdjustments _color;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private LensDistortion _lens;
        private Vignette _vignette;
        private FilmGrain _grain;
        private ChannelMixer _channels;
        private WhiteBalance _whiteBalance;
        private SplitToning _splitToning;
        private ColorCurves _curves;
        private ShadowsMidtonesHighlights _grade;
        private PaniniProjection _panini;

        private Mesh _patternMesh;
        private Material _patternMaterial;
        private Texture2D[] _patternTextures;

        public bool IsActive => _active;

        public DMTEffectController(GameObject owner)
        {
            _owner = owner;
        }

        public void Initialize()
        {
            try
            {
                CreateVolume();
                _ready = true;

                WvcLog.Msg("[DMT Effect] Staged post-processing ready.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DMT Effect] Setup failed: " + ex);
            }
        }

        public void Tick()
        {
            if (_disposed)
                return;

            try
            {
                if (DMTScreenEffect.ShouldStop)
                {
                    DMTScreenEffect.ShouldStop = false;
                    DMTScreenEffect.ShouldStart = false;
                    Stop();
                    return;
                }

                if (DMTScreenEffect.ShouldStart)
                {
                    DMTScreenEffect.ShouldStart = false;
                    StartOrRefresh();
                }

                if (!_ready || !_active)
                    return;

                NativePlayer currentPlayer = ResolveLocalPlayer();

                if (_player == null ||
                    currentPlayer == null ||
                    currentPlayer != _player ||
                    !_player.gameObject.activeInHierarchy)
                {
                    Stop();
                    return;
                }

                float dt = Time.deltaTime;

                if (dt <= 0f)
                    return;

                _elapsed += dt;
                _animationTime += dt;

                if (_elapsed >= _duration)
                {
                    Stop();
                    return;
                }

                UpdateStageEnvelopes(dt);

                float intensity = GetIntensity();
                ApplyPostProcessing(intensity);

                if (!_patternsReady &&
                    !_patternsFailed &&
                    DMTScreenEffect.WorldPatternsEnabled &&
                    intensity > 0f)
                {
                    EnsurePatternResources();
                }

                if (!_patternsReady)
                    return;

                UpdatePatterns(dt, intensity);

                _spawnTimer -= dt;

                if (_spawnTimer <= 0f)
                {
                    _spawnTimer =
                        Mathf.Lerp(0.30f, 0.08f, _peakBlend);

                    // More than one at a time: a single pattern per tick never fills a pool this size
                    // inside a trip, and the point of the walls is that they are everywhere at once.
                    int perTick =
                        1 + Mathf.RoundToInt(_patternBlend * (2f + _peakBlend * 6f));

                    for (int i = 0; i < perTick; i++)
                    {
                        if (!TrySpawnPattern(intensity))
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Stop();

                MelonLogger.Error(
                    "[DMT Effect] Runtime failure; effect stopped: " + ex
                );
            }
        }

        private void StartOrRefresh()
        {
            if (!_ready)
                return;

            NativePlayer player = ResolveLocalPlayer();
            Camera camera = GetGameplayCamera();

            if (player == null || camera == null)
            {
                WvcLog.Msg(
                    "[DMT Effect] Not started: local player/main gameplay camera unavailable."
                );
                return;
            }

            if (_active && _player == player)
            {
                _elapsed = Mathf.Min(_elapsed, _duration * 0.28f);
                _spawnTimer = 0f;

                WvcLog.Msg("[DMT Effect] Trip refreshed.");
                return;
            }

            Stop();

            float requestedDuration = DMTScreenEffect.EffectDuration;

            if (float.IsNaN(requestedDuration) ||
                float.IsInfinity(requestedDuration))
            {
                requestedDuration = 90f;
            }

            _duration = Mathf.Clamp(requestedDuration, 20f, 600f);

            _player = player;
            _camera = camera;

            _elapsed = 0f;
            _animationTime = 0f;
            _masterBlend = 0f;
            _peakBlend = 0f;
            _patternBlend = 0f;
            _spawnTimer = 0f;
            _stage = -1;

            ConfigureCameraLayers(camera);

            _active = true;
            _volume.weight = 0f;
            _volume.enabled = true;

            // The trip's own noise, started here rather than when the trip was asked for. Stop() above
            // has just silenced whatever ran before this, so this is the first moment a whisper started
            // for this trip could be heard at all - and it is the moment the trip is actually running,
            // which is when it belongs.
            DMTWhisper.Play();

            WvcLog.Msg(
                "[DMT Effect] Geometry trip started. Duration=" +
                _duration.ToString("0") + "s."
            );
        }

        public void Stop()
        {
            bool wasActive = _active;

            _active = false;
            _elapsed = 0f;
            _masterBlend = 0f;
            _peakBlend = 0f;
            _patternBlend = 0f;
            _spawnTimer = 0f;

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }

            HideAllPatterns();

            _player = null;
            _camera = null;

            // The trip's noise goes with the trip. This is where every ending arrives - the trip running
            // out of time calls straight into here, and the cuke calls StopImmediately which comes here
            // through the controller - so this is the one place that has to stop the whisper, or it loops
            // on under whatever the player does next.
            DMTWhisper.Stop();

            if (wasActive)
                WvcLog.Msg("[DMT Effect] Trip ended.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _disposed = true;

            DestroyPatternResources();

            if (_volume != null)
                _volume.sharedProfile = null;

            if (_profile != null)
            {
                for (int i = _profile.components.Count - 1; i >= 0; i--)
                {
                    if (_profile.components[i] != null)
                        UnityEngine.Object.Destroy(_profile.components[i]);
                }

                _profile.components.Clear();
                UnityEngine.Object.Destroy(_profile);
                _profile = null;
            }

            if (_volumeObject != null)
                UnityEngine.Object.Destroy(_volumeObject);

            _volumeObject = null;
            _volume = null;
        }

        private void UpdateStageEnvelopes(float dt)
        {
            float progress = Mathf.Clamp01(_elapsed / _duration);

            // The trip fades in over a set number of seconds rather than a slice of its own length, so
            // it arrives the same way however long the trip is. The fade is quick, but it is a fade:
            // the trip eases up from nothing instead of switching on at full strength.
            float onset = Ramp(
                0f,
                Mathf.Max(0.1f, DMTScreenEffect.OnsetSeconds),
                _elapsed
            );

            // The stages come on sooner and hand over sooner than they used to: the trip reaches its
            // peak in about a sixth of its length and holds it for a good stretch of the middle.
            float masterTarget =
                onset *
                (1f - Ramp(0.88f, 1f, progress));

            float peakTarget =
                onset *
                Ramp(0.05f, 0.17f, progress) *
                (1f - Ramp(0.62f, 0.88f, progress));

            float patternTarget =
                onset *
                Ramp(0.03f, 0.15f, progress) *
                (1f - Ramp(0.62f, 0.87f, progress));

            if (!DMTScreenEffect.WorldPatternsEnabled)
                patternTarget = 0f;

            _masterBlend = Mathf.MoveTowards(
                _masterBlend, masterTarget, dt * 2f
            );

            _peakBlend = Mathf.MoveTowards(
                _peakBlend, peakTarget, dt * 1.6f
            );

            _patternBlend = Mathf.MoveTowards(
                _patternBlend, patternTarget, dt * 1.6f
            );

            int nextStage =
                progress < 0.12f ? 0 :
                progress < 0.28f ? 1 :
                progress < 0.55f ? 2 :
                progress < 0.85f ? 3 : 4;

            if (nextStage != _stage)
            {
                _stage = nextStage;
                WvcLog.Msg("[DMT Effect] Stage: " + StageNames[_stage]);
            }
        }

        private static float Ramp(float start, float end, float value)
        {
            float t = (value - start) / (end - start);
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - 2f * t);
        }

        /// <summary>A three-point curve, for the colour curves that are set once and left alone.</summary>
        private static TextureCurve Curve(float start, float middle, float end)
        {
            AnimationCurve keys = new AnimationCurve(
                new Keyframe[]
                {
                    new Keyframe(0f, start),
                    new Keyframe(0.5f, middle),
                    new Keyframe(1f, end)
                });

            // The colour curves run over the whole 0..1 range of their input, so that is the bound the
            // texture is baked against. Zero is the value an untouched key would carry, and nothing here
            // loops.
            Vector2 bounds = new Vector2(0f, 1f);

            return new TextureCurve(keys, 0f, false, ref bounds);
        }

        private static float GetIntensity()
        {
            float value = DMTScreenEffect.Intensity;

            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1f;

            // Up to twice the baseline, so the trip can be turned up rather than only down.
            return Mathf.Clamp(value, 0f, 2f);
        }

        private void CreateVolume()
        {
            _volumeObject = new GameObject("WVC_DMT_PostProcess");
            _volumeObject.transform.SetParent(_owner.transform, false);

            _volume = _volumeObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10000f;
            _volume.weight = 0f;
            _volume.enabled = false;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "WVC_DMT_RuntimeProfile";

            _volume.sharedProfile = _profile;

            _color = _profile.Add<ColorAdjustments>(true);
            _bloom = _profile.Add<Bloom>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _lens = _profile.Add<LensDistortion>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);
            _channels = _profile.Add<ChannelMixer>(true);
            _whiteBalance = _profile.Add<WhiteBalance>(true);
            _splitToning = _profile.Add<SplitToning>(true);
            _curves = _profile.Add<ColorCurves>(true);
            _grade = _profile.Add<ShadowsMidtonesHighlights>(true);
            _panini = _profile.Add<PaniniProjection>(true);

            // Only the genuinely bright parts of the picture bloom, and they spread less: the trip is meant
            // to be a lot of colour rather than a lot of light, and a low threshold with a wide scatter is
            // what was washing the whole screen out.
            _bloom.threshold.value = 1.05f;
            _bloom.scatter.value = 0.58f;

            _lens.center.value = new Vector2(0.5f, 0.5f);
            _lens.scale.value = 1f;
            _lens.xMultiplier.value = 1f;
            _lens.yMultiplier.value = 1f;

            _vignette.smoothness.value = 0.80f;

            _grain.type.value = FilmGrainLookup.Thin2;
            _grain.response.value = 1f;

            // The colour curves are fixed rather than animated: they are what turns the hue rotation and
            // the channel mixing into something that reads as colour coming apart rather than as a tint,
            // and rebuilding an animation curve on every frame to do it would only cost memory.
            _curves.hueVsHue.value = Curve(0.12f, 0.5f, 0.88f);
            _curves.hueVsSat.value = Curve(0.80f, 1f, 0.80f);
            _curves.satVsSat.value = Curve(0.65f, 1f, 1f);
            _curves.lumVsSat.value = Curve(1f, 1f, 0.55f);

            // Crop-to-fit is a 0..1 switch on this effect rather than a boolean, and leaving it on keeps
            // the stretched view free of the black wedges a bare projection would show.
            _panini.cropToFit.value = 1f;

            _grade.shadowsStart.value = 0.05f;
            _grade.shadowsEnd.value = 0.30f;
            _grade.highlightsStart.value = 0.70f;
            _grade.highlightsEnd.value = 0.95f;

        }

        private void ApplyPostProcessing(float intensity)
        {
            float t = _animationTime;
            float peak = _peakBlend;

            float breath = 0.5f + Mathf.Sin(t * 0.75f) * 0.5f;
            float colorCycle = 0.5f + Mathf.Sin(t * 0.20f) * 0.5f;
            float motion = DMTScreenEffect.ReducedMotion ? 0.25f : 1f;

            _volume.weight = _masterBlend * intensity;

            _color.saturation.value = 25f + peak * (85f + breath * 20f);

            // Contrast and exposure both lift the picture towards white, so both are held well down. The
            // saturation above is what carries the effect, and it does not brighten anything.
            _color.contrast.value = 4f + peak * 14f;
            _color.postExposure.value = -0.09f - peak * 0.04f;
            _color.hueShift.value = Mathf.Sin(t * 0.22f) * 110f * peak;

            Color peakFilter = Color.Lerp(
                new Color(0.80f, 0.66f, 0.56f),
                new Color(0.52f, 0.70f, 0.80f),
                colorCycle
            );

            // A colour filter multiplies the whole picture, so it is kept below white at both ends: a pale
            // filter is the other half of what made the trip read as bright rather than as coloured.
            Color afterglow = new Color(0.82f, 0.79f, 0.75f);

            _color.colorFilter.value =
                Color.Lerp(afterglow, peakFilter, peak * 0.42f);

            // Bloom is the one effect that adds light rather than moving it about, so it is held low and
            // its threshold is above white: only the genuinely blown parts of the picture glow, and the
            // trip stays a lot of colour instead of a lot of light.
            _bloom.intensity.value = 0.14f + peak * 0.70f;
            _bloom.tint.value = Color.Lerp(
                new Color(1f, 0.93f, 0.80f),
                Color.Lerp(Gold, Teal, colorCycle),
                peak * 0.5f
            );

            _chromatic.intensity.value =
                (0.05f + peak * (0.8f + breath * 0.28f)) * motion;

            float distortion =
                (Mathf.Sin(t * 0.65f) * 0.42f +
                 Mathf.Sin(t * 0.31f) * 0.14f) * peak * motion;

            _lens.intensity.value = distortion;
            _lens.scale.value = 1f + Mathf.Abs(distortion) * 0.32f;

            _lens.center.value = new Vector2(
                0.5f + Mathf.Sin(t * 0.25f) * 0.035f * peak * motion,
                0.5f + Mathf.Cos(t * 0.21f) * 0.025f * peak * motion
            );

            _lens.xMultiplier.value = 1f - 0.20f * peak * breath;
            _lens.yMultiplier.value = 1f - 0.15f * peak * (1f - breath);

            _vignette.intensity.value = 0.12f + peak * 0.6f;
            _vignette.color.value = Color.Lerp(
                new Color(0.06f, 0.04f, 0.08f),
                new Color(0.16f, 0.06f, 0.26f),
                peak
            );

            _grain.intensity.value = 0.05f + peak * 0.30f;

            float channelMix = peak * 70f;
            float a = colorCycle;

            _channels.redOutRedIn.value = 100f - channelMix;
            _channels.redOutGreenIn.value = channelMix * a;
            _channels.redOutBlueIn.value = channelMix * (1f - a);

            _channels.greenOutRedIn.value = channelMix * (1f - a);
            _channels.greenOutGreenIn.value = 100f - channelMix;
            _channels.greenOutBlueIn.value = channelMix * a;

            _channels.blueOutRedIn.value = channelMix * a;
            _channels.blueOutGreenIn.value = channelMix * (1f - a);
            _channels.blueOutBlueIn.value = 100f - channelMix;

            _whiteBalance.temperature.value =
                10f + Mathf.Sin(t * 0.18f) * 40f * peak;

            _whiteBalance.tint.value =
                Mathf.Sin(t * 0.16f) * 30f * peak;

            _splitToning.shadows.value = Color.Lerp(
                Color.gray,
                Color.Lerp(Violet, Teal, colorCycle),
                peak * 0.45f
            );

            _splitToning.highlights.value = Color.Lerp(
                Color.gray,
                Gold,
                0.06f + peak * 0.20f
            );

            _splitToning.balance.value = peak * 20f;

            // The grade the curves cannot do on their own: the darks, the middle and the highlights are
            // each pulled a different way, and they swap over through the trip as the palette cycles.
            Color shadowsTarget = Color.Lerp(Violet, Teal, colorCycle);

            int palette = PatternColors.Length;
            int midFirst = (int)(colorCycle * palette) % palette;
            int midSecond = (midFirst + 4) % palette;

            Color midTarget = Color.Lerp(
                PatternColors[midFirst],
                PatternColors[midSecond],
                colorCycle
            );

            // The grade is a tint, not a lift: the middle and the highlights are pulled less far towards the
            // palette than the darks are, so the picture keeps its shape instead of blooming into white.
            _grade.shadows.value = Color.Lerp(
                Color.gray,
                shadowsTarget,
                peak * 0.30f
            );

            _grade.midtones.value = Color.Lerp(
                Color.gray,
                midTarget,
                peak * 0.20f
            );

            _grade.highlights.value = Color.Lerp(
                Color.gray,
                Color.Lerp(Gold, Rose, breath),
                peak * 0.12f
            );

            _grade.shadowsStart.value = 0.05f + breath * 0.06f * peak;
            _grade.shadowsEnd.value = 0.30f + breath * 0.10f * peak;
            _grade.highlightsStart.value = 0.70f - breath * 0.10f * peak;
            _grade.highlightsEnd.value = 0.95f - breath * 0.06f * peak;

            // The view through a warped lens: the picture bows outwards and back as the trip breathes.
            _panini.distance.value =
                (0.16f + breath * 0.38f) * peak * motion;
        }

        private void ConfigureCameraLayers(Camera camera)
        {
            _patternLayer = FirstLayer(camera.cullingMask);

            var cameraData =
                camera.GetComponent<UniversalAdditionalCameraData>();

            if (cameraData != null)
            {
                _volumeObject.layer =
                    FirstLayer(cameraData.volumeLayerMask.value);

                if (!cameraData.renderPostProcessing)
                {

                }
            }
        }

        private static int FirstLayer(int mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }

        private static NativePlayer ResolveLocalPlayer()
        {
            try { return NativePlayer.Local; }
            catch { return null; }
        }

        private static Camera GetGameplayCamera()
        {
            Camera camera = Camera.main;

            if (camera == null ||
                !camera.enabled ||
                !camera.gameObject.activeInHierarchy ||
                camera.targetTexture != null)
            {
                return null;
            }

            return camera;
        }

        private void EnsurePatternResources()
        {
            if (_patternsReady || _patternsFailed)
                return;

            try
            {
                Shader shader =
                    Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Transparent");

                if (shader == null)
                    throw new InvalidOperationException(
                        "No transparent pattern shader available."
                    );

                _patternMesh = CreatePatternQuad();

                _patternTextures = new Texture2D[PatternVariantCount];

                for (int i = 0; i < _patternTextures.Length; i++)
                {
                    _patternTextures[i] =
                        BuildPatternTexture(PatternTextureSize, i);
                }

                _patternMaterial = new Material(shader);
                _patternMaterial.name = "WVC_DMT_PatternTemplate";

                ConfigureTransparency(_patternMaterial);

                for (int i = 0; i < _patterns.Length; i++)
                {
                    DmtSurfacePattern pattern = new DmtSurfacePattern();
                    _patterns[i] = pattern;

                    pattern.Root =
                        new GameObject("WVC_DMT_SurfacePattern_" + i);

                    pattern.Root.SetActive(false);
                    pattern.Root.transform.SetParent(_owner.transform, false);

                    var filter = pattern.Root.AddComponent<MeshFilter>();
                    pattern.Renderer =
                        pattern.Root.AddComponent<MeshRenderer>();

                    pattern.Material = new Material(_patternMaterial);
                    pattern.Material.name = "WVC_DMT_PatternMaterial_" + i;

                    filter.sharedMesh = _patternMesh;
                    pattern.Renderer.sharedMaterial = pattern.Material;

                    pattern.Renderer.shadowCastingMode =
                        ShadowCastingMode.Off;

                    pattern.Renderer.receiveShadows = false;

                    SetPatternColor(pattern.Material, Color.clear);
                }

                _patternsReady = true;

                WvcLog.Msg(
                    "[DMT Effect] Three geometry textures and " +
                    PatternPoolSize + " pooled surface patterns ready."
                );
            }
            catch (Exception)
            {
                _patternsFailed = true;
                DestroyPatternResources();


            }
        }

        private static void ConfigureTransparency(Material material)
        {
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);

            SetFloatIfPresent(
                material, "_SrcBlend", (float)BlendMode.SrcAlpha
            );

            SetFloatIfPresent(
                material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha
            );

            SetFloatIfPresent(
                material, "_SrcBlendAlpha", (float)BlendMode.One
            );

            SetFloatIfPresent(
                material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha
            );

            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");

            material.SetShaderPassEnabled("ShadowCaster", false);
        }

        private static void SetFloatIfPresent(
            Material material,
            string property,
            float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetPatternTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }

        private static void SetPatternColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        /// <summary>
        /// Puts one more pattern on a surface in front of the player, if there is room for it. Answers
        /// whether it did, so the caller can ask for several in a row and stop as soon as the pool is
        /// full rather than silently asking for the rest of the tick.
        /// </summary>
        private bool TrySpawnPattern(float intensity)
        {
            float strength = _patternBlend * intensity;

            if (!_patternsReady || strength < 0.06f)
                return false;

            if (_camera == null ||
                !_camera.enabled ||
                !_camera.gameObject.activeInHierarchy)
            {
                _camera = GetGameplayCamera();

                if (_camera == null)
                    return false;

                ConfigureCameraLayers(_camera);
            }

            int activeCount = 0;
            DmtSurfacePattern free = null;

            foreach (DmtSurfacePattern pattern in _patterns)
            {
                if (pattern == null)
                    continue;

                if (pattern.Active)
                    activeCount++;
                else if (free == null)
                    free = pattern;
            }

            int targetCount = Mathf.Clamp(
                Mathf.CeilToInt(strength * PatternPoolSize),
                1,
                PatternPoolSize
            );

            if (free == null || activeCount >= targetCount)
                return false;

            for (int attempt = 0; attempt < 4; attempt++)
            {
                // The golden ratio spread over the viewport: successive aims land far apart from each
                // other and only come back near an earlier one after many of them, so the patterns end
                // up spread over the room instead of piled onto whatever is straight ahead.
                _spawnCounter++;

                float screenX = Fraction(_spawnCounter * 0.6180339887f);
                float screenY = Fraction(_spawnCounter * 0.7548776662f);

                Ray ray = _camera.ViewportPointToRay(
                    new Vector3(
                        0.10f + screenX * 0.80f,
                        0.14f + screenY * 0.72f,
                        0f
                    )
                );

                if (!Physics.Raycast(
                        ray,
                        out RaycastHit hit,
                        PatternMaxDistance,
                        ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (hit.collider == null || hit.distance < 3f)
                    continue;

                if (hit.collider.GetComponentInParent<NativePlayer>() != null ||
                    hit.collider.GetComponentInParent<NativeNPC>() != null)
                {
                    continue;
                }

                if (IsTooCloseToExistingPattern(hit.point))
                    continue;

                ActivatePattern(free, hit);
                return true;
            }

            return false;
        }

        private bool IsTooCloseToExistingPattern(Vector3 point)
        {
            foreach (DmtSurfacePattern pattern in _patterns)
            {
                if (pattern == null ||
                    !pattern.Active ||
                    pattern.Root == null)
                {
                    continue;
                }

                // Roughly a whole pattern's width: two of them may not sit closer than that, which is what
                // keeps them spread out over the surfaces rather than stacking on top of each other. It is
                // also what keeps the see-through overdraw down, which is the one cost of these that the
                // frame rate actually pays for.
                float spacing = pattern.BaseSize * 0.9f;

                if ((pattern.Root.transform.position - point).sqrMagnitude
                    < spacing * spacing)
                {
                    return true;
                }
            }

            return false;
        }

        private void ActivatePattern(
            DmtSurfacePattern pattern,
            RaycastHit hit)
        {
            Transform surface = hit.collider.transform;

            Vector3 up =
                Vector3.ProjectOnPlane(_camera.transform.up, hit.normal);

            if (up.sqrMagnitude < 0.001f)
            {
                up = Vector3.ProjectOnPlane(
                    _camera.transform.right,
                    hit.normal
                );
            }

            if (up.sqrMagnitude < 0.001f)
                return;

            pattern.Surface = surface;
            pattern.LocalPoint = surface.InverseTransformPoint(hit.point);
            pattern.LocalNormal =
                surface.InverseTransformDirection(hit.normal);

            pattern.LocalUp =
                surface.InverseTransformDirection(up.normalized);

            pattern.Age = 0f;
            pattern.Lifetime = RandomRange(3.6f, 6.5f);

            // The pattern is sized from how far away the surface is, so it always covers a good part of
            // the view rather than a patch of wall: these are the numbers that decide how big the trip
            // looks, and they are deliberately large - but only as large as the surface will take.
            pattern.Root.layer = _patternLayer;
            pattern.Root.transform.position = hit.point + hit.normal * 0.015f;
            pattern.Root.transform.rotation =
                Quaternion.LookRotation(hit.normal, up.normalized);

            pattern.MaxHalfWidth = SurfaceHalfWidth(
                hit,
                pattern.Root.transform.right,
                pattern.Root.transform.up
            );

            pattern.BaseSize = Mathf.Min(
                Mathf.Clamp(hit.distance * 0.42f, 1.8f, 8f),
                pattern.MaxHalfWidth * 2f
            );

            pattern.SpinSpeed = RandomRange(-7f, 7f);
            pattern.Phase = RandomRange(0f, Mathf.PI * 2f);

            pattern.BaseColor =
                PatternColors[_random.Next(PatternColors.Length)];

            SetPatternTexture(
                pattern.Material,
                _patternTextures[_random.Next(_patternTextures.Length)]
            );

            Color initial = pattern.BaseColor;
            initial.a = 0f;
            SetPatternColor(pattern.Material, initial);

            pattern.Root.transform.localScale =
                Vector3.one * pattern.BaseSize;

            pattern.Active = true;
            pattern.Root.SetActive(true);
        }

        /// <summary>
        /// How much room the surface a pattern landed on has around it, as a half-width: the distance
        /// from the point to the edge of the surface's own bounds, along the two directions the pattern
        /// is laid out in, whichever is nearer.
        ///
        /// The pattern's size is chosen from how far away the surface is, which is what makes it cover
        /// the view - and which is also how it ended up wider than the door, crate or car it was painted
        /// on. The surface is therefore asked how big it is, and the answer is what the pattern is held
        /// under.
        /// </summary>
        private static float SurfaceHalfWidth(RaycastHit hit, Vector3 right, Vector3 up)
        {
            try
            {
                Collider collider = hit.collider;

                if (collider == null)
                    return float.MaxValue;

                Bounds bounds = collider.bounds;

                float across = ReachToEdge(bounds, hit.point, right);
                float along = ReachToEdge(bounds, hit.point, up);

                return Mathf.Min(across, along) * 0.9f;
            }
            catch
            {
                return float.MaxValue;
            }
        }

        /// <summary>How far a point is from the edge of a box, travelling along one direction.</summary>
        private static float ReachToEdge(Bounds bounds, Vector3 point, Vector3 direction)
        {
            float reach = float.MaxValue;

            for (int axis = 0; axis < 3; axis++)
            {
                float along = direction[axis];

                if (Mathf.Abs(along) < 0.0001f)
                    continue;

                float offset = point[axis] - bounds.center[axis];
                float edge = bounds.extents[axis];

                float distance = along > 0f
                    ? (edge - offset) / along
                    : (-edge - offset) / along;

                if (distance < reach)
                    reach = Mathf.Max(0f, distance);
            }

            return reach;
        }

        private void UpdatePatterns(float dt, float intensity)
        {
            float strength = _patternBlend * intensity;
            float motion = DMTScreenEffect.ReducedMotion ? 0f : 1f;

            foreach (DmtSurfacePattern pattern in _patterns)
            {
                if (pattern == null || !pattern.Active)
                    continue;

                if (pattern.Root == null ||
                    pattern.Material == null ||
                    pattern.Surface == null)
                {
                    HidePattern(pattern);
                    continue;
                }

                pattern.Age += dt;

                if (pattern.Age >= pattern.Lifetime)
                {
                    HidePattern(pattern);
                    continue;
                }

                float lifeFade =
                    Ramp(0f, 1.1f, pattern.Age) *
                    (1f - Ramp(
                        pattern.Lifetime - 1.4f,
                        pattern.Lifetime,
                        pattern.Age
                    ));

                float breath =
                    0.5f + Mathf.Sin(
                        _animationTime * 0.80f + pattern.Phase
                    ) * 0.5f;

                float colorShift =
                    0.5f + Mathf.Sin(
                        _animationTime * 0.22f + pattern.Phase
                    ) * 0.5f;

                // Two palette colours either side of the pattern's own, so the wall is never one flat
                // colour and the colours keep moving past each other through the trip.
                int palette = PatternColors.Length;
                int first = (int)(colorShift * palette) % palette;
                int second = (first + 5) % palette;

                Color shimmer = Color.Lerp(
                    PatternColors[first],
                    PatternColors[second],
                    colorShift
                );

                Color color = Color.Lerp(
                    pattern.BaseColor,
                    shimmer,
                    0.5f
                );

                color.a = strength * lifeFade * (0.30f + breath * 0.14f);
                SetPatternColor(pattern.Material, color);

                Vector3 normal = pattern.Surface.TransformDirection(
                    pattern.LocalNormal
                ).normalized;

                Vector3 up = pattern.Surface.TransformDirection(
                    pattern.LocalUp
                ).normalized;

                pattern.Root.transform.position =
                    pattern.Surface.TransformPoint(pattern.LocalPoint) +
                    normal * 0.015f;

                pattern.Root.transform.rotation =
                    Quaternion.LookRotation(normal, up) *
                    Quaternion.AngleAxis(
                        pattern.Age * pattern.SpinSpeed * motion,
                        Vector3.forward
                    );

                // The pattern grows as it lives, so a big one starts as a spot on the wall and opens
                // out over the couple of seconds it is up - but never past the edge of the surface its
                // own size was measured against.
                float opening =
                    1f + Ramp(0f, pattern.Lifetime * 0.85f, pattern.Age) * 1.1f * motion;

                float size =
                    pattern.BaseSize * opening *
                    (1f + (breath - 0.5f) * 0.22f * motion * _peakBlend);

                size = Mathf.Min(size, pattern.MaxHalfWidth * 2f);

                pattern.Root.transform.localScale = Vector3.one * size;
            }
        }

        private static void HidePattern(DmtSurfacePattern pattern)
        {
            pattern.Active = false;
            pattern.Surface = null;

            if (pattern.Root != null)
                pattern.Root.SetActive(false);
        }

        private void HideAllPatterns()
        {
            foreach (DmtSurfacePattern pattern in _patterns)
            {
                if (pattern != null)
                    HidePattern(pattern);
            }
        }

        private void DestroyPatternResources()
        {
            for (int i = 0; i < _patterns.Length; i++)
            {
                DmtSurfacePattern pattern = _patterns[i];

                if (pattern == null)
                    continue;

                if (pattern.Root != null)
                    UnityEngine.Object.Destroy(pattern.Root);

                if (pattern.Material != null)
                    UnityEngine.Object.Destroy(pattern.Material);

                _patterns[i] = null;
            }

            if (_patternTextures != null)
            {
                foreach (Texture2D texture in _patternTextures)
                {
                    if (texture != null)
                        UnityEngine.Object.Destroy(texture);
                }
            }

            if (_patternMaterial != null)
                UnityEngine.Object.Destroy(_patternMaterial);

            if (_patternMesh != null)
                UnityEngine.Object.Destroy(_patternMesh);

            _patternTextures = null;
            _patternMaterial = null;
            _patternMesh = null;
            _patternsReady = false;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private static Mesh CreatePatternQuad()
        {
            Mesh mesh = new Mesh();
            mesh.name = "WVC_DMT_SurfacePatternQuad";

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Texture2D BuildPatternTexture(int size, int type)
        {
            Texture2D texture =
                new Texture2D(size, size, TextureFormat.RGBA32, true);

            texture.name = "WVC_DMT_Geometry_" + type;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1) * 2f - 1f;
                    float v = y / (float)(size - 1) * 2f - 1f;

                    float alpha = PatternAlpha(u, v, type);

                    byte a = (byte)Math.Max(
                        0,
                        Math.Min(255, (int)(alpha * 255f))
                    );

                    pixels[y * size + x] =
                        new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, true);

            return texture;
        }

        /// <summary>
        /// The shape of one surface pattern, as a coverage value for every pixel of its square: thin bright
        /// lines over black, faded out at the rim so that every variant ends the same way. The variants are
        /// deliberately different kinds of figure rather than variations on one of them, so that no two of
        /// the patterns up at the same time read as the same wall.
        /// </summary>
        private static float PatternAlpha(float x, float y, int type)
        {
            float radius = (float)Math.Sqrt(x * x + y * y);

            if (radius >= 1f)
                return 0f;

            float edge = 1f - Ramp(0.72f, 1f, radius);
            float angle = (float)Math.Atan2(y, x);
            float value;

            if (type == 0)
            {
                float petalRadius =
                    0.53f + 0.16f * (float)Math.Cos(angle * 8f);

                float petals = Line(radius - petalRadius, 0.025f);

                float rings = Line(
                    (float)Math.Sin(radius * 23f),
                    0.065f
                );

                float spokes = Line(
                    (float)Math.Sin(angle * 8f + radius * 3f),
                    0.05f
                ) * Ramp(0.12f, 0.30f, radius);

                value = Math.Max(
                    petals,
                    Math.Max(rings * 0.75f, spokes * 0.75f)
                );
            }
            else if (type == 1)
            {
                float a = Line(
                    (float)Math.Sin((x * 0.8660254f + y * 0.5f) * 18f),
                    0.065f
                );

                float b = Line(
                    (float)Math.Sin((x * 0.8660254f - y * 0.5f) * 18f),
                    0.065f
                );

                float c = Line(
                    (float)Math.Sin(y * 18f),
                    0.065f
                );

                float outerRing = Line(radius - 0.75f, 0.018f);

                value = Math.Max(
                    Math.Max(a, Math.Max(b, c)) * 0.85f,
                    outerRing
                );
            }
            else if (type == 2)
            {
                float square = Math.Max(Math.Abs(x), Math.Abs(y));

                float rotatedX = (x + y) * 0.7071068f;
                float rotatedY = (y - x) * 0.7071068f;
                float diamond = Math.Max(
                    Math.Abs(rotatedX),
                    Math.Abs(rotatedY)
                );

                float squareLines = Line(
                    (float)Math.Sin(square * 25f),
                    0.07f
                );

                float diamondLines = Line(
                    (float)Math.Sin(diamond * 25f + 0.7f),
                    0.07f
                );

                value = Math.Max(
                    squareLines * 0.85f,
                    diamondLines * 0.75f
                );
            }

            else if (type == 3)
            {
                // Three arms winding out of the middle, each crossing the last as it goes.
                float spiral = Line(
                    (float)Math.Sin(angle * 3f + radius * 9f),
                    0.075f
                );

                float arms = Line(
                    (float)Math.Sin(angle * 3f),
                    0.02f
                ) * Ramp(0.35f, 0.95f, radius);

                value = Math.Max(spiral, arms * 0.8f);
            }
            else if (type == 4)
            {
                // A target: rings all the way out, ticks along the rim, and a spot in the middle.
                float rings = Line(
                    (float)Math.Sin(radius * 30f),
                    0.055f
                );

                float ticks = Line(
                    (float)Math.Sin(angle * 20f),
                    0.035f
                ) * Ramp(0.58f, 0.92f, radius);

                float core = 1f - Ramp(0.05f, 0.14f, radius);

                value = Math.Max(rings, Math.Max(ticks, core));
            }
            else if (type == 5)
            {
                // Gratings laid across each other and multiplied, so what comes out is the moire between
                // them: a lattice of bright nodes with darker cells around it.
                float left = Line(
                    (float)Math.Sin((x * 0.8660254f + y * 0.5f) * 26f),
                    0.06f
                );

                float right = Line(
                    (float)Math.Sin((x * 0.8660254f - y * 0.5f) * 26f),
                    0.06f
                );

                float level = Line(
                    (float)Math.Sin(y * 26f),
                    0.06f
                );

                value = Math.Max(
                    Math.Max(left * right, left * level),
                    right * level
                );
            }
            else if (type == 6)
            {
                // A burst of long thin rays out of the middle, with a halo where they start.
                float rays = Line(
                    (float)Math.Sin(angle * 24f),
                    0.028f
                ) * Ramp(0.12f, 0.85f, radius);

                float halo = Line(radius - 0.22f, 0.02f);

                value = Math.Max(rays, halo);
            }
            else if (type == 7)
            {
                // The figure a yantra is built from: diamonds inside one another, a circle drawn through
                // them, and a spot at the centre.
                float rotatedX = (x + y) * 0.7071068f;
                float rotatedY = (y - x) * 0.7071068f;
                float diamond = Math.Max(
                    Math.Abs(rotatedX),
                    Math.Abs(rotatedY)
                );

                float nested = Math.Max(
                    Line(diamond - 0.30f, 0.03f),
                    Math.Max(
                        Line(diamond - 0.58f, 0.03f),
                        Line(diamond - 0.86f, 0.03f)
                    )
                );

                float circle = Line(radius - 0.62f, 0.018f);
                float spot = 1f - Ramp(0.04f, 0.10f, radius);

                value = Math.Max(nested, Math.Max(circle, spot));
            }

            else if (type == 8)
            {
                // A halftone field: dots on a grid, opening wider the further out they are, the way a
                // printed gradient is built out of dots.
                float cell = 0.20f;

                float inCellX = Fraction(x / cell + 0.5f) - 0.5f;
                float inCellY = Fraction(y / cell + 0.5f) - 0.5f;

                float fromCentre = (float)Math.Sqrt(
                    inCellX * inCellX + inCellY * inCellY
                ) * cell;

                float dotRadius = cell * (0.10f + 0.30f * radius);

                value = 1f - Ramp(dotRadius * 0.55f, dotRadius, fromCentre);
            }
            else if (type == 9)
            {
                // Ripples: rings pushed about by a slower wave, so they read as water rather than as
                // another target.
                float wobble = (float)Math.Sin(angle * 5f) * 1.1f;

                float ripples = Line(
                    (float)Math.Sin(radius * 21f + wobble),
                    0.07f
                );

                float crest = Line(
                    (float)Math.Sin(radius * 7f + wobble * 0.4f),
                    0.02f
                ) * 0.7f;

                value = Math.Max(ripples, crest);
            }
            else if (type == 10)
            {
                // Chevrons marching across the square, with a second set crossing them.
                float zigzag = Line(
                    (float)Math.Sin(y * 18f + Math.Abs(x) * 13f),
                    0.06f
                );

                float crossZigzag = Line(
                    (float)Math.Sin(x * 16f + Math.Abs(y) * 11f),
                    0.05f
                ) * 0.65f;

                value = Math.Max(zigzag, crossZigzag);
            }
            else if (type == 12)
            {
                // A diamond lattice: straight lines crossing at forty-five degrees, which reads as a
                // tunnel of squares where the rings-and-spokes figure reads as something circular.
                float diamond = Math.Abs(x) + Math.Abs(y);

                float lattice = Line(
                    (float)Math.Sin(diamond * 11f),
                    0.055f
                );

                float second = Line(
                    (float)Math.Sin((Math.Abs(x) - Math.Abs(y)) * 11f),
                    0.035f
                ) * 0.6f;

                value = Math.Max(lattice, second);
            }
            else if (type == 13)
            {
                // A sunburst: far more rays than the flower has, all of them starting just clear of the
                // middle, with a rim to stop them trailing off the edge.
                float rays = Line(
                    (float)Math.Sin(angle * 24f),
                    0.02f
                ) * Ramp(0.05f, 0.20f, radius);

                float rim = Line(radius - 0.86f, 0.018f);

                value = Math.Max(rays, rim);
            }
            else if (type == 14)
            {
                // Nested squares: the square equivalent of the target, opened out far enough to read as
                // boxed levels rather than as a grid.
                float square = Math.Max(Math.Abs(x), Math.Abs(y));

                float nested = Line(
                    (float)Math.Sin(square * 15f),
                    0.05f
                );

                float border = Line(radius - 0.90f, 0.018f);

                value = Math.Max(nested, border);
            }
            else if (type == 15)
            {
                // A mosaic: two families of diagonals crossing a straight grid, which breaks the square
                // into triangles instead of into cells.
                float rising = Line(
                    (float)Math.Sin((x + y) * 9f),
                    0.05f
                );

                float falling = Line(
                    (float)Math.Sin((x - y) * 9f),
                    0.05f
                ) * 0.8f;

                float grid = Line(
                    (float)Math.Sin(x * 9f),
                    0.03f
                ) * 0.5f;

                value = Math.Max(rising, Math.Max(falling, grid));
            }
            else if (type == 16)
            {
                // Moire: three fine wave trains multiplied together, so the figure that shows is the
                // interference between them rather than any one of them. Multiplying them is what makes
                // the pattern crawl as the pattern itself turns.
                float a = (float)Math.Sin(radius * 26f);
                float b = (float)Math.Sin((x * 0.6f + y) * 24f);
                float c = (float)Math.Sin((x - y * 0.6f) * 24f);

                value = Line(a * b * c, 0.16f);
            }
            else if (type == 17)
            {
                // A weave: two sets of close parallel lines where one is lifted over the other in
                // alternating squares, the way a basket is woven.
                float across = (float)Math.Sin(x * 20f);
                float down = (float)Math.Sin(y * 20f);
                float over = Ramp(0.35f, 0.65f, across * down);

                float warp = Line(across, 0.05f);
                float weft = Line(down, 0.05f);

                value = Math.Max(warp * (1f - over), weft * over);
            }
            else if (type == 18)
            {
                // A mandala: petals that are painted rather than outlined, and cut into rings across their
                // length, so it reads as a rosette laid on the wall instead of as more drawn curves.
                float petal = 0.36f + 0.30f * (float)Math.Cos(angle * 6f);

                float fill = 1f - Ramp(petal - 0.09f, petal, radius);

                value = fill * Bands(radius * 2.6f, 0.62f);
            }
            else if (type == 19)
            {
                // A kaleidoscope: mirrored wedges, each one a solid blade, banded along its length so the
                // mirror line itself shows as a seam. The mirroring is what makes it kaleidoscopic rather
                // than just radial.
                float sector = Fraction(angle * 1.2732395f);

                float mirror = Math.Abs(sector * 2f - 1f);

                float blade = 1f - Ramp(0.34f, 0.72f, mirror);

                value = blade * (0.55f + 0.45f * Bands(radius * 4.2f, 0.62f));
            }
            else if (type == 20)
            {
                // Topographic contours: a lumpy height field cut into filled bands, which is the look of a
                // contoured map. There is no circle and no straight line in it anywhere.
                float height =
                    Noise(x * 3.1f + 1.7f, y * 3.1f + 4.3f) +
                    0.45f * Noise(x * 7.3f - 2.1f, y * 7.3f + 0.6f);

                value = Bands(height * 5.5f, 0.55f) * 0.95f;
            }
            else if (type == 21)
            {
                // Fish scales: solid discs on an offset grid, each row overlapping the one above it, which
                // is what turns a field of dots into scales.
                float cell = 4.6f;

                float row = y * cell;
                float rowIndex = (float)Math.Floor(row);
                float offset = Fraction(rowIndex * 0.5f);

                float cx = Fraction(x * cell + offset + 0.5f) - 0.5f;
                float cy = row - rowIndex - 0.5f;

                float disc = (float)Math.Sqrt(cx * cx + cy * cy);

                value = 1f - Ramp(0.36f, 0.46f, disc);
            }
            else if (type == 22)
            {
                // A brick wall: solid bricks in offset rows with the mortar left dark. It is the only
                // variant that is a built structure rather than a figure.
                float brickWidth = 2.2f;
                float brickHeight = 4.6f;

                float row = y * brickHeight;
                float rowIndex = (float)Math.Floor(row);
                float offset = Fraction(rowIndex * 0.5f);

                float bx = Fraction(x * brickWidth + offset);
                float by = row - rowIndex;

                float mortar = Math.Min(
                    Math.Min(bx, 1f - bx),
                    Math.Min(by, 1f - by)
                );

                value = 1f - Ramp(0.05f, 0.11f, mortar);
            }
            else if (type == 23)
            {
                // Stains: soft irregular blobs out of a smooth noise field. It is the one variant with no
                // grid, no repetition and no symmetry in it at all, which is what makes the wall it lands
                // on look like it is growing something rather than wearing a pattern.
                float field =
                    0.55f * Noise(x * 2.3f, y * 2.3f) +
                    0.30f * Noise(x * 5.1f + 11.0f, y * 5.1f + 3.0f) +
                    0.15f * Noise(x * 9.7f, y * 9.7f + 7.0f);

                value = 1f - Ramp(0.40f, 0.60f, field);
            }
            else if (type == 24)
            {
                // A solid five-pointed star, cut through with soft bands, so the points read as raised out
                // of the surface rather than as part of a drawn figure.
                float star = 0.34f + 0.42f * (float)Math.Cos(angle * 5f);

                float solid = 1f - Ramp(star - 0.07f, star, radius);

                value = solid * (0.72f + 0.28f * Bands(radius * 5f, 0.60f));
            }
            else if (type == 25)
            {
                // A pinwheel: solid blades that come out curved, because the angle they are measured in is
                // pushed round by the distance from the middle, so the blades sweep out as they go.
                float spun = angle + radius * 1.8f;

                float sector = Fraction(spun * 1.1140846f);

                value = 1f - Ramp(0.38f, 0.62f, Math.Abs(sector - 0.5f) * 2f);
            }
            else if (type == 26)
            {
                // A honeycomb: cells on an offset grid with their walls left dark. Unlike the grid and the
                // weave it has no straight line in it either, so it does not read as another set of bars.
                float cell = 4.3f;

                float row = y * cell * 0.87f;
                float rowIndex = (float)Math.Floor(row);
                float offset = Fraction(rowIndex * 0.5f);

                float cx = Math.Abs(Fraction(x * cell + offset + 0.5f) - 0.5f);
                float cy = Math.Abs(row - rowIndex - 0.5f);

                float hex = Math.Max(cx * 1.12f, cx * 0.56f + cy * 0.97f);

                value = 1f - Ramp(0.30f, 0.37f, hex);
            }
            else if (type == 27)
            {
                // A circuit board: pads joined by right-angle traces on a grid. It is the one figure here
                // that was never a curve and was never a stripe.
                float cell = 4.1f;

                float ex = Fraction(x * cell + 0.5f) - 0.5f;
                float ey = Fraction(y * cell + 0.5f) - 0.5f;

                float pad = 1f - Ramp(0.09f, 0.15f, (float)Math.Sqrt(ex * ex + ey * ey));

                float ring = Math.Abs(
                    Math.Max(Math.Abs(ex), Math.Abs(ey)) - 0.40f
                );

                float trace = 1f - Ramp(0.035f, 0.070f, ring);

                value = Math.Max(pad, trace);
            }
            else
            {
                // A web: rays with rings strung between them, the rings held back at the middle so the
                // gaps a cobweb has are left open.
                float rays = Line(
                    (float)Math.Sin(angle * 14f),
                    0.03f
                );

                float rings = Line(
                    (float)Math.Sin(radius * 16f),
                    0.03f
                ) * Ramp(0.08f, 0.30f, radius);

                value = Math.Max(rays * 0.9f, rings);
            }

            return Math.Max(0f, Math.Min(1f, value * edge));
        }

        /// <summary>
        /// The part of a value past the decimal point. A repeating grid cell can be found from a position
        /// with it, without having to keep a running count of where the grid started.
        /// </summary>
        private static float Fraction(float value)
        {
            return value - (float)Math.Floor(value);
        }

        /// <summary>
        /// Alternating solid bands across a repeating value, with a soft edge to each band. Multiplying a
        /// shape by it gives that shape areas of colour rather than only lines, which is how the filled
        /// variants stay clear of the outlined ones.
        /// </summary>
        private static float Bands(float value, float width)
        {
            float across = Fraction(value);

            float fromCentre = Math.Abs(across - 0.5f) * 2f;

            return Ramp(1f, 1f - width, fromCentre);
        }

        /// <summary>
        /// A smooth lump of noise at a position: the same place always gives the same value, and it
        /// changes as gradually as a hill does rather than as a wave. That is what a stain or a contoured
        /// field needs and what a sine cannot give, because a sine repeats.
        /// </summary>
        private static float Noise(float x, float y)
        {
            int cellX = (int)Math.Floor(x);
            int cellY = (int)Math.Floor(y);

            float fx = x - cellX;
            float fy = y - cellY;

            // Smoothed so the value has no steps in it where the cells meet.
            float smoothX = fx * fx * (3f - 2f * fx);
            float smoothY = fy * fy * (3f - 2f * fy);

            float a = Hash(cellX, cellY);
            float b = Hash(cellX + 1, cellY);
            float c = Hash(cellX, cellY + 1);
            float d = Hash(cellX + 1, cellY + 1);

            return Mathf.Lerp(
                Mathf.Lerp(a, b, smoothX),
                Mathf.Lerp(c, d, smoothX),
                smoothY
            );
        }

        /// <summary>A repeatable value between zero and one for a grid cell, used as the raw material for
        /// the noise above. It is deliberately plain arithmetic: it has to be the same on every run, which
        /// a random number generator would not be.</summary>
        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;

                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;

                return (h & 0x7FFFFFFF) / 2147483647f;
            }
        }

        private static float Line(float distance, float width)
        {
            return 1f - Ramp(
                width,
                width * 2f,
                Math.Abs(distance)
            );
        }
    }

    internal sealed class DmtSurfacePattern
    {
        public GameObject Root;
        public MeshRenderer Renderer;
        public Material Material;

        public bool Active;
        public Transform Surface;

        public Vector3 LocalPoint;
        public Vector3 LocalNormal;
        public Vector3 LocalUp;

        public Color BaseColor;

        public float Age;
        public float Lifetime;
        public float BaseSize;
        public float SpinSpeed;
        public float Phase;

        /// <summary>
        /// The most the pattern may reach across before it hangs off the end of the surface it is on,
        /// as a half-width: taken from the surface's own bounds when it is placed, because a pattern
        /// wider than a door is the one part of the trip that reads as a mistake.
        /// </summary>
        public float MaxHalfWidth = float.MaxValue;
    }
}
