using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Quests
{
    public static class SnitchPhoneDelivery
    {
        private sealed class PendingMessage
        {
            public string Id;
            public string Sender;
            public string Text;
        }

        private static readonly MelonPreferences_Category Category =
            MelonPreferences.CreateCategory(
                "WVC_SnitchPhone",
                "Westville Connection - Snitch Phone"
            );

        private static readonly MelonPreferences_Entry<string> PendingPref =
            Category.CreateEntry("Pending", "");

        private static readonly MelonPreferences_Entry<string> DeliveredPref =
            Category.CreateEntry("Delivered", "");

        private static readonly List<PendingMessage> Pending =
            new List<PendingMessage>();

        private static readonly HashSet<string> Delivered =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool _loaded;
        private static float _retryTimer;
        private static float _warningTimer;

        public static bool Queue(
            string id,
            string sender,
            string text)
        {
            if (string.IsNullOrWhiteSpace(sender) ||
                string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(id))
                id = CreateStableId(sender, text);

            if (Delivered.Contains(id))
                return true;

            for (int i = 0; i < Pending.Count; i++)
            {
                if (string.Equals(
                        Pending[i].Id,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var pending = new PendingMessage
            {
                Id = id,
                Sender = sender,
                Text = text
            };

            Pending.Add(pending);
            Save();

            MelonLogger.Msg(
                "[WVC Snitch] Queued phone message from " +
                sender + "."
            );

            TryDeliver(pending);
            return true;
        }

        public static void Update()
        {
            EnsureLoaded();

            if (Pending.Count == 0)
                return;

            _retryTimer += Time.deltaTime;
            _warningTimer += Time.deltaTime;

            if (_retryTimer < 2f)
                return;

            _retryTimer = 0f;

            if (TryDeliver(Pending[0]))
            {
                _warningTimer = 0f;
                return;
            }

            if (_warningTimer >= 30f)
            {
                _warningTimer = 0f;

                MelonLogger.Warning(
                    "[WVC Snitch] Damon phone message is still pending. " +
                    "Damon or his MSGConversation is not ready."
                );
            }
        }

        public static void Reset()
        {
            EnsureLoaded();

            Pending.Clear();
            Delivered.Clear();

            _retryTimer = 0f;
            _warningTimer = 0f;

            Save();
        }

        private static bool TryDeliver(PendingMessage pending)
        {
            if (pending == null)
                return false;

            object senderObject =
                SnitchPhoneEndpoint.ResolveSender(pending.Sender);

            if (senderObject == null)
                return false;

            if (!SnitchPhoneEndpoint.TrySend(
                    senderObject,
                    pending.Sender,
                    pending.Text))
            {
                return false;
            }

            Delivered.Add(pending.Id);
            Pending.Remove(pending);
            Save();

            MelonLogger.Msg(
                "[WVC Snitch] Phone message delivered from " +
                pending.Sender + "."
            );

            return true;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;
            Pending.Clear();
            Delivered.Clear();

            if (!string.IsNullOrEmpty(DeliveredPref.Value))
            {
                string[] values =
                    DeliveredPref.Value.Split('|');

                for (int i = 0; i < values.Length; i++)
                {
                    try
                    {
                        string id = Decode(values[i]);

                        if (!string.IsNullOrEmpty(id))
                            Delivered.Add(id);
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(PendingPref.Value))
                return;

            string[] records =
                PendingPref.Value.Split(';');

            for (int i = 0; i < records.Length; i++)
            {
                try
                {
                    string[] fields = records[i].Split(',');

                    if (fields.Length != 3)
                        continue;

                    string id = Decode(fields[0]);
                    string sender = Decode(fields[1]);
                    string text = Decode(fields[2]);

                    if (string.IsNullOrEmpty(id) ||
                        string.IsNullOrEmpty(sender) ||
                        string.IsNullOrEmpty(text) ||
                        Delivered.Contains(id))
                    {
                        continue;
                    }

                    Pending.Add(new PendingMessage
                    {
                        Id = id,
                        Sender = sender,
                        Text = text
                    });
                }
                catch { }
            }
        }

        private static void Save()
        {
            var pendingRecords = new List<string>();

            for (int i = 0; i < Pending.Count; i++)
            {
                pendingRecords.Add(
                    Encode(Pending[i].Id) + "," +
                    Encode(Pending[i].Sender) + "," +
                    Encode(Pending[i].Text)
                );
            }

            var deliveredRecords = new List<string>();

            foreach (string id in Delivered)
                deliveredRecords.Add(Encode(id));

            PendingPref.Value =
                string.Join(";", pendingRecords);

            DeliveredPref.Value =
                string.Join("|", deliveredRecords);

            Category.SaveToFile();
        }

        private static string CreateStableId(
            string sender,
            string text)
        {
            string input = sender + "\n" + text;
            uint hash = 2166136261;

            for (int i = 0; i < input.Length; i++)
            {
                hash ^= input[i];
                hash *= 16777619;
            }

            return "wvc_" + hash.ToString("x8");
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value ?? "")
            );
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(value ?? "")
            );
        }
    }

    internal static class SnitchPhoneEndpoint
    {
        private sealed class SearchNode
        {
            public object Value;
            public int Depth;
        }

        private sealed class ReferenceComparer :
            IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj == null
                    ? 0
                    : RuntimeHelpers.GetHashCode(obj);
            }
        }

        private static readonly string[] ImportantMembers =
        {
            "NPC",
            "NativeNPC",
            "NPCObject",
            "S1NPC",
            "Native",
            "Entity",
            "BaseNPC",
            "NPCBehaviour",
            "NPCBehavior",
            "MSGConversation",
            "MessageConversation",
            "Conversation",
            "Messaging",
            "Contact"
        };

        private static readonly string[] SendMethodNames =
        {
            "SendTextMessage",
            "ReceiveTextMessage",
            "SendMessageChain",
            "ReceiveMessage",
            "AddMessage",
            "SendMessage"
        };

        public static object ResolveSender(string sender)
        {
            string normalizedSender = Normalize(sender);

            if (string.IsNullOrEmpty(normalizedSender))
                return null;

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;

                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                    continue;

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];

                    if (type == null)
                        continue;

                    string ns = type.Namespace ?? "";

                    if (!ns.StartsWith(
                            "CustomNPCExample.NPCs",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.Equals(
                            Normalize(type.Name),
                            normalizedSender,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    object instance =
                        GetStaticMember(type, "Instance") ??
                        GetStaticMember(type, "instance") ??
                        GetStaticMember(type, "Singleton");

                    if (instance != null)
                        return instance;
                }
            }

            return null;
        }

        public static bool TrySend(
            object senderRoot,
            string sender,
            string text)
        {
            if (senderRoot == null)
                return false;

            var queue = new Queue<SearchNode>();
            var visited = new HashSet<object>(
                new ReferenceComparer()
            );

            queue.Enqueue(new SearchNode
            {
                Value = senderRoot,
                Depth = 0
            });

            int inspected = 0;

            while (queue.Count > 0 && inspected < 128)
            {
                SearchNode node = queue.Dequeue();
                object current = node.Value;

                if (current == null ||
                    visited.Contains(current) ||
                    IsDestroyedUnityObject(current))
                {
                    continue;
                }

                visited.Add(current);
                inspected++;

                if (TryInvokeSendMethod(
                        current,
                        sender,
                        text))
                {
                    return true;
                }

                if (node.Depth >= 4)
                    continue;

                EnqueueImportantMembers(
                    queue,
                    current,
                    node.Depth + 1
                );

                EnqueueSafeMembers(
                    queue,
                    current,
                    node.Depth + 1
                );

                EnqueueUnityComponents(
                    queue,
                    current,
                    node.Depth + 1
                );
            }

            return false;
        }

        private static bool TryInvokeSendMethod(
            object target,
            string sender,
            string text)
        {
            Type type = target.GetType();
            string fullName = type.FullName ?? "";

            bool isConversationType =
                fullName.IndexOf(
                    "Conversation",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "Messaging",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "MSG",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            MethodInfo[] methods;

            try
            {
                methods = type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );
            }
            catch
            {
                return false;
            }

            for (int nameIndex = 0;
                 nameIndex < SendMethodNames.Length;
                 nameIndex++)
            {
                string expectedName =
                    SendMethodNames[nameIndex];

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (!method.Name.Equals(
                            expectedName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (method.ContainsGenericParameters)
                        continue;

                    Type declaringType =
                        method.DeclaringType;

                    string declaringNamespace =
                        declaringType?.Namespace ?? "";

                    if (declaringNamespace.StartsWith(
                            "UnityEngine",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if ((expectedName == "SendMessage" ||
                         expectedName == "AddMessage" ||
                         expectedName == "ReceiveMessage" ||
                         expectedName == "SendMessageChain") &&
                        !isConversationType)
                    {
                        continue;
                    }

                    object[] arguments = BuildArguments(
                        method.GetParameters(),
                        sender,
                        text
                    );

                    if (arguments == null)
                        continue;

                    try
                    {
                        object result =
                            method.Invoke(target, arguments);

                        if (method.ReturnType == typeof(bool) &&
                            result is bool &&
                            !(bool)result)
                        {
                            continue;
                        }

                        return true;
                    }
                    catch
                    {
                        // Try another supported overload.
                    }
                }
            }

            return false;
        }

        private static object[] BuildArguments(
            ParameterInfo[] parameters,
            string sender,
            string text)
        {
            if (parameters == null ||
                parameters.Length == 0)
            {
                return null;
            }

            var values = new object[parameters.Length];
            bool messageAssigned = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;
                string parameterName =
                    (parameter.Name ?? "").ToLowerInvariant();

                if (parameterType == typeof(string))
                {
                    if (parameterName.Contains("sender") ||
                        parameterName.Contains("contact") ||
                        parameterName.Contains("name"))
                    {
                        values[i] = sender;
                    }
                    else
                    {
                        values[i] = text;
                        messageAssigned = true;
                    }

                    continue;
                }

                object collection =
                    CreateStringCollection(
                        parameterType,
                        text
                    );

                if (collection != null)
                {
                    values[i] = collection;
                    messageAssigned = true;
                    continue;
                }

                if (parameterType == typeof(bool))
                {
                    if (parameterName.Contains("player") ||
                        parameterName.Contains("outgoing") ||
                        parameterName.Contains("fromself"))
                    {
                        values[i] = false;
                    }
                    else if (parameter.HasDefaultValue)
                    {
                        values[i] = parameter.DefaultValue;
                    }
                    else
                    {
                        values[i] = true;
                    }

                    continue;
                }

                if (parameterType == typeof(float))
                {
                    values[i] = parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : 0.1f;

                    continue;
                }

                if (parameterType == typeof(double))
                {
                    values[i] = parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : 0.1d;

                    continue;
                }

                if (parameterType == typeof(int))
                {
                    values[i] = parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : 0;

                    continue;
                }

                if (parameterType.IsEnum)
                {
                    values[i] =
                        Activator.CreateInstance(parameterType);

                    continue;
                }

                if (parameter.HasDefaultValue)
                {
                    values[i] = parameter.DefaultValue;
                    continue;
                }

                return null;
            }

            return messageAssigned ? values : null;
        }

        private static object CreateStringCollection(
            Type type,
            string text)
        {
            if (type == typeof(string[]))
                return new[] { text };

            if (type.IsArray &&
                type.GetElementType() == typeof(string))
            {
                Array array =
                    Array.CreateInstance(typeof(string), 1);

                array.SetValue(text, 0);
                return array;
            }

            if (type.IsAssignableFrom(
                    typeof(List<string>)))
            {
                return new List<string> { text };
            }

            string fullName = type.FullName ?? "";

            if (fullName.IndexOf(
                    "List",
                    StringComparison.OrdinalIgnoreCase) < 0 ||
                fullName.IndexOf(
                    "String",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            try
            {
                object instance =
                    Activator.CreateInstance(type);

                MethodInfo add = type.GetMethod(
                    "Add",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null
                );

                if (instance != null && add != null)
                {
                    add.Invoke(instance, new object[] { text });
                    return instance;
                }
            }
            catch { }

            return null;
        }

        private static void EnqueueImportantMembers(
            Queue<SearchNode> queue,
            object target,
            int depth)
        {
            for (int i = 0;
                 i < ImportantMembers.Length;
                 i++)
            {
                object child =
                    GetMember(target, ImportantMembers[i]);

                Enqueue(queue, child, depth);
            }
        }

        private static void EnqueueSafeMembers(
            Queue<SearchNode> queue,
            object target,
            int depth)
        {
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo[] fields;

                try
                {
                    fields = type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );
                }
                catch
                {
                    fields = new FieldInfo[0];
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    try
                    {
                        object value =
                            fields[i].GetValue(target);

                        if (ShouldTraverse(
                                fields[i].Name,
                                value))
                        {
                            Enqueue(queue, value, depth);
                        }
                    }
                    catch { }
                }

                PropertyInfo[] properties;

                try
                {
                    properties = type.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );
                }
                catch
                {
                    properties = new PropertyInfo[0];
                }

                for (int i = 0;
                     i < properties.Length;
                     i++)
                {
                    try
                    {
                        if (properties[i].GetIndexParameters().Length != 0)
                            continue;

                        string name = properties[i].Name;

                        if (!LooksImportant(name))
                            continue;

                        object value =
                            properties[i].GetValue(target);

                        if (ShouldTraverse(name, value))
                            Enqueue(queue, value, depth);
                    }
                    catch { }
                }

                type = type.BaseType;
            }
        }

        private static void EnqueueUnityComponents(
            Queue<SearchNode> queue,
            object target,
            int depth)
        {
            try
            {
                GameObject gameObject = null;

                if (target is GameObject)
                    gameObject = (GameObject)target;
                else if (target is Component)
                    gameObject = ((Component)target).gameObject;

                if (gameObject == null)
                    return;

                Component[] components =
                    gameObject.GetComponents<Component>();

                if (components == null)
                    return;

                for (int i = 0;
                     i < components.Length;
                     i++)
                {
                    Enqueue(
                        queue,
                        components[i],
                        depth
                    );
                }
            }
            catch { }
        }

        private static bool ShouldTraverse(
            string memberName,
            object value)
        {
            if (value == null ||
                value is string ||
                value is Type ||
                value is Delegate)
            {
                return false;
            }

            Type type = value.GetType();

            if (type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(decimal))
            {
                return false;
            }

            string ns = type.Namespace ?? "";

            return LooksImportant(memberName) ||
                   ns.StartsWith(
                       "CustomNPCExample",
                       StringComparison.Ordinal) ||
                   ns.StartsWith(
                       "S1API",
                       StringComparison.Ordinal) ||
                   ns.StartsWith(
                       "Il2CppScheduleOne",
                       StringComparison.Ordinal) ||
                   value is GameObject ||
                   value is Component;
        }

        private static bool LooksImportant(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string normalized = name.ToLowerInvariant();

            return normalized.Contains("npc") ||
                   normalized.Contains("native") ||
                   normalized.Contains("entity") ||
                   normalized.Contains("message") ||
                   normalized.Contains("conversation") ||
                   normalized.Contains("contact");
        }

        private static void Enqueue(
            Queue<SearchNode> queue,
            object value,
            int depth)
        {
            if (value == null)
                return;

            if (value is IEnumerable &&
                !(value is string) &&
                !(value is GameObject) &&
                !(value is Component))
            {
                try
                {
                    int count = 0;

                    foreach (object item in (IEnumerable)value)
                    {
                        queue.Enqueue(new SearchNode
                        {
                            Value = item,
                            Depth = depth
                        });

                        count++;

                        if (count >= 16)
                            break;
                    }

                    return;
                }
                catch { }
            }

            queue.Enqueue(new SearchNode
            {
                Value = value,
                Depth = depth
            });
        }

        private static bool IsDestroyedUnityObject(
            object value)
        {
            try
            {
                UnityEngine.Object unityObject =
                    value as UnityEngine.Object;

                return !ReferenceEquals(unityObject, null) &&
                       unityObject == null;
            }
            catch
            {
                return false;
            }
        }

        private static object GetMember(
            object target,
            string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(target);
                    }

                    FieldInfo field = type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (field != null)
                        return field.GetValue(target);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetStaticMember(
            Type type,
            string name)
        {
            try
            {
                PropertyInfo property = type.GetProperty(
                    name,
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                if (property != null)
                    return property.GetValue(null);

                FieldInfo field = type.GetField(
                    name,
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                return field?.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var result = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                {
                    result.Append(
                        char.ToLowerInvariant(value[i])
                    );
                }
            }

            return result.ToString();
        }
    }
}