using System;
using MelonLoader;

using NativeLog = MelonLoader.MelonLogger;

namespace CustomNPCExample.Utils
{
    public static class WvcLog
    {
        private static MelonPreferences_Entry<bool> _verboseLogging;

        public static bool VerboseEnabled =>
            _verboseLogging != null && _verboseLogging.Value;

        public static void Initialize()
        {
            if (_verboseLogging != null)
                return;

            var category = MelonPreferences.CreateCategory(
                "WVC_Logging",
                "Westville Connection Logging"
            );

            _verboseLogging = category.CreateEntry<bool>(
                "VerboseLogging",
                false,
                "Enable detailed diagnostic messages"
            );
        }

        public static void Msg(string message)
        {
            if (VerboseEnabled)
                NativeLog.Msg(message);
        }

        public static void Msg(string format, params object[] args)
        {
            if (VerboseEnabled)
                NativeLog.Msg(string.Format(format, args));
        }

        public static void Msg(object message)
        {
            if (VerboseEnabled)
                NativeLog.Msg(message == null ? "(null)" : message.ToString());
        }

        public static void Always(string message)
        {
            NativeLog.Msg(message);
        }
    }
}
