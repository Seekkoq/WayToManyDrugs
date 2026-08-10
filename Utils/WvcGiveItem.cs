using System;
using System.Reflection;
using MelonLoader;

namespace CustomNPCExample.Utils
{
    public static class WvcGiveItem
    {
        private static MethodInfo _submitMethod;
        private static bool _initialized;

        public static bool TryGive(string consoleItemName, int amount)
        {
            if (!_initialized)
                Initialize();

            if (_submitMethod == null)
            {
                MelonLogger.Warning("[WvcGive] SubmitCommand not resolved.");
                return false;
            }

            string cmd = "give " + consoleItemName + " " + amount;

            try
            {
                _submitMethod.Invoke(null, new object[] { cmd });
                MelonLogger.Msg("[WvcGive] Executed: " + cmd);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[WvcGive] Invoke failed: " + ex.Message);
                return false;
            }
        }

        private static void Initialize()
        {
            _initialized = true;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type consoleType = asm.GetType("Il2CppScheduleOne.Console");

                    if (consoleType == null)
                        continue;

                    // Use the overload that takes a single System.String
                    _submitMethod = consoleType.GetMethod(
                        "SubmitCommand",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(string) },
                        null
                    );

                    if (_submitMethod != null)
                    {
                        MelonLogger.Msg(
                            "[WvcGive] Resolved: SubmitCommand(string)"
                        );
                    }
                    else
                    {
                        MelonLogger.Warning(
                            "[WvcGive] SubmitCommand(string) not found on " +
                            consoleType.FullName
                        );
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WvcGive] Init failed: " + ex.Message);
            }
        }
    }
}