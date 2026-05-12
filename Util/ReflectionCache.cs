using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace QuestMod
{
    internal static class ReflectionCache
    {
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<(Type, string), FieldInfo> fieldCache = new Dictionary<(Type, string), FieldInfo>();
        private static readonly Dictionary<(Type, string), PropertyInfo> propCache = new Dictionary<(Type, string), PropertyInfo>();
        private static readonly Dictionary<(Type, string), MemberInfo> memberCache = new Dictionary<(Type, string), MemberInfo>();
        private static readonly HashSet<string> warned = new HashSet<string>();

        internal static Type GetType(string name)
        {
            if (typeCache.TryGetValue(name, out var cached)) return cached;
            var type = AccessTools.TypeByName(name);
            if (type == null) WarnOnce($"Type '{name}' not found");
            typeCache[name] = type;
            return type;
        }

        internal static FieldInfo GetField(Type type, string name)
        {
            if (type == null) return null;
            var key = (type, name);
            if (fieldCache.TryGetValue(key, out var cached)) return cached;
            var field = AccessTools.Field(type, name);
            if (field == null) WarnOnce($"Field '{name}' not found on {type.Name}");
            fieldCache[key] = field;
            return field;
        }

        internal static FieldInfo GetField(string typeName, string fieldName)
        {
            var type = GetType(typeName);
            return type != null ? GetField(type, fieldName) : null;
        }

        internal static PropertyInfo GetProperty(Type type, string name)
        {
            if (type == null) return null;
            var key = (type, name);
            if (propCache.TryGetValue(key, out var cached)) return cached;
            var prop = AccessTools.Property(type, name);
            if (prop == null) WarnOnce($"Property '{name}' not found on {type.Name}");
            propCache[key] = prop;
            return prop;
        }

        internal static PropertyInfo GetProperty(string typeName, string propName)
        {
            var type = GetType(typeName);
            return type != null ? GetProperty(type, propName) : null;
        }

        internal static MemberInfo FindMember(Type type, string name)
        {
            if (type == null) return null;
            var key = (type, name);
            if (memberCache.TryGetValue(key, out var cached)) return cached;

            MemberInfo member = (MemberInfo)AccessTools.Field(type, name)
                             ?? AccessTools.Property(type, name);
            if (member == null) WarnOnce($"Member '{name}' not found on {type.Name}");
            memberCache[key] = member;
            return member;
        }

        internal static T Read<T>(object obj, FieldInfo field, T fallback = default)
        {
            if (obj == null || field == null) return fallback;
            var val = field.GetValue(obj);
            return val is T typed ? typed : fallback;
        }

        internal static T Read<T>(object obj, PropertyInfo prop, T fallback = default)
        {
            if (obj == null || prop == null) return fallback;
            var val = prop.GetValue(obj);
            return val is T typed ? typed : fallback;
        }

        internal static object ReadMember(MemberInfo member, object obj)
        {
            if (member == null || obj == null) return null;
            if (member is PropertyInfo p) return p.GetValue(obj);
            if (member is FieldInfo f) return f.GetValue(obj);
            return null;
        }

        internal static void WriteMember(MemberInfo member, object obj, object value)
        {
            if (member == null || obj == null) return;
            if (member is PropertyInfo p) p.SetValue(obj, value);
            else if (member is FieldInfo f) f.SetValue(obj, value);
        }

        internal static void Write(object obj, FieldInfo field, object value)
        {
            if (obj == null || field == null) return;
            field.SetValue(obj, value);
        }

        internal static void WriteToArray(Array array, int index, object element, FieldInfo field, object value)
        {
            if (array == null || field == null) return;
            field.SetValue(element, value);
            if (array.GetType().GetElementType().IsValueType)
                array.SetValue(element, index);
        }

        internal static string MemberTag(MemberInfo m)
        {
            if (m is PropertyInfo) return "prop";
            if (m is FieldInfo) return "field";
            return "NOT FOUND";
        }

        private static void WarnOnce(string msg)
        {
            if (warned.Contains(msg)) return;
            warned.Add(msg);
            QuestModPlugin.Log.LogWarning($"ReflectionCache: {msg}");
        }
    }
}
