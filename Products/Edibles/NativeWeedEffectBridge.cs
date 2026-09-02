using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

using NativeEffectHandler = Il2CppScheduleOne.Effects.EffectHandler;
using NativeEffectController = Il2CppScheduleOne.Effects.EffectController;

namespace CustomNPCExample.Products.Edibles
{
    public static class NativeWeedEffectBridge
    {
        private static readonly List<NativeEffectHandler> _activeHandlers =
            new List<NativeEffectHandler>();

        private static readonly List<NativeEffectController> _activeControllers =
            new List<NativeEffectController>();

        private static bool _active;
        private static float _timeRemaining;

        /*
         * If the auto matcher does not find the weed effect,
         * press your debug key to dump all effect IDs, then add
         * the exact ID/name here.
         */
        private static readonly string[] ForcedEffectIds =
        {
            "weed",
            "stoned",
            "cannabis",
            "marijuana"
        };

        private static readonly string[] WeedKeywords =
        {
            "weed",
            "stoned",
            "stone",
            "cannabis",
            "marijuana"
        };

        private static readonly string[] ExcludeKeywords =
        {
            "mdma",
            "molly",
            "ecstasy",
            "coke",
            "cocaine",
            "meth",
            "shroom",
            "mushroom",
            "lsd",
            "acid",
            "dead",
            "poison"
        };

        public static void Start(float durationSeconds)
        {
            Stop();

            _timeRemaining = durationSeconds;

            int activated = 0;

            activated += ActivateMatchingEffectHandlers();
            activated += ActivateMatchingEffectControllers();

            if (activated <= 0)
            {
                MelonLogger.Warning(
                    "[WVC WeedFX] No native weed effect handler matched. " +
                    "Dumping available effects so we can identify the real ID."
                );

                DumpAvailableEffects();
                return;
            }

            _active = true;

            MelonLogger.Msg(
                "[WVC WeedFX] Native weed effect started. Handlers/controllers activated: " +
                activated
            );
        }

        public static void Stop()
        {
            for (int i = 0; i < _activeHandlers.Count; i++)
            {
                NativeEffectHandler handler = _activeHandlers[i];

                if (handler == null)
                    continue;

                try
                {
                    handler.Deactivate();
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[WVC WeedFX] Handler deactivate failed: " + ex.Message
                    );
                }
            }

            for (int i = 0; i < _activeControllers.Count; i++)
            {
                NativeEffectController controller = _activeControllers[i];

                if (controller == null)
                    continue;

                try
                {
                    controller.Deactivate();
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[WVC WeedFX] Controller deactivate failed: " + ex.Message
                    );
                }
            }

            _activeHandlers.Clear();
            _activeControllers.Clear();

            _active = false;
            _timeRemaining = 0f;
        }

        public static void Update()
        {
            if (!_active)
                return;

            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                MelonLogger.Msg("[WVC WeedFX] Native weed effect expired.");
                Stop();
            }
        }

        public static void DumpAvailableEffects()
        {
            try
            {
                MelonLogger.Msg("[WVC WeedFX] ===== EffectHandler dump =====");

                NativeEffectHandler[] handlers =
                    Resources.FindObjectsOfTypeAll<NativeEffectHandler>();

                for (int i = 0; i < handlers.Length; i++)
                {
                    NativeEffectHandler h = handlers[i];

                    if (h == null)
                        continue;

                    string id = SafeId(h);
                    string type = h.GetType().FullName;
                    string path = SafePath(h.gameObject);

                    MelonLogger.Msg(
                        "[WVC WeedFX] Handler: id='" + id +
                        "' type='" + type +
                        "' path='" + path + "'"
                    );
                }

                MelonLogger.Msg("[WVC WeedFX] ===== EffectController dump =====");

                NativeEffectController[] controllers =
                    Resources.FindObjectsOfTypeAll<NativeEffectController>();

                for (int i = 0; i < controllers.Length; i++)
                {
                    NativeEffectController c = controllers[i];

                    if (c == null)
                        continue;

                    string type = c.GetType().FullName;
                    string path = SafePath(c.gameObject);

                    bool active = false;

                    try
                    {
                        active = c.IsActive;
                    }
                    catch { }

                    MelonLogger.Msg(
                        "[WVC WeedFX] Controller: active='" + active +
                        "' type='" + type +
                        "' path='" + path + "'"
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC WeedFX] Dump failed: " + ex
                );
            }
        }

        private static int ActivateMatchingEffectHandlers()
        {
            int count = 0;

            NativeEffectHandler[] handlers =
                Resources.FindObjectsOfTypeAll<NativeEffectHandler>();

            for (int i = 0; i < handlers.Length; i++)
            {
                NativeEffectHandler handler = handlers[i];

                if (handler == null)
                    continue;

                string id = SafeId(handler);
                string type = handler.GetType().FullName;
                string path = SafePath(handler.gameObject);

                string combined =
                    (id + " " + type + " " + path).ToLowerInvariant();

                if (!MatchesWeedEffect(combined))
                    continue;

                try
                {
                    try
                    {
                        handler.Initialise();
                    }
                    catch { }

                    handler.Activate();

                    _activeHandlers.Add(handler);
                    count++;

                    MelonLogger.Msg(
                        "[WVC WeedFX] Activated EffectHandler: id='" +
                        id + "' type='" + type + "' path='" + path + "'"
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[WVC WeedFX] EffectHandler activate failed: " +
                        id + " / " + ex.Message
                    );
                }
            }

            return count;
        }

        private static int ActivateMatchingEffectControllers()
        {
            int count = 0;

            NativeEffectController[] controllers =
                Resources.FindObjectsOfTypeAll<NativeEffectController>();

            for (int i = 0; i < controllers.Length; i++)
            {
                NativeEffectController controller = controllers[i];

                if (controller == null)
                    continue;

                string type = controller.GetType().FullName;
                string path = SafePath(controller.gameObject);

                string combined =
                    (type + " " + path).ToLowerInvariant();

                if (!MatchesWeedEffect(combined))
                    continue;

                try
                {
                    controller.Activate();

                    _activeControllers.Add(controller);
                    count++;

                    MelonLogger.Msg(
                        "[WVC WeedFX] Activated EffectController: type='" +
                        type + "' path='" + path + "'"
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[WVC WeedFX] EffectController activate failed: " +
                        type + " / " + ex.Message
                    );
                }
            }

            return count;
        }

        private static bool MatchesWeedEffect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            for (int i = 0; i < ExcludeKeywords.Length; i++)
            {
                if (text.Contains(ExcludeKeywords[i]))
                    return false;
            }

            for (int i = 0; i < ForcedEffectIds.Length; i++)
            {
                if (text.Contains(ForcedEffectIds[i]))
                    return true;
            }

            for (int i = 0; i < WeedKeywords.Length; i++)
            {
                if (text.Contains(WeedKeywords[i]))
                    return true;
            }

            return false;
        }

        private static string SafeId(NativeEffectHandler handler)
        {
            if (handler == null)
                return "";

            try
            {
                string id = handler.Id;

                if (!string.IsNullOrEmpty(id))
                    return id;
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(handler._id))
                    return handler._id;
            }
            catch { }

            return "";
        }

        private static string SafePath(GameObject go)
        {
            if (go == null)
                return "";

            try
            {
                Transform current = go.transform;
                string path = current.name;

                while (current.parent != null)
                {
                    current = current.parent;
                    path = current.name + "/" + path;
                }

                return path;
            }
            catch
            {
                return go.name;
            }
        }
    }
}