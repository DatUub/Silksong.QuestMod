using HutongGames.PlayMaker;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    public static class SilverBellPatch
    {
        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("SilverBellPatch: Registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name))
                return;

            QuestModPlugin.Instance.InvokeAfterSeconds(() => PatchDroppers(), 1f);
        }

        private static void PatchDroppers()
        {
            if (!QuestModPlugin.GuaranteedSilverBells.Value)
                return;

            int patched = 0;

            foreach (var fsm in Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fsm == null || !fsm.gameObject.name.StartsWith("Quest Bell Dropper"))
                    continue;

                if (fsm.FsmName != "Control")
                    continue;

                if (PatchDropperFSM(fsm))
                    patched++;
            }

            if (patched > 0)
                QuestModPlugin.Log.LogInfo($"SilverBellPatch: Patched {patched} Quest Bell Droppers for guaranteed silver");
        }

        private static bool PatchDropperFSM(PlayMakerFSM fsm)
        {
            if (fsm.Fsm == null || fsm.FsmStates == null)
                return false;

            FsmState dropType = null;
            foreach (var state in fsm.FsmStates)
            {
                if (state != null && state.Name == "Drop Type")
                {
                    dropType = state;
                    break;
                }
            }

            if (dropType == null || dropType.Actions == null)
                return false;

            bool modified = false;

            foreach (var action in dropType.Actions)
            {
                if (action == null)
                    continue;

                string typeName = action.GetType().Name;

                if (typeName == "CheckAlertRangeByName" ||
                    typeName == "SendRandomEventV4" ||
                    typeName == "CheckIfToolEquipped")
                {
                    action.Enabled = false;
                    modified = true;
                    QuestModPlugin.Log.LogInfo($"SilverBellPatch: Disabled {typeName} on {fsm.gameObject.name}");
                }
            }

            return modified;
        }
    }
}
