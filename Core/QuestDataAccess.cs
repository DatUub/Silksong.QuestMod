using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace QuestMod
{
    internal static class QuestDataAccess
    {
        private static readonly FieldInfo compField;
        private static readonly FieldInfo rtField;

        private static MemberInfo hasBeenSeenMember;
        private static MemberInfo isAcceptedMember;
        private static MemberInfo isCompletedMember;
        private static MemberInfo wasEverCompletedMember;
        private static bool membersResolved;

        static QuestDataAccess()
        {
            compField = AccessTools.Field(typeof(PlayerData), "QuestCompletionData");
            if (compField != null)
            {
                rtField = AccessTools.Field(compField.FieldType, "RuntimeData");
                if (rtField == null)
                {
                    var baseType = compField.FieldType.BaseType;
                    while (baseType != null && rtField == null)
                    {
                        rtField = baseType.GetField("RuntimeData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        baseType = baseType.BaseType;
                    }
                }
            }
        }

        private static void ResolveMembers(System.Type runtimeType)
        {
            if (membersResolved) return;
            hasBeenSeenMember = FindMember(runtimeType, "HasBeenSeen");
            isAcceptedMember = FindMember(runtimeType, "IsAccepted");
            isCompletedMember = FindMember(runtimeType, "IsCompleted");
            wasEverCompletedMember = FindMember(runtimeType, "WasEverCompleted");
            membersResolved = true;

            QuestModPlugin.Log.LogInfo($"QuestDataAccess resolved members on runtime type {runtimeType.FullName}:");
            QuestModPlugin.Log.LogInfo($"  HasBeenSeen={MemberTag(hasBeenSeenMember)}, IsAccepted={MemberTag(isAcceptedMember)}, IsCompleted={MemberTag(isCompletedMember)}, WasEverCompleted={MemberTag(wasEverCompletedMember)}");
        }

        private static string MemberTag(MemberInfo m)
        {
            if (m is PropertyInfo) return "prop";
            if (m is FieldInfo) return "field";
            return "NOT FOUND";
        }

        private static MemberInfo FindMember(System.Type type, string name)
        {
            PropertyInfo prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null) return prop;
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field;
        }

        private static object GetValue(MemberInfo member, object obj)
        {
            if (member is PropertyInfo p) return p.GetValue(obj);
            if (member is FieldInfo f) return f.GetValue(obj);
            return null;
        }

        private static void SetValue(MemberInfo member, object obj, object value)
        {
            if (member is PropertyInfo p) p.SetValue(obj, value);
            else if (member is FieldInfo f) f.SetValue(obj, value);
        }

        internal static IDictionary GetRuntimeData()
        {
            if (compField == null) return null;
            if (rtField == null) return null;
            if (PlayerData.instance == null) return null;
            var comp = compField.GetValue(PlayerData.instance);
            if (comp == null) return null;
            return rtField.GetValue(comp) as IDictionary;
        }

        internal static bool IsAccepted(object qd)
        {
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(GetValue(isAcceptedMember, qd) ?? false);
        }

        internal static bool IsCompleted(object qd)
        {
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(GetValue(isCompletedMember, qd) ?? false);
        }

        internal static bool HasBeenSeen(object qd)
        {
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(GetValue(hasBeenSeenMember, qd) ?? false);
        }

        internal static object SetFields(object qd, bool seen, bool accepted, bool completed, bool wasEver)
        {
            if (!membersResolved) ResolveMembers(qd.GetType());
            object boxed = qd;
            SetValue(hasBeenSeenMember, boxed, seen);
            SetValue(isAcceptedMember, boxed, accepted);
            SetValue(isCompletedMember, boxed, completed);
            SetValue(wasEverCompletedMember, boxed, wasEver);
            return boxed;
        }
    }
}
