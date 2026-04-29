// CLUSTER M: Bug 2 - Pinstress / Fatal Resolve ordering in All-Wishes mode.
//
// Diagnosis (verified via Resources/Data/Quests/QuestPrereqsFull.md):
//   "Pinstress Battle Pre (Fatal Resolve)
//    pdTest: (pinstressQuestReady=True AND hasSuperJump=True)"
//
// The original "Needle Strike" framing was wrong - the actual prereq for
// Pinstress's combat dialogue is Super Jump (the high-jump movement ability),
// not the Needle Strike art. Under All-Wishes Mode, AllQuestsAvailable flips
// IsAvailable=true on Pinstress Battle Pre before BOTH gates fire, so when
// the player walks into Pinstress's room she opens her post-progression
// branch and either softlocks (waiting for a flag the game hasn't set) or
// never gates the dialogue at all.
//
// Fix approach (this file):
//   Postfix on FullQuestBase.IsAvailable that runs AFTER QuestAvailabilityPatch
//   (priority=Last). When the quest is "Pinstress Battle Pre" and All-Wishes
//   would force it available, we narrow it back to false unless BOTH
//   pinstressQuestReady AND hasSuperJump are true. The legacy method name
//   HasNeedleStrike() is preserved as a back-compat shim so any external
//   callers still resolve.
//
// Confidence: HIGH - both PD field names verified to exist on the live
//   Assembly-CSharp.dll. Quest name + chain structure verified in
//   QuestCapabilities.json. Fail-open if either field is missing on a
//   future game patch (rename safety).

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
                _superJumpField = pdType.GetField("hasSuperJump", BindingFlags.Public | BindingFlags.Instance);
                _pinstressReadyField = pdType.GetField("pinstressQuestReady", BindingFlags.Public | BindingFlags.Instance);
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
            // CLUSTER Q: Pinstress narrowing is an Adjusted-only safety net so
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
