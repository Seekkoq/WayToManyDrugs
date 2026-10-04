using System;
using System.Reflection;
using MelonLoader;
using S1API.Items;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    public static class WvcIcon
    {
        public static bool Apply(string itemId, Sprite icon)
        {
            if (string.IsNullOrEmpty(itemId) || icon == null)
                return false;

            bool applied = false;

            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);
                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    CustomNPCExample.Products.CartItem.GetRawDefinition(itemId);

                applied |= TrySetMember(wrapper, "Icon", icon);
                applied |= TrySetMember(raw, "Icon", icon);
                applied |= TrySetAnyIconSpriteMember(wrapper, icon);
                applied |= TrySetAnyIconSpriteMember(raw, icon);

                if (!applied)
                {

                }
            }
            catch (Exception)
            {

            }

            return applied;
        }

        /// <summary>
        /// Reads the icon that is currently on the item definition. Used to tell whether the game
        /// produced an icon of its own before a hand-authored sprite is written over it.
        /// </summary>
        public static Sprite GetIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);

                if (TryGetSpriteMember(wrapper, "Icon", out Sprite sprite) && sprite != null)
                    return sprite;

                Il2CppScheduleOne.ItemFramework.ItemDefinition raw =
                    CustomNPCExample.Products.CartItem.GetRawDefinition(itemId);

                if (TryGetSpriteMember(raw, "Icon", out sprite) && sprite != null)
                    return sprite;
            }
            catch (Exception)
            {

            }

            return null;
        }

        /// <summary>
        /// Source-over compositing: draws the foreground on top of the background, the way a painting
        /// program layers shapes. The drawn icons are built shape by shape - a shadow, then a body,
        /// then grains - so each one has to sit on what is already there rather than replace it.
        /// </summary>
        public static Color Over(Color foreground, Color background)
        {
            float alpha = foreground.a + background.a * (1f - foreground.a);

            if (alpha <= 0.0001f)
                return new Color(0f, 0f, 0f, 0f);

            return new Color(
                (foreground.r * foreground.a + background.r * background.a * (1f - foreground.a)) / alpha,
                (foreground.g * foreground.a + background.g * background.a * (1f - foreground.a)) / alpha,
                (foreground.b * foreground.a + background.b * background.a * (1f - foreground.a)) / alpha,
                alpha);
        }

        private static bool TryGetSpriteMember(object target, string name, out Sprite sprite)
        {
            sprite = null;

            if (target == null) return false;

            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
                {
                    try
                    {
                        sprite = field.GetValue(target) as Sprite;
                        if (sprite != null) return true;
                    }
                    catch { }
                }

                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null && prop.CanRead && typeof(Sprite).IsAssignableFrom(prop.PropertyType))
                {
                    try
                    {
                        sprite = prop.GetValue(target) as Sprite;
                        if (sprite != null) return true;
                    }
                    catch { }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool TrySetMember(object target, string name, object value)
        {
            if (target == null || value == null) return false;
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    try { field.SetValue(target, value); return true; } catch { }
                }

                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null && prop.CanWrite && prop.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try { prop.SetValue(target, value); return true; } catch { }
                }
                type = type.BaseType;
            }
            return false;
        }

        private static bool TrySetAnyIconSpriteMember(object target, Sprite icon)
        {
            if (target == null || icon == null) return false;
            bool applied = false;
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo[] fields =
                    type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (!typeof(Sprite).IsAssignableFrom(field.FieldType)) continue;
                    if (!field.Name.ToLowerInvariant().Contains("icon")) continue;

                    try { field.SetValue(target, icon); applied = true; } catch { }
                }

                PropertyInfo[] properties =
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (PropertyInfo property in properties)
                {
                    if (!property.CanWrite) continue;
                    if (!typeof(Sprite).IsAssignableFrom(property.PropertyType)) continue;
                    if (!property.Name.ToLowerInvariant().Contains("icon")) continue;

                    try { property.SetValue(target, icon, null); applied = true; } catch { }
                }

                type = type.BaseType;
            }

            return applied;
        }
    }
}
