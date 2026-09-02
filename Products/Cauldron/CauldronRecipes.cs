using System;
using System.Collections.Generic;
using System.Reflection;
using CustomNPCExample.Products.Edibles;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using Il2CppInterop.Runtime.InteropTypes;
using CustomNPCExample.Products;

namespace CustomNPCExample.Products.Cauldrons
{
    /// <summary>
    /// Shared cauldron data: ingredient IDs, slot allow-list,
    /// and the output-definition swap used by every recipe.
    /// </summary>
    public static class CauldronRecipes
    {
        public const string SafroleId =
            "westvilleconnection:ingredients/safrole_oil";

        public const string PmkId =
            "westvilleconnection:ingredients/pmk_powder";

        public const string ThcOilId =
            "westvilleconnection:ingredients/thc_oil";

        public const string GelatinId =
            "westvilleconnection:ingredients/gelatin";

        public const string SugarId =
            "westvilleconnection:ingredients/sugar";

        public static string MdmaId => MDMA.ProductId;

        public static string GummyMixId => UnbakedGummyMix.ItemId;

        /// <summary>
        /// Items allowed into cauldron slots by our filter override.
        /// </summary>
        public static string CrudeDmtExtractId =>
    DMTIntermediates.CrudeExtractId;

        public static readonly HashSet<string> AllowedIngredients =
    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SafroleId,
        PmkId,
        MollyIngredients.PmkRefinedId,
        MollyIngredients.PmkLabGradeId,
        ThcOilId,
        GelatinId,
        SugarId,
        CookieIngredients.ButterscotchChipsId,
        CookieIngredients.CannabisFlourId,

        // DMT extraction
        DMTIngredients.DreamrootId,
        DMTIngredients.CausticBaseId,
        DMTIngredients.LabSolventId,
        DMTIngredients.CrystalizerId,
    };

        private static readonly Dictionary<int, QualityItemDefinition>
            SwappedDefinitions =
                new Dictionary<int, QualityItemDefinition>();

        public static bool IsAllowedIngredient(ItemInstance item)
        {
            if (item == null)
                return false;

            try
            {
                string id = item.Definition?.ID;

                if (string.IsNullOrEmpty(id))
                    return false;

                return AllowedIngredients.Contains(id);
            }
            catch
            {
                return false;
            }
        }

        public static string GetItemId(ItemSlot slot)
        {
            try
            {
                return slot?.ItemInstance?.Definition?.ID;
            }
            catch
            {
                return null;
            }
        }

        public static bool IdEquals(string a, string b)
        {
            return string.Equals(
                a,
                b,
                StringComparison.OrdinalIgnoreCase
            );
        }

        public static bool HasRequiredGasoline(Cauldron cauldron)
        {
            if (cauldron == null || cauldron.LiquidSlot == null)
                return false;

            ItemInstance item = cauldron.LiquidSlot.ItemInstance;

            if (item == null)
                return false;

            string id = item.Definition?.ID;

            return IdEquals(
                id,
                CauldronRecipeAmounts.GasolineId
            )
            &&
            cauldron.LiquidSlot.Quantity >=
                CauldronRecipeAmounts.GasolineRequired;
        }

        public static bool ConsumeRequiredGasoline(Cauldron cauldron)
        {
            if (!HasRequiredGasoline(cauldron))
                return false;

            try
            {
                ItemSlot liquidSlot = cauldron.LiquidSlot;

                if (liquidSlot == null ||
                    liquidSlot.ItemInstance == null)
                {
                    return false;
                }

                int remaining =
                    Math.Max(
                        0,
                        liquidSlot.Quantity -
                        CauldronRecipeAmounts.GasolineRequired
                    );

                if (remaining <= 0)
                {
                    liquidSlot.SetStoredItem(
                        null,
                        true
                    );
                }
                else
                {
                    liquidSlot.SetStoredItem(
                        liquidSlot.ItemInstance.GetCopy(
                            remaining
                        ),
                        true
                    );
                }

                MelonLogger.Msg(
                    "[WVC Cauldron] Consumed gasoline. Remaining=" +
                    remaining
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Failed to consume gasoline: " +
                    ex.Message
                );

                return false;
            }
        }

        public static void ConsumeSlot(
            Cauldron cauldron,
            int index,
            int amount)
        {
            if (cauldron == null || index < 0 || amount <= 0)
                return;

            try
            {
                var slots = cauldron.ItemSlots;

                if (slots == null ||
                    index >= slots.Count ||
                    slots[index] == null)
                {
                    return;
                }

                int remaining =
                    Math.Max(0, slots[index].Quantity - amount);

                cauldron.SetItemSlotQuantity(index, remaining);
            }
            catch { }
        }

