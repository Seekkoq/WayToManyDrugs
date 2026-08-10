using MelonLoader;

namespace CustomNPCExample.Products
{
    public static class PlayerCapturePatch
    {
        // Runs when Player.Awake() is called
        public static void Postfix(object __instance)
        {
            MDMAEyeEffect.SetPlayer(__instance);
        }
    }
}