using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

using NativeItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

using NativeStorableDefinition =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products.Cauldrons
{
    /// <summary>
    /// Tracks WVC cauldron recipes and replaces the native Raw Cocaine
    /// Base output after native cooking and network updates finish.
    /// </summary>
    public static class CauldronFinishedOutput
    {
        private static readonly Dictionary<int, string> PendingOutputs =
            new Dictionary<int, string>();

        private static readonly Dictionary<int, EQuality> PendingQualities =
            new Dictionary<int, EQuality>();

        private static readonly HashSet<int> ScheduledReplacements =
            new HashSet<int>();

        public static void MarkPending(
            Cauldron cauldron,
            string outputItemId)
        {
            MarkPending(
                cauldron,
                outputItemId,
                EQuality.Standard
            );
        }

        public static void MarkPending(
            Cauldron cauldron,
            string outputItemId,
            EQuality quality)
        {
            if (cauldron == null ||
                string.IsNullOrWhiteSpace(outputItemId))
            {
                return;
            }

            int cauldronId =
                cauldron.GetInstanceID();

            PendingOutputs[cauldronId] = outputItemId;
            PendingQualities[cauldronId] = quality;

            MelonLogger.Msg(
                "[WVC Cauldron] Pending finished output: " +
                outputItemId +
                " quality=" +
                quality
            );
        }

        public static bool IsPending(
            Cauldron cauldron)
        {
            return cauldron != null &&
                   PendingOutputs.ContainsKey(
                       cauldron.GetInstanceID()
                   );
        }

        public static bool TryGetPendingOutput(
            Cauldron cauldron,
            out string outputItemId)
        {
            outputItemId = null;

            if (cauldron == null)
                return false;

            return PendingOutputs.TryGetValue(
                cauldron.GetInstanceID(),
                out outputItemId
            );
        }

        public static bool TryGetPendingQuality(
            Cauldron cauldron,
            out EQuality quality)
        {
            quality = EQuality.Standard;

            if (cauldron == null)
                return false;

            return PendingQualities.TryGetValue(
                cauldron.GetInstanceID(),
                out quality
            );
        }

        public static void ClearPending(
            Cauldron cauldron)
        {
            if (cauldron == null)
                return;

            int id = cauldron.GetInstanceID();

            PendingOutputs.Remove(id);
            PendingQualities.Remove(id);
            ScheduledReplacements.Remove(id);
        }

        public static bool TryCreateOutputInstance(
            string itemId,
            int quantity,
            out NativeItemInstance instance)
        {
            return TryCreateOutputInstance(
                itemId,
                quantity,
                EQuality.Standard,
                out instance
            );
        }

        public static bool TryCreateOutputInstance(
            string itemId,
            int quantity,
            EQuality quality,
            out NativeItemInstance instance)
        {
            if (!TryCreateInstance(
                    itemId,
                    quantity,
                    out instance))
            {
                return false;
            }

            try
            {
                QualityItemInstance qualityInstance =
                    instance.TryCast<QualityItemInstance>();

                if (qualityInstance != null)
                {
                    qualityInstance.SetQuality(quality);

                    MelonLogger.Msg(
                        "[WVC Cauldron] Output quality stamped: " +
                        quality
                    );
                }
                else
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Output is not a QualityItemInstance; " +
                        "quality not applied."
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] SetQuality failed: " +
                    ex.Message
                );
            }

            return instance != null;
        }

        public static void ScheduleReplacement(
            Cauldron cauldron)
        {
            if (cauldron == null)
                return;

            int cauldronId =
                cauldron.GetInstanceID();

            if (!PendingOutputs.ContainsKey(cauldronId))
                return;

            if (!ScheduledReplacements.Add(cauldronId))
                return;

            MelonLogger.Msg(
                "[WVC Cauldron] Scheduled delayed output replacement."
            );

            MelonCoroutines.Start(
                ReplaceAfterNativeFinish(
                    cauldron,
                    cauldronId
                )
            );
        }

        private static IEnumerator ReplaceAfterNativeFinish(
            Cauldron cauldron,
            int cauldronId)
        {
            MelonLogger.Msg(
                "[WVC Cauldron] Replacement coroutine started for " +
                cauldronId
            );

            /*
             * Native FinishCookOperation and its observer/RPC paths
             * may write Raw Cocaine Base after the direct finish call.
             * Wait before replacing so we run after those writes.
             */
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.15f);

            if (!PendingOutputs.TryGetValue(
                    cauldronId,
                    out string outputItemId))
            {
                ScheduledReplacements.Remove(cauldronId);
                PendingQualities.Remove(cauldronId);
                yield break;
            }

            EQuality pendingQuality = EQuality.Standard;
            PendingQualities.TryGetValue(
                cauldronId,
                out pendingQuality
            );

            int batchSize =
                CauldronRecipeAmounts.GetOutputBatchSize(
                    outputItemId
                );

            NativeItemInstance customOutput;

            if (!TryCreateOutputInstance(
                    outputItemId,
                    batchSize,
                    pendingQuality,
                    out customOutput))
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Could not create output: " +
                    outputItemId
                );

                PendingOutputs.Remove(cauldronId);
                PendingQualities.Remove(cauldronId);
                ScheduledReplacements.Remove(cauldronId);
                yield break;
            }

            bool replaced = false;

            /*
             * Retry for roughly 1.5 seconds. Depending on timing,
             * native output can arrive after the first finish callback.
             */
            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (TryReplaceOutput(
                        cauldron,
                        customOutput,
                        outputItemId))
                {
                    replaced = true;
                    break;
                }

                yield return new WaitForSeconds(0.05f);
            }

            PendingOutputs.Remove(cauldronId);
            PendingQualities.Remove(cauldronId);
            ScheduledReplacements.Remove(cauldronId);

            if (replaced)
            {
                MelonLogger.Msg(
                    "[WVC Cauldron] Replaced native output with: " +
                    outputItemId +
                    " x" +
                    batchSize +
                    " quality=" +
                    pendingQuality
                );
            }
            else
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Delayed output replacement failed: " +
                    outputItemId
                );
            }
        }

        private static bool TryReplaceOutput(
            Cauldron cauldron,
            NativeItemInstance customOutput,
            string expectedItemId)
        {
            try
            {
                if (cauldron == null ||
                    customOutput == null ||
                    cauldron.OutputSlot == null)
                {
                    return false;
                }

                ItemSlot outputSlot =
                    cauldron.OutputSlot;

                /*
                 * Wait until native cooking has actually written output.
                 */
                if (outputSlot.ItemInstance == null)
                    return false;

                string beforeId =
                    outputSlot.ItemInstance.Definition?.ID
                    ?? "null";

                if (string.Equals(
                        beforeId,
                        expectedItemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                MelonLogger.Msg(
                    "[WVC Cauldron] Native output before replacement: " +
                    beforeId
                );

                /*
                 * _internal=true bypasses normal slot locking/filtering.
                 */
                outputSlot.SetStoredItem(
                    customOutput,
                    true
                );

                try
                {
                    outputSlot.ReplicateStoredInstance();
                }
                catch
                {
                    // Local replacement can still succeed.
                }

                string placedId =
                    outputSlot.ItemInstance?.Definition?.ID;

                MelonLogger.Msg(
                    "[WVC Cauldron] Output after replacement attempt: " +
                    (placedId ?? "null")
                );

                return string.Equals(
                    placedId,
                    expectedItemId,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Output replace attempt failed: " +
                    ex.Message
                );

                return false;
            }
        }

        private static bool TryCreateInstance(
            string itemId,
            int quantity,
            out NativeItemInstance instance)
        {
            instance = null;

            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(itemId);

                if (wrapper == null)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Definition missing: " +
                        itemId
                    );

                    return false;
                }

                object raw =
                    GetMemberValue(
                        wrapper,
                        "S1ItemDefinition"
                    );

                NativeStorableDefinition definition =
                    raw as NativeStorableDefinition;

                if (definition == null)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Output is not storable: " +
                        itemId
                    );

                    return false;
                }

                instance =
                    definition.GetDefaultInstance(quantity);

                return instance != null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Instance creation failed: " +
                    ex.Message
                );

                return false;
            }
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null)
                    {
                        try
                        {
                            return property.GetValue(target);
                        }
                        catch { }
                    }

                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                    {
                        try
                        {
                            return field.GetValue(target);
                        }
                        catch { }
                    }

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }
    }
}