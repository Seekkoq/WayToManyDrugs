using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

using S1PhoneApp = S1API.PhoneApp.PhoneApp;

namespace CustomNPCExample.UI
{
    public sealed class WvcRecipesPhoneApp : S1PhoneApp
    {
        private const string LogPrefix = "[WayToManyDrugs Recipes App]";

        private static WvcRecipesPhoneApp _instance;
        private static bool _registered;
        private static Sprite _iconSprite;

        private readonly List<RecipeRow> _rows = new List<RecipeRow>();
        private readonly List<Action> _clickActions = new List<Action>();

        private RectTransform _contentRect;
        private Font _font;

        protected override string AppName => "WayToManyDrugs";
        protected override string AppTitle => "WayToManyDrugs" +
            " Recipes";
        protected override string IconLabel => "Recipes";
        protected override string IconFileName => "wvc_recipes_icon.png";
        protected override Sprite IconSprite => GetIconSprite();
        protected override S1PhoneApp.EOrientation Orientation => S1PhoneApp.EOrientation.Horizontal;

        public static void Initialize()
        {
            if (_registered)
                return;

            _instance = new WvcRecipesPhoneApp();

            if (TryRegisterPhoneApp(_instance))
            {
                _registered = true;
                global::CustomNPCExample.Utils.WvcLog.Msg(LogPrefix + " Registered.");
            }
            else
            {
                MelonLogger.Error(LogPrefix + " Failed to register phone app.");
            }
        }

        private static bool TryRegisterPhoneApp(WvcRecipesPhoneApp app)
        {
            if (app == null)
                return false;

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy;

            string[] lifecycleNames =
            {
                "Register",
                "Create"
            };

            foreach (string methodName in lifecycleNames)
            {
                try
                {
                    MethodInfo method =
                        app.GetType().GetMethod(
                            methodName,
                            flags,
                            null,
                            Type.EmptyTypes,
                            null
                        );

                    if (method == null)
                        continue;

                    method.Invoke(app, null);
                    return true;
                }
                catch (TargetInvocationException)
                {


                    return false;
                }
                catch (Exception)
                {


                    return false;
                }
            }

            try
            {
                MethodInfo onCreated =
                    typeof(S1PhoneApp).GetMethod(
                        "OnCreated",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );

                if (onCreated != null)
                {
                    onCreated.Invoke(app, null);
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                Type registryType =
                    typeof(S1PhoneApp).Assembly.GetType(
                        "S1API.PhoneApp.PhoneAppRegistry"
                    );

                if (registryType == null)
                    return false;

                MethodInfo[] methods =
                    registryType.GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "Register")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1)
                        continue;

                    if (!parameters[0].ParameterType.IsAssignableFrom(app.GetType()))
                        continue;

                    method.Invoke(null, new object[] { app });
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        protected override void OnCreatedUI(GameObject container)
        {
            try
            {
                BuildUI(container);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    LogPrefix +
                    " UI creation failed: " +
                    ex
                );
            }
        }

