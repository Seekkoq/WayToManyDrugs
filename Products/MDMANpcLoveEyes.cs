using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

using GameAvatar = Il2CppScheduleOne.AvatarFramework.Avatar;

namespace CustomNPCExample.Products
{
    public static class MDMANpcLoveEyes
    {
        private sealed class SavedEyes
        {
            public float PupilDilation;
            public Color EyeBallTint;
            public Color LeftEyeLidColor;
            public Color RightEyeLidColor;
        }

        private static readonly Dictionary<object, SavedEyes> Originals =
            new Dictionary<object, SavedEyes>();

        /// <summary>
        /// The native pointers of the avatars whose eyes have already been painted.
        ///
        /// The pink is written into each NPC's own settings object, so once it is there the game paints
        /// it whenever it repaints that avatar - there is nothing to keep writing. This is the list that
        /// says which ones have been done, and it is keyed by the native pointer rather than by the
        /// wrapper object, because a fresh wrapper is handed out for the same NPC on every scan and a
        /// dictionary of those would grow without ever matching. Re-applying the same four values to
        /// every NPC in the world every two seconds was the mod's own biggest recurring cost by far.
        /// </summary>
        private static readonly HashSet<IntPtr> Painted = new HashSet<IntPtr>();

        private static bool _active;
        private static float _scanTimer;

        public static void Start()
        {
            if (_active)
                Stop();

            _active = true;
            _scanTimer = 0f;

            ScanAndApply();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                $"[MDMA NPC Eyes] Started. NPCs affected: {Originals.Count}"
            );
        }

        public static void Update()
        {
            if (!_active) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;

            _scanTimer = 3f;
            ScanAndApply();
        }

        public static void Stop()
        {
            foreach (KeyValuePair<object, SavedEyes> kvp in Originals)
            {
                try
                {
                    object avatar = kvp.Key;
                    SavedEyes data = kvp.Value;

                    object settings = GetMember(avatar, "CurrentSettings");
                    if (settings == null) continue;

                    SetFloat(settings, "PupilDilation", data.PupilDilation);
                    SetColor(settings, "EyeBallTint", data.EyeBallTint);
                    SetColor(settings, "LeftEyeLidColor", data.LeftEyeLidColor);
                    SetColor(settings, "RightEyeLidColor", data.RightEyeLidColor);

                    ApplyEyeSettings(avatar, settings);
                }
                catch { }
            }

            Originals.Clear();
            Painted.Clear();
            _active = false;
            _scanTimer = 0f;

            global::CustomNPCExample.Utils.WvcLog.Msg("[MDMA NPC Eyes] NPC eyes reverted.");
        }

        private static void ScanAndApply()
        {
            try
            {
                GameAvatar[] avatars =
                    UnityEngine.Object.FindObjectsOfType<GameAvatar>();

                if (avatars == null) return;

                object playerAvatar = MDMAEyeEffect.CachedPlayerAvatar;

                foreach (GameAvatar avatar in avatars)
                {
                    if (avatar == null) continue;
                    if (playerAvatar != null && avatar.Equals(playerAvatar)) continue;

                    IntPtr key = avatar.Pointer;

                    if (key == IntPtr.Zero) continue;

                    // Already painted, and its own settings still carry the paint: nothing here changes
                    // on its own, so there is nothing to write.
                    if (Painted.Contains(key)) continue;

                    object avatarObject = avatar;
                    object settings = GetMember(avatarObject, "CurrentSettings");
                    if (settings == null) continue;

                    if (!Originals.ContainsKey(avatarObject))
                    {
                        Originals[avatarObject] = new SavedEyes
                        {
                            PupilDilation = GetFloat(settings, "PupilDilation", 0.5f),
                            EyeBallTint = GetColor(settings, "EyeBallTint", Color.white),
                            LeftEyeLidColor = GetColor(settings, "LeftEyeLidColor", Color.white),
                            RightEyeLidColor = GetColor(settings, "RightEyeLidColor", Color.white)
                        };
                    }

                    SetFloat(settings, "PupilDilation", 0.98f);
                    SetColor(settings, "EyeBallTint", new Color(1.0f, 0.12f, 0.60f, 1f));

                    Color lidPink = new Color(0.95f, 0.34f, 0.60f, 1f);
                    SetColor(settings, "LeftEyeLidColor", lidPink);
                    SetColor(settings, "RightEyeLidColor", lidPink);

                    ApplyEyeSettings(avatarObject, settings);

                    // Only now, so an avatar the game would not take the settings for is tried again on
                    // the next scan rather than being written off.
                    Painted.Add(key);
                }
            }
            catch (Exception)
            {

            }
        }

        private static void ApplyEyeSettings(object avatar, object settings)
        {
            CallMethod(avatar, "ApplyEyeBallSettings", settings);
            CallMethod(avatar, "ApplyEyeLidColorSettings", settings);
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null) return null;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Type type = obj.GetType();

            try
            {
                PropertyInfo prop = type.GetProperty(name, flags);
                if (prop != null) return prop.GetValue(obj);
            }
            catch { }

            try
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) return field.GetValue(obj);
            }
            catch { }

            return null;
        }

        private static float GetFloat(object obj, string name, float fallback)
        {
            object value = GetMember(obj, name);
            return value is float result ? result : fallback;
        }

        private static Color GetColor(object obj, string name, Color fallback)
        {
            object value = GetMember(obj, name);
            return value is Color result ? result : fallback;
        }

        private static void SetFloat(object obj, string name, float value)
        {
            if (obj == null) return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(name, flags);
                if (prop != null && prop.CanWrite) { prop.SetValue(obj, value); return; }
            }
            catch { }

            try
            {
                FieldInfo field = obj.GetType().GetField(name, flags);
                if (field != null) field.SetValue(obj, value);
            }
            catch { }
        }

        private static void SetColor(object obj, string name, Color value)
        {
            if (obj == null) return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(name, flags);
                if (prop != null && prop.CanWrite) { prop.SetValue(obj, value); return; }
            }
            catch { }

            try
            {
                FieldInfo field = obj.GetType().GetField(name, flags);
                if (field != null) field.SetValue(obj, value);
            }
            catch { }
        }

        private static void CallMethod(object obj, string methodName, object arg)
        {
            if (obj == null) return;

            MethodInfo method = obj.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null) return;

            try { method.Invoke(obj, new object[] { arg }); }
            catch { }
        }
    }
}