using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomNPCExample.Products
{
    public static class MDMAEyeEffect
    {
        private static object _player;
        private static object _avatar;
        private static object _settings;

        public static object CachedPlayerAvatar => _avatar;

        private static bool _active;
        private static bool _savedOriginals;
        private static float _pulseTimer;

        /// <summary>
        /// The pulse that was last written to the game's own eye settings, and how long it has been since
        /// the last write. Writing the look means calling into the game to rebuild that part of the
        /// avatar, which is not something to do once a frame: it is written on a timer and only when the
        /// pulse has moved far enough to be seen.
        /// </summary>
        private static float _appliedPulse = -1f;
        private static float _lookTimer;

        private const float LookApplyInterval = 0.12f;

        private static float _originalPupilDilation;
        private static Color _originalEyeBallTint;
        private static Color _originalLeftEyelidColor;
        private static Color _originalRightEyelidColor;
        private static float _originalEyebrowHeight;
        private static float _originalEyebrowAngle;

        /// <summary>
        /// The heart pupils are switched off for now.
        ///
        /// Everything that draws them - the eye-rig writes, the per-frame repair, the overlays
        /// themselves - sits behind this one flag, so turning them off touches no call site:
        /// ShowHeartPupils is still called, it just does nothing, and the whole feature comes back by
        /// flipping this back to true. A readonly field rather than a const, so the checks are ordinary
        /// runtime branches and the compiler does not flag everything they guard as unreachable code.
        /// </summary>
        internal static readonly bool HeartPupilsEnabled = false;

        /// <summary>
        /// How much of the eye a heart spans, and the sizes it is never allowed past. A pupil-sized heart
        /// is what reads as a pupil rather than as a sticker: the eye is measured from its eyeball, and a
        /// third of that keeps the heart clear of the lid and well off the eyebrow.
        /// </summary>
        private const float HeartToEyeRatio = 0.34f;
        private const float FallbackHeartSize = 0.008f;
        private const float MaxHeartSize = 0.026f;

        /// <summary>
        /// How far in front of the pupil a heart sits, as a share of the eyeball's radius, on top of the
        /// curve it has to clear. A flat quad laid on a ball dips into it at the corners, and without this
        /// lift those corners end up inside the head.
        /// </summary>
        private const float HeartLift = 0.08f;

        /// <summary>The hearts laid over the pupils while the effect runs, all of them the mod's own.</summary>
        private static readonly List<GameObject> HeartOverlays = new List<GameObject>();

        /// <summary>
        /// The renderers of those hearts, kept alongside them so the check that runs every frame can look
        /// at a renderer without asking the game for one, and so a heart the icon guard has hidden can be
        /// shown again the same frame.
        /// </summary>
        private static readonly List<Renderer> HeartRenderers = new List<Renderer>();

        /// <summary>How many times the hearts have had to be put back, for the log.</summary>
        private static int _heartRepairs;

        /// <summary>
        /// The most times the hearts will be replaced in one trip. If the game were rebuilding its eye
        /// rig every frame, putting them back every frame would cost more than the hearts are worth, so
        /// the mod gives up after this many and says so.
        /// </summary>
        private const int MaxHeartRepairs = 200;

        /// <summary>How wide the last heart placed came out, in world units, for the log.</summary>
        private static float _lastHeartSize;

        private static Texture2D _heartTexture;
        private static Material _heartMaterial;
        private static Mesh _heartQuad;

        public static void SetPlayer(object player)
        {
            if (player == null) return;
            _player = player;
            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Eyes] Player reference captured.");
        }

        public static void StartEyeEffect()
        {
            try
            {
                if (!TryGetAvatarAndSettings())
                {

                    return;
                }

                if (!_savedOriginals)
                {
                    _originalPupilDilation = GetFloat(_settings, "PupilDilation", 0.5f);
                    _originalEyeBallTint = GetColor(_settings, "EyeBallTint", Color.white);
                    _originalLeftEyelidColor = GetColor(_settings, "LeftEyeLidColor", Color.white);
                    _originalRightEyelidColor = GetColor(_settings, "RightEyeLidColor", Color.white);
                    _originalEyebrowHeight = GetFloat(_settings, "EyebrowRestingHeight", 0f);
                    _originalEyebrowAngle = GetFloat(_settings, "EyebrowRestingAngle", 0f);
                    _savedOriginals = true;
                }

                _pulseTimer = 0f;
                _lookTimer = 0f;
                _appliedPulse = -1f;
                _heartRepairs = 0;
                _active = true;

                ApplyEyeLook(1f);
                ShowHeartPupils();

                global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Eyes] Pink dilated eyes applied.");
            }
            catch (Exception)
            {

            }
        }

        public static void UpdateEyeEffect()
        {
            if (!_active) return;

            try
            {
                if (!TryGetAvatarAndSettings()) return;

                _pulseTimer += Time.deltaTime;

                float pulse = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.65f) * 0.5f;

                // The hearts are checked before anything else, and every frame: they are the mod's own
                // objects sitting on the game's eye rig, so anything the game does to that rig - or
                // anything that hides a renderer, such as an icon capture - can take them away. Two
                // objects compared against null costs nothing and it means a heart is never gone for
                // longer than a frame.
                RepairHeartPupilsIfNeeded();

                _lookTimer += Time.deltaTime;

                if (_lookTimer < LookApplyInterval)
                    return;

                _lookTimer = 0f;

                // Nothing to write when the pulse has barely moved, and every write is a rebuild of the
                // part of the avatar it touches.
                if (Mathf.Abs(pulse - _appliedPulse) < 0.01f)
                    return;

                _appliedPulse = pulse;

                ApplyEyeLook(pulse);
            }
            catch { }
        }

        public static void StopEyeEffect()
        {
            if (!_active) return;

            try
            {
                // Taken off first: they are the mod's own objects, and nothing else in here has to have
                // worked for them to come away.
                HideHeartPupils();

                if (TryGetAvatarAndSettings() && _savedOriginals)
                {
                    SetFloat(_settings, "PupilDilation", _originalPupilDilation);
                    SetColor(_settings, "EyeBallTint", _originalEyeBallTint);
                    SetColor(_settings, "LeftEyeLidColor", _originalLeftEyelidColor);
                    SetColor(_settings, "RightEyeLidColor", _originalRightEyelidColor);
                    SetFloat(_settings, "EyebrowRestingHeight", _originalEyebrowHeight);
                    SetFloat(_settings, "EyebrowRestingAngle", _originalEyebrowAngle);
                    ApplyAllEyeSettings();
                }

                _active = false;
                _savedOriginals = false;
                _pulseTimer = 0f;

                global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Eyes] Original eyes restored.");
            }
            catch (Exception)
            {

            }
        }

        private static void ApplyEyeLook(float pulse)
        {
            float pupilSize = Mathf.Lerp(0.90f, 0.995f, pulse);

            Color eyePink = Color.Lerp(
                new Color(1.00f, 0.62f, 0.78f, 1f),
                new Color(1.00f, 0.12f, 0.55f, 1f),
                pulse
            );

            Color lidPink = Color.Lerp(
                new Color(0.85f, 0.30f, 0.48f, 1f),
                new Color(1.00f, 0.12f, 0.45f, 1f),
                pulse
            );

            SetFloat(_settings, "PupilDilation", pupilSize);
            SetColor(_settings, "EyeBallTint", eyePink);
            SetColor(_settings, "LeftEyeLidColor", lidPink);
            SetColor(_settings, "RightEyeLidColor", lidPink);

            SetFloat(_settings, "EyebrowRestingHeight", _originalEyebrowHeight + 0.07f + pulse * 0.03f);
            SetFloat(_settings, "EyebrowRestingAngle", _originalEyebrowAngle + 3f);

            ApplyAllEyeSettings();
        }

        /// <summary>
        /// Lays a heart over each pupil for as long as the effect runs.
        ///
        /// The eyes are the game's own, and the game's own <c>Eye</c> component says where its pupil is
        /// drawn (<c>PupilContainer</c>, with the pupil mesh in <c>PupilRend</c>), so a heart is a small
        /// flat quad parented to that container and scaled to the pupil it covers. Nothing of the game's
        /// is written to - no material of theirs is changed and no texture of theirs is replaced - which
        /// is what makes this something that can always be taken back off: the overlays are the mod's own
        /// objects and ending the effect destroys them.
        ///
        /// One heart per eye, laid over the pupil, for as long as the effect runs.
        ///
        /// The eyes are the game's own, and the game's own <c>Eye</c> component says where its pupil is
        /// drawn (<c>PupilContainer</c>, with the pupil mesh in <c>PupilRend</c>), so a heart is a small
        /// flat quad parented to that container and scaled to the pupil it covers. Nothing of the game's
        /// is written to - no material of theirs is changed and no texture of theirs is replaced - which
        /// is what makes this something that can always be taken back off: the overlays are the mod's own
        /// objects and ending the effect destroys them.
        ///
        /// There is deliberately no second heart behind the first. A quad facing the other way is the one
        /// whose corners end up inside the head, and the pupil already hides anything the heart's own
        /// facing leaves behind it.
        /// </summary>
        private static void ShowHeartPupils()
        {
            if (!HeartPupilsEnabled)
                return;

            if (HeartOverlays.Count > 0)
                return;

            try
            {
                Il2CppScheduleOne.AvatarFramework.Avatar avatar =
                    _avatar as Il2CppScheduleOne.AvatarFramework.Avatar;

                if (avatar == null)
                {


                    return;
                }

                Il2CppScheduleOne.AvatarFramework.Eye[] eyes =
                    avatar.GetComponentsInChildren<Il2CppScheduleOne.AvatarFramework.Eye>(true);

                if (eyes == null || eyes.Length == 0)
                {

                    return;
                }

                int done = 0;

                for (int i = 0; i < eyes.Length; i++)
                {
                    if (AddHeartsTo(eyes[i]))
                        done++;
                }

                if (HeartOverlays.Count == 0)
                {

                    return;
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[MDMA Eyes] Heart pupils: " + HeartOverlays.Count +
                    " overlay(s) over " + done + " eye(s), " +
                    _lastHeartSize.ToString("0.0000") + " across.");
            }
            catch (Exception)
            {

            }
        }

        private static void HideHeartPupils(bool quiet = false)
        {
            if (HeartOverlays.Count == 0)
                return;

            for (int i = 0; i < HeartOverlays.Count; i++)
            {
                if (HeartOverlays[i] != null)
                    UnityEngine.Object.Destroy(HeartOverlays[i]);
            }

            HeartOverlays.Clear();
            HeartRenderers.Clear();

            if (!quiet)
                global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA Eyes] Heart pupils taken off.");
        }

        /// <summary>
        /// Puts the hearts back when they have gone, and shows them again when something has hidden them.
        ///
        /// A heart is the mod's own object parented to the game's pupil container, so it lasts exactly as
        /// long as the game leaves that container alone: when the avatar's eye rig is rebuilt - which the
        /// game does whenever its eye settings are written - the hearts go with the old one, and an icon
        /// capture hides every renderer it finds under the player. Rather than trust the hearts to stay
        /// put, they are checked every single frame: a missing set is put back and a hidden set is turned
        /// back on, both within the frame it happened on. The first few repairs of each kind are logged,
        /// so the reason they were needed is on the record instead of guessed at.
        /// </summary>
        private static void RepairHeartPupilsIfNeeded()
        {
            if (!HeartPupilsEnabled)
                return;

            if (HeartOverlays.Count == 0)
                return;

            int alive = 0;
            int shown = 0;

            for (int i = 0; i < HeartOverlays.Count; i++)
            {
                if (HeartOverlays[i] == null)
                    continue;

                alive++;

                if (i >= HeartRenderers.Count)
                    continue;

                Renderer renderer = HeartRenderers[i];

                if (renderer == null)
                    continue;

                try
                {
                    if (!renderer.enabled)
                    {
                        renderer.enabled = true;
                        shown++;
                    }
                }
                catch { }
            }

            if (shown > 0 && _heartRepairs < 3)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[MDMA Eyes] Heart pupils had been hidden by something else; shown again.");
            }

            if (alive == HeartOverlays.Count)
                return;

            // Some are gone: the game rebuilt the eye rig underneath them. A fresh set goes on, but not
            // for ever - a fight with the game over the eye rig would cost more than the hearts are
            // worth, so after enough of them the mod stops trying and says so once.
            _heartRepairs++;

            if (_heartRepairs > MaxHeartRepairs)
                return;

            if (_heartRepairs <= 3)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[MDMA Eyes] " + (HeartOverlays.Count - alive) +
                    " heart pupil(s) went with the eye rig; putting them back.");
            }
            else if (_heartRepairs == MaxHeartRepairs)
            {

            }

            HideHeartPupils(true);
            ShowHeartPupils();
        }

        /// <summary>The widest side of a renderer's bounds, or zero when there is nothing to measure.</summary>
        private static float LargestSide(Renderer renderer)
        {
            if (renderer == null)
                return 0f;

            try
            {
                Vector3 size = renderer.bounds.size;

                return Mathf.Max(size.x, size.y);
            }
            catch
            {
                return 0f;
            }
        }

        private static bool AddHeartsTo(Il2CppScheduleOne.AvatarFramework.Eye eye)
        {
            try
            {
                if (eye == null)
                    return false;

                Transform container = eye.PupilContainer;

                if (container == null && eye.PupilRend != null)
                    container = eye.PupilRend.transform;

                if (container == null)
                    return false;

                // How big a heart is comes from the eye it is drawn on, and the eye is measured from its
                // eyeball rather than its pupil. The pupil is a skinned mesh that can carry more than one
                // eye, and measuring against that is what made the hearts wider than the head they sit on.
                float eyeSize = LargestSide(eye.EyeBallRend);
                float pupilSize = LargestSide(eye.PupilRend);

                float worldSize;

                if (eyeSize > 0.0005f)
                {
                    // The pupil is only trusted when it is smaller than the eye it is attached to: a pupil
                    // mesh any bigger than that is measuring something other than this one pupil.
                    worldSize = pupilSize > 0.0005f && pupilSize < eyeSize
                        ? Mathf.Min(pupilSize, eyeSize * HeartToEyeRatio)
                        : eyeSize * HeartToEyeRatio;
                }
                else
                {
                    worldSize = pupilSize > 0.0005f
                        ? pupilSize * 0.8f
                        : FallbackHeartSize;
                }

                worldSize = Mathf.Min(worldSize, MaxHeartSize);

                _lastHeartSize = worldSize;

                Vector3 parentScale = container.lossyScale;

                float widest = Mathf.Max(
                    Mathf.Abs(parentScale.x),
                    Mathf.Max(Mathf.Abs(parentScale.y), Mathf.Abs(parentScale.z)));

                float local = worldSize / Mathf.Max(0.0001f, widest);

                // The direction the eye faces, taken from the eyeball itself rather than from the pupil
                // container's own axes, so the heart always lies flat on the eye.
                Vector3 localAxis = container.InverseTransformDirection(
                    OutwardDirection(eye, container)
                );

                if (localAxis.sqrMagnitude < 0.0001f)
                    localAxis = Vector3.forward;

                localAxis = localAxis.normalized;

                // One heart per eye, in front of the pupil, lifted by however much the curve of the eyeball
                // needs. There is no second copy behind it: a quad facing the other way is the one that
                // ends up sticking out of the head.
                float lift = EyeballLift(eye, worldSize, widest);

                AddHeart(container, local, localAxis * lift);

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        /// <summary>
        /// How far in front of the pupil a heart has to sit for its corners to clear the curve of the
        /// eyeball: the sagitta of a chord as long as the heart over the eyeball's own sphere, which is
        /// exactly the depth that chord cuts into it, plus a margin. Measured from the eyeball's bounds
        /// rather than guessed, because a flat quad laid on a ball is what was dipping inside the head.
        /// </summary>
        private static float EyeballLift(
            Il2CppScheduleOne.AvatarFramework.Eye eye,
            float worldSize,
            float widest)
        {
            float scale = Mathf.Max(0.0001f, widest);
            float radius = 0f;

            try
            {
                MeshRenderer eyeball = eye.EyeBallRend;

                if (eyeball != null)
                {
                    Vector3 extent = eyeball.bounds.extents;

                    radius = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
                }
            }
            catch { }

            float sagitta = 0f;

            if (radius > 0.0005f)
            {
                // A heart wider than the ball it sits on can never clear it, so the length is held just
                // inside the radius and what is left of the curve becomes the lift.
                float half = Mathf.Min(worldSize * 0.5f, radius * 0.98f);

                sagitta = radius - Mathf.Sqrt(
                    Mathf.Max(0f, radius * radius - half * half)
                );
            }

            float world = sagitta + radius * HeartLift + 0.0005f;

            return world / scale;
        }

        /// <summary>
        /// The way the eye faces, taken as the line from the eyeball's centre out through the pupil. That
        /// is the direction a heart has to lie along to sit flat on the eye, and unlike the pupil
        /// container's own axes it does not depend on how the game happens to have turned that container.
        /// </summary>
        private static Vector3 OutwardDirection(
            Il2CppScheduleOne.AvatarFramework.Eye eye,
            Transform container)
        {
            try
            {
                MeshRenderer eyeball = eye.EyeBallRend;

                if (eyeball != null && container != null)
                {
                    Vector3 direction = container.position - eyeball.bounds.center;

                    if (direction.sqrMagnitude > 0.0000001f)
                        return direction.normalized;
                }
            }
            catch { }

            return container != null ? container.forward : Vector3.forward;
        }

        private static void AddHeart(Transform parent, float size, Vector3 offset)
        {
            GameObject heart = new GameObject("WVC_MDMA_Heart");

            heart.transform.SetParent(parent, false);
            heart.transform.localPosition = offset;

            // Facing along the way it was pushed out, so the heart is neither edge-on to the camera nor
            // showing its back to it.
            heart.transform.localRotation = offset.sqrMagnitude > 0.0000001f
                ? Quaternion.LookRotation(offset.normalized)
                : Quaternion.identity;

            heart.transform.localScale = Vector3.one * size;

            MeshFilter filter = heart.AddComponent<MeshFilter>();
            filter.sharedMesh = HeartQuad();

            MeshRenderer renderer = heart.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = HeartMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            HeartOverlays.Add(heart);
            HeartRenderers.Add(renderer);
        }

        private static Mesh HeartQuad()
        {
            if (_heartQuad != null)
                return _heartQuad;

            Mesh mesh = new Mesh();
            mesh.name = "WVC_MDMA_HeartQuad";

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

            _heartQuad = mesh;

            return mesh;
        }

        /// <summary>One transparent, unlit material for every heart: they only ever differ by where they are.</summary>
        private static Material HeartMaterial()
        {
            if (_heartMaterial != null)
                return _heartMaterial;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Transparent");

            if (shader == null)
            {

                return null;
            }

            Material material = new Material(shader);
            material.name = "WVC_MDMA_HeartMaterial";

            Texture2D texture = HeartTexture();

            if (texture != null)
            {
                material.mainTexture = texture;

                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);

            ConfigureTransparency(material);

            _heartMaterial = material;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[MDMA Eyes] Heart material ready (shader '" + shader.name + "').");

            return material;
        }

        /// <summary>The same transparency recipe the mod's other runtime materials use, for whichever
        /// shader was found.</summary>
        private static void ConfigureTransparency(Material material)
        {
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);

            // No depth write: the heart sits a hair in front of the pupil and must not hide it, and the
            // copy facing the other way is meant to be covered by the pupil rather than to cover it.
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property))
                material.SetFloat(property, value);
        }

        /// <summary>
        /// Draws the heart: a fill of the well-known heart curve, with a darker rim so it still reads as a
        /// shape when it is only a few pixels wide, and an edge smoothed by the curve's own slope rather
        /// than by chance.
        /// </summary>
        private static Texture2D HeartTexture()
        {
            if (_heartTexture != null)
                return _heartTexture;

            const int size = 128;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "WVC_MDMA_Heart";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color core = new Color(1f, 0.08f, 0.34f, 1f);
            Color rim = new Color(0.62f, 0.02f, 0.18f, 1f);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // The curve is measured in its own units, where the heart is about two and a half
                    // across and sits just below the middle; these map the texture onto it with a margin.
                    float u = ((x + 0.5f) / size * 2f - 1f) * 1.3f;
                    float v = ((y + 0.5f) / size * 2f - 1f) * 1.25f - 0.15f;

                    float value = Heart(u, v);

                    float slope = Mathf.Max(
                        1e-5f,
                        Mathf.Abs(Heart(u + 0.01f, v) - Heart(u - 0.01f, v)) +
                        Mathf.Abs(Heart(u, v + 0.01f) - Heart(u, v - 0.01f)));

                    float inside = -value / slope * 0.02f;

                    Color colour = Color.Lerp(rim, core, Mathf.Clamp01(inside * 12f));
                    colour.a = Mathf.Clamp01(inside * 40f);

                    pixels[y * size + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            _heartTexture = texture;

            return texture;
        }

        /// <summary>Negative inside a heart, positive outside it: (x² + y² - 1)³ - x²y³.</summary>
        private static float Heart(float x, float y)
        {
            float a = x * x + y * y - 1f;

            return a * a * a - x * x * y * y * y;
        }

        private static bool TryGetAvatarAndSettings()
        {
            if (_player == null) return false;
            _avatar = GetMember(_player, "Avatar");
            if (_avatar == null) return false;
            _settings = GetMember(_avatar, "CurrentSettings");
            return _settings != null;
        }

        private static void ApplyAllEyeSettings()
        {
            CallMethod(_avatar, "ApplyEyeBallSettings", _settings);
            CallMethod(_avatar, "ApplyEyeLidSettings", _settings);
            CallMethod(_avatar, "ApplyEyeLidColorSettings", _settings);
            CallMethod(_avatar, "ApplyEyebrowSettings", _settings);
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = obj.GetType();
            try { PropertyInfo p = type.GetProperty(name, flags); if (p != null) return p.GetValue(obj); } catch { }
            try { FieldInfo f = type.GetField(name, flags); if (f != null) return f.GetValue(obj); } catch { }
            return null;
        }

        private static float GetFloat(object obj, string name, float fallback)
        {
            object val = GetMember(obj, name);
            return val is float f ? f : fallback;
        }

        private static Color GetColor(object obj, string name, Color fallback)
        {
            object val = GetMember(obj, name);
            return val is Color c ? c : fallback;
        }

        private static void SetFloat(object obj, string name, float value)
        {
            if (obj == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try { PropertyInfo p = obj.GetType().GetProperty(name, flags); if (p != null && p.CanWrite) { p.SetValue(obj, value); return; } } catch { }
            try { FieldInfo f = obj.GetType().GetField(name, flags); if (f != null) f.SetValue(obj, value); } catch { }
        }

        private static void SetColor(object obj, string name, Color value)
        {
            if (obj == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try { PropertyInfo p = obj.GetType().GetProperty(name, flags); if (p != null && p.CanWrite) { p.SetValue(obj, value); return; } } catch { }
            try { FieldInfo f = obj.GetType().GetField(name, flags); if (f != null) f.SetValue(obj, value); } catch { }
        }

        private static void CallMethod(object obj, string methodName, object arg)
        {
            if (obj == null) return;
            MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) try { method.Invoke(obj, new object[] { arg }); } catch { }
        }
    }
}
