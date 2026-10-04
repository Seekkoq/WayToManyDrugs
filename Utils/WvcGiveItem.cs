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

                return false;
            }

            string cmd = "give " + consoleItemName + " " + amount;

            try
            {
                _submitMethod.Invoke(null, new object[] { cmd });
                global::CustomNPCExample.Utils.WvcLog.Msg("[WvcGive] Executed: " + cmd);
                return true;
            }
            catch (Exception)
            {

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

                    _submitMethod = consoleType.GetMethod(
                        "SubmitCommand",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(string) },
                        null
                    );

                    if (_submitMethod != null)
                    {
                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[WvcGive] Resolved: SubmitCommand(string)"
                        );
                    }
                    else
                    {

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
