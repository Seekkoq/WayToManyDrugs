using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using NativeEQuality = Il2CppScheduleOne.ItemFramework.EQuality;
using NativeItemDefinition = Il2CppScheduleOne.ItemFramework.ItemDefinition;
using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using NativeShroomInstance = Il2CppScheduleOne.Product.ShroomInstance;

namespace CustomNPCExample.Products
{
    // ============================================================
    // Thin IL2CPP component. This is the ONLY type that gets
    // registered with ClassInjector. Keep it boring.
    // ============================================================
    public sealed class DMTScreenEffect : MonoBehaviour
    {
        // Public API used by DMTEffectManager. Statics are not injected.
        public static float EffectDuration = 90f;
        public static bool ShouldStart;
        public static bool ShouldStop;

        private DMTEffectController _controller;

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

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Dispose();
                _controller = null;
            }
        }

        private void EnsureController()
        {
            if (_controller != null)
                return;

            _controller = new DMTEffectController();
            _controller.Initialize();
        }
    }

    // ============================================================
    // Plain managed controller. NOT registered with IL2CPP.
    // All logic from the old component lives here.
    // ============================================================
    internal sealed class DMTEffectController
    {
        private const int MaxWorldPatterns = 8;
        private const float PatternMaxDistance = 28f;

        private float _timeRemaining;
        private float _pulseTimer;
        private float _nextPatternSpawnTime;

        private bool _active;
        private bool _setupOk;
        private bool _nativeShroomApplied;

        private Volume _volume;
        private GameObject _volumeObject;
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

        private Shader _patternShader;
        private Mesh _patternQuadMesh;

        private Texture2D _mandalaPatternTexture;
        private Texture2D _facePatternTexture;
        private Texture2D _geometryPatternTexture;

        private bool _patternsAvailable;
        private bool _patternFailureLogged;

        private readonly List<WorldPattern> _worldPatterns = new List<WorldPattern>();

        private static readonly Color Amber = new Color(1.00f, 0.55f, 0.12f);
        private static readonly Color Gold = new Color(1.00f, 0.83f, 0.26f);
        private static readonly Color Magenta = new Color(1.00f, 0.14f, 0.55f);
        private static readonly Color HotPink = new Color(1.00f, 0.34f, 0.70f);
        private static readonly Color Violet = new Color(0.53f, 0.18f, 0.92f);
        private static readonly Color Teal = new Color(0.08f, 0.88f, 0.80f);
        private static readonly Color Lime = new Color(0.62f, 1.00f, 0.25f);
        private static readonly Color DeepPurple = new Color(0.20f, 0.04f, 0.38f);

        // ------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------

        public void Initialize()
        {
            try
            {
                CreateVolume();
                CreatePatternResources();
                _setupOk = true;

                MelonLogger.Msg("[DMT Effect] Native shroom warp + world hallucination patterns ready.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DMT Effect] Setup failed: " + ex);
            }
        }

        public void Tick()
        {
            if (!_setupOk)
                return;

            if (DMTScreenEffect.ShouldStop)
            {
                DMTScreenEffect.ShouldStop = false;
                StopEffect();
                return;
            }

            if (DMTScreenEffect.ShouldStart)
            {
                DMTScreenEffect.ShouldStart = false;
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
            UpdateWorldPatterns();
            TrySpawnWorldPattern();
        }

        public void Dispose()
        {
            ClearWorldPatterns();
            ClearNativeShroomEffects();

            if (_volumeObject != null)
            {
                UnityEngine.Object.Destroy(_volumeObject);
                _volumeObject = null;
                _volume = null;
            }
        }

        private void StartEffect()
        {
            _timeRemaining = DMTScreenEffect.EffectDuration;
            _pulseTimer = 0f;
            _nextPatternSpawnTime = 0f;
            _active = true;

            ClearWorldPatterns();

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = true;
            }

            ApplyNativeShroomEffects();

            MelonLogger.Msg("[DMT Effect] Breakthrough started. Duration: " + DMTScreenEffect.EffectDuration + "s");
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

            ClearWorldPatterns();
            ClearNativeShroomEffects();

            MelonLogger.Msg("[DMT Effect] Breakthrough ended.");
        }

        // ------------------------------------------------------------
        // URP post processing
        // ------------------------------------------------------------

        private void CreateVolume()
        {
            if (_volume != null)
                return;

            _volumeObject = new GameObject("WVC_DMT_PostProcess");
            UnityEngine.Object.DontDestroyOnLoad(_volumeObject);

            _volume = _volumeObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10000f;
            _volume.weight = 0f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _profile;

            _colorAdjust = _profile.Add<ColorAdjustments>(true);
            _bloom = _profile.Add<Bloom>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _lens = _profile.Add<LensDistortion>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);
            _channelMixer = _profile.Add<ChannelMixer>(true);
            _whiteBalance = _profile.Add<WhiteBalance>(true);
            _splitToning = _profile.Add<SplitToning>(true);

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
            _bloom.scatter.value = 0.82f;

            _chromatic.active = true;
            _chromatic.intensity.overrideState = true;

            _lens.active = true;
            _lens.intensity.overrideState = true;
            _lens.scale.overrideState = true;
            _lens.xMultiplier.overrideState = true;
            _lens.yMultiplier.overrideState = true;
            _lens.scale.value = 1f;
            _lens.xMultiplier.value = 1f;
            _lens.yMultiplier.value = 1f;

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

            _volume.enabled = false;
        }

        private void ApplyEffects()
        {
            float duration = DMTScreenEffect.EffectDuration;
            float elapsed = duration - _timeRemaining;

            float fadeIn = Mathf.Clamp01(elapsed / 2.0f);
            float fadeOut = Mathf.Clamp01(_timeRemaining / 15f);
            float fade = Mathf.Min(fadeIn, fadeOut);

            float pulseA = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.50f) * 0.5f;
            float pulseB = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 1.15f) * 0.5f;
            float pulseC = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 2.30f) * 0.5f;
            float swirl = _pulseTimer * 0.72f;

            _volume.weight = fade;

            _colorAdjust.saturation.value = (38f + pulseA * 34f) * fade;
            _colorAdjust.contrast.value = (14f + pulseB * 13f) * fade;
            _colorAdjust.postExposure.value = (0.08f + pulseA * 0.20f) * fade;
            _colorAdjust.hueShift.value = (Mathf.Sin(swirl * 0.9f) * 42f + Mathf.Sin(swirl * 0.28f) * 18f) * fade;

            Color worldFilter = Color.Lerp(
                Color.Lerp(Amber, Magenta, pulseA),
                Color.Lerp(Teal, Violet, pulseB),
                pulseC);

            _colorAdjust.colorFilter.value = Color.Lerp(Color.white, worldFilter, fade * 0.68f);

            _bloom.intensity.value = (1.10f + pulseA * 1.25f) * fade;
            _bloom.tint.value = Color.Lerp(Gold, HotPink, pulseB);

            _chromatic.intensity.value = (0.20f + pulseC * 0.24f) * fade;

            _lens.intensity.value = (Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.40f) * 0.23f + pulseB * 0.07f) * fade;
            _lens.scale.value = 1f - 0.05f * pulseA * fade;
            _lens.xMultiplier.value = 1f + pulseB * 0.10f * fade;
            _lens.yMultiplier.value = 1f + pulseC * 0.14f * fade;

            _vignette.intensity.value = (0.33f + pulseA * 0.20f) * fade;
            _vignette.color.value = Color.Lerp(DeepPurple, Magenta, pulseB * 0.5f);

            _grain.intensity.value = (0.12f + pulseC * 0.15f) * fade;

            float mix = Mathf.Sin(swirl * 1.10f) * 26f * fade;

            _channelMixer.redOutRedIn.value = 100f;
            _channelMixer.redOutGreenIn.value = mix * 0.60f;
            _channelMixer.redOutBlueIn.value = -mix * 0.20f;
            _channelMixer.greenOutRedIn.value = mix * 0.22f;
            _channelMixer.greenOutGreenIn.value = 100f;
            _channelMixer.greenOutBlueIn.value = mix * 0.34f;
            _channelMixer.blueOutRedIn.value = mix * 0.40f;
            _channelMixer.blueOutGreenIn.value = -mix * 0.18f;
            _channelMixer.blueOutBlueIn.value = 100f;

            _whiteBalance.temperature.value = Mathf.Lerp(-18f, 38f, pulseA) * fade;
            _whiteBalance.tint.value = Mathf.Sin(swirl) * 18f * fade;

            _splitToning.shadows.value = Color.Lerp(DeepPurple, Violet, pulseB);
            _splitToning.highlights.value = Color.Lerp(Gold, Teal, pulseA * 0.35f);
            _splitToning.balance.value = Mathf.Lerp(-20f, 20f, pulseC) * fade;
        }

        // ------------------------------------------------------------
        // World-space patterns / faces
        // ------------------------------------------------------------

        private void CreatePatternResources()
        {
            if (_patternsAvailable)
                return;

            _patternShader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            if (_patternShader == null)
            {
                _patternsAvailable = false;
                MelonLogger.Warning("[DMT Effect] No usable shader found for world patterns. Patterns disabled, post-processing still active.");
                return;
            }

            _patternQuadMesh = CreateDoubleSidedQuadMesh();
            _mandalaPatternTexture = BuildPatternTexture(256, 0);
            _facePatternTexture = BuildPatternTexture(256, 1);
            _geometryPatternTexture = BuildPatternTexture(256, 2);

            _patternsAvailable =
                _patternQuadMesh != null &&
                _mandalaPatternTexture != null &&
                _facePatternTexture != null &&
                _geometryPatternTexture != null;

            MelonLogger.Msg("[DMT Effect] World pattern resources ready. Shader: " + _patternShader.name);
        }

        private void TrySpawnWorldPattern()
        {
            if (!_patternsAvailable)
                return;

            if (Time.time < _nextPatternSpawnTime)
                return;

            if (_worldPatterns.Count >= MaxWorldPatterns)
                return;

            _nextPatternSpawnTime = Time.time + UnityEngine.Random.Range(0.75f, 1.35f);

            try
            {
                Camera camera = GetWorldCamera();
                if (camera == null)
                    return;

                for (int attempt = 0; attempt < 4; attempt++)
                {
                    float viewportX = UnityEngine.Random.Range(0.10f, 0.90f);
                    float viewportY = UnityEngine.Random.Range(0.12f, 0.86f);

                    Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));

                    if (!Physics.Raycast(ray, out RaycastHit hit, PatternMaxDistance, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    if (hit.collider == null || hit.distance < 1.1f)
                        continue;

                    string hitName = hit.collider.gameObject.name.ToLowerInvariant();

                    if (hitName.Contains("player") || hitName.Contains("npc") || hitName.Contains("ragdoll"))
                        continue;

                    SpawnPatternOnSurface(camera, hit);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (!_patternFailureLogged)
                {
                    _patternFailureLogged = true;
                    MelonLogger.Warning("[DMT Effect] Pattern spawn failed. Disabling world patterns to prevent stutter: " + ex.Message);
                }

                _patternsAvailable = false;
            }
        }

        private void SpawnPatternOnSurface(Camera camera, RaycastHit hit)
        {
            if (!_patternsAvailable || _patternShader == null || _patternQuadMesh == null)
                return;

            int patternType = UnityEngine.Random.Range(0, 3);
            Texture2D texture = GetCachedPatternTexture(patternType);
            if (texture == null)
                return;

            Material material = new Material(_patternShader);
            if (material == null)
                return;

            material.name = "WVC_DMT_WorldPattern";
            material.mainTexture = texture;

            Color baseColor = GetRandomPatternColor();
            Color transparentColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", transparentColor);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", transparentColor);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);

            material.renderQueue = 3000;

            GameObject root = new GameObject("WVC_DMT_Hallucination");
            MeshFilter filter = root.AddComponent<MeshFilter>();
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();

            filter.sharedMesh = _patternQuadMesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Vector3 surfaceUp = Vector3.ProjectOnPlane(camera.transform.up, hit.normal);

            if (surfaceUp.sqrMagnitude < 0.001f)
                surfaceUp = Vector3.ProjectOnPlane(camera.transform.right, hit.normal);

            if (surfaceUp.sqrMagnitude < 0.001f)
                surfaceUp = Vector3.up;

            Quaternion baseRotation = Quaternion.LookRotation(hit.normal, surfaceUp.normalized);

            float scale = Mathf.Clamp(hit.distance * 0.16f, 0.35f, 1.65f);
            scale *= UnityEngine.Random.Range(0.65f, 1.10f);

            root.transform.position = hit.point + hit.normal * 0.012f;
            root.transform.rotation = baseRotation;
            root.transform.localScale = Vector3.one * scale;

            _worldPatterns.Add(new WorldPattern
            {
                Root = root,
                Renderer = renderer,
                Material = material,
                AnchorPosition = hit.point,
                SurfaceNormal = hit.normal,
                BaseRotation = baseRotation,
                BaseColor = baseColor,
                SpawnTime = Time.time,
                Lifetime = UnityEngine.Random.Range(3.0f, 6.0f),
                BaseScale = scale,
                SpinSpeed = UnityEngine.Random.Range(-18f, 18f),
                Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
            });
        }

        private Texture2D GetCachedPatternTexture(int type)
        {
            switch (type)
            {
                case 1: return _facePatternTexture;
                case 2: return _geometryPatternTexture;
                default: return _mandalaPatternTexture;
            }
        }

        private void UpdateWorldPatterns()
        {
            for (int i = _worldPatterns.Count - 1; i >= 0; i--)
            {
                WorldPattern pattern = _worldPatterns[i];

                if (pattern == null || pattern.Root == null)
                {
                    _worldPatterns.RemoveAt(i);
                    continue;
                }

                float age = Time.time - pattern.SpawnTime;

                if (age >= pattern.Lifetime)
                {
                    DestroyPattern(pattern);
                    _worldPatterns.RemoveAt(i);
                    continue;
                }

                float fadeIn = Mathf.Clamp01(age / 0.25f);
                float fadeOut = Mathf.Clamp01((pattern.Lifetime - age) / 0.75f);
                float lifeFade = Mathf.Min(fadeIn, fadeOut);

                float pulse = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 1.3f + pattern.Phase) * 0.5f;
                float colorShift = 0.5f + Mathf.Sin(_pulseTimer * 0.9f + pattern.Phase) * 0.5f;

                Color color = Color.Lerp(pattern.BaseColor, Color.Lerp(HotPink, Teal, colorShift), 0.38f);
                color.a = (0.08f + pulse * 0.09f) * lifeFade;
                pattern.Material.color = color;

                float breathe = 1f + Mathf.Sin(_pulseTimer * 1.8f + pattern.Phase) * 0.11f;

                pattern.Root.transform.localScale = Vector3.one * pattern.BaseScale * breathe;
                pattern.Root.transform.position = pattern.AnchorPosition + pattern.SurfaceNormal * (0.010f + pulse * 0.004f);
                pattern.Root.transform.rotation = pattern.BaseRotation * Quaternion.AngleAxis(age * pattern.SpinSpeed, Vector3.forward);
            }
        }

        private void ClearWorldPatterns()
        {
            for (int i = 0; i < _worldPatterns.Count; i++)
                DestroyPattern(_worldPatterns[i]);

            _worldPatterns.Clear();
        }

        private static void DestroyPattern(WorldPattern pattern)
        {
            if (pattern == null)
                return;

            if (pattern.Root != null)
                UnityEngine.Object.Destroy(pattern.Root);

            if (pattern.Material != null)
                UnityEngine.Object.Destroy(pattern.Material);

            // Textures are cached globally and reused; never destroy them here.
        }

        private static Color GetRandomPatternColor()
        {
            Color[] colors = { Amber, Gold, Magenta, HotPink, Violet, Teal, Lime };
            Color selected = colors[UnityEngine.Random.Range(0, colors.Length)];
            return Color.Lerp(selected, Color.white, UnityEngine.Random.Range(0.04f, 0.18f));
        }

        private static Camera GetWorldCamera()
        {
            if (Camera.main != null)
                return Camera.main;

            Camera[] cameras = Camera.allCameras;
            Camera best = null;
            float highestDepth = float.MinValue;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || !camera.enabled)
                    continue;

                if (camera.depth > highestDepth)
                {
                    highestDepth = camera.depth;
                    best = camera;
                }
            }

            return best;
        }

        private static Mesh CreateDoubleSidedQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "WVC_DMT_WorldPatternQuad";

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.triangles = new int[]
            {
                0, 1, 2,  0, 2, 3,   // front
                2, 1, 0,  3, 2, 0    // back
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------
        // Procedural texture generation
        // 0 = mandala / eye, 1 = abstract face, 2 = warped geometry
        // ------------------------------------------------------------

        private static Texture2D BuildPatternTexture(int size, int type)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x / (float)(size - 1)) * 2f - 1f;
                    float v = (y / (float)(size - 1)) * 2f - 1f;
                    Vector2 p = new Vector2(u, v);

                    float alpha;
                    switch (type)
                    {
                        case 1: alpha = BuildFaceAlpha(p); break;
                        case 2: alpha = BuildWarpedGeometryAlpha(p); break;
                        default: alpha = BuildMandalaAlpha(p); break;
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static float BuildMandalaAlpha(Vector2 p)
        {
            float radius = p.magnitude;
            if (radius > 1.05f)
                return 0f;

            float angle = Mathf.Atan2(p.y, p.x);

            float starLines = ThinLine(Mathf.Abs(Mathf.Sin(angle * 8f + radius * 7f)), 0.055f);
            float ringLines = ThinLine(Mathf.Abs(Mathf.Sin(radius * 23f - Mathf.Sin(angle * 4f) * 2.5f)), 0.060f);
            float petalMask = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 1.8f);
            float innerEye = ThinRing(radius, 0.25f, 0.025f);
            float outerEye = ThinRing(radius, 0.72f, 0.025f);
            float edgeFade = 1f - Mathf.SmoothStep(0.72f, 1.05f, radius);

            float pattern = Mathf.Max(
                starLines * (0.4f + petalMask * 0.7f),
                ringLines * (0.45f + petalMask * 0.55f));

            pattern = Mathf.Max(pattern, innerEye * 0.85f);
            pattern = Mathf.Max(pattern, outerEye * 0.75f);

            return Mathf.Clamp01(pattern * edgeFade);
        }

        private static float BuildFaceAlpha(Vector2 p)
        {
            float faceRadius = Mathf.Sqrt((p.x * p.x) / 0.62f + (p.y * p.y) / 0.90f);
            float faceOutline = ThinRing(faceRadius, 1f, 0.030f);

            Vector2 leftEyeCenter = new Vector2(-0.27f, 0.18f);
            Vector2 rightEyeCenter = new Vector2(0.27f, 0.18f);

            float leftEye = ThinRing(Vector2.Distance(p, leftEyeCenter), 0.14f, 0.022f);
            float rightEye = ThinRing(Vector2.Distance(p, rightEyeCenter), 0.14f, 0.022f);

            float leftPupil = 1f - Mathf.SmoothStep(0.035f, 0.070f, Vector2.Distance(p, leftEyeCenter));
            float rightPupil = 1f - Mathf.SmoothStep(0.035f, 0.070f, Vector2.Distance(p, rightEyeCenter));

            float browLeft = ThinLine(Mathf.Abs(p.y - (0.38f + p.x * 0.18f)), 0.018f) * HorizontalMask(p.x, -0.55f, -0.06f);
            float browRight = ThinLine(Mathf.Abs(p.y - (0.38f - p.x * 0.18f)), 0.018f) * HorizontalMask(p.x, 0.06f, 0.55f);

            float nose = ThinLine(Mathf.Abs(p.x + Mathf.Sin(p.y * 12f) * 0.035f), 0.018f) * VerticalMask(p.y, -0.18f, 0.15f);

            float mouthCurve = -0.34f + Mathf.Cos(p.x * 8f) * 0.065f;
            float mouth = ThinLine(Mathf.Abs(p.y - mouthCurve), 0.018f) * HorizontalMask(p.x, -0.42f, 0.42f);

            float cheekLines = ThinLine(Mathf.Abs(Mathf.Sin(p.x * 13f + p.y * 5f)), 0.045f) * VerticalMask(p.y, -0.62f, -0.05f);

            float sideMask = 1f - Mathf.SmoothStep(0.75f, 1.05f, faceRadius);

            float alpha = Mathf.Max(faceOutline, Mathf.Max(leftEye, rightEye));
            alpha = Mathf.Max(alpha, Mathf.Max(leftPupil, rightPupil) * 0.85f);
            alpha = Mathf.Max(alpha, browLeft);
            alpha = Mathf.Max(alpha, browRight);
            alpha = Mathf.Max(alpha, nose);
            alpha = Mathf.Max(alpha, mouth);
            alpha = Mathf.Max(alpha, cheekLines * 0.45f);

            return Mathf.Clamp01(alpha * sideMask);
        }

        private static float BuildWarpedGeometryAlpha(Vector2 p)
        {
            float radius = p.magnitude;
            if (radius > 1.08f)
                return 0f;

            float angle = Mathf.Atan2(p.y, p.x);

            float waveA = ThinLine(Mathf.Abs(Mathf.Sin(p.x * 13f + Mathf.Sin(p.y * 7f) * 2.5f)), 0.070f);
            float waveB = ThinLine(Mathf.Abs(Mathf.Sin(p.y * 15f + Mathf.Cos(p.x * 6f) * 2.8f)), 0.070f);
            float spiral = ThinLine(Mathf.Abs(Mathf.Sin(angle * 5f + radius * 22f)), 0.055f);
            float diamond = ThinLine(Mathf.Abs(Mathf.Abs(p.x) + Mathf.Abs(p.y) - 0.58f), 0.030f);
            float edgeFade = 1f - Mathf.SmoothStep(0.72f, 1.08f, radius);

            float alpha = Mathf.Max(waveA * 0.75f, waveB * 0.75f);
            alpha = Mathf.Max(alpha, spiral * 0.85f);
            alpha = Mathf.Max(alpha, diamond * 0.60f);

            return Mathf.Clamp01(alpha * edgeFade);
        }

        private static float ThinLine(float distance, float width)
        {
            return 1f - Mathf.SmoothStep(width, width * 2f, distance);
        }

        private static float ThinRing(float distance, float radius, float width)
        {
            return ThinLine(Mathf.Abs(distance - radius), width);
        }

        private static float HorizontalMask(float value, float min, float max)
        {
            float left = Mathf.SmoothStep(min - 0.06f, min + 0.02f, value);
            float right = 1f - Mathf.SmoothStep(max - 0.02f, max + 0.06f, value);
            return left * right;
        }

        private static float VerticalMask(float value, float min, float max)
        {
            float bottom = Mathf.SmoothStep(min - 0.06f, min + 0.02f, value);
            float top = 1f - Mathf.SmoothStep(max - 0.02f, max + 0.06f, value);
            return bottom * top;
        }

        // ------------------------------------------------------------
        // Native shroom wall warping
        // ------------------------------------------------------------

        private void ApplyNativeShroomEffects()
        {
            if (_nativeShroomApplied)
                return;

            try
            {
                NativePlayer player = ResolvePlayer();
                NativeItemDefinition shroomDef = FindNativeShroomDefinition();

                if (player == null || shroomDef == null)
                {
                    MelonLogger.Warning("[DMT Effect] Native shroom distortion could not start.");
                    return;
                }

                var shroomInstance = new NativeShroomInstance(shroomDef, 1, (NativeEQuality)2, null);
                shroomInstance.ApplyEffectsToPlayer(player);

                _nativeShroomApplied = true;
                MelonLogger.Msg("[DMT Effect] Native shroom world-warping applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[DMT Effect] Native shroom apply failed: " + ex.Message);
            }
        }

        private void ClearNativeShroomEffects()
        {
            if (!_nativeShroomApplied)
                return;

            try
            {
                NativePlayer player = ResolvePlayer();
                NativeItemDefinition shroomDef = FindNativeShroomDefinition();

                if (player == null || shroomDef == null)
                {
                    _nativeShroomApplied = false;
                    return;
                }

                var shroomInstance = new NativeShroomInstance(shroomDef, 1, (NativeEQuality)2, null);
                shroomInstance.ClearEffectsFromPlayer(player);

                _nativeShroomApplied = false;
                MelonLogger.Msg("[DMT Effect] Native shroom world-warping cleared.");
            }
            catch (Exception ex)
            {
                _nativeShroomApplied = false;
                MelonLogger.Warning("[DMT Effect] Native shroom clear failed: " + ex.Message);
            }
        }

        private static NativePlayer ResolvePlayer()
        {
            try { return NativePlayer.Local; }
            catch { return null; }
        }

        private static NativeItemDefinition FindNativeShroomDefinition()
        {
            string[] ids = { "shroom", "magicmushroom" };

            foreach (string id in ids)
            {
                try
                {
                    object wrapper = S1API.Items.ItemManager.GetDefinition(id);
                    if (wrapper == null)
                        continue;

                    NativeItemDefinition definition = UnwrapDefinition(wrapper);
                    if (definition != null)
                        return definition;
                }
                catch
                {
                }
            }

            return null;
        }

        private static NativeItemDefinition UnwrapDefinition(object wrapper)
        {
            if (wrapper == null)
                return null;

            if (wrapper is NativeItemDefinition native)
                return native;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                string[] names = { "Pointer", "NativePointer", "ObjectPointer", "_pointer" };

                IntPtr pointer = IntPtr.Zero;
                Type type = wrapper.GetType();

                foreach (string name in names)
                {
                    PropertyInfo property = type.GetProperty(name, flags);
                    if (property != null && property.PropertyType == typeof(IntPtr))
                    {
                        pointer = (IntPtr)property.GetValue(wrapper);
                        if (pointer != IntPtr.Zero)
                            break;
                    }

                    FieldInfo field = type.GetField(name, flags);
                    if (field != null && field.FieldType == typeof(IntPtr))
                    {
                        pointer = (IntPtr)field.GetValue(wrapper);
                        if (pointer != IntPtr.Zero)
                            break;
                    }
                }

                if (pointer == IntPtr.Zero)
                    return null;

                return new NativeItemDefinition(pointer);
            }
            catch
            {
                return null;
            }
        }
    }

    // ============================================================
    // Plain data holder. Top-level, NOT nested in the component,
    // NOT registered with IL2CPP.
    // ============================================================
    internal sealed class WorldPattern
    {
        public GameObject Root;
        public MeshRenderer Renderer;
        public Material Material;

        public Vector3 AnchorPosition;
        public Vector3 SurfaceNormal;
        public Quaternion BaseRotation;

        public Color BaseColor;

        public float SpawnTime;
        public float Lifetime;
        public float BaseScale;
        public float SpinSpeed;
        public float Phase;
    }
}