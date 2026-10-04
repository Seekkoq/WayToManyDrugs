using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppTMPro;
using CustomNPCExample.Weather;
using MelonLoader;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

using GameSettingsScreen = Il2CppScheduleOne.UI.MainMenu.SettingsScreen;

namespace CustomNPCExample.UI
{
    /// <summary>
    /// The mod's own rows inside the game's settings screen.
    ///
    /// Copying a row the game had already built did not survive: the game's rows belong to their own
    /// settings, and a copy of one comes apart when the panel is shown - it keeps the space it takes
    /// and draws nothing. These rows are built instead: a background, a label, and a button carrying
    /// the value that steps through the choices when it is clicked. Nothing about them belongs to the
    /// game, so nothing about them can be taken away with it. They are styled from a row the game did
    /// build (its font, its text size, the height of its row) so that they look as though they belong
    /// there, and they are placed at the top of the tab the screen opens on, which is where the player
    /// is already looking.
    ///
    /// Being styled from one of the game's rows is a thing that has to be done twice. A panel that has
    /// only just been built still carries the row heights out of its prefab, and a row built to those
    /// is a different size from the rows beside it - which is what the in-game settings list showed,
    /// and the main menu, whose panel is already laid out when it is filled, did not. So the rows are
    /// shaped once when they are built and again on the first frames the screen is up, from the row
    /// heights the list has actually been given.
    /// </summary>
    public static class WvcSettingsTab
    {
        private const string LogPrefix = "[WVC Settings]";
        private const string RowPrefix = "WVC_SnowRow_";
        private const int ExpectedRows = 4;

        private sealed class RowSpec
        {
            public string Key;
            public string Label;
            public string[] Options;
            public Func<int> Read;
            public Action<int> Write;
        }

        private sealed class RowView
        {
            public RowSpec Spec;

            /// <summary>The row itself, which is what is shaped like one of the game's rows.</summary>
            public RectTransform Row;

            /// <summary>The label's rect and the label on it, which is sized to the room left over.</summary>
            public RectTransform Label;
            public TMP_Text LabelText;

            /// <summary>The box the value sits in, and the value written in it.</summary>
            public RectTransform Button;
            public TMP_Text Value;

            /// <summary>The height the row takes up in the list.</summary>
            public LayoutElement Element;
        }

        private static readonly List<RowSpec> Specs = new List<RowSpec>();
        private static readonly List<RowView> Rows = new List<RowView>();

        private static bool _patched;
        private static bool _dumped;
        private static bool _noRoomWarned;

        /// <summary>The lists already written into the log, so one tab's report is made once.</summary>
        private static readonly HashSet<int> Reported = new HashSet<int>();

        private static bool _wasVisible;
        private static float _tickTimer;
        private static int _repairPasses;
        private static RectTransform _container;

        /// <summary>
        /// The game's own row the mod's rows are shaped from, kept so that they can be shaped again
        /// once the panel has been laid out.
        /// </summary>
        private static RectTransform _reference;

        /// <summary>The size the panel's own writing is set at, which is the size the mod's rows use.</summary>
        private static float _styleSize;
        private static GameObject _panel;
        private static GameSettingsScreen _screen;

