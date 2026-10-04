using System;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace CustomNPCExample.Products.Cauldrons
{
    public static class CauldronOutputInterceptor
    {
        public static bool SetStoredItem_Prefix(
            ItemSlot __instance,
            ref ItemInstance __0)
        {
            try
            {
                if (__instance == null || __instance.SlotOwner == null)
                    return true;

                Il2CppObjectBase ownerBase =
                    __instance.SlotOwner as Il2CppObjectBase;

                Cauldron cauldron = ownerBase?.TryCast<Cauldron>();

                if (cauldron == null || cauldron.OutputSlot == null)
                    return true;

                if (cauldron.OutputSlot.Pointer != __instance.Pointer)
                    return true;

                if (!CauldronFinishedOutput.TryGetPendingOutput(
                        cauldron, out string outputItemId))
                {
                    return true;
                }

                string incomingId = __0?.Definition?.ID ?? "null";

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cauldron] Output write detected: " + incomingId
                );

                int batchSize =
                    CauldronRecipeAmounts.GetOutputBatchSize(outputItemId);

                EQuality cookQuality;

                if (!CauldronFinishedOutput.TryGetPendingQuality(
                        cauldron, out cookQuality))
                {
                    cookQuality = EQuality.Standard;

                    var incomingQuality =
                        __0?.TryCast<QualityItemInstance>();

                    if (incomingQuality != null)
                        cookQuality = incomingQuality.Quality;
                }

                if (CauldronRecipes.IdEquals(incomingId, outputItemId))
                {
                    var already = __0?.TryCast<QualityItemInstance>();
                    if (already != null)
                        already.SetQuality(cookQuality);

                    CauldronFinishedOutput.ClearPending(cauldron);
                    return true;
                }

                if (!CauldronFinishedOutput.TryCreateOutputInstance(
                        outputItemId,
                        batchSize,
                        cookQuality,
                        out ItemInstance replacement) ||
                    replacement == null)
                {

                    return true;
                }

                __0 = replacement;

                CauldronFinishedOutput.ClearPending(cauldron);

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Cauldron] Redirecting output to: " +
                    outputItemId +
                    " x" + batchSize +
                    " quality=" + cookQuality
                );

                return true;
            }
            catch (Exception)
            {

                return true;
            }
        }
    }
}
