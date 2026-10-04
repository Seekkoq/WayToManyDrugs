using MelonLoader;

namespace CustomNPCExample.Products
{
    public static class PlayerCapturePatch
    {
        public static void Postfix(object __instance)
        {
            MDMAEyeEffect.SetPlayer(__instance);
        }
    }
}
