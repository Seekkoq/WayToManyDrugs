using UnityEngine;

namespace CustomNPCExample.UI
{
    /// <summary>
    /// A change-log card the main menu draws on its right-hand side. Pure IMGUI (driven from
    /// MainMod.OnGUI) so it needs no scene object; it reuses MainMenuWatermark's main-menu detection
    /// so both overlays agree about when the menu is up. Rounded, drop-shadowed panel with a green
    /// accent, coloured section headers and hanging bullets; it sizes itself to its text so nothing
    /// is clipped.
    /// </summary>
    public static class WvcChangelogOverlay
    {
        private const float PanelWidth = 452f;
        private const float MarginRight = 60f;
        private const float PadX = 26f;
        private const float PadTop = 22f;
        private const float PadBottom = 26f;
        private const float BulletIndent = 20f;
        private const float ItemSpacing = 5f;
        private const float SectionSpacing = 15f;
        private const int CornerRadius = 13;
        private const int BorderWidth = 2;

        // Green-accented palette, matched to the watermark so the two read as one set.
        private static readonly Color Fill = new Color(0.045f, 0.055f, 0.070f, 0.955f);
        private static readonly Color Border = new Color(0.30f, 0.82f, 0.40f, 0.92f);
        private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color Divider = new Color(0.30f, 0.82f, 0.40f, 0.40f);
        private static readonly Color Accent = new Color(0.38f, 0.92f, 0.48f, 1f);
        private static readonly Color TitleText = new Color(0.93f, 1f, 0.94f, 1f);
        private static readonly Color SubText = new Color(0.45f, 0.95f, 0.52f, 1f);
        private static readonly Color HeaderText = new Color(0.58f, 0.96f, 0.62f, 1f);
        private static readonly Color ItemText = new Color(0.85f, 0.88f, 0.86f, 1f);

        private static readonly string[] SectionTitles =
        {
            "ADDITIONS",
            "FIXED / CHANGES",
            "KNOWN ISSUES"
        };

        private static readonly string[][] SectionItems =
        {
            new[]
            {
                "New dialogue for Mick Lubbin and Philip Wentworth",
                "Pawn shop & casino laundering (up to $65k through the casino every 2 days, $20k through the pawn shop every day)",
                "DMT effects now fade out with a come-down instead of stopping abruptly, plus new effects",
                "Change log panel added to the main menu"
            },
            new[]
            {
                "Fixed NPC faces on packaged products",
                "S1API compatibility and new update compatibility",
                "Fixed NPC weather dialogue repeating the same line after sleeping",
                "Fixed more reported FPS drops"
            },
            new[]
            {
                "Loading times are inconsistent (will be fixed next update)"
            }
        };

        private static Texture2D _panel;
        private static int _panelW;
        private static int _panelH;

        private static bool _stylesReady;
        private static GUIStyle _title;
        private static GUIStyle _sub;
        private static GUIStyle _header;
        private static GUIStyle _item;
        private static GUIContent _titleContent;
        private static GUIContent _subContent;
        private static GUIContent[] _sectionContents;
        private static GUIContent[][] _itemContents;
        private static GUIStyle _panelStyle;
        private static GUIStyle _solidStyle;

        public static void Draw()
        {
            if (!MainMenuWatermark.Visible)
            {
                ReleasePanel();
                return;
            }

            EnsureStyles();

            float contentW = PanelWidth - PadX * 2f;
            float itemW = contentW - BulletIndent;

            // Measure pass (draw = false) walks the same layout as the real draw so the card is
            // exactly as tall as its text - no clipping, no dead space.
            float contentHeight = Walk(false, 0f, 0f, contentW, itemW);
            float panelH = PadTop + contentHeight + PadBottom;

            EnsurePanelTexture((int)PanelWidth, Mathf.CeilToInt(panelH));

            float x = Screen.width - PanelWidth - MarginRight;
            float maxUp = Mathf.Max(10f, Screen.height - panelH - 10f);
            float y = Mathf.Clamp((Screen.height - panelH) * 0.5f, 10f, maxUp);
            Rect panel = new Rect(x, y, PanelWidth, panelH);

            Color saved = GUI.color;

            // Drop shadow (the panel silhouette tinted black, offset). This Il2Cpp build strips the
            // public GUI.DrawTexture overload ("Method unstripping failed"), so every solid draw
            // goes through GUI.Box with a style background - the same primitive AdminMenu uses.
            GUI.color = Shadow;
            GUI.Box(new Rect(panel.x + 5f, panel.y + 6f, panel.width, panel.height), GUIContent.none, _panelStyle);
            GUI.color = Color.white;

            // The card itself (colours are baked into the texture).
            GUI.Box(panel, GUIContent.none, _panelStyle);

            // Content.
            Walk(true, panel.y + PadTop, panel.x + PadX, contentW, itemW);

            GUI.color = saved;
        }

        public static void ResetRuntime()
        {
            ReleasePanel();
            _stylesReady = false;
        }

        private static void ReleasePanel()
        {
            if (_panel == null)
                return;

            UnityEngine.Object.Destroy(_panel);
            _panel = null;
            _panelW = 0;
            _panelH = 0;
        }