        private void BuildUI(GameObject container)
        {
            _rows.Clear();
            _clickActions.Clear();
            _contentRect = null;

            RectTransform root =
                container.GetComponent<RectTransform>();

            if (root == null)
                root = container.AddComponent<RectTransform>();

            GameObject bg =
                CreatePanel(
                    "Background",
                    container.transform,
                    new Color(0.055f, 0.045f, 0.075f, 0.96f)
                );

            Stretch(bg.GetComponent<RectTransform>());

            Text title =
                CreateText(
                    "Title",
                    container.transform,
                    "WayToManyDrugs Recipes",
                    34,
                    new Color(1f, 0.82f, 0.35f, 1f),
                    TextAnchor.MiddleLeft
                );

            SetAnchors(
                title.rectTransform,
                new Vector2(0.035f, 0.885f),
                new Vector2(0.965f, 0.975f),
                Vector2.zero,
                Vector2.zero
            );

            title.fontStyle = FontStyle.Bold;

            Text subtitle =
                CreateText(
                    "Subtitle",
                    container.transform,
                    "Tap a product below to drop down its in-game ingredient list and crafting steps.",
                    18,
                    new Color(0.82f, 0.82f, 0.88f, 1f),
                    TextAnchor.MiddleLeft
                );

            SetAnchors(
                subtitle.rectTransform,
                new Vector2(0.035f, 0.835f),
                new Vector2(0.965f, 0.885f),
                Vector2.zero,
                Vector2.zero
            );

            CreateScrollArea(container.transform);

            RecipeEntry[] recipes =
                BuildRecipes();

            for (int i = 0; i < recipes.Length; i++)
            {
                AddRecipeDropdown(recipes[i], i);
            }

            if (_rows.Count > 0)
                SetRowExpanded(0, true);

            RefreshLayout();

            Text footer =
                CreateText(
                    "Footer",
                    container.transform,
                    "WayToManyDrugs in-game Recipe Guide.",
                    14,
                    new Color(0.65f, 0.65f, 0.72f, 1f),
                    TextAnchor.MiddleCenter
                );

            SetAnchors(
                footer.rectTransform,
                new Vector2(0.035f, 0.005f),
                new Vector2(0.965f, 0.04f),
                Vector2.zero,
                Vector2.zero
            );
        }

        private void CreateScrollArea(Transform parent)
        {
            GameObject scrollRoot =
                CreatePanel(
                    "RecipeScroll",
                    parent,
                    new Color(0f, 0f, 0f, 0.18f)
                );

            RectTransform scrollRectTransform =
                scrollRoot.GetComponent<RectTransform>();

            SetAnchors(
                scrollRectTransform,
                new Vector2(0.035f, 0.055f),
                new Vector2(0.965f, 0.82f),
                Vector2.zero,
                Vector2.zero
            );

            ScrollRect scroll =
                scrollRoot.AddComponent<ScrollRect>();

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.scrollSensitivity = 25f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport =
                CreatePanel(
                    "Viewport",
                    scrollRoot.transform,
                    new Color(0f, 0f, 0f, 0f)
                );

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            Stretch(viewportRect);

            Image viewportImage =
                viewport.GetComponent<Image>();

            if (viewportImage != null)
                viewportImage.raycastTarget = true;

            viewport.AddComponent<RectMask2D>();

            GameObject content =
                CreateUIObject(
                    "Content",
                    viewport.transform
                );

            _contentRect =
                content.GetComponent<RectTransform>();

            _contentRect.anchorMin = new Vector2(0f, 1f);
            _contentRect.anchorMax = new Vector2(1f, 1f);
            _contentRect.pivot = new Vector2(0.5f, 1f);
            _contentRect.anchoredPosition = Vector2.zero;
            _contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                content.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                content.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = _contentRect;
        }

        private void AddRecipeDropdown(
            RecipeEntry recipe,
            int index)
        {
            Text headerText;
            Button button =
                CreateHeaderButton(
                    _contentRect,
                    recipe,
                    out headerText
                );

            GameObject body =
                CreateBodyPanel(
                    _contentRect,
                    recipe
                );

            body.SetActive(false);

            RecipeRow row =
                new RecipeRow
                {
                    Entry = recipe,
                    HeaderText = headerText,
                    Body = body,
                    Expanded = false
                };

            _rows.Add(row);

            int capturedIndex = index;

            Action clickAction =
                delegate
                {
                    ToggleRow(capturedIndex);
                };

            _clickActions.Add(clickAction);

            EventHelper.AddListener(
                clickAction,
                button.onClick
            );

            SetRowExpanded(index, false);
        }

