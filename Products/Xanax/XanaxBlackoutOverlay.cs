using UnityEngine;
using UnityEngine.UI;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// The black-screen fade used by the Xanax blackout.
    ///
    /// This deliberately lives in its own plain class instead of on
    /// <see cref="XanaxScreenEffect"/>: Il2CppInterop has to inject any MonoBehaviour it is asked
    /// to add, and a member of type <see cref="Image"/> on that class breaks the injection, which
    /// makes AddComponent fail with a TypeInitializationException. Keeping UnityEngine.UI out of
    /// the component type keeps the component injectable.
    /// </summary>
    internal static class XanaxBlackoutOverlay
    {
        private static GameObject _overlayObject;
        private static CanvasGroup _group;
        private static Image _overlayImage;
        private static float _alpha = -1f;

        /// <summary>
        /// Fades the black screen in or out.
        ///
        /// The alpha is driven through a <see cref="CanvasGroup"/> rather than the image colour on
        /// purpose: changing a Graphic's colour marks the canvas dirty and forces a rebuild, and
        /// this runs every frame while a blink or a blackout plays - that rebuild was a source of
        /// the little hitches. A canvas group alpha is applied at render time and rebuilds nothing.
        /// While the value does not change, no native call is made at all.
        /// </summary>
        internal static void SetAlpha(float alpha)
        {
            Ensure();

            if (_group == null)
                return;

            float clamped = Mathf.Clamp01(alpha);

            if (Mathf.Abs(clamped - _alpha) < 0.002f)
                return;

            _alpha = clamped;
            _group.alpha = clamped;
        }

        internal static void Hide()
        {
            if (_alpha != 0f)
            {
                _alpha = 0f;

                if (_group != null)
                    _group.alpha = 0f;
            }

            if (_overlayObject != null && _overlayObject.activeSelf)
                _overlayObject.SetActive(false);
        }

        /// <summary>Brings the overlay back up without touching the fade value.</summary>
        internal static void Show()
        {
            Ensure();

            if (_overlayObject != null && !_overlayObject.activeSelf)
                _overlayObject.SetActive(true);
        }

        /// <summary>True once the overlay has been created, i.e. something has faded at least once.</summary>
        internal static bool IsVisible => _overlayObject != null;

        private static void Ensure()
        {
            if (_overlayObject != null)
                return;

            _overlayObject = new GameObject("WVC_Xanax_BlackoutOverlay");
            UnityEngine.Object.DontDestroyOnLoad(_overlayObject);

            Canvas canvas = _overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            _group = _overlayObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            GameObject imageObject = new GameObject("Fade");
            imageObject.transform.SetParent(_overlayObject.transform, false);

            _overlayImage = imageObject.AddComponent<Image>();
            _overlayImage.raycastTarget = false;
            _overlayImage.color = new Color(0f, 0f, 0f, 1f);

            RectTransform rect = _overlayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // The canvas stays up for the whole high instead of being toggled every blink: an
            // inactive-to-active flip rebuilds the canvas, and the blinks come every few seconds.
            _overlayObject.SetActive(true);

            _alpha = 0f;
        }
    }
}