        /// <summary>
        /// Draws (or, when draw is false, only measures) the card contents starting at top. Returns
        /// the bottom edge, so the measure pass reports the content height. Both passes call
        /// CalcHeight identically, so the recorded layout always matches what gets drawn.
        /// </summary>
        private static float Walk(bool draw, float top, float panelX, float contentW, float itemW)
        {
            float y = top;
            float itemX = panelX + BulletIndent;

            float h = _title.CalcHeight(_titleContent, contentW);
            if (draw)
                GUI.Label(new Rect(panelX, y, contentW, h), _titleContent, _title);
            y += h + 2f;

            h = _sub.CalcHeight(_subContent, contentW);
            if (draw)
                GUI.Label(new Rect(panelX, y, contentW, h), _subContent, _sub);
            y += h + 12f;

            if (draw)
                DrawSolid(new Rect(panelX, y, contentW, 2f), Divider);
            y += 2f + 14f;

            for (int s = 0; s < SectionTitles.Length; s++)
            {
                GUIContent header = _sectionContents[s];
                h = _header.CalcHeight(header, contentW);
                if (draw)
                    GUI.Label(new Rect(panelX, y, contentW, h), header, _header);
                y += h + 7f;

                GUIContent[] items = _itemContents[s];
                for (int i = 0; i < items.Length; i++)
                {
                    GUIContent item = items[i];
                    h = _item.CalcHeight(item, itemW);

                    if (draw)
                    {
                        const float bullet = 6f;
                        DrawSolid(new Rect(itemX - 14f, y + 4f, bullet, bullet), Accent);
                        GUI.Label(new Rect(itemX, y, itemW, h), item, _item);
                    }

                    y += h + ItemSpacing;
                }

                y += SectionSpacing;
            }

            return y;
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color saved = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, _solidStyle);
            GUI.color = saved;
        }

        private static void EnsurePanelTexture(int w, int h)
        {
            if (_panel != null && _panelW == w && _panelH == h)
                return;

            ReleasePanel();
            _panel = MakeRoundedRect(w, h, CornerRadius, BorderWidth);
            _panelW = w;
            _panelH = h;

            if (_panelStyle != null)
                _panelStyle.normal.background = _panel;
        }

        /// <summary>
        /// Builds a rounded rectangle with a thin accent border, anti-aliased at the edge and the
        /// corners. The signed-distance approach keeps corners smooth at any panel height, which
        /// matters because the card grows with its text.
        /// </summary>
        private static Texture2D MakeRoundedRect(int w, int h, int radius, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[w * h];
            float r = radius;
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            float bx = halfW - r;
            float by = halfH - r;

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float cx = (px + 0.5f) - halfW;
                    float cy = (py + 0.5f) - halfH;
                    float qx = Mathf.Abs(cx) - bx;
                    float qy = Mathf.Abs(cy) - by;
                    float ax = Mathf.Max(qx, 0f);
                    float ay = Mathf.Max(qy, 0f);
                    float sdf = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                    float coverage = Mathf.Clamp01(0.5f - sdf);
                    if (coverage <= 0f)
                    {
                        pixels[py * w + px] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    bool isBorder = sdf > -border;
                    Color baseColor = isBorder ? Border : Fill;
                    pixels[py * w + px] = new Color(baseColor.r, baseColor.g, baseColor.b, coverage * baseColor.a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        private static void EnsureStyles()
        {
            if (_stylesReady)
                return;

            _title = new GUIStyle(GUI.skin.label);
            _title.fontSize = 27;
            _title.fontStyle = FontStyle.Bold;
            _title.alignment = TextAnchor.UpperLeft;
            _title.wordWrap = true;
            _title.normal.textColor = TitleText;

            _sub = new GUIStyle(GUI.skin.label);
            _sub.fontSize = 13;
            _sub.fontStyle = FontStyle.Bold;
            _sub.alignment = TextAnchor.UpperLeft;
            _sub.wordWrap = true;
            _sub.normal.textColor = SubText;

            _header = new GUIStyle(GUI.skin.label);
            _header.fontSize = 15;
            _header.fontStyle = FontStyle.Bold;
            _header.alignment = TextAnchor.UpperLeft;
            _header.wordWrap = true;
            _header.normal.textColor = HeaderText;

            _item = new GUIStyle(GUI.skin.label);
            _item.fontSize = 14;
            _item.fontStyle = FontStyle.Normal;
            _item.alignment = TextAnchor.UpperLeft;
            _item.wordWrap = true;
            _item.normal.textColor = ItemText;

            _titleContent = new GUIContent("CHANGELOG");
            _subContent = new GUIContent("WayToManyDrugs   " + MainMenuWatermark.ModVersion);

            _sectionContents = new GUIContent[SectionTitles.Length];
            for (int s = 0; s < SectionTitles.Length; s++)
                _sectionContents[s] = new GUIContent(SectionTitles[s]);

            _itemContents = new GUIContent[SectionItems.Length][];
            for (int s = 0; s < SectionItems.Length; s++)
            {
                _itemContents[s] = new GUIContent[SectionItems[s].Length];
                for (int i = 0; i < SectionItems[s].Length; i++)
                    _itemContents[s][i] = new GUIContent(SectionItems[s][i]);
            }

            // GUI.Box with a style background is the only IMGUI primitive this Il2Cpp build can draw
            // reliably (the public GUI.DrawTexture overload is stripped and throws at runtime). The
            // panel background is the rounded card; solids are a white background tinted by GUI.color.
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.padding = new RectOffset(0, 0, 0, 0);
            _panelStyle.margin = new RectOffset(0, 0, 0, 0);
            _panelStyle.border = new RectOffset(0, 0, 0, 0);
            if (_panel != null)
                _panelStyle.normal.background = _panel;

            _solidStyle = new GUIStyle(GUI.skin.box);
            _solidStyle.padding = new RectOffset(0, 0, 0, 0);
            _solidStyle.margin = new RectOffset(0, 0, 0, 0);
            _solidStyle.border = new RectOffset(0, 0, 0, 0);
            _solidStyle.normal.background = Texture2D.whiteTexture;

            _stylesReady = true;
        }
    }
}
