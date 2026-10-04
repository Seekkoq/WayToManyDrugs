using System;
using System.Diagnostics;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using NativeMapUtility = Il2CppScheduleOne.Map.MapPositionUtility;
using NativeMapApp = Il2CppScheduleOne.UI.Phone.Map.MapApp;

namespace CustomNPCExample.UI
{
    /// <summary>
    /// The corner minimap.
    ///
    /// The map itself is the game's own map art, taken from the phone map, shown through a small
    /// window in the corner of the screen and slid around so the player is always in the middle. Where
    /// the art sits in the world is taken from the two corner markers the game's own map utility keeps,
    /// so the minimap agrees with the phone map about where the edges are and needs nothing from the
    /// game that has to be initialised first.
    ///
    /// Nothing here renders the world a second time. There is no second camera and no render texture:
    /// the art is a single image and everything that moves on it is a UI element that is moved, which
    /// is the cheapest a minimap can possibly be and the reason it is safe on a weak card.
    ///
    /// The window is kept up rather than rebuilt: it is created once, and after that the only work a
    /// frame does is move a RectTransform and turn an arrow, neither of which rebuilds a canvas.
    /// </summary>
    internal static class WvcMinimap
    {
        /// <summary>
        /// The key that turns the minimap on and off. F1, F3 and F4 are the free function keys - F5 is
        /// deliberately not one of them, because F5 is PillVille's bulk meetup and the minimap being off
        /// by default is not much use if the key that turns it on is the key that asks for an order.
        /// </summary>
        internal static readonly KeyCode ToggleKey = KeyCode.F4;

        /// <summary>The side of the window, in the canvas's own units.</summary>
        private const float WindowSize = 220f;

        /// <summary>The width of the player's arrow, in the canvas's own units.</summary>
        private const float ArrowSize = 26f;

        /// <summary>How far the window sits from the corner it is anchored to.</summary>
        private const float Margin = 24f;

        /// <summary>How much of the world the window shows across, in metres.</summary>
        private const float MetersAcross = 160f;

        /// <summary>
        /// The smallest share of the map the window is ever allowed to show, and the largest. A window
        /// showing the whole map would leave nothing to slide, and one showing almost nothing would
        /// turn the art into a smear; both are clamped so a bad world size cannot produce either.
        /// </summary>
        private const float MinZoomFraction = 0.05f;
        private const float MaxZoomFraction = 0.6f;

        /// <summary>
        /// How long the player and the camera are trusted before they are looked up again. Looking up
        /// either one walks the scene, and doing that every frame for something that changes at most
        /// once a session is how a cheap feature stops being cheap; a second is far more often than
        /// either actually changes.
        /// </summary>
        private const float ResolveInterval = 1f;

        /// <summary>
        /// How long to wait before looking for the phone's map art again, and the longest that wait is
        /// allowed to grow to. The art is a scene object that only exists once the phone has been built,
        /// so it is looked for on a timer that backs away the longer it keeps missing - never once a
        /// frame, because a scene walk per frame while a save is loading is what cost this feature its
        /// first attempt at the game's frame rate.
        /// </summary>
        private const float SpriteRetryInterval = 1.5f;
        private const float SpriteRetryMaxInterval = 30f;

        /// <summary>How many frames one cost reading covers, and how many readings are ever written.</summary>
        private const int PerfWindow = 600;
        private const int MaxPerfReports = 3;

        /// <summary>The canvas order the window sits at: over the mod's own watermark, under the blackout.</summary>
        private const int CanvasSortingOrder = 30500;

        /// <summary>
        /// Where the game keeps the phone's map, and so where the map art is.
        ///
        /// This is the path the established minimap mods look the art up by. It is one object lookup
        /// against the hierarchy the game itself built, rather than a scan of the whole scene, which is
        /// what makes it cheap enough to try on a timer.
        /// </summary>
        private const string PhoneMapContentPath =
            "GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content";

        /// <summary>The colour of the panel the window is cut into, and of the player's arrow.</summary>
        private static readonly Color FrameColor = new Color(0.03f, 0.05f, 0.04f, 0.85f);
        private static readonly Color ArrowColor = new Color(0.92f, 1f, 0.92f, 1f);
        private static readonly Color ArrowOutlineColor = new Color(0.06f, 0.12f, 0.08f, 1f);

        // ---- calibration knobs ----------------------------------------------------------------
        // These are the only things that can be wrong about how the art is laid out, and each of them
        // is settled by reading the log the first time the minimap runs rather than by guesswork.