        private Button CreateHeaderButton(
            Transform parent,
            RecipeEntry recipe,
            out Text label)
        {
            GameObject go =
                CreateUIObject(
                    "Header_" + SafeName(recipe.Name),
                    parent
                );

            Image image =
                go.AddComponent<Image>();

            image.color =
                new Color(0.14f, 0.11f, 0.18f, 0.98f);

            Button button =
                go.AddComponent<Button>();

            button.targetGraphic = image;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                new Color(0.14f, 0.11f, 0.18f, 1f);

            colors.highlightedColor =
                new Color(0.22f, 0.17f, 0.29f, 1f);

            colors.pressedColor =
                new Color(0.30f, 0.22f, 0.38f, 1f);

            colors.selectedColor =
                colors.highlightedColor;

            colors.colorMultiplier = 1f;
            button.colors = colors;

            LayoutElement layoutElement =
                go.AddComponent<LayoutElement>();

            layoutElement.preferredHeight = 50f;
            layoutElement.flexibleWidth = 1f;

            label =
                CreateText(
                    "Label",
                    go.transform,
                    recipe.Name,
                    21,
                    recipe.TitleColor,
                    TextAnchor.MiddleLeft
                );

            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;

            SetAnchors(
                label.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(18f, 0f),
                new Vector2(-12f, 0f)
            );

            return button;
        }

        private GameObject CreateBodyPanel(
            Transform parent,
            RecipeEntry recipe)
        {
            GameObject body =
                CreatePanel(
                    "Body_" + SafeName(recipe.Name),
                    parent,
                    new Color(0.085f, 0.075f, 0.105f, 0.98f)
                );

            Image image =
                body.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = false;

            LayoutElement bodyLayoutElement =
                body.AddComponent<LayoutElement>();

            bodyLayoutElement.flexibleWidth = 1f;

            VerticalLayoutGroup layout =
                body.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                body.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text details =
                CreateText(
                    "Details",
                    body.transform,
                    FormatRecipe(recipe),
                    17,
                    new Color(0.92f, 0.92f, 0.96f, 1f),
                    TextAnchor.UpperLeft
                );

            details.raycastTarget = false;
            details.supportRichText = true;
            details.horizontalOverflow = HorizontalWrapMode.Wrap;
            details.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement textLayout =
                details.gameObject.AddComponent<LayoutElement>();

            textLayout.flexibleWidth = 1f;

            return body;
        }

        private void ToggleRow(int index)
        {
            if (index < 0 || index >= _rows.Count)
                return;

            bool shouldOpen =
                !_rows[index].Expanded;

            for (int i = 0; i < _rows.Count; i++)
                SetRowExpanded(i, false);

            if (shouldOpen)
                SetRowExpanded(index, true);

            RefreshLayout();
        }

        private void SetRowExpanded(
            int index,
            bool expanded)
        {
            if (index < 0 || index >= _rows.Count)
                return;

            RecipeRow row =
                _rows[index];

            row.Expanded = expanded;

            if (row.Body != null)
                row.Body.SetActive(expanded);

            if (row.HeaderText != null)
            {
                row.HeaderText.text =
                    (expanded ? "▼ " : "▶ ") +
                    row.Entry.Name;
            }
        }

        private void RefreshLayout()
        {
            if (_contentRect == null)
                return;

            try
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            }
            catch
            {
            }
        }

