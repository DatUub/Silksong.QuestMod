using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Silksong.UnityHelper.Extensions;

namespace QuestMod
{
    // GourmandStopDecay halts the static TakeHit. GourmandDecaySeconds overrides
    // totalTimer on every DeliveryQuestItem on scene load. Decay is on the base
    // class, not the Standalone subclass. Segment count is HUD-only (no quest
    // field) so it's not exposed.
    public static class GourmandTimerPatch
    {
        private static Type? deliveryQuestItemType;
        private static FieldInfo? totalTimerField;
        private static MethodInfo? takeHitIntMethod;
        private static bool resolved;
        private static bool harmonyApplied;

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            // Resolve types + apply TakeHit Harmony at plugin init so SelfTest
            // cluster_L can see the patch before the first gameplay scene.
            // totalTimer instance overrides still re-apply on scene load below.
            EnsureResolved();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || QuestModConstants.IsTransientMenuScene(scene.name))
                return;

            EnsureResolved();

            // Re-apply totalTimer overrides on any DeliveryQuestItem instances
            // that get loaded with this scene. Two-shot like SilverBellPatch.
            QuestModPlugin.Instance.InvokeAfterSeconds(ApplyRuntimeOverrides, 0.5f);
            QuestModPlugin.Instance.InvokeAfterSeconds(ApplyRuntimeOverrides, 2f);
        }

        private static void EnsureResolved()
        {
            if (resolved) return;

            // Find DeliveryQuestItem (BASE class). Direct reflection load fails
            // on Mono-only constructs in this game, so iterate loaded assemblies
            // and pick whichever DLL has it via GetType-with-no-throw.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType("DeliveryQuestItem", throwOnError: false);
                    if (t != null) { deliveryQuestItemType = t; break; }
                }
                catch { /* assembly can't be inspected */ }
            }

            if (deliveryQuestItemType == null)
            {
                // Game assemblies may not be loaded at plugin Awake — retry on scene load.
                QuestModPlugin.Log.LogWarning(
                    "GourmandTimerPatch: DeliveryQuestItem type not found yet; will retry on scene load.");
                return;
            }

            resolved = true;

            // totalTimer is a private/serialized float on DeliveryQuestItem.
            totalTimerField = deliveryQuestItemType.GetField(
                "totalTimer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (totalTimerField == null)
                QuestModPlugin.Log.LogWarning("GourmandTimerPatch: totalTimer field missing on DeliveryQuestItem; decay-seconds override is a no-op.");

            // Static TakeHit(int) - the decay tick. We patch this with a Harmony
            // prefix that returns false (skip original) when StopDecay is on.
            takeHitIntMethod = deliveryQuestItemType.GetMethod(
                "TakeHit",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);

            if (takeHitIntMethod == null)
                QuestModPlugin.Log.LogWarning("GourmandTimerPatch: static TakeHit(int) method missing on DeliveryQuestItem; StopDecay toggle is a no-op.");

            // Apply Harmony prefix once.
            if (!harmonyApplied && takeHitIntMethod != null)
            {
                try
                {
                    var harmony = new Harmony(QuestModConstants.HarmonyId_GourmandTimer);
                    var prefix = typeof(GourmandTimerPatch).GetMethod(
                        nameof(TakeHitPrefix),
                        BindingFlags.NonPublic | BindingFlags.Static);
                    harmony.Patch(takeHitIntMethod, prefix: new HarmonyMethod(prefix));
                    harmonyApplied = true;
                    QuestModPlugin.Log.LogInfo($"GourmandTimerPatch: prefixed {deliveryQuestItemType.Name}.TakeHit(int) for StopDecay toggle.");
                }
                catch (Exception ex)
                {
                    QuestModPlugin.Log.LogWarning($"GourmandTimerPatch: failed to patch TakeHit(int): {ex.Message}");
                }
            }
        }

        // Skips the original method (no decay) when the user has flipped
        // GourmandStopDecay on. TakeHit is static + iterates ActiveItems, so
        // skipping it stops decay for ALL delivery items, which is the intended
        // semantic of the toggle (Rasher / any timed delivery alike).
        private static bool TakeHitPrefix()
        {
            if (QuestModPlugin.GourmandStopDecay == null) return true;
            if (!QuestModPlugin.GourmandStopDecay.Value) return true;
            return false;
        }

        private static void ApplyRuntimeOverrides()
        {
            if (deliveryQuestItemType == null || totalTimerField == null) return;
            if (QuestModPlugin.GourmandDecaySeconds == null) return;

            float configSeconds = QuestModPlugin.GourmandDecaySeconds.Value;
            // Vanilla Rasher default is 47s; only override if user changed it.
            // Use a small tolerance so float comparison doesn't trip on serialized values.
            if (Math.Abs(configSeconds - 47f) < 0.01f) return;

            // Find every DeliveryQuestItem ScriptableObject instance loaded in memory
            // and patch its totalTimer. Since DQI is a CollectableItem (ScriptableObject),
            // we use Resources.FindObjectsOfTypeAll to grab them.
            try
            {
                var instances = Resources.FindObjectsOfTypeAll(deliveryQuestItemType);
                int patched = 0;
                foreach (var obj in instances)
                {
                    try
                    {
                        var current = (float)totalTimerField.GetValue(obj);
                        if (Math.Abs(current - configSeconds) > 0.01f)
                        {
                            totalTimerField.SetValue(obj, configSeconds);
                            patched++;
                        }
                    }
                    catch { /* skip */ }
                }
                if (patched > 0)
                    QuestModPlugin.LogDebugInfo($"GourmandTimerPatch: applied totalTimer={configSeconds:F1}s to {patched} DeliveryQuestItem instance(s).");
            }
            catch (Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"GourmandTimerPatch: ApplyRuntimeOverrides failed: {ex.Message}");
            }
        }
    }
}