        /// <summary>Set when the map art runs the other way along the world's X.</summary>
        /// <remarks>
        /// Left false until the first run's log says otherwise. Readonly rather than const so the
        /// checks that use it are ordinary branches; flip it here if the art turns out mirrored.
        /// </remarks>
        private static readonly bool FlipU = false;

        /// <summary>Set when the map art runs the other way along the world's Z.</summary>
        /// <remarks>Left false until the first run's log says otherwise.</remarks>
        private static readonly bool FlipV = false;

        /// <summary>Added to the camera's heading to point the arrow at the art's idea of north.</summary>
        /// <remarks>Left at zero until the first run's log says otherwise.</remarks>
        private const float HeadingOffset = 0f;

        // ---- runtime state --------------------------------------------------------------------

        private static GameObject _root;
        private static Image _mapImage;
        private static RectTransform _mapRect;
        private static RectTransform _arrowRect;

        private static Sprite _arrowSprite;

        /// <summary>The player and the camera, looked up on a timer rather than every frame.</summary>
        private static NativePlayer _player;
        private static Transform _playerTransform;
        private static Camera _camera;
        private static float _resolveTimer;

        private static bool _userEnabled;
        private static bool _created;
        private static bool _shown;

        private static bool _projectionReady;
        private static bool _projectionFailed;
        private static bool _calibrationLogged;

        /// <summary>When the map art is looked for again, and how many times in a row it has missed.</summary>
        private static float _spriteTimer;
        private static int _spriteMisses;

        /// <summary>Set once the art has been found and logged, and once the "press F5" hint is shown.</summary>
        private static bool _artLogged;
        private static bool _hintLogged;

        /// <summary>The earliest time the map utility is looked for again, so the lookup is not per frame.</summary>
        private static float _retryTimer;

        /// <summary>Ticks spent in Update and frames counted, for the cost reading.</summary>
        private static long _perfTotal;
        private static int _perfFrames;
        private static int _perfReports;

        /// <summary>The opposite corners of the map in the world.</summary>
        private static Vector3 _worldMin;
        private static Vector3 _worldMax;

        private static float _mapImageSize = WindowSize;

        /// <summary>
        /// Runs once a frame from the mod's own update, and measures itself while it does.
        ///
        /// The measurement is here rather than assumed: "cheap" is a claim that should be backed by a
        /// number, and if this feature is ever blamed for the frame rate the log already says what it
        /// costs. It is a stopwatch read and two additions a frame, which is nothing next to what it
        /// measures.
        /// </summary>
        internal static void Update()
        {
            long start = Stopwatch.GetTimestamp();

            try
            {
                UpdateCore();
            }
            catch { }

            _perfTotal += Stopwatch.GetTimestamp() - start;
            _perfFrames++;

            if (_perfFrames >= PerfWindow)
            {
                if (_perfReports < MaxPerfReports)
                {
                    _perfReports++;

                    double micro = _perfTotal * 1000000.0 /
                                   Stopwatch.Frequency / _perfFrames;


                }

                _perfFrames = 0;
                _perfTotal = 0;
            }
        }

        private static void UpdateCore()
        {
            try
            {
                if (Input.GetKeyDown(ToggleKey))
                {
                    _userEnabled = !_userEnabled;


                }
            }
            catch { }

            if (!_userEnabled)
            {
                if (!_hintLogged && IsGameplayScene())
                {
                    _hintLogged = true;


                }

                Hide();
                return;
            }

            if (!IsGameplayScene())
            {
                Hide();
                return;
            }

            if (Time.unscaledTime >= _resolveTimer)
            {
                _resolveTimer = Time.unscaledTime + ResolveInterval;

                _player = ResolveLocalPlayer();
                _camera = GetGameplayCamera();

                _playerTransform = _player == null ? null : _player.transform;
            }

            if (_playerTransform == null || _camera == null)
            {
                Hide();
                return;
            }

            if (!EnsureCreated() || !EnsureProjection())
            {
                Hide();
                return;
            }

            Vector2 uv;

            if (!TryToUV(_playerTransform.position, out uv))
            {
                Hide();
                return;
            }

            Show();

            ApplyCrop(uv);
            ApplyHeading(_camera.transform.eulerAngles.y);
        }

        /// <summary>
        /// True only in the gameplay scene.
        ///
        /// The minimap is meaningless anywhere else and the game's map converter is not up until the
        /// world is, so the window is kept out of the main menu and the loading scenes entirely rather
        /// than shown empty.
        /// </summary>
        private static bool IsGameplayScene()
        {
            try
            {
                return SceneManager.GetActiveScene().name == "Main";
            }
            catch
            {
                return false;
            }
        }

        private static NativePlayer ResolveLocalPlayer()
        {
            try { return NativePlayer.Local; }
            catch { return null; }
        }

        /// <summary>
        /// The camera the player actually looks through, or null while there is not one.
        ///
        /// The same test the DMT effect uses: a camera that is disabled, sitting on an inactive object
        /// or already drawing to a texture is not the one on screen, and using it would point the
        /// arrow the wrong way.
        /// </summary>
        private static Camera GetGameplayCamera()
        {
            try
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
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds the window once, and says whether it is there.
        ///
        /// Everything about the window is settled on the frame it is built - its corner, its size, the
        /// art behind it - because none of that changes afterwards. The three parts are plain scene
        /// objects: a dark frame, a rect mask that is the window, and the map art inside it. A rect
        /// mask needs no image of its own, so the art is simply not drawn outside the window.
        ///
        /// Once it exists it is only checked for, not rebuilt: if the art it was built around has gone
        /// (the game rebuilds the phone between sessions) the window is thrown away and built again on
        /// the next frame rather than left showing nothing.
        /// </summary>
        private static bool EnsureCreated()
        {
            if (_created)
            {
                if (_root != null && _mapImage != null && _mapImage.sprite != null)
                    return true;

                ResetRuntime();
            }

            if (Time.unscaledTime < _spriteTimer)
                return false;

            Sprite sprite = ResolveMapSprite();

            if (sprite == null)
            {
                _spriteMisses++;

                float wait = Mathf.Min(
                    SpriteRetryInterval * _spriteMisses, SpriteRetryMaxInterval);

                _spriteTimer = Time.unscaledTime + wait;

                if (_spriteMisses == 1 || _spriteMisses % 6 == 0)
                {

                }

                return false;
            }

            _spriteMisses = 0;

            try
            {
                _root = new GameObject("WVC_Minimap");
                UnityEngine.Object.DontDestroyOnLoad(_root);

                Canvas canvas = _root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = CanvasSortingOrder;

                CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject frame = new GameObject("Frame");
                frame.transform.SetParent(_root.transform, false);

                Image frameImage = frame.AddComponent<Image>();
                frameImage.color = FrameColor;
                frameImage.raycastTarget = false;

                RectTransform frameRect = frameImage.rectTransform;
                frameRect.anchorMin = new Vector2(1f, 1f);
                frameRect.anchorMax = new Vector2(1f, 1f);
                frameRect.pivot = new Vector2(1f, 1f);
                frameRect.sizeDelta = new Vector2(WindowSize + 8f, WindowSize + 8f);
                frameRect.anchoredPosition = new Vector2(-Margin, -Margin);

                GameObject window = new GameObject("Window");
                window.transform.SetParent(frame.transform, false);

                RectTransform windowRect = window.AddComponent<RectTransform>();
                windowRect.anchorMin = new Vector2(0.5f, 0.5f);
                windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                windowRect.pivot = new Vector2(0.5f, 0.5f);
                windowRect.sizeDelta = new Vector2(WindowSize, WindowSize);
                windowRect.anchoredPosition = Vector2.zero;

                window.AddComponent<RectMask2D>();

                GameObject mapObject = new GameObject("Map");
                mapObject.transform.SetParent(window.transform, false);

                _mapImage = mapObject.AddComponent<Image>();
                _mapImage.sprite = sprite;
                _mapImage.color = Color.white;
                _mapImage.raycastTarget = false;
                _mapImage.preserveAspect = false;

                _mapRect = _mapImage.rectTransform;
                _mapRect.anchorMin = new Vector2(0.5f, 0.5f);
                _mapRect.anchorMax = new Vector2(0.5f, 0.5f);
                _mapRect.pivot = new Vector2(0.5f, 0.5f);
                _mapRect.sizeDelta = new Vector2(WindowSize, WindowSize);
                _mapRect.anchoredPosition = Vector2.zero;

                _arrowSprite = BuildArrowSprite();

                GameObject arrow = new GameObject("Arrow");
                arrow.transform.SetParent(window.transform, false);

                Image arrowImage = arrow.AddComponent<Image>();
                arrowImage.sprite = _arrowSprite;
                arrowImage.color = Color.white;
                arrowImage.raycastTarget = false;

                _arrowRect = arrowImage.rectTransform;
                _arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
                _arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
                _arrowRect.pivot = new Vector2(0.5f, 0.5f);
                _arrowRect.sizeDelta = new Vector2(ArrowSize, ArrowSize);
                _arrowRect.anchoredPosition = Vector2.zero;

                _created = true;

                _root.SetActive(false);



                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Minimap] The window would not build: " + ex.Message);

                ResetRuntime();
                return false;
            }
        }