        /// <summary>
        /// Temporarily replaces the cauldron's output definition so it
        /// produces our item instead of cocaine base.
        /// </summary>
        public static bool SwapOutputTo(
    Cauldron cauldron,
    string itemId)
        {
            if (cauldron == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            int id =
                cauldron.GetInstanceID();

            if (SwappedDefinitions.ContainsKey(id))
                return true;

            try
            {
                S1API.Items.ItemDefinition wrapper =
                    S1API.Items.ItemManager.GetDefinition(
                        itemId
                    );

                if (wrapper == null)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Missing definition: " +
                        itemId
                    );

                    return false;
                }

                /*
                 * Some custom products expose S1ItemDefinition,
                 * others expose S1ProductDefinition. Try both.
                 */
                object raw =
                    GetMember(
                        wrapper,
                        "S1ItemDefinition"
                    )
                    ??
                    GetMember(
                        wrapper,
                        "S1ProductDefinition"
                    );

                if (raw == null)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Could not unwrap native definition for " +
                        itemId +
                        ". Wrapper type=" +
                        wrapper.GetType().FullName
                    );

                    return false;
                }

                QualityItemDefinition quality =
                    raw as QualityItemDefinition;

                if (quality == null)
                {
                    Il2CppObjectBase native =
                        raw as Il2CppObjectBase;

                    if (native != null)
                    {
                        try
                        {
                            quality =
                                native.TryCast<
                                    QualityItemDefinition
                                >();
                        }
                        catch
                        {
                        }
                    }
                }

                /*
                 * This is the important fallback. Your old MDMA cauldron
                 * patch did this manually:
                 *
                 * new QualityItemDefinition(rawDefinition.Pointer)
                 *
                 * Some custom product wrappers will not TryCast cleanly,
                 * but their pointer is still a valid QualityItemDefinition.
                 */
                if (quality == null)
                {
                    ItemDefinition itemDefinition =
                        raw as ItemDefinition;

                    if (itemDefinition == null)
                    {
                        Il2CppObjectBase native =
                            raw as Il2CppObjectBase;

                        if (native != null)
                        {
                            try
                            {
                                itemDefinition =
                                    native.TryCast<
                                        ItemDefinition
                                    >();
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (itemDefinition != null &&
                        itemDefinition.Pointer != IntPtr.Zero)
                    {
                        try
                        {
                            quality =
                                new QualityItemDefinition(
                                    itemDefinition.Pointer
                                );
                        }
                        catch
                        {
                            quality = null;
                        }
                    }
                }

                if (quality == null)
                {
                    MelonLogger.Warning(
                        "[WVC Cauldron] Output is not a QualityItemDefinition: " +
                        itemId +
                        ", native type=" +
                        raw.GetType().FullName
                    );

                    return false;
                }

                SwappedDefinitions[id] =
                    cauldron.CocaineBaseDefinition;

                cauldron.CocaineBaseDefinition =
                    quality;

                MelonLogger.Msg(
                    "[WVC Cauldron] Output swapped to " +
                    itemId +
                    " using native type " +
                    raw.GetType().FullName
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[WVC Cauldron] Swap failed for " +
                    itemId +
                    ": " +
                    ex
                );

                return false;
            }
        }

        public static void RestoreOutput(Cauldron cauldron)
        {
            if (cauldron == null)
                return;

            int id = cauldron.GetInstanceID();

            if (!SwappedDefinitions.TryGetValue(
                    id,
                    out QualityItemDefinition original))
            {
                return;
            }

            cauldron.CocaineBaseDefinition = original;
            SwappedDefinitions.Remove(id);

            MelonLogger.Msg(
                "[WVC Cauldron] Output definition restored."
            );
        }



        public static bool ConsumeByItemId(
    Cauldron cauldron,
    string itemId,
    int amount)
        {
            if (cauldron?.ItemSlots == null ||
                string.IsNullOrEmpty(itemId) ||
                amount <= 0)
            {
                return false;
            }

            try
            {
                int available = 0;

                for (int i = 0; i < cauldron.ItemSlots.Count; i++)
                {
                    ItemSlot slot = cauldron.ItemSlots[i];

                    if (slot?.ItemInstance == null)
                        continue;

                    string currentId =
                        slot.ItemInstance.Definition?.ID;

                    if (IdEquals(currentId, itemId))
                        available += slot.Quantity;
                }

                if (available < amount)
                    return false;

                int leftToConsume = amount;

                for (int i = 0;
                     i < cauldron.ItemSlots.Count &&
                     leftToConsume > 0;
                     i++)
                {
                    ItemSlot slot = cauldron.ItemSlots[i];

                    if (slot?.ItemInstance == null)
                        continue;

                    string currentId =
                        slot.ItemInstance.Definition?.ID;

                    if (!IdEquals(currentId, itemId))
                        continue;

                    int take =
                        Math.Min(
                            slot.Quantity,
                            leftToConsume
                        );

                    int remaining =
                        slot.Quantity - take;

                    cauldron.SetItemSlotQuantity(
                        i,
                        Math.Max(0, remaining)
                    );

                    leftToConsume -= take;
                }

                return leftToConsume <= 0;
            }
            catch
            {
                return false;
            }
        }

        public static object GetMember(object target, string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (property != null)
                        return property.GetValue(target);

                    FieldInfo field = type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (field != null)
                        return field.GetValue(target);

                    type = type.BaseType;
                }
            }
            catch { }

            return null;
        }
    }
}