using System;

namespace CustomNPCExample.Products.Edibles
{
    /// <summary>
    /// Legacy patch holder - replaced by unified CauldronPatch &amp; CookieCauldronRecipe.
    /// </summary>
    [Obsolete("Cookie recipe handling is now unified in CauldronPatch and CookieCauldronRecipe.")]
    public static class CookieCauldronPatch
    {
        public static void ApplyPatch()
        {
            // No-op: unified CauldronPatch handles all cauldron recipes.
        }
    }
}