        private static RecipeEntry[] BuildRecipes()
        {
            return new RecipeEntry[]
            {
                new RecipeEntry(
                    "MDMA",
                    new Color(1.00f, 0.25f, 0.62f, 1f),
                    "Chemistry Station / Mixer",
                    "$95 base price",
                    new string[]
                    {
                        "Safrole Oil",
                        "PMK Powder",
                        "Refined PMK",
                        "Lab Grade PMK",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Use Safrole Oil plus a PMK powder in the cauldron.",
                        "PMK Powder is the standard grade.",
                        "Refined PMK and Lab Grade PMK are higher-grade PMK powders for the same recipe.",
                        "Package finished tablets as baggies, jars, or bricks."
                    },
                    new string[]
                    {
                        "Console aliases: mdma, molly, ecstasy",
                        "Better PMK grade = better finished quality."
                    }
                ),

                new RecipeEntry(
                    "THC Gummies",
                    new Color(0.95f, 0.10f, 0.08f, 1f),
                    "Cauldron / Gummy Oven",
                    "$45 base price",
                    new string[]
                    {
                        "THC Oil — buy from Army Fogarty, do not craft",
                        "Gelatin",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Buy THC Oil from Army Fogarty.",
                        "Mix THC Oil and Gelatin into Unbaked Gummy Mix.",
                        "Cook the mix in the gummy oven.",
                        "Package finished gummies."
                    },
                    new string[]
                    {
                        "Console aliases: gummies, gummy, thcgummies",
                        "THC Oil is purchased, not produced."
                    }
                ),

                new RecipeEntry(
                    "DMT",
                    new Color(0.55f, 0.35f, 1.00f, 1f),
                    "Cauldron → DMT Lab Oven",
                    "High-value psychedelic",
                    new string[]
                    {
                        "Dreamroot",
                        "Caustic Base",
                        "Lab Solvent",
                        "Crystalizer (optional, for premium)",
                        "Gasoline (consumed by the cauldron)",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Load Dreamroot, Caustic Base, and Lab Solvent into the cauldron.",
                        "Gasoline is consumed by the cauldron as fuel.",
                        "Standard cook outputs Crude Extract.",
                        "Add Crystalizer to output Premium Crude Extract instead.",
                        "Finish the extract in the DMT Lab Oven to get DMT.",
                        "Package finished DMT."
                    },
                    new string[]
                    {
                        "No Crystalizer = Crude Extract / Standard quality.",
                        "Crystalizer present = Premium Crude Extract / Premium quality."
                    }
                ),

                new RecipeEntry(
                    "Vape Cart",
                    new Color(0.20f, 0.85f, 1.00f, 1f),
                    "Army Fogarty",
                    "Purchased product — not crafted",
                    new string[]
                    {
                        "Vape Cart — buy from Army Fogarty",
                        "THC Oil — also bought from Army Fogarty"
                    },
                    new string[]
                    {
                        "Do not craft the Vape Cart.",
                        "Buy the cart from Army Fogarty.",
                        "THC Oil is also bought from Army Fogarty. You do not make it."
                    },
                    new string[]
                    {
                        "Shop item, not a station recipe.",
                        "Same vendor as THC Oil."
                    }
                ),

                new RecipeEntry(
                    "Brownie",
                    new Color(0.55f, 0.28f, 0.12f, 1f),
                    "Brownie Oven",
                    "$75 base price",
                    new string[]
                    {
                        "Baker's Cocoa x3",
                        "Infused Butter x1",
                        "Leavening Mix x1",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Select Unbaked Brownie Mix at the chemistry station.",
                        "Insert the ingredients and press BEGIN.",
                        "The mix cooks in about 4 minutes.",
                        "Bake the Unbaked Brownie Mix in the Lab Oven.",
                        "Package finished brownies as baggies, jars, or bricks."
                    },
                    new string[]
                    {
                        "Console aliases: brownie, edible",
                    }
                ),

                new RecipeEntry(
                    "THC Cookie",
                    new Color(0.92f, 0.62f, 0.22f, 1f),
                    "Cookie Oven",
                    "$80 base price",
                    new string[]
                    {
                        "Cannabis Flour",
                        "Butterscotch Chips",
                        "Unbaked Cookie Dough",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Craft Unbaked Cookie Dough from Cannabis Flour and Butterscotch Chips.",
                        "Bake the dough in the Cookie Oven.",
                        "Package finished THC Cookies."
                    },
                    new string[]
                    {
                        "Console aliases: cookie, thccookie",
                    }
                ),

                new RecipeEntry(
                    "Salvia",
                    new Color(0.35f, 0.78f, 0.45f, 1f),
                    "Sal Viah / Soil Pot",
                    "$70 base price",
                    new string[]
                    {
                        "Salvia Seed buy from Sal Viah",
                        "Soil & Pot (for cultivation)",
                        "Packaging: baggie / jar / brick"
                    },
                    new string[]
                    {
                        "Order Salvia Seed from Sal Viah",
                        "Place soil into pot along with the seed, place the soil in first",
                        "Harvested Salvia can be packaged in baggies, jars, or pressed into bricks.",
                        "Can also be blended in mixers for custom mixtures."
                    },
                    new string[]
                    {
                        "Console aliases: salvia, salviaseed, salviacutting",
                        "Supplier: Sal Viah (Park Gazebo drop)."
                    }
                ),

                new RecipeEntry(
                    "Xanax",
                    new Color(0.92f, 0.93f, 0.90f),
                    "Brick Press",
                    "$45 base price",
                    new string[]
                    {
                        "Xanax Powder - order from Dr. Scrivens (Pillville, Westville)",
                        "Brick Press - buy the machine, then clear the last batch out of it",
                        "Packaging: baggie or jar (a press will not brick a bar)"
                    },
                    new string[]
                    {
                        "Pour Xanax Powder into the press - it fits the mould slot, the raw slot " +
                        "or the equipment tray.",
                        "Press BEGIN: ten units of powder come out as ten bars.",
                        "One press takes up to ten units, so twenty powder takes two runs; " +
                        "everything left over stays in the machine.",
                        "Take the bars from the output, then load the next batch."
                    },
                    new string[]
                    {
                        "Console aliases: xanax",
                        "Supplier: Dr. Eleanor Scrivens (stash behind the medical practice).",
                        "Calming, sedating and foggy - habit forming."
                    }
                )
            };
        }

