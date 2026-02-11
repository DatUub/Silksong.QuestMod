using System.Collections.Generic;
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

            var keepActions = new List<FsmStateAction>();
            foreach (var action in dropType.Actions)
            {
                if (action == null)
                    continue;

                string typeName = action.GetType().Name;
                if (typeName == "SendEvent")
                {
                    keepActions.Add(action);
                    QuestModPlugin.Log.LogInfo($"SilverBellPatch: Keeping {typeName} on {fsm.gameObject.name}");
                }
                else
                {
                    QuestModPlugin.Log.LogInfo($"SilverBellPatch: Removing {typeName} from {fsm.gameObject.name}");
                }
            }

            dropType.Actions = keepActions.ToArray();

            var silverTransition = new List<FsmTransition>();
            foreach (var t in dropType.Transitions)
            {
                if (t.EventName == "SILVER")
                    silverTransition.Add(t);
            }
            dropType.Transitions = silverTransition.ToArray();

            return true;
        }
    }
}
