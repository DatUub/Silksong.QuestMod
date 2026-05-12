// Pinstress Battle Pre's real prereq is pinstressQuestReady AND hasSuperJump.
// AllWishes flips IsAvailable=true too early. Postfix at Priority.Last
// narrows it back to false until both gates are met. Fails open if either
// PD field renames upstream.

using System.Reflection;
using HarmonyLib;

namespace QuestMod
{
    public static class EdgeBugPinstressNeedleStrike
    {
        public const string PinstressPreQuest = "Pinstress Battle Pre";

        private static FieldInfo? _superJumpField;
        private static FieldInfo? _pinstressReadyField;
        private static bool _resolved;

        public static void Initialize()
        {
            QuestModPlugin.Log.LogInfo("EdgeBugPinstressNeedleStrike: registered (gates Pinstress Battle Pre on hasSuperJump + pinstressQuestReady)");
        }

        // Back-compat shim. Earlier name; delegates to the corrected check.
        internal static bool HasNeedleStrike() => PinstressIsReady();

        internal static bool PinstressIsReady()
        {
            if (PlayerData.instance == null) return false;

            if (!_resolved)
            {
                _resolved = true;
                var pdType = PlayerData.instance.GetType();
                _superJumpField = pdType.GetField(nameof(PlayerData.hasSuperJump), BindingFlags.Public | BindingFlags.Instance);
                _pinstressReadyField = pdType.GetField(nameof(PlayerData.pinstressQuestReady), BindingFlags.Public | BindingFlags.Instance);
                if (_superJumpField == null)
                    QuestModPlugin.Log.LogWarning("EdgeBugPinstressNeedleStrike: PlayerData.hasSuperJump missing - failing open (no narrowing)");
                if (_pinstressReadyField == null)
                    QuestModPlugin.Log.LogWarning("EdgeBugPinstressNeedleStrike: PlayerData.pinstressQuestReady missing - failing open (no narrowing)");
            }

            // Fail open if either field is missing on a future game patch.
            if (_superJumpField == null || _pinstressReadyField == null) return true;

            try
            {
                var sj = (bool)_superJumpField.GetValue(PlayerData.instance);
                var pr = (bool)_pinstressReadyField.GetValue(PlayerData.instance);
                return sj && pr;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(FullQuestBase), "IsAvailable", MethodType.Getter)]
    [HarmonyPriority(Priority.Last)]
    public static class PinstressIsAvailableNarrowingPatch
    {
        public static void Postfix(FullQuestBase __instance, ref bool __result)
        {
            // Pinstress narrowing is an Adjusted-only safety net so
            // the pair stays gated together. Pure mode wants raw -- the player
            // can accept Pinstress Pre without superJump and live with the
            // soft-lock that follows.
            if (!QuestModPlugin.IsAdjustedWishes) return;
            if (!__result) return;
            if (__instance == null || __instance.name != EdgeBugPinstressNeedleStrike.PinstressPreQuest) return;

            if (!EdgeBugPinstressNeedleStrike.PinstressIsReady())
            {
                QuestModPlugin.LogDebugInfo($"PinstressNarrow: '{__instance.name}' gated until pinstressQuestReady && hasSuperJump");
                __result = false;
            }
        }
    }
}