        private static string FormatRecipe(RecipeEntry recipe)
        {
            StringBuilder sb =
                new StringBuilder();

            sb.AppendLine(
                "<color=#FFD36B><b>Station:</b></color> " +
                recipe.Station
            );

            sb.AppendLine(
                "<color=#FFD36B><b>Value:</b></color> " +
                recipe.Value
            );

            sb.AppendLine();

            sb.AppendLine(
                "<color=#8EE6A8><b>Ingredients</b></color>"
            );

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                sb.AppendLine(
                    "- " +
                    recipe.Ingredients[i]
                );
            }

            sb.AppendLine();

            sb.AppendLine(
                "<color=#8EC8FF><b>Steps</b></color>"
            );

            for (int i = 0; i < recipe.Steps.Length; i++)
            {
                sb.AppendLine(
                    (i + 1) +
                    ". " +
                    recipe.Steps[i]
                );
            }

            if (recipe.Notes != null &&
                recipe.Notes.Length > 0)
            {
                sb.AppendLine();

                sb.AppendLine(
                    "<color=#CBA8FF><b>Notes</b></color>"
                );

                for (int i = 0; i < recipe.Notes.Length; i++)
                {
                    sb.AppendLine(
                        "- " +
                        recipe.Notes[i]
                    );
                }
            }

            return sb.ToString();
        }

        private GameObject CreatePanel(
            string name,
            Transform parent,
            Color color)
        {
            GameObject go =
                CreateUIObject(name, parent);

            Image image =
                go.AddComponent<Image>();

            image.color = color;

            return go;
        }

        private Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment)
        {
            GameObject go =
                CreateUIObject(name, parent);

            Text text =
                go.AddComponent<Text>();

            Font font =
                GetFont();

            if (font != null)
                text.font = font;

            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private Font GetFont()
        {
            if (_font != null)
                return _font;

            try
            {
                Text existingText =
                    UnityEngine.Object.FindObjectOfType<Text>();

                if (existingText != null &&
                    existingText.font != null)
                {
                    _font = existingText.font;
                    return _font;
                }
            }
            catch
            {
            }

            try
            {
                _font =
                    Resources.GetBuiltinResource<Font>(
                        "Arial.ttf"
                    );
            }
            catch
            {
                _font = null;
            }

            return _font;
        }

        private static GameObject CreateUIObject(
            string name,
            Transform parent)
        {
            GameObject go =
                new GameObject(name);

            RectTransform rect =
                go.AddComponent<RectTransform>();

            rect.SetParent(
                parent,
                false
            );

            go.layer =
                parent.gameObject.layer;

            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(
                rect,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero
            );
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static string SafeName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "Recipe";

            return text
                .Replace(" ", "_")
                .Replace("/", "_")
                .Replace(":", "_")
                .Replace("-", "_");
        }

        private static Sprite GetIconSprite()
        {
            if (_iconSprite != null)
                return _iconSprite;

            const int size = 256;

            Texture2D tex =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );

            tex.name = "WVC_Recipes_AppIcon_Texture";
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Color top =
                new Color(0.18f, 0.08f, 0.26f, 1f);

            Color bottom =
                new Color(0.06f, 0.04f, 0.09f, 1f);

            for (int y = 0; y < size; y++)
            {
                float t =
                    y / (float)(size - 1);

                Color c =
                    Color.Lerp(bottom, top, t);

                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, c);
            }

            FillRect(tex, 54, 54, 148, 148, new Color(0.94f, 0.88f, 0.68f, 1f));
            FillRect(tex, 64, 66, 128, 124, new Color(0.18f, 0.10f, 0.22f, 1f));
            FillRect(tex, 70, 78, 116, 12, new Color(1.00f, 0.72f, 0.22f, 1f));
            FillRect(tex, 70, 106, 92, 10, new Color(0.78f, 0.62f, 0.95f, 1f));
            FillRect(tex, 70, 130, 104, 10, new Color(0.78f, 0.62f, 0.95f, 1f));
            FillRect(tex, 70, 154, 74, 10, new Color(0.78f, 0.62f, 0.95f, 1f));

            FillCircle(tex, 178, 82, 9, new Color(1f, 0.24f, 0.58f, 1f));
            FillCircle(tex, 174, 164, 8, new Color(0.95f, 0.18f, 0.12f, 1f));
            FillCircle(tex, 154, 182, 7, new Color(0.90f, 0.62f, 0.24f, 1f));

            tex.Apply(false, true);

            UnityEngine.Object.DontDestroyOnLoad(tex);

            _iconSprite =
                Sprite.Create(
                    tex,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

            _iconSprite.name =
                "WVC_Recipes_AppIcon";

            UnityEngine.Object.DontDestroyOnLoad(_iconSprite);

            return _iconSprite;
        }

        private static void FillRect(
            Texture2D tex,
            int x,
            int y,
            int width,
            int height,
            Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
                    if (px < 0 ||
                        py < 0 ||
                        px >= tex.width ||
                        py >= tex.height)
                    {
                        continue;
                    }

                    tex.SetPixel(px, py, color);
                }
            }
        }

        private static void FillCircle(
            Texture2D tex,
            int centerX,
            int centerY,
            int radius,
            Color color)
        {
            int r2 =
                radius * radius;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x < 0 ||
                        y < 0 ||
                        x >= tex.width ||
                        y >= tex.height)
                    {
                        continue;
                    }

                    int dx =
                        x - centerX;

                    int dy =
                        y - centerY;

                    if (dx * dx + dy * dy <= r2)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        private sealed class RecipeRow
        {
            public RecipeEntry Entry;
            public Text HeaderText;
            public GameObject Body;
            public bool Expanded;
        }

        private sealed class RecipeEntry
        {
            public readonly string Name;
            public readonly Color TitleColor;
            public readonly string Station;
            public readonly string Value;
            public readonly string[] Ingredients;
            public readonly string[] Steps;
            public readonly string[] Notes;

            public RecipeEntry(
                string name,
                Color titleColor,
                string station,
                string value,
                string[] ingredients,
                string[] steps,
                string[] notes)
            {
                Name = name;
                TitleColor = titleColor;
                Station = station;
                Value = value;
                Ingredients = ingredients ?? new string[0];
                Steps = steps ?? new string[0];
                Notes = notes ?? new string[0];
            }
        }
    }
}
