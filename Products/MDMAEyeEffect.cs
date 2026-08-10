using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class MDMAEyeEffect
    {
        private static object _player;
        private static object _avatar;
        private static object _settings;

        // Exposed so MDMANpcLoveEyes can skip the player's own avatar.
        public static object CachedPlayerAvatar => _avatar;

        private static bool _active;
        private static bool _savedOriginals;
        private static float _pulseTimer;

        private static float _originalPupilDilation;
        private static Color _originalEyeBallTint;
        private static Color _originalLeftEyelidColor;
        private static Color _originalRightEyelidColor;
        private static float _originalEyebrowHeight;
        private static float _originalEyebrowAngle;

        public static void SetPlayer(object player)
        {
            if (player == null) return;
            _player = player;
            MelonLogger.Msg("[MDMA Eyes] Player reference captured.");
        }

        public static void StartEyeEffect()
        {
            try
            {
                if (!TryGetAvatarAndSettings())
                {
                    MelonLogger.Warning("[MDMA Eyes] Player avatar/settings not ready.");
                    return;
                }

                if (!_savedOriginals)
                {
                    _originalPupilDilation = GetFloat(_settings, "PupilDilation", 0.5f);
                    _originalEyeBallTint = GetColor(_settings, "EyeBallTint", Color.white);
                    _originalLeftEyelidColor = GetColor(_settings, "LeftEyeLidColor", Color.white);
                    _originalRightEyelidColor = GetColor(_settings, "RightEyeLidColor", Color.white);
                    _originalEyebrowHeight = GetFloat(_settings, "EyebrowRestingHeight", 0f);
                    _originalEyebrowAngle = GetFloat(_settings, "EyebrowRestingAngle", 0f);
                    _savedOriginals = true;
                }

                _pulseTimer = 0f;
                _active = true;

                ApplyEyeLook(1f);

                MelonLogger.Msg("[MDMA Eyes] Pink dilated eyes applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[MDMA Eyes] Start failed: " + ex.Message);
            }
        }

        public static void UpdateEyeEffect()
        {
            if (!_active) return;

            try
            {
                if (!TryGetAvatarAndSettings()) return;

                _pulseTimer += Time.deltaTime;

                float pulse = 0.5f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f * 0.65f) * 0.5f;

                ApplyEyeLook(pulse);
            }
            catch { }
        }

        public static void StopEyeEffect()
        {
            if (!_active) return;

            try
            {
                if (TryGetAvatarAndSettings() && _savedOriginals)
                {
                    SetFloat(_settings, "PupilDilation", _originalPupilDilation);
                    SetColor(_settings, "EyeBallTint", _originalEyeBallTint);
                    SetColor(_settings, "LeftEyeLidColor", _originalLeftEyelidColor);
                    SetColor(_settings, "RightEyeLidColor", _originalRightEyelidColor);
                    SetFloat(_settings, "EyebrowRestingHeight", _originalEyebrowHeight);
                    SetFloat(_settings, "EyebrowRestingAngle", _originalEyebrowAngle);
                    ApplyAllEyeSettings();
                }

                _active = false;
                _savedOriginals = false;
                _pulseTimer = 0f;

                MelonLogger.Msg("[MDMA Eyes] Original eyes restored.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[MDMA Eyes] Stop failed: " + ex.Message);
            }
        }

        private static void ApplyEyeLook(float pulse)
        {
            float pupilSize = Mathf.Lerp(0.90f, 0.995f, pulse);

            Color eyePink = Color.Lerp(
                new Color(1.00f, 0.62f, 0.78f, 1f),
                new Color(1.00f, 0.12f, 0.55f, 1f),
                pulse
            );

            Color lidPink = Color.Lerp(
                new Color(0.85f, 0.30f, 0.48f, 1f),
                new Color(1.00f, 0.12f, 0.45f, 1f),
                pulse
            );

            SetFloat(_settings, "PupilDilation", pupilSize);
            SetColor(_settings, "EyeBallTint", eyePink);
            SetColor(_settings, "LeftEyeLidColor", lidPink);
            SetColor(_settings, "RightEyeLidColor", lidPink);

            SetFloat(_settings, "EyebrowRestingHeight", _originalEyebrowHeight + 0.07f + pulse * 0.03f);
            SetFloat(_settings, "EyebrowRestingAngle", _originalEyebrowAngle + 3f);

            ApplyAllEyeSettings();
        }

        private static bool TryGetAvatarAndSettings()
        {
            if (_player == null) return false;
            _avatar = GetMember(_player, "Avatar");
            if (_avatar == null) return false;
            _settings = GetMember(_avatar, "CurrentSettings");
            return _settings != null;
        }

        private static void ApplyAllEyeSettings()
        {
            CallMethod(_avatar, "ApplyEyeBallSettings", _settings);
            CallMethod(_avatar, "ApplyEyeLidSettings", _settings);
            CallMethod(_avatar, "ApplyEyeLidColorSettings", _settings);
            CallMethod(_avatar, "ApplyEyebrowSettings", _settings);
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = obj.GetType();
            try { PropertyInfo p = type.GetProperty(name, flags); if (p != null) return p.GetValue(obj); } catch { }
            try { FieldInfo f = type.GetField(name, flags); if (f != null) return f.GetValue(obj); } catch { }
            return null;
        }

        private static float GetFloat(object obj, string name, float fallback)
        {
            object val = GetMember(obj, name);
            return val is float f ? f : fallback;
        }

        private static Color GetColor(object obj, string name, Color fallback)
        {
            object val = GetMember(obj, name);
            return val is Color c ? c : fallback;
        }

        private static void SetFloat(object obj, string name, float value)
        {
            if (obj == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try { PropertyInfo p = obj.GetType().GetProperty(name, flags); if (p != null && p.CanWrite) { p.SetValue(obj, value); return; } } catch { }
            try { FieldInfo f = obj.GetType().GetField(name, flags); if (f != null) f.SetValue(obj, value); } catch { }
        }

        private static void SetColor(object obj, string name, Color value)
        {
            if (obj == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try { PropertyInfo p = obj.GetType().GetProperty(name, flags); if (p != null && p.CanWrite) { p.SetValue(obj, value); return; } } catch { }
            try { FieldInfo f = obj.GetType().GetField(name, flags); if (f != null) f.SetValue(obj, value); } catch { }
        }

        private static void CallMethod(object obj, string methodName, object arg)
        {
            if (obj == null) return;
            MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) try { method.Invoke(obj, new object[] { arg }); } catch { }
        }
    }
}