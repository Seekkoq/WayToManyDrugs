using System;
using Il2CppScheduleOne.UI.MainMenu;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace CustomNPCExample.UI
{
    public static class MainMenuWatermark
    {
        public const string ModName =
            "WAYTOMANYDRUGS";

        public const string ModVersion =
            "V3.5.0";

        private const float DetectionInterval = 2f;

        private const int TextureWidth = 340;
        private const int TextureHeight = 62;

        private static readonly Color ClearColor =
            new Color(0f, 0f, 0f, 0f);

        private static readonly Color CardColor =
            new Color(0.035f, 0.075f, 0.045f, 0.94f);

        private static readonly Color BorderColor =
            new Color(0.30f, 0.85f, 0.35f, 1f);

        private static readonly Color LeafColor =
            new Color(0.32f, 0.88f, 0.36f, 1f);

        private static readonly Color LeafDarkColor =
            new Color(0.16f, 0.55f, 0.20f, 1f);

        private static readonly Color TitleColor =
            new Color(0.88f, 1f, 0.88f, 1f);

        private static readonly Color VersionColor =
            new Color(0.45f, 0.95f, 0.50f, 1f);

        private static GameObject _root;
        private static Texture2D _texture;

        private static bool _created;
        private static bool _logged;
        private static float _timer;
        private static bool _worldLoaded;

        // ------------------------------------------------------------
        // Runtime
        // ------------------------------------------------------------

        public static void Update()
        {
            if (_worldLoaded)
                return;

            _timer += Time.unscaledDeltaTime;

            if (_timer < DetectionInterval)
                return;

            _timer = 0f;

            bool menuVisible =
                IsMainMenuVisible();

            if (menuVisible)
            {
                if (!_created)
                    CreateWatermark();

                if (_root != null && !_root.activeSelf)
                    _root.SetActive(true);
            }
            else
            {
                if (_root != null && _root.activeSelf)
                    _root.SetActive(false);

                // Once we are out of the menu, stop polling.
                if (_created)
                    _worldLoaded = true;
            }
        }

        public static void ResetRuntime()
        {
            _timer = 0f;
            _logged = false;
            _created = false;
            _worldLoaded = false;

            try
            {
                if (_root != null)
                    UnityEngine.Object.Destroy(_root);

                if (_texture != null)
                    UnityEngine.Object.Destroy(_texture);
            }
            catch { }

            _root = null;
            _texture = null;
        }

        // ------------------------------------------------------------
        // Detection
        // ------------------------------------------------------------

        private static bool IsMainMenuVisible()
        {
            try
            {
                MainMenuRig rig =
                    UnityEngine.Object.FindObjectOfType<MainMenuRig>();

                return rig != null &&
                       rig.gameObject != null &&
                       rig.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------
        // Creation
        // ------------------------------------------------------------

        private static void CreateWatermark()
        {
            if (_created)
                return;

            try
            {
                _texture = BuildTexture();

                if (_texture == null)
                    return;

                _root = new GameObject("WVC_Watermark");

                UnityEngine.Object.DontDestroyOnLoad(_root);

                Canvas canvas = _root.AddComponent<Canvas>();

                canvas.renderMode =
                    RenderMode.ScreenSpaceOverlay;

                canvas.sortingOrder = 30000;

                CanvasScaler scaler =
                    _root.AddComponent<CanvasScaler>();

                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;

                scaler.referenceResolution =
                    new Vector2(1920f, 1080f);

                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                scaler.matchWidthOrHeight = 0.5f;

                GameObject card =
                    new GameObject("WVC_Watermark_Card");

                card.transform.SetParent(_root.transform, false);

                RectTransform rect =
                    card.AddComponent<RectTransform>();

                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);

                rect.sizeDelta =
                    new Vector2(TextureWidth, TextureHeight);

                rect.anchoredPosition =
                    new Vector2(24f, -24f);

                RawImage image =
                    card.AddComponent<RawImage>();

                image.texture = _texture;
                image.color = Color.white;
                image.raycastTarget = false;

                _created = true;
                _root.SetActive(true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC UI] Watermark failed: " + ex.Message
                );

                ResetRuntime();
            }
        }

        private static Texture2D BuildTexture()
        {
            Texture2D texture =
                new Texture2D(
                    TextureWidth,
                    TextureHeight,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name = "WVC_Watermark_Texture";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                    texture.SetPixel(x, y, ClearColor);
            }

            DrawRoundedCard(texture);

            DrawLeaf(texture, 10, 12);

            DrawText(
                texture,
                ModName,
                48,
                12,
                2,
                TitleColor
            );

            DrawText(
                texture,
                ModVersion,
                48,
                34,
                2,
                VersionColor
            );

            texture.Apply();

            return texture;
        }

        // ------------------------------------------------------------
        // Card
        // ------------------------------------------------------------

        private static void DrawRoundedCard(Texture2D texture)
        {
            const int radius = 10;
            const int border = 2;

            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    bool outer =
                        IsInsideRounded(
                            x,
                            y,
                            TextureWidth,
                            TextureHeight,
                            radius
                        );

                    if (!outer)
                        continue;

                    bool inner =
                        IsInsideRounded(
                            x - border,
                            y - border,
                            TextureWidth - border * 2,
                            TextureHeight - border * 2,
                            radius - border
                        );

                    texture.SetPixel(
                        x,
                        y,
                        inner ? CardColor : BorderColor
                    );
                }
            }
        }

        private static bool IsInsideRounded(
            int x,
            int y,
            int width,
            int height,
            int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            if (x >= radius && x < width - radius)
                return true;

            if (y >= radius && y < height - radius)
                return true;

            float cx =
                x < radius
                    ? radius - 0.5f
                    : width - radius - 0.5f;

            float cy =
                y < radius
                    ? radius - 0.5f
                    : height - radius - 0.5f;

            float dx = x - cx;
            float dy = y - cy;

            return dx * dx + dy * dy <= radius * radius;
        }

        // ------------------------------------------------------------
        // Weed leaf
        // ------------------------------------------------------------

        private static void DrawLeaf(
            Texture2D texture,
            int left,
            int top)
        {
            string[] leaf =
            {
                "..1.....1.....1..",
                "..1.....1.....1..",
                ".11..1..1..1..11.",
                ".11..1..1..1..11.",
                "..1..11.1.11..1..",
                "..11..1.1.1..11..",
                "...1..1111..1....",
                "....11.111.11....",
                ".....1.111.1.....",
                "......11111......",
                ".......111.......",
                "........1........",
                "........1........",
                "........1........"
            };

            for (int row = 0; row < leaf.Length; row++)
            {
                string line = leaf[row];

                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '1')
                        continue;

                    Color color =
                        row > 8
                            ? LeafDarkColor
                            : LeafColor;

                    int px = left + col * 2;
                    int pyTop = top + row * 2;

                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            int fx = px + sx;
                            int fyTop = pyTop + sy;
                            int fy =
                                TextureHeight - 1 - fyTop;

                            if (fx < 0 ||
                                fy < 0 ||
                                fx >= TextureWidth ||
                                fy >= TextureHeight)
                            {
                                continue;
                            }

                            texture.SetPixel(fx, fy, color);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------
        // Bitmap text
        // ------------------------------------------------------------

        private static void DrawText(
            Texture2D texture,
            string text,
            int left,
            int top,
            int scale,
            Color color)
        {
            if (texture == null ||
                string.IsNullOrEmpty(text) ||
                scale <= 0)
            {
                return;
            }

            int cursorX = left;

            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);

                if (c == ' ')
                {
                    cursorX += 4 * scale;
                    continue;
                }

                string[] glyph = GetGlyph(c);

                if (glyph == null)
                {
                    cursorX += 6 * scale;
                    continue;
                }

                for (int row = 0; row < glyph.Length; row++)
                {
                    string line = glyph[row];

                    for (int col = 0; col < line.Length; col++)
                    {
                        if (line[col] != '1')
                            continue;

                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int px =
                                    cursorX + col * scale + sx;

                                int pyTop =
                                    top + row * scale + sy;

                                int py =
                                    TextureHeight - 1 - pyTop;

                                if (px < 0 ||
                                    py < 0 ||
                                    px >= TextureWidth ||
                                    py >= TextureHeight)
                                {
                                    continue;
                                }

                                texture.SetPixel(px, py, color);
                            }
                        }
                    }
                }

                cursorX += 6 * scale;
            }
        }

        private static string[] GetGlyph(char c)
        {
            switch (c)
            {
                case 'A':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "11111",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'B':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "11110",
                        "10001",
                        "10001",
                        "11110"
                    };

                case 'C':
                    return new[]
                    {
                        "01111",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "01111"
                    };

                case 'D':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "11110"
                    };

                case 'E':
                    return new[]
                    {
                        "11111",
                        "10000",
                        "10000",
                        "11110",
                        "10000",
                        "10000",
                        "11111"
                    };

                case 'F':
                    return new[]
                    {
                        "11111",
                        "10000",
                        "10000",
                        "11110",
                        "10000",
                        "10000",
                        "10000"
                    };

                case 'G':
                    return new[]
                    {
                        "01111",
                        "10000",
                        "10000",
                        "10011",
                        "10001",
                        "10001",
                        "01111"
                    };

                case 'H':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "10001",
                        "11111",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'I':
                    return new[]
                    {
                        "11111",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "11111"
                    };

                case 'J':
                    return new[]
                    {
                        "00111",
                        "00010",
                        "00010",
                        "00010",
                        "00010",
                        "10010",
                        "01100"
                    };

                case 'K':
                    return new[]
                    {
                        "10001",
                        "10010",
                        "10100",
                        "11000",
                        "10100",
                        "10010",
                        "10001"
                    };

                case 'L':
                    return new[]
                    {
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "11111"
                    };

                case 'M':
                    return new[]
                    {
                        "10001",
                        "11011",
                        "10101",
                        "10101",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'N':
                    return new[]
                    {
                        "10001",
                        "11001",
                        "10101",
                        "10011",
                        "10001",
                        "10001",
                        "10001"
                    };

                case 'O':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "01110"
                    };

                case 'P':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "11110",
                        "10000",
                        "10000",
                        "10000"
                    };

                case 'Q':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "10001",
                        "10101",
                        "10010",
                        "01101"
                    };

                case 'R':
                    return new[]
                    {
                        "11110",
                        "10001",
                        "10001",
                        "11110",
                        "10100",
                        "10010",
                        "10001"
                    };

                case 'S':
                    return new[]
                    {
                        "01111",
                        "10000",
                        "10000",
                        "01110",
                        "00001",
                        "00001",
                        "11110"
                    };

                case 'T':
                    return new[]
                    {
                        "11111",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "00100"
                    };

                case 'U':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "01110"
                    };

                case 'V':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "10001",
                        "01010",
                        "00100"
                    };

                case 'W':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "10001",
                        "10101",
                        "10101",
                        "10101",
                        "01010"
                    };

                case 'X':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "01010",
                        "00100",
                        "01010",
                        "10001",
                        "10001"
                    };

                case 'Y':
                    return new[]
                    {
                        "10001",
                        "10001",
                        "01010",
                        "00100",
                        "00100",
                        "00100",
                        "00100"
                    };

                case 'Z':
                    return new[]
                    {
                        "11111",
                        "00001",
                        "00010",
                        "00100",
                        "01000",
                        "10000",
                        "11111"
                    };

                case '0':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10011",
                        "10101",
                        "11001",
                        "10001",
                        "01110"
                    };

                case '1':
                    return new[]
                    {
                        "00100",
                        "01100",
                        "00100",
                        "00100",
                        "00100",
                        "00100",
                        "01110"
                    };

                case '2':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "00001",
                        "00010",
                        "00100",
                        "01000",
                        "11111"
                    };

                case '3':
                    return new[]
                    {
                        "11110",
                        "00001",
                        "00001",
                        "01110",
                        "00001",
                        "00001",
                        "11110"
                    };

                case '4':
                    return new[]
                    {
                        "00010",
                        "00110",
                        "01010",
                        "10010",
                        "11111",
                        "00010",
                        "00010"
                    };

                case '5':
                    return new[]
                    {
                        "11111",
                        "10000",
                        "10000",
                        "11110",
                        "00001",
                        "00001",
                        "11110"
                    };

                case '6':
                    return new[]
                    {
                        "01110",
                        "10000",
                        "10000",
                        "11110",
                        "10001",
                        "10001",
                        "01110"
                    };

                case '7':
                    return new[]
                    {
                        "11111",
                        "00001",
                        "00010",
                        "00100",
                        "01000",
                        "01000",
                        "01000"
                    };

                case '8':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "01110",
                        "10001",
                        "10001",
                        "01110"
                    };

                case '9':
                    return new[]
                    {
                        "01110",
                        "10001",
                        "10001",
                        "01111",
                        "00001",
                        "00001",
                        "01110"
                    };

                case '.':
                    return new[]
                    {
                        "00000",
                        "00000",
                        "00000",
                        "00000",
                        "00000",
                        "00110",
                        "00110"
                    };

                case '-':
                    return new[]
                    {
                        "00000",
                        "00000",
                        "00000",
                        "11111",
                        "00000",
                        "00000",
                        "00000"
                    };

                default:
                    return null;
            }
        }
    }
}