        /// <summary>
        /// The player's arrow, drawn once into a small texture.
        ///
        /// It is built here rather than shipped as an image, so the mod adds nothing to the game's own
        /// assets. A dark triangle is drawn first and a light one over it, which leaves the outline
        /// that keeps the arrow readable against the light parts of the map.
        /// </summary>
        private static Sprite BuildArrowSprite()
        {
            const int size = 32;

            Texture2D texture =
                new Texture2D(size, size, TextureFormat.RGBA32, false);

            texture.name = "WVC_Minimap_Arrow";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (x + 0.5f) / size;
                    float fy = (y + 0.5f) / size;

                    Color color = clear;

                    if (InTriangle(fx, fy, 0.99f, 0.05f, 0.40f))
                        color = ArrowOutlineColor;

                    if (InTriangle(fx, fy, 0.92f, 0.15f, 0.30f))
                        color = ArrowColor;

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        /// <summary>True when a point lies inside a triangle that points up the middle of its tile.</summary>
        private static bool InTriangle(
            float x,
            float y,
            float apexY,
            float baseY,
            float halfWidth)
        {
            if (y < baseY || y > apexY)
                return false;

            float across = (apexY - y) / (apexY - baseY);

            return Mathf.Abs(x - 0.5f) <= halfWidth * across;
        }

        /// <summary>
        /// The game's map art, or null until it can be reached.
        ///
        /// It is the phone map's own sprite, which is the whole world drawn once. Reaching it is the
        /// one thing about this feature that cannot be settled by reading code - it depends on the
        /// phone having been built - so what was found is logged: if the window ever comes up blank,
        /// that line says whether the art was there at all.
        /// </summary>
        private static Sprite ResolveMapSprite()
        {
            try
            {
                GameObject content = GameObject.Find(PhoneMapContentPath);

                if (content != null)
                {
                    Image image = content.GetComponent<Image>();

                    if (image == null)
                        image = content.GetComponentInChildren<Image>();

                    if (image != null && image.sprite != null)
                        return AcceptSprite(image.sprite, "the phone map");
                }
            }
            catch { }

            try
            {
                NativeMapApp app = UnityEngine.Object.FindObjectOfType<NativeMapApp>(true);

                if (app != null && app.MainMapSprite != null)
                    return AcceptSprite(app.MainMapSprite, "the map app");
            }
            catch { }

            return null;
        }

        /// <summary>Writes the art that was found into the log once, and hands it back.</summary>
        private static Sprite AcceptSprite(Sprite sprite, string from)
        {
            if (!_artLogged)
            {
                _artLogged = true;

                string size = sprite.texture == null
                    ? "?"
                    : sprite.texture.width + "x" + sprite.texture.height;


            }

            return sprite;
        }

        /// <summary>
        /// The game's map utility, which holds the two corners the art is laid over.
        ///
        /// Only the corners are taken from it, never its own coordinate conversion. That conversion
        /// depends on a factor the game works out when the world first wakes, and reading it too early
        /// hands back the same point for every position in the world at once - which is exactly what it
        /// did, and why the first cut of this had no map at all. The corners are plain scene transforms
        /// and are right from the moment the world exists.
        /// </summary>
        private static NativeMapUtility ResolveMapUtility()
        {
            try
            {
                if (NativeMapUtility.InstanceExists)
                {
                    NativeMapUtility instance = NativeMapUtility.Instance;

                    if (instance != null)
                        return instance;
                }
            }
            catch { }

            try
            {
                return UnityEngine.Object.FindObjectOfType<NativeMapUtility>(true);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>A world position written out for the log, on the two axes the map uses.</summary>
        private static string Describe(Vector3 v)
        {
            return "(" + v.x.ToString("0") + "," + v.z.ToString("0") + ")";
        }

        /// <summary>
        /// Works out what part of the world the map art covers, once, from the game's own corner
        /// markers.
        ///
        /// The utility keeps a transform at one corner of the map and another at the opposite one.
        /// Their world positions become the art's extent, and everything after that is a plain ratio:
        /// how far along the art the player is, and how much of the art the window reaches across.
        /// </summary>
        private static bool EnsureProjection()
        {
            if (_projectionReady)
                return true;

            if (_projectionFailed)
                return false;

            // Looking the utility up walks the scene, so it is tried on a timer rather than every
            // frame; a world that is still loading simply gets another try a second from now.
            if (Time.unscaledTime < _retryTimer)
                return false;

            _retryTimer = Time.unscaledTime + ResolveInterval;

            NativeMapUtility utility = ResolveMapUtility();

            if (utility == null)
                return false;

            try
            {
                Transform origin = utility.OriginPoint;
                Transform edge = utility.EdgePoint;

                if (origin == null || edge == null)
                    return false;

                Vector3 a = origin.position;
                Vector3 b = edge.position;

                float spanX = Mathf.Abs(a.x - b.x);
                float spanZ = Mathf.Abs(a.z - b.z);
                float span = Mathf.Max(spanX, spanZ);

                if (!_calibrationLogged)
                {
                    _calibrationLogged = true;


                }

                if (spanX < 1f || spanZ < 1f)
                {
                    _projectionFailed = true;



                    return false;
                }

                _worldMin = new Vector3(
                    Mathf.Min(a.x, b.x), 0f, Mathf.Min(a.z, b.z));

                _worldMax = new Vector3(
                    Mathf.Max(a.x, b.x), 0f, Mathf.Max(a.z, b.z));

                float fraction = Mathf.Clamp(
                    MetersAcross / span, MinZoomFraction, MaxZoomFraction);

                _mapImageSize = WindowSize / fraction;

                _projectionReady = true;



                return true;
            }
            catch (Exception)
            {
                _projectionFailed = true;



                return false;
            }
        }

        /// <summary>
        /// Where a world position lands on the art, as a share of it: 0 at one edge, 1 at the other.
        ///
        /// A plain ratio across the two corners the game marks out. Nothing here calls into the game,
        /// so once the corners are known it cannot fail, and it costs a handful of subtractions a frame.
        /// </summary>
        private static bool TryToUV(Vector3 world, out Vector2 uv)
        {
            float spanX = _worldMax.x - _worldMin.x;
            float spanZ = _worldMax.z - _worldMin.z;

            if (spanX < 1f || spanZ < 1f)
            {
                uv = new Vector2(0.5f, 0.5f);
                return false;
            }

            float u = (world.x - _worldMin.x) / spanX;
            float v = (world.z - _worldMin.z) / spanZ;

            if (FlipU)
                u = 1f - u;

            if (FlipV)
                v = 1f - v;

            uv = new Vector2(u, v);

            return true;
        }

        /// <summary>
        /// Puts the player in the middle of the window by sliding the art under it.
        ///
        /// The art is one big image and the window is a mask over part of it, so centring the player is
        /// nothing more than moving that image. Moving a RectTransform does not rebuild the canvas the
        /// way changing its art or its colour would, which is why this is safe to do every frame.
        /// </summary>
        private static void ApplyCrop(Vector2 uv)
        {
            if (_mapRect == null)
                return;

            float wanted = _mapImageSize;

            if (Mathf.Abs(_mapRect.sizeDelta.x - wanted) > 0.1f)
                _mapRect.sizeDelta = new Vector2(wanted, wanted);

            _mapRect.anchoredPosition = new Vector2(
                -(uv.x - 0.5f) * _mapImageSize,
                -(uv.y - 0.5f) * _mapImageSize);
        }

        /// <summary>
        /// Turns the arrow to the direction the player is facing.
        ///
        /// The map itself never turns - it is the arrow that swings, exactly as it does on the phone's
        /// map - so the outside world stays put as the player looks around.
        /// </summary>
        private static void ApplyHeading(float yaw)
        {
            if (_arrowRect == null)
                return;

            _arrowRect.localRotation = Quaternion.Euler(0f, 0f, -(yaw + HeadingOffset));
        }

        private static void Show()
        {
            if (_root == null || _shown)
                return;

            if (!_root.activeSelf)
                _root.SetActive(true);

            _shown = true;
        }

        private static void Hide()
        {
            if (_root == null || !_shown)
                return;

            if (_root.activeSelf)
                _root.SetActive(false);

            _shown = false;
        }

        /// <summary>Throws the window away so the next frame builds it again from scratch.</summary>
        private static void ResetRuntime()
        {
            try
            {
                if (_root != null)
                    UnityEngine.Object.Destroy(_root);

                if (_arrowSprite != null)
                    UnityEngine.Object.Destroy(_arrowSprite);
            }
            catch { }

            _root = null;
            _mapImage = null;
            _mapRect = null;
            _arrowRect = null;
            _arrowSprite = null;

            _created = false;
            _shown = false;
        }
    }
}