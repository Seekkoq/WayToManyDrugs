using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

using NativeItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

using NativeStorableDefinition =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products.Ovens
{
    public static class LabOvenFinishedOutput
    {
        private static readonly Dictionary<int, string> PendingOutputs =
            new Dictionary<int, string>();

        public static void MarkPending(
            LabOven oven,
            string outputItemId)
        {
            if (oven == null ||
                string.IsNullOrWhiteSpace(outputItemId))
            {
                return;
            }

            PendingOutputs[oven.GetInstanceID()] =
                outputItemId;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Oven] Pending baked output: " +
                outputItemId
            );
        }

        public static bool IsPending(
            LabOven oven)
        {
            return oven != null &&
                   PendingOutputs.ContainsKey(
                       oven.GetInstanceID()
                   );
        }

        public static bool TryGetPendingOutput(
            LabOven oven,
            out string outputItemId)
        {
            outputItemId = null;

            return oven != null &&
                   PendingOutputs.TryGetValue(
                       oven.GetInstanceID(),
                       out outputItemId
                   );
        }

        public static void ClearPending(
            LabOven oven)
        {
            if (oven == null)
                return;

            PendingOutputs.Remove(oven.GetInstanceID());
        }

        public static bool TryCreateOutputInstance(
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


                    return false;
                }

                object raw =
                    GetMemberValue(wrapper, "S1ItemDefinition");

                NativeStorableDefinition definition =
                    raw as NativeStorableDefinition;

                if (definition == null)
                {


                    return false;
                }

                instance =
                    definition.GetDefaultInstance(quantity);

                return instance != null;
            }
            catch (Exception)
            {


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
                    PropertyInfo property = type.GetProperty(
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
                        catch
                        {
                        }
                    }

                    FieldInfo field = type.GetField(
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
                        catch
                        {
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}