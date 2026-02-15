using System.Collections;
using System.Reflection;

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
        private static MemberInfo completedCountMember;
        private static bool membersResolved;

        static QuestDataAccess()
        {
            compField = ReflectionCache.GetField(typeof(PlayerData), "QuestCompletionData");
            if (compField != null)
            {
                rtField = ReflectionCache.GetField(compField.FieldType, "RuntimeData");
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
            hasBeenSeenMember = ReflectionCache.FindMember(runtimeType, "HasBeenSeen");
            isAcceptedMember = ReflectionCache.FindMember(runtimeType, "IsAccepted");
            isCompletedMember = ReflectionCache.FindMember(runtimeType, "IsCompleted");
            wasEverCompletedMember = ReflectionCache.FindMember(runtimeType, "WasEverCompleted");
            completedCountMember = ReflectionCache.FindMember(runtimeType, "CompletedCount");
            membersResolved = true;

            QuestModPlugin.LogDebugInfo($"QuestDataAccess resolved members on runtime type {runtimeType.FullName}:");
            QuestModPlugin.LogDebugInfo($"  HasBeenSeen={ReflectionCache.MemberTag(hasBeenSeenMember)}, IsAccepted={ReflectionCache.MemberTag(isAcceptedMember)}, IsCompleted={ReflectionCache.MemberTag(isCompletedMember)}, WasEverCompleted={ReflectionCache.MemberTag(wasEverCompletedMember)}, CompletedCount={ReflectionCache.MemberTag(completedCountMember)}");
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
            if (qd == null) return false;
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(ReflectionCache.ReadMember(isAcceptedMember, qd) ?? false);
        }

        internal static bool IsCompleted(object qd)
        {
            if (qd == null) return false;
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(ReflectionCache.ReadMember(isCompletedMember, qd) ?? false);
        }

        internal static bool HasBeenSeen(object qd)
        {
            if (qd == null) return false;
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (bool)(ReflectionCache.ReadMember(hasBeenSeenMember, qd) ?? false);
        }

        internal static int GetCompletedCount(object qd)
        {
            if (qd == null) return 0;
            if (!membersResolved) ResolveMembers(qd.GetType());
            return (int)(ReflectionCache.ReadMember(completedCountMember, qd) ?? 0);
        }

        internal static object SetFields(object qd, bool seen, bool accepted, bool completed, bool wasEver)
        {
            if (qd == null) return qd;
            if (!membersResolved) ResolveMembers(qd.GetType());
            object boxed = qd;
            ReflectionCache.WriteMember(hasBeenSeenMember, boxed, seen);
            ReflectionCache.WriteMember(isAcceptedMember, boxed, accepted);
            ReflectionCache.WriteMember(isCompletedMember, boxed, completed);
            ReflectionCache.WriteMember(wasEverCompletedMember, boxed, wasEver);
            return boxed;
        }
    }
}
