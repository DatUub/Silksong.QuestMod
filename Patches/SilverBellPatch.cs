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
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name))
                return;

            QuestModPlugin.Instance.InvokeAfterSeconds(() => PatchDroppers(), 0.5f);
            QuestModPlugin.Instance.InvokeAfterSeconds(() => PatchDroppers(), 2f);
        }

        private static void PatchDroppers()
        {
            if (!QuestModPlugin.GuaranteedSilverBells.Value)
                return;

            foreach (var fsm in Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fsm == null || !fsm.gameObject.name.StartsWith("Quest Bell Dropper"))
                    continue;

                if (fsm.FsmName != "Control")
                    continue;

                PatchDropperFSM(fsm);
            }
        }

        private static void PatchDropperFSM(PlayMakerFSM fsm)
        {
            if (fsm.Fsm == null || fsm.FsmStates == null)
                return;

            foreach (var state in fsm.FsmStates)
            {
                if (state == null || state.Name != "Drop Type")
                    continue;

                if (state.Transitions == null)
                    return;

                foreach (var transition in state.Transitions)
                {
                    if (transition.EventName == "STANDARD")
                        transition.ToState = "Silver";
                }

                return;
            }
        }
    }
}