        public static void Initialize(HarmonyLib.Harmony harmony)
        {
            if (_patched || harmony == null)
                return;

            _patched = true;

            BuildSpecs();

            try
            {
                MethodInfo awake = AccessTools.Method(typeof(GameSettingsScreen), "Awake");
                MethodInfo open = AccessTools.Method(typeof(GameSettingsScreen), "OnOpen");

                if (awake != null)
                {
                    harmony.Patch(
                        awake,
                        postfix: new HarmonyMethod(
                            typeof(WvcSettingsTab),
                            nameof(SettingsScreen_Ready)));
                }

                if (open != null)
                {
                    harmony.Patch(
                        open,
                        postfix: new HarmonyMethod(
                            typeof(WvcSettingsTab),
                            nameof(SettingsScreen_Opened)));
                }

                WvcSnowSettings.Changed += RefreshRows;


            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Describes the rows once: what each one says, what its values are, and how to read and write
        /// the setting behind it.
        /// </summary>
        private static void BuildSpecs()
        {
            if (Specs.Count > 0)
                return;

            Specs.Add(
                new RowSpec
                {
                    Key = "ScheduleEnabled",
                    Label = "Snow",
                    Options = new[] { "Off", "On" },
                    Read = ScheduleIndex,
                    Write = index => WvcSnowSettings.ScheduleEnabled = index == 1
                });

            Specs.Add(
                new RowSpec
                {
                    Key = "BlizzardsPerWeek",
                    Label = "Blizzards",
                    Options = new[] { "0", "1", "2", "3", "4" },
                    Read = () => NumberIndex(
                        WvcSnowSettings.BlizzardsPerWeek,
                        new[] { 0, 1, 2, 3, 4 }),
                    Write = index => WvcSnowSettings.BlizzardsPerWeek =
                        new[] { 0, 1, 2, 3, 4 }[Clamp(index, 5)]
                });

            Specs.Add(
                new RowSpec
                {
                    Key = "SnowFlakeCount",
                    Label = "Calm snow",
                    Options = new[] { "300", "500", "700", "850", "1200", "1800" },
                    Read = () => NumberIndex(
                        WvcSnowSettings.SnowFlakes,
                        new[] { 300, 500, 700, 850, 1200, 1800 }),
                    Write = index => WvcSnowSettings.SnowFlakes =
                        new[] { 300, 500, 700, 850, 1200, 1800 }[Clamp(index, 6)]
                });

            Specs.Add(
                new RowSpec
                {
                    Key = "StormFlakeCount",
                    Label = "Storm snow",
                    Options = new[] { "900", "1100", "1600", "2600", "3600", "4800" },
                    Read = () => NumberIndex(
                        WvcSnowSettings.StormFlakes,
                        new[] { 900, 1100, 1600, 2600, 3600, 4800 }),
                    Write = index => WvcSnowSettings.StormFlakes =
                        new[] { 900, 1100, 1600, 2600, 3600, 4800 }[Clamp(index, 6)]
                });
        }

        private static int ScheduleIndex()
        {
            return WvcSnowSettings.ScheduleEnabled ? 1 : 0;
        }

        /// <summary>Where a value sits in a list of choices, or -1 when it is not in the list at all.</summary>
        private static int NumberIndex(int value, int[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == value)
                    return i;
            }

            return -1;
        }

        private static int Clamp(int index, int count)
        {
            if (index < 0)
                return 0;

            if (index >= count)
                return count - 1;

            return index;
        }

        private static int SafeRead(RowSpec spec)
        {
            try
            {
                return spec.Read();
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>What a row shows: the choice it is on, or that it is on something else.</summary>
        private static string DescribeValue(RowSpec spec)
        {
            int index = SafeRead(spec);

            if (index >= 0 && index < spec.Options.Length)
                return spec.Options[index];

            return "set elsewhere";
        }

        private static void SettingsScreen_Ready(GameSettingsScreen __instance)
        {
            TryInject(__instance, "awake");
        }

        private static void SettingsScreen_Opened(GameSettingsScreen __instance)
        {
            TryInject(__instance, "open");

            RefreshRows();
        }

        /// <summary>
        /// Runs from the mod's own update.
        ///
        /// This is how the rows are brought to the player's attention, because there is no hook that
        /// fires when a settings tab is shown. The moment the tab comes up, the list is returned to
        /// its top - a list sitting a little way down hides the rows at the top of it and shows the
        /// game's own rows instead, which reads exactly like the mod's rows missing - and the rows are
        /// asked to draw again, in case they were built while the panel was switched off.
        /// </summary>
        public static void Tick()
        {
            if (_container == null || Rows.Count == 0)
                return;

            bool visible;

            try
            {
                visible = _container.gameObject.activeInHierarchy;
            }
            catch
            {
                visible = false;
            }

            if (!visible)
            {
                _wasVisible = false;
                return;
            }

            if (!_wasVisible)
            {
                _wasVisible = true;
                _repairPasses = 0;



                RestyleRows();
                RevealRows();
                RepairRows();
                ReportLiveStateOnce();
            }

            _tickTimer += Time.deltaTime;

            if (_tickTimer < 0.25f)
                return;

            _tickTimer = 0f;

            if (_repairPasses >= 8)
                return;

            _repairPasses++;

            RestyleRows();
            RepairRows();
        }

        /// <summary>Takes the list to its top, where the mod's rows are.</summary>
        private static void RevealRows()
        {
            try
            {
                if (_container == null)
                    return;

                ScrollRect scroll = _container.GetComponentInParent<ScrollRect>();

                LayoutRebuilder.ForceRebuildLayoutImmediate(_container);
                Canvas.ForceUpdateCanvases();

                if (scroll == null)
                    return;

                float was = scroll.verticalNormalizedPosition;

                scroll.verticalNormalizedPosition = 1f;
                scroll.velocity = Vector2.zero;


            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Marks the rows dirty so they are drawn.
        ///
        /// A graphic built while its panel was switched off can be left out of the canvas until
        /// something marks it dirty: it then keeps its place in the layout and draws nothing.
        /// </summary>
        private static void RepairRows()
        {
            try
            {
                if (_container != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_container);

                Canvas.ForceUpdateCanvases();
            }
            catch { }

            for (int i = 0; i < Rows.Count; i++)
            {
                try
                {
                    TMP_Text value = Rows[i].Value;

                    if (value == null)
                        continue;

                    Graphic graphic = value.TryCast<Graphic>();

                    if (graphic != null)
                    {
                        if (!graphic.enabled)
                            graphic.enabled = true;

                        graphic.SetAllDirty();
                    }

                    value.SetAllDirty();
                }
                catch { }
            }
        }

        private static void TryInject(GameSettingsScreen screen, string reason)
        {
            if (screen == null)
                return;

            try
            {
                if (!_dumped)
                {
                    _dumped = true;

                    DumpScreen(screen);
                }

                GameSettingsScreen.SettingsCategory home =
                    FindHomeCategory(screen, out int homeIndex);

                if (home == null || home.Panel == null)
                {
                    if (!_noRoomWarned)
                    {
                        _noRoomWarned = true;


                    }

                    return;
                }

                _screen = screen;
                _panel = home.Panel;



                EnsureRows(home.Panel, reason);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The tab the rows go in: the first tab that has a list of rows, which is the tab the screen
        /// opens on.
        /// </summary>
        private static GameSettingsScreen.SettingsCategory FindHomeCategory(
            GameSettingsScreen screen,
            out int index)
        {
            index = -1;

            try
            {
                var categories = screen.Categories;

                if (categories == null)
                    return null;

                // The tab the rows belong in is the Display one in both screens. "First tab with a
                // list" was only ever right in the main menu, where Display happens to come first: the
                // in-game screen's first tab is Game, so the same code dropped the snow rows into the
                // game settings there. The tab is therefore looked for by name, with the old
                // first-with-a-list behaviour kept as the fallback for a layout that has no Display tab.
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 0; i < categories.Length; i++)
                    {
                        var category = categories[i];

                        if (category == null || category.Panel == null)
                            continue;

                        if (FindContent(category.Panel.transform) == null)
                            continue;

                        if (pass == 0 && !IsDisplayTab(category))
                            continue;

                        index = i;
                        return category;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>Whether a tab is the Display one, by its panel's name or its tab's own label.</summary>
        private static bool IsDisplayTab(GameSettingsScreen.SettingsCategory category)
        {
            try
            {
                if (category.Panel != null &&
                    string.Equals(category.Panel.name, "Display", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch { }

            try
            {
                if (category.Toggle != null)
                {
                    TMP_Text label = FindControl<TMP_Text>(category.Toggle.gameObject);

                    if (label != null && !string.IsNullOrEmpty(label.text) &&
                        label.text.IndexOf("display", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>Builds the rows into a tab's list, or updates them when they are already there.</summary>
        private static void EnsureRows(GameObject panel, string reason)
        {
            RectTransform container = FindContent(panel.transform);

            if (container == null)
            {
                if (!_noRoomWarned)
                {
                    _noRoomWarned = true;


                }

                return;
            }

            _container = container;

            int present = CountOurRows(container);

            if (present >= ExpectedRows)
            {
                RefreshRows();
                return;
            }



            RemoveOurRows(container);
            Rows.Clear();

            RectTransform reference = FindReferenceRow(container);

            // A panel that has only just been built still answers with its prefab's row heights;
            // laying the list out is what turns those into the heights the rows have on screen.
            Settle(container);

            _reference = reference;

            float height = RowHeight(container, reference);



            int slot = 0;

            for (int i = 0; i < Specs.Count; i++)
                BuildRow(container, reference, Specs[i], slot++, height);

            RefreshRows();
        }

        /// <summary>
        /// Builds one row: a background, a label, and a button carrying the value that steps through
        /// the choices when it is clicked.
        /// </summary>
        private static void BuildRow(
            RectTransform container,
            RectTransform reference,
            RowSpec spec,
            int slot,
            float height)
        {
            try
            {
                GameObject row = CreateObject(RowPrefix + spec.Key, container);

                RectTransform rect = row.transform.TryCast<RectTransform>();

                if (rect != null)
                {
                    Stretch(rect, reference);

                    if (slot >= 0 && slot < container.childCount)
                        row.transform.SetSiblingIndex(slot);
                }

                Image background = row.AddComponent<Image>();

                // Faint, so the row reads as part of the panel, and solid enough to catch a click.
                background.color = new Color(1f, 1f, 1f, 0.05f);

                var view = new RowView
                {
                    Spec = spec,
                    Row = rect,
                    Element = row.AddComponent<LayoutElement>()
                };

                TMP_Text label = CreateText(row.transform, "Name", reference);

                if (label != null)
                {
                    view.Label = label.transform.TryCast<RectTransform>();
                    view.LabelText = label;

                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.text = spec.Label;
                }

                GameObject buttonObject = CreateObject("Value", row.transform);

                view.Button = buttonObject.transform.TryCast<RectTransform>();

                Image buttonImage = buttonObject.AddComponent<Image>();

                buttonImage.color = new Color(1f, 1f, 1f, 0.16f);

                Button button = buttonObject.AddComponent<Button>();

                button.targetGraphic = buttonImage;

                TMP_Text value = CreateText(buttonObject.transform, "Value Text", reference);

                if (value != null)
                {
                    RectTransform valueRect = value.transform.TryCast<RectTransform>();

                    if (valueRect != null)
                    {
                        valueRect.anchorMin = Vector2.zero;
                        valueRect.anchorMax = Vector2.one;
                        valueRect.offsetMin = Vector2.zero;
                        valueRect.offsetMax = Vector2.zero;
                    }

                    value.alignment = TextAlignmentOptions.Center;
                    view.Value = value;
                }

                ShapeRow(view, reference, height);

                RowSpec captured = spec;

                EventHelper.AddListener(
                    new Action(() => Step(captured)),
                    button.onClick);

                Rows.Add(view);


            }
            catch (Exception)
            {

            }
        }

        /// <summary>Moves a row on to its next choice, and writes the setting it stands for.</summary>
        private static void Step(RowSpec spec)
        {
            try
            {
                int index = SafeRead(spec);

                index = index < 0 ? 0 : (index + 1) % spec.Options.Length;

                spec.Write(index);



                RefreshRows();
            }
            catch (Exception)
            {

            }
        }

        private static void RefreshRows()
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                RowView row = Rows[i];

                try
                {
                    if (row.Value != null)
                        row.Value.text = DescribeValue(row.Spec);
                }
                catch { }
            }
        }

        /// <summary>A GameObject with a RectTransform, which is what every piece of UI here needs.</summary>
        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;

            return go;
        }

        /// <summary>
        /// A text line styled from the game's own text, so that the row's writing matches the panel's.
        /// </summary>
        private static TMP_Text CreateText(Transform parent, string name, RectTransform reference)
        {
            try
            {
                GameObject go = CreateObject(name, parent);

                TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();

                TMP_Text source = reference == null
                    ? null
                    : FindControl<TMP_Text>(reference.gameObject);

                if (source == null)
                    source = FindAnyText();

                if (source != null)
                {
                    if (source.font != null)
                        text.font = source.font;

                    text.fontSize = source.fontSize;
                    text.color = source.color;
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.enableWordWrapping = false;

                    if (source.fontSize > 1f)
                        _styleSize = source.fontSize;
                }
                else
                {
                    text.color = Color.white;
                    text.fontSize = 18f;
                }

                return text;
            }
            catch (Exception)
            {


                return null;
            }
        }

        private static TMP_Text FindAnyText()
        {
            try
            {
                return UnityEngine.Object.FindObjectOfType<TMP_Text>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Gives a new object the shape of the row the look was copied from.</summary>
        private static void Stretch(RectTransform target, RectTransform reference)
        {
            if (target == null)
                return;

            if (reference != null)
            {
                target.anchorMin = reference.anchorMin;
                target.anchorMax = reference.anchorMax;
                target.pivot = reference.pivot;
                target.sizeDelta = reference.sizeDelta;
                target.anchoredPosition = reference.anchoredPosition;

                return;
            }

            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(1f, 1f);
            target.pivot = new Vector2(0.5f, 1f);
            target.sizeDelta = new Vector2(0f, 50f);
            target.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// How tall a row in this list is, from the list rather than from a row.
        ///
        /// The row itself is not asked: a list that has only just been built still answers with the
        /// height out of its prefab, and rows built to that are a different size from the rows beside
        /// them. The distance between two of the game's own rows is what the layout actually gave
        /// them, so that is what is measured.
        /// </summary>
        private static float RowHeight(RectTransform container, RectTransform reference)
        {
            float height = 0f;

            try
            {
                float spacing;
                float pitch = RowPitch(container, out spacing);

                if (pitch > 1f)
                    height = pitch - spacing;

                if (reference != null)
                {
                    float own = reference.rect.height;

                    // Both readings are taken. The gap between two rows can be measured around
                    // something that is not a setting at all, and a row that has not been laid out
                    // yet still answers with the height out of its prefab; the smaller of the two is
                    // the safer way round, because a row that comes out too short leaves a gap where
                    // a row that comes out too tall is drawn over the one below it.
                    if (own > 1f && (height < 1f || own < height))
                        height = own;
                }
            }
            catch { }

            if (height < 20f)
                height = 50f;

            return Mathf.Clamp(height, 20f, 160f);
        }

        /// <summary>The distance between two of the game's own rows in a list, and the list's spacing
        /// between rows.</summary>
        private static float RowPitch(RectTransform container, out float spacing)
        {
            spacing = 0f;

            if (container == null)
                return 0f;

            try
            {
                VerticalLayoutGroup group =
                    container.gameObject.GetComponent<VerticalLayoutGroup>();

                if (group != null)
                    spacing = group.spacing;
            }
            catch { }

            float previous = float.NaN;
            float pitch = 0f;

            try
            {
                int count = container.childCount;

                for (int i = 0; i < count; i++)
                {
                    Transform child = container.GetChild(i);

                    if (child == null || child.name.StartsWith(RowPrefix))
                        continue;

                    float y = child.position.y;

                    if (!float.IsNaN(previous))
                    {
                        pitch = Mathf.Abs(previous - y);
                        break;
                    }

                    previous = y;
                }
            }
            catch { }

            return pitch;
        }



        /// <summary>Lays a list out now, so what is read from it is what is on screen rather than what
        /// the prefab was built with.</summary>
        private static void Settle(RectTransform container)
        {
            try
            {
                if (container == null)
                    return;

                LayoutRebuilder.ForceRebuildLayoutImmediate(container);
                Canvas.ForceUpdateCanvases();
            }
            catch { }
        }

        /// <summary>
        /// Shapes the mod's rows like the game's own once more, from a list that has been laid out.
        ///
        /// This is the pass that fixes a panel that has only just been built: its rows keep the size
        /// the prefab gives them until the list is laid out, and rows shaped from them then stand out -
        /// too tall, with their writing where the game's writing is not. Run once the screen is up, and
        /// again for a moment after, so that the game's own layout has had its say.
        /// </summary>
        private static void RestyleRows()
        {
            if (_container == null || Rows.Count == 0)
                return;

            try
            {
                Settle(_container);

                RectTransform reference = FindReferenceRow(_container);

                if (reference != null)
                    _reference = reference;

                if (_reference == null)
                    return;

                float height = RowHeight(_container, _reference);

                for (int i = 0; i < Rows.Count; i++)
                    ShapeRow(Rows[i], _reference, height);
            }
            catch { }
        }

        /// <summary>
        /// Gives one row its shape: the size and the place the game's own rows have, a label that takes
        /// the room left over, and a value box to the right of it.
        ///
        /// The numbers come from the row's own width rather than from fixed ones, because the panel the
        /// rows go in is not the same size in both screens: the main menu's list is a wide one and the
        /// in-game settings list is not, and a fixed-size value box on a narrow row is drawn over the
        /// label.
        /// </summary>
        private static void ShapeRow(RowView row, RectTransform reference, float height)
        {
            if (row == null || row.Row == null)
                return;

            Stretch(row.Row, reference);

            if (row.Element != null)
            {
                row.Element.preferredHeight = height;
                row.Element.minHeight = height;
                row.Element.flexibleHeight = 0f;
            }

            float width = RowWidth(reference, row.Row);

            float inset = Mathf.Clamp(width * 0.03f, 8f, 24f);

            // The value box is sized to the writing it actually holds, not to a share of the row.
            // A share was the wrong measure both ways round: on the main menu's wide rows it left
            // the box far wider than anything in it, and on the in-game screen's narrow rows it came
            // out narrower than the value did - so "set elsewhere" spilled out of its box and was
            // drawn straight over the label beside it, which is the overlap that showed up in game.
            float buttonWidth = ValueWidth(row, width);
            float buttonHeight = Mathf.Clamp(height - 10f, 22f, 40f);

            float labelLeft = inset + 6f;
            float labelRight = buttonWidth + inset * 2f + 10f;

            if (row.Label != null)
            {
                row.Label.anchorMin = new Vector2(0f, 0f);
                row.Label.anchorMax = new Vector2(1f, 1f);
                row.Label.offsetMin = new Vector2(labelLeft, 0f);
                row.Label.offsetMax = new Vector2(-labelRight, 0f);
            }

            SizeLabel(row, width - labelLeft - labelRight);

            if (row.Button != null)
            {
                row.Button.anchorMin = new Vector2(1f, 0.5f);
                row.Button.anchorMax = new Vector2(1f, 0.5f);
                row.Button.pivot = new Vector2(1f, 0.5f);
                row.Button.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                row.Button.anchoredPosition = new Vector2(-inset, 0f);
            }
        }

        /// <summary>The width a row is drawn at: the game's own row's, or the row's own, or a guess.</summary>
        private static float RowWidth(RectTransform reference, RectTransform row)
        {
            float width = 0f;

            try
            {
                if (reference != null)
                    width = reference.rect.width;

                if (width < 40f && row != null)
                    width = row.rect.width;
            }
            catch { }

            return width < 40f ? 520f : width;
        }

        /// <summary>
        /// How wide the value box has to be to hold its writing.
        ///
        /// The value is brought up to date first, because a box sized for the value the row used to
        /// be on would be the wrong width for the one it is on now. The measurement is capped at half
        /// the row so a very long value is clipped by its box rather than taking the label's room.
        /// </summary>
        private static float ValueWidth(RowView row, float rowWidth)
        {
            float needed = 56f;

            try
            {
                TMP_Text text = row == null ? null : row.Value;

                if (text != null)
                {
                    string wanted = DescribeValue(row.Spec);

                    if (text.text != wanted)
                        text.text = wanted;

                    float measured = text.preferredWidth;

                    if (measured > 1f)
                        needed = measured + 24f;
                }
            }
            catch { }

            float cap = Mathf.Max(64f, rowWidth * 0.5f);

            return Mathf.Clamp(needed, 56f, cap);
        }

        /// <summary>
        /// Sets the label's writing to the largest size that fits the room the row leaves it, up to the
        /// size the panel's own writing is set at: on a narrow row a label left at the panel's size runs
        /// into the value box, or wraps and runs into the row below.
        /// </summary>
        private static void SizeLabel(RowView row, float room)
        {
            TMP_Text text = row == null ? null : row.LabelText;

            if (text == null)
                return;

            float size = _styleSize > 1f ? _styleSize : text.fontSize;


            if (size < 1f)
                size = 18f;

            text.fontSize = size;

            if (room > 1f)
            {
                float needed = text.preferredWidth;

                if (needed > 1f && needed > room)
                    text.fontSize = Mathf.Max(8f, size * (room / needed));
            }
        }

        /// <summary>The widest row in the list, used as the shape and the styling to copy.</summary>
        private static RectTransform FindReferenceRow(RectTransform container)
        {
            RectTransform widest = null;

            try
            {
                int count = container.childCount;

                for (int i = 0; i < count; i++)
                {
                    Transform child = container.GetChild(i);

                    if (child == null || child.name.StartsWith(RowPrefix))
                        continue;

                    RectTransform rect = child.TryCast<RectTransform>();

                    if (rect == null)
                        continue;

                    if (widest == null || rect.rect.width > widest.rect.width)
                        widest = rect;
                }
            }
            catch { }

            return widest;
        }

        private static void RemoveOurRows(RectTransform container)
        {
            int removed = 0;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);

                if (child == null || !child.name.StartsWith(RowPrefix))
                    continue;

                UnityEngine.Object.Destroy(child.gameObject);
                removed++;
            }

            if (removed > 0)
                { }
        }

        private static int CountOurRows(RectTransform container)
        {
            if (container == null)
                return 0;

            int found = 0;

            try
            {
                int count = container.childCount;

                for (int i = 0; i < count; i++)
                {
                    Transform child = container.GetChild(i);

                    if (child != null && child.name.StartsWith(RowPrefix))
                        found++;
                }
            }
            catch { }

            return found;
        }

        /// <summary>
        /// The row list of a settings panel: the scrolled list when it has one, otherwise the object
        /// that stacks rows, otherwise the panel itself.
        /// </summary>
        private static RectTransform FindContent(Transform panel)
        {
            try
            {
                List<ScrollRect> scrolls =
                    Controls(panel.gameObject, t => t.GetComponent<ScrollRect>());

                if (scrolls.Count > 0 && scrolls[0] != null && scrolls[0].content != null)
                    return scrolls[0].content;

                List<VerticalLayoutGroup> groups =
                    Controls(panel.gameObject, t => t.GetComponent<VerticalLayoutGroup>());

                if (groups.Count > 0)
                    return groups[0].transform.TryCast<RectTransform>();

                return panel.TryCast<RectTransform>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Every component of a kind under an object.
        ///
        /// Two searches are merged, because each misses something the other finds. The hierarchy is
        /// walked by hand and each object asked directly, which reaches components on a panel the game
        /// has switched off - a search of children answers with nothing there. The native search is
        /// then added, because it matches derived classes.
        ///
        /// The lookup is handed in as a lambda, because a component lookup written with a type
        /// parameter of this method answers with nothing through the interop layer.
        /// </summary>
        private static List<T> Controls<T>(GameObject root, Func<GameObject, T> pick)
            where T : Component
        {
            List<T> found = new List<T>();

            if (root == null || root.transform == null)
                return found;

            Walk(root, found, pick);

            try
            {
                T[] native = root.GetComponentsInChildren<T>(true);

                if (native != null)
                {
                    for (int i = 0; i < native.Length; i++)
                    {
                        if (native[i] != null && !found.Contains(native[i]))
                            found.Add(native[i]);
                    }
                }
            }
            catch { }

            return found;
        }

        private static void Walk<T>(
            GameObject root,
            List<T> found,
            Func<GameObject, T> pick,
            int depth = 0)
            where T : Component
        {
            if (root == null || depth > 9)
                return;

            try
            {
                T component = pick(root);

                if (component != null && !found.Contains(component))
                    found.Add(component);
            }
            catch { }

            try
            {
                Transform transform = root.transform;

                if (transform == null)
                    return;

                int count = transform.childCount;

                for (int i = 0; i < count; i++)
                {
                    Transform child = transform.GetChild(i);

                    if (child != null)
                        Walk(child.gameObject, found, pick, depth + 1);
                }
            }
            catch { }
        }

        private static T FindControl<T>(GameObject root)
            where T : Component
        {
            List<T> found = Controls(root, t => t.GetComponent<T>());

            return found.Count > 0 ? found[0] : null;
        }

        private static string Path(Transform transform)
        {
            if (transform == null)
                return "?";

            string text = transform.name;

            Transform current = transform.parent;

            for (int depth = 0; current != null && depth < 4; depth++)
            {
                text = current.name + "/" + text;
                current = current.parent;
            }

            return text;
        }

        /// <summary>
        /// Writes what the rows look like while the screen is up, and what the list around them is.
        ///
        /// This is the report that says whether a row that is not on screen is there at all: its place
        /// in the list, whether it is active, where it sits in the world, and whether the list it is in
        /// is scrolled away from it.
        /// </summary>
        private static void ReportLiveStateOnce()
        {
            if (_container == null)
                return;

            if (!Reported.Add(_container.GetInstanceID()))
                return;

            try
            {
                ScrollRect scroll = _container.GetComponentInParent<ScrollRect>();




                for (int i = 0; i < Rows.Count; i++)
                {
                    RowView view = Rows[i];

                    if (view.Row == null || view.Value == null)
                    {


                        continue;
                    }

                    Vector3 position = view.Row.position;


                }

                // Every child of the list, top to bottom: this is what shows a row that came out a
                // different height from the game's own, which is a thing that is only visible on the
                // screen otherwise.


                for (int i = 0; i < _container.childCount; i++)
                {
                    Transform child = _container.GetChild(i);

                    if (child == null)
                        continue;

                    RectTransform rect = child.TryCast<RectTransform>();


                }


            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Writes the settings screen's shape to the log, once.
        ///
        /// The rows are placed by shape rather than by a path read out of the game's files, so this is
        /// what makes a run where they did not appear say why. The tab list is always written; the
        /// lines under each tab are held back until verbose logging is on.
        /// </summary>
        private static void DumpScreen(GameSettingsScreen screen)
        {
            try
            {
                var categories = screen.Categories;



                if (categories == null)
                    return;

                for (int i = 0; i < categories.Length; i++)
                {
                    var category = categories[i];

                    if (category == null)
                    {

                        continue;
                    }

                    string tabLabel = "(no label)";

                    try
                    {
                        if (category.Toggle != null)
                        {
                            TMP_Text label =
                                category.Toggle.GetComponentInChildren<TMP_Text>(true);

                            if (label != null)
                                tabLabel = label.text;
                        }
                    }
                    catch { }



                    if (category.Panel != null)
                        DumpChildren(category.Panel.transform, 2);
                }
            }
            catch (Exception)
            {

            }
        }

        private static void DumpChildren(Transform transform, int depth)
        {
            if (transform == null)
                return;

            int count = transform.childCount;

            for (int i = 0; i < count; i++)
            {
                Transform child = transform.GetChild(i);

                if (child == null)
                    continue;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    LogPrefix + "  " + new string(' ', depth * 2 + 2) + child.name);

                if (depth < 3)
                    DumpChildren(child, depth + 1);
            }
        }
    }
}
