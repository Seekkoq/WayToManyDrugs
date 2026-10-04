using System;
using System.Collections.Generic;
using CustomNPCExample.NpcMemory;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using CustomNPCExample.CustomerLoyalty;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Admin
{
    public static class AdminMenu
    {
        public static bool IsOpen { get; private set; }

        private static Rect _windowRect = new Rect(80f, 80f, 920f, 560f);

        private static bool _dragging;
        private static Vector2 _dragOffset;

        private static Vector2 _listScroll;
        private static Vector2 _detailScroll;

        private static string _search = "";
        private static bool _searchFocused;
        private static NPC _selected;

        private static readonly List<NPC> AllNpcs = new List<NPC>();
        private static readonly List<NPC> VisibleNpcs = new List<NPC>();

        private static bool _stylesReady;
        private static Texture2D _panelTex;
        private static Texture2D _titleTex;
        private static Texture2D _selectedTex;
        private static Texture2D _searchTex;
        private static GUIStyle _panelStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _selectedButtonStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _searchStyle;

        private static CursorLockMode _savedLockMode;
        private static bool _savedCursorVisible;

        public static void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                SetOpen(!IsOpen);

            if (!IsOpen)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            HandleSearchTyping();
        }

        public static void Draw()
        {
            if (!IsOpen)
                return;

            try
            {
                EnsureStyles();
                HandleDrag();

                GUI.Box(_windowRect, GUIContent.none, _panelStyle);

                GUILayout.BeginArea(
                    new Rect(
                        _windowRect.x + 12f,
                        _windowRect.y + 8f,
                        _windowRect.width - 24f,
                        _windowRect.height - 16f
                    )
                );

                DrawTitleBar();
                GUILayout.Space(8f);
                DrawBody();

                GUILayout.EndArea();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Admin] Draw failed: " + ex);
            }
        }

        private static void SetOpen(bool open)
        {
            if (open == IsOpen)
                return;

            IsOpen = open;
            _searchFocused = false;
            _dragging = false;

            if (open)
            {
                _savedLockMode = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ScanNpcs();
            }
            else
            {
                Cursor.lockState = _savedLockMode;
                Cursor.visible = _savedCursorVisible;
            }
        }

        private static void HandleSearchTyping()
        {
            if (!_searchFocused)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _searchFocused = false;
                return;
            }

            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed))
                return;

            bool changed = false;

            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];

                if (c == '\b')
                {
                    if (_search.Length > 0)
                    {
                        _search = _search.Substring(0, _search.Length - 1);
                        changed = true;
                    }
                }
                else if (!char.IsControl(c))
                {
                    if (_search.Length < 40)
                    {
                        _search += c;
                        changed = true;
                    }
                }
            }

            if (changed)
                FilterNpcs();
        }

        private static void HandleDrag()
        {
            Event current = Event.current;
            if (current == null)
                return;

            Rect title = new Rect(
                _windowRect.x,
                _windowRect.y,
                _windowRect.width,
                36f
            );

            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                title.Contains(current.mousePosition))
            {
                _dragging = true;
                _searchFocused = false;
                _dragOffset = current.mousePosition -
                    new Vector2(_windowRect.x, _windowRect.y);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _dragging)
            {
                Vector2 pos = current.mousePosition - _dragOffset;
                _windowRect.x = Mathf.Clamp(pos.x, 0f, Screen.width - 120f);
                _windowRect.y = Mathf.Clamp(pos.y, 0f, Screen.height - 80f);
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                _dragging = false;
            }
        }

        private static void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(_titleStyle, GUILayout.Height(28f));
            GUILayout.Label("WVC Admin Menu", _titleStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", _buttonStyle, GUILayout.Width(80f), GUILayout.Height(24f)))
                SetOpen(false);

            GUILayout.EndHorizontal();
            GUILayout.Label(
                "F12 toggles this menu. Drag the top bar to move it.",
                _labelStyle
            );
        }

        private static void DrawBody()
        {
            float bodyHeight = _windowRect.height - 70f;

            GUILayout.BeginHorizontal(
                GUILayout.Height(bodyHeight),
                GUILayout.ExpandWidth(true)
            );

            DrawNpcList(bodyHeight);
            GUILayout.Space(10f);
            DrawNpcDetails(bodyHeight);

            GUILayout.EndHorizontal();
        }

        private static void DrawNpcList(float bodyHeight)
        {
            GUILayout.BeginVertical(
                _boxStyle,
                GUILayout.Width(300f),
                GUILayout.Height(bodyHeight)
            );

            GUILayout.Label("<b>NPCs</b>", _labelStyle);

            GUILayout.BeginHorizontal();

            string searchLabel =
                string.IsNullOrEmpty(_search)
                    ? (_searchFocused ? "|" : "(click, then type)")
                    : _search + (_searchFocused ? "|" : "");

            GUILayout.Label("Search", _labelStyle, GUILayout.Width(50f));

            if (GUILayout.Button(
                    searchLabel,
                    _buttonStyle,
                    GUILayout.ExpandWidth(true)))
            {
                _searchFocused = true;
            }

            if (GUILayout.Button("X", _buttonStyle, GUILayout.Width(28f)))
            {
                _search = "";
                _searchFocused = false;
                FilterNpcs();
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh List", _buttonStyle))
            {
                ScanNpcs();
            }

            float listHeight = bodyHeight - 95f;

            _listScroll = GUILayout.BeginScrollView(
                _listScroll,
                GUILayout.Height(listHeight),
                GUILayout.ExpandWidth(true)
            );

            ApplyMouseWheelToScroll(ref _listScroll);

            if (VisibleNpcs.Count == 0)
            {
                GUILayout.Label("No NPCs found.", _labelStyle);
            }

            for (int i = 0; i < VisibleNpcs.Count; i++)
            {
                NPC npc = VisibleNpcs[i];

                if (npc == null)
                    continue;

                bool selected =
                    _selected != null &&
                    string.Equals(
                        _selected.ID,
                        npc.ID,
                        StringComparison.OrdinalIgnoreCase
                    );

                GUIStyle style =
                    selected ? _selectedButtonStyle : _buttonStyle;

                if (GUILayout.Button(
                        GetNpcDisplayName(npc),
                        style))
                {
                    _selected = npc;
                    _searchFocused = false;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void DrawNpcDetails(float bodyHeight)
        {
            GUILayout.BeginVertical(
                _boxStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(bodyHeight)
            );

            if (_selected == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    "Select an NPC from the list.",
                    _labelStyle
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            float detailHeight = bodyHeight - 20f;

            _detailScroll = GUILayout.BeginScrollView(
                _detailScroll,
                GUILayout.Height(detailHeight),
                GUILayout.ExpandWidth(true)
            );

            ApplyMouseWheelToScroll(ref _detailScroll);

            NPC npc = _selected;
            NPCRelationData relation = npc.RelationData;
            Customer customer = npc.gameObject.GetComponent<Customer>();

            GUILayout.Label("<b>Selected NPC</b>", _labelStyle);
            GUILayout.Label("Name: " + GetNpcDisplayName(npc), _labelStyle);
            GUILayout.Label("ID: " + (npc.ID ?? "(none)"), _labelStyle);
            GUILayout.Label("Region: " + npc.Region, _labelStyle);
            GUILayout.Label(
                "Is Customer: " + (customer != null ? "Yes" : "No"),
                _labelStyle
            );

            GUILayout.Space(8f);
            GUILayout.Label("<b>Relationship Status</b>", _labelStyle);

            if (relation == null)
            {
                GUILayout.Label(
                    "This NPC has no RelationData.",
                    _labelStyle
                );
            }
            else
            {
                float min = NPCRelationData.MinRelationship;
                float max = NPCRelationData.MaxRelationship;
                float current = relation.RelationDelta;

                GUILayout.Label(
                    "RelationDelta: <b>" +
                    current.ToString("0.00") +
                    "</b> (" +
                    min.ToString("0.0") +
                    " to " +
                    max.ToString("0.0") +
                    ")",
                    _labelStyle
                );

                GUILayout.Label(
                    "Status: <b>" +
                    DescribeRelationship(current, min, max) +
                    "</b>",
                    _labelStyle
                );

                try
                {
                    float next = GUILayout.HorizontalSlider(
                        current,
                        min,
                        max
                    );

                    if (Mathf.Abs(next - current) > 0.01f)
                    {
                        relation.SetRelationship(next, true);
                    }
                }
                catch
                {
                    GUILayout.Label(
                        "Slider unavailable. Use buttons.",
                        _labelStyle
                    );
                }

                GUILayout.Space(4f);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Hostile", _buttonStyle))
                    relation.SetRelationship(min, true);

                if (GUILayout.Button("Neutral", _buttonStyle))
                    relation.SetRelationship(0f, true);

                if (GUILayout.Button("Friendly", _buttonStyle))
                    relation.SetRelationship(GetFriendlyValue(min, max), true);

                if (GUILayout.Button("Loyal", _buttonStyle))
                    relation.SetRelationship(max, true);

                if (GUILayout.Button("Loyal (+25% pay)", _buttonStyle))
                {
                    relation.SetRelationship(max, true);

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Admin] Set " +
                        GetNpcDisplayName(npc) +
                        " to loyal relationship."
                    );
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(6f);

                GUILayout.Label(
                    "Unlocked: <b>" +
                    (relation.Unlocked ? "YES" : "NO") +
                    "</b>",
                    _labelStyle
                );

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Unlock", _buttonStyle))
                {
                    relation.Unlock(
                        NPCRelationData.EUnlockType.DirectApproach,
                        false
                    );
                }

                if (GUILayout.Button("Lock", _buttonStyle))
                {
                    relation._Unlocked_k__BackingField = false;
                    relation._UnlockType_k__BackingField =
                        NPCRelationData.EUnlockType.DirectApproach;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label("<b>NPC Memory</b>", _labelStyle);

            int strikes = NpcMemoryManager.GetStrikes(npc.ID);
            bool needsSample = NpcMemoryManager.RequiresSample(npc.ID);

            GUILayout.Label(
                "Strikes: <b>" +
                strikes +
                " / " +
                NpcMemoryManager.MaxStrikes +
                "</b>",
                _labelStyle
            );

            GUILayout.Label(
                "Requires Sample: <b>" +
                (needsSample ? "YES" : "NO") +
                "</b>",
                _labelStyle
            );

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Strike", _buttonStyle))
            {
                NpcMemoryManager.AddStrike(npc.ID);
                NpcMemoryManager.Save();
            }

            if (GUILayout.Button("Reset Memory", _buttonStyle))
            {
                NpcMemoryManager.Reset(npc.ID);
                NpcMemoryManager.Save();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    needsSample
                        ? "Clear Sample Req"
                        : "Require Sample",
                    _buttonStyle))
            {
                if (needsSample)
                    NpcMemoryManager.CompleteSample(npc.ID);
                else
                    NpcMemoryManager.RequireSample(npc.ID);

                NpcMemoryManager.Save();
            }

            GUILayout.EndHorizontal();

            if (customer != null)
            {
                GUILayout.Space(10f);

                GUILayout.Label(
                    "<b>Customer Deal Controls</b>",
                    _labelStyle
                );

                bool loyal =
                    relation != null &&
                    LoyalCustomerPaymentPatch.IsLoyal(relation);

                float multiplier =
                    relation != null
                        ? LoyalCustomerPaymentPatch.GetPaymentMultiplier(relation)
                        : 1f;

                GUILayout.Label(
                    "Loyal Customer: <b>" +
                    (loyal ? "YES" : "NO") +
                    "</b>",
                    _labelStyle
                );

                GUILayout.Label(
                    "Loyal threshold: <b>" +
                    LoyalCustomerPaymentPatch
                        .GetLoyalThreshold()
                        .ToString("0.00") +
                    "</b>",
                    _labelStyle
                );

                GUILayout.Label(
                    "Deal payment multiplier: <b>x" +
                    multiplier.ToString("0.00") +
                    "</b>",
                    _labelStyle
                );

                GUILayout.Label(
                    "Offered Contract: <b>" +
                    (customer.offeredContractInfo != null
                        ? "YES"
                        : "NO") +
                    "</b>",
                    _labelStyle
                );

                if (GUILayout.Button(
                        "Force Customer Deal Text",
                        _buttonStyle))
                {
                    ForceCustomerDeal(customer, npc);
                }

                if (GUILayout.Button(
                        "Clear Offered Contract",
                        _buttonStyle))
                {
                    customer.offeredContractInfo = null;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Admin] Cleared offered contract for " +
                        GetNpcDisplayName(npc) +
                        "."
                    );
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        private static void ApplyMouseWheelToScroll(
    ref Vector2 scroll)
        {
            try
            {
                Event current = Event.current;

                if (current == null)
                    return;

                if (current.type != EventType.ScrollWheel)
                    return;

                scroll.y += current.delta.y * 22f;

                if (scroll.y < 0f)
                    scroll.y = 0f;

                current.Use();
            }
            catch
            {
            }
        }

        private static void ForceCustomerDeal(
            Customer customer,
            NPC npc)
        {
            try
            {
                if (customer == null)
                {


                    return;
                }

                if (npc == null)
                {


                    return;
                }

                if (npc.RelationData == null)
                {


                    return;
                }

                if (!npc.RelationData.Unlocked)
                {


                    return;
                }

                if (customer.offeredContractInfo != null)
                {
                    customer.offeredContractInfo = null;
                }

                customer.ForceDealOffer();

                if (customer.offeredContractInfo != null)
                {
                    float payment =
                        customer.offeredContractInfo.Payment;

                    bool loyal =
                        LoyalCustomerPaymentPatch.IsLoyal(
                            npc.RelationData
                        );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Admin] Forced a deal from " +
                        GetNpcDisplayName(npc) +
                        ". Payment=$" +
                        payment.ToString("0") +
                        ", Loyal=" +
                        loyal +
                        "."
                    );
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Admin] Force deal failed for " +
                    GetNpcDisplayName(npc) +
                    ": " +
                    ex
                );
            }
        }

        private static void ScanNpcs()
        {
            AllNpcs.Clear();

            NPC[] found = UnityEngine.Object.FindObjectsOfType<NPC>();
            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    NPC npc = found[i];
                    if (npc == null || string.IsNullOrWhiteSpace(npc.ID))
                        continue;

                    AllNpcs.Add(npc);
                }
            }

            AllNpcs.Sort((a, b) =>
                string.Compare(
                    GetNpcDisplayName(a),
                    GetNpcDisplayName(b),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            FilterNpcs();

            if (_selected == null)
                return;

            for (int i = 0; i < AllNpcs.Count; i++)
            {
                if (AllNpcs[i] != null &&
                    string.Equals(
                        AllNpcs[i].ID,
                        _selected.ID,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _selected = AllNpcs[i];
                    return;
                }
            }

            _selected = null;
        }

        private static void FilterNpcs()
        {
            VisibleNpcs.Clear();
            string query = (_search ?? "").Trim();

            for (int i = 0; i < AllNpcs.Count; i++)
            {
                NPC npc = AllNpcs[i];
                if (npc == null)
                    continue;

                if (query.Length == 0)
                {
                    VisibleNpcs.Add(npc);
                    continue;
                }

                string name = GetNpcDisplayName(npc);
                string id = npc.ID ?? "";

                if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    VisibleNpcs.Add(npc);
                }
            }
        }

        private static string GetNpcDisplayName(NPC npc)
        {
            if (npc == null)
                return "(null)";

            if (!string.IsNullOrWhiteSpace(npc.FullName))
                return npc.FullName;

            if (!string.IsNullOrWhiteSpace(npc.ID))
                return npc.ID;

            return npc.name;
        }

        private static float GetFriendlyValue(float min, float max)
        {
            if (max > 0f)
                return max * 0.5f;

            return (min + max) * 0.5f;
        }

        private static string DescribeRelationship(
            float value,
            float min,
            float max)
        {
            float friendly = GetFriendlyValue(min, max);

            if (value <= min + 0.01f)
                return "Hostile";
            if (value < -0.01f)
                return "Unfriendly";
            if (value <= 0.25f)
                return "Neutral";
            if (value < friendly)
                return "Friendly";
            if (value < max - 0.01f)
                return "Very Friendly";
            return "Loyal";
        }

        private static void EnsureStyles()
        {
            if (_stylesReady && _panelTex != null)
                return;

            _panelTex = MakeTex(new Color(0.10f, 0.10f, 0.12f, 0.96f));
            _titleTex = MakeTex(new Color(0.16f, 0.16f, 0.20f, 1f));
            _selectedTex = MakeTex(new Color(0.18f, 0.32f, 0.55f, 1f));
            _searchTex = MakeTex(new Color(0.08f, 0.08f, 0.10f, 1f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTex, textColor = Color.white }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { background = _titleTex, textColor = Color.white },
                padding = new RectOffset(8, 8, 4, 4)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            _selectedButtonStyle = new GUIStyle(_buttonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = _selectedTex, textColor = Color.white }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8)
            };

            _searchStyle = new GUIStyle(_buttonStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { background = _searchTex, textColor = Color.white }
            };

            _stylesReady = true;
        }

        private static Texture2D MakeTex(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}