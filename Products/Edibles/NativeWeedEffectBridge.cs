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


                DumpAvailableEffects();
                return;
            }

            _active = true;

            global::CustomNPCExample.Utils.WvcLog.Msg(
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
                catch (Exception)
                {

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
                catch (Exception)
                {

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
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC WeedFX] Native weed effect expired.");
                Stop();
            }
        }

        public static void DumpAvailableEffects()
        {
            try
            {
                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC WeedFX] ===== EffectHandler dump =====");

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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC WeedFX] Handler: id='" + id +
                        "' type='" + type +
                        "' path='" + path + "'"
                    );
                }

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC WeedFX] ===== EffectController dump =====");

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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC WeedFX] Controller: active='" + active +
                        "' type='" + type +
                        "' path='" + path + "'"
                    );
                }
            }
            catch (Exception)
            {

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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC WeedFX] Activated EffectHandler: id='" +
                        id + "' type='" + type + "' path='" + path + "'"
                    );
                }
                catch (Exception)
                {

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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC WeedFX] Activated EffectController: type='" +
                        type + "' path='" + path + "'"
                    );
                }
                catch (Exception)
                {

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
