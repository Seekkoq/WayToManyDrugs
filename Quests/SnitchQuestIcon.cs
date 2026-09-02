using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Quests
{
    public static class SnitchQuestIcon
    {
        private static Sprite _icon;

        public static Sprite Get()
        {
            if (_icon != null)
                return _icon;

            const int size = 128;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            tex.filterMode = FilterMode.Bilinear;

            Color transparent = new Color(0, 0, 0, 0);
            Color outerRing = new Color(0.10f, 0.25f, 0.50f, 1f); // Deep blue
            Color innerFill = new Color(0.20f, 0.55f, 0.95f, 1f); // Bright blue
            Color center = new Color(0.95f, 0.98f, 1.00f, 1f); // White dot

            float radius = size * 0.5f;
            Vector2 mid = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), mid);

                    if (dist > radius - 1f)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else if (dist > radius * 0.85f)
                    {
                        tex.SetPixel(x, y, outerRing);
                    }
                    else if (dist > radius * 0.35f)
                    {
                        tex.SetPixel(x, y, innerFill);
                    }
                    else
                    {
                        tex.SetPixel(x, y, center);
                    }
                }
            }

            tex.Apply(false, true);

            _icon = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );

            _icon.name = "WVC_SnitchQuest_Icon";

            MelonLogger.Msg("[WVC Snitch] Round quest icon generated.");
            return _icon;
        }
    }
}