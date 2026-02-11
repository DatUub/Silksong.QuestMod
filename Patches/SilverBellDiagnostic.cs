using System.Collections.Generic;
using System.Text;
using HutongGames.PlayMaker;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    public static class SilverBellDiagnostic
    {
        private static bool _dumped;

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("SilverBellDiagnostic: Registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_dumped || string.IsNullOrEmpty(scene.name))
                return;

            QuestModPlugin.Log.LogInfo($"[BellDiag] Scene loaded: '{scene.name}'");
            QuestModPlugin.Instance.InvokeAfterSeconds(() => DumpScene(scene), 2f);
        }

        private static void DumpScene(Scene scene)
        {
            if (_dumped)
                return;

            var allFsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            QuestModPlugin.Log.LogInfo($"[BellDiag] Scene '{scene.name}': {allFsms.Length} total FSMs");

            var matched = new List<PlayMakerFSM>();

            foreach (var fsm in allFsms)
            {
                if (fsm == null)
                    continue;

                string path = GetGameObjectPath(fsm.gameObject);

                if (path.Contains("Quest Bell Dropper") ||
                    path.Contains("NoSilver") ||
                    path.Contains("Drop Bell") ||
                    fsm.gameObject.name.Contains("Quest Bell Dropper"))
                {
                    matched.Add(fsm);
                }
            }

            if (matched.Count == 0)
                return;

            _dumped = true;
            QuestModPlugin.Log.LogInfo($"[BellDiag] ===== BELL DROPPER DUMP — Scene: {scene.name} — {matched.Count} FSMs =====");

            foreach (var fsm in matched)
                DumpFSM(fsm);

            QuestModPlugin.Log.LogInfo("[BellDiag] ===== END BELL DROPPER DUMP =====");
        }

        private static void DumpFSM(PlayMakerFSM fsm)
        {
            var sb = new StringBuilder();
            string path = GetGameObjectPath(fsm.gameObject);
            sb.AppendLine($"[BellDiag] --- FSM: '{fsm.FsmName}' on '{path}' ---");
            sb.AppendLine($"[BellDiag]   Active: {fsm.gameObject.activeInHierarchy}, Enabled: {fsm.enabled}");

            if (fsm.Fsm == null || fsm.FsmStates == null)
            {
                sb.AppendLine("[BellDiag]   (FSM not initialized)");
                QuestModPlugin.Log.LogInfo(sb.ToString());
                return;
            }

            sb.AppendLine($"[BellDiag]   Start State: {fsm.Fsm.StartState}");

            foreach (var fsmVar in fsm.FsmVariables.FloatVariables)
                sb.AppendLine($"[BellDiag]   Var(float): {fsmVar.Name} = {fsmVar.Value}");
            foreach (var fsmVar in fsm.FsmVariables.IntVariables)
                sb.AppendLine($"[BellDiag]   Var(int): {fsmVar.Name} = {fsmVar.Value}");
            foreach (var fsmVar in fsm.FsmVariables.BoolVariables)
                sb.AppendLine($"[BellDiag]   Var(bool): {fsmVar.Name} = {fsmVar.Value}");
            foreach (var fsmVar in fsm.FsmVariables.StringVariables)
                sb.AppendLine($"[BellDiag]   Var(string): {fsmVar.Name} = \"{fsmVar.Value}\"");

            foreach (var state in fsm.FsmStates)
            {
                if (state == null) continue;
                sb.AppendLine($"[BellDiag]   State: '{state.Name}'");

                if (state.Transitions != null)
                    foreach (var t in state.Transitions)
                        sb.AppendLine($"[BellDiag]     Transition: '{t.EventName}' -> '{t.ToState}'");

                if (state.Actions != null)
                {
                    foreach (var action in state.Actions)
                    {
                        if (action == null) continue;
                        string typeName = action.GetType().Name;
                        sb.AppendLine($"[BellDiag]     Action: {typeName}");
                        DumpAllFields(sb, action);
                    }
                }
            }

            QuestModPlugin.Log.LogInfo(sb.ToString());
        }

        private static void DumpAllFields(StringBuilder sb, FsmStateAction action)
        {
            var fields = action.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    var val = field.GetValue(action);
                    if (val == null) continue;

                    string valStr;
                    if (val is FsmFloat fFloat) valStr = fFloat.Value.ToString();
                    else if (val is FsmInt fInt) valStr = fInt.Value.ToString();
                    else if (val is FsmBool fBool) valStr = fBool.Value.ToString();
                    else if (val is FsmString fStr) valStr = $"\"{fStr.Value}\"";
                    else if (val is FsmEvent fEvent) valStr = fEvent.Name;
                    else if (val is FsmFloat[] fFloats)
                    {
                        var parts = new List<string>();
                        foreach (var f in fFloats) parts.Add(f.Value.ToString());
                        valStr = "[" + string.Join(", ", parts) + "]";
                    }
                    else if (val is FsmEvent[] fEvents)
                    {
                        var parts = new List<string>();
                        foreach (var e in fEvents) parts.Add(e != null ? e.Name : "null");
                        valStr = "[" + string.Join(", ", parts) + "]";
                    }
                    else if (val is float[] rawFloats)
                    {
                        var parts = new List<string>();
                        foreach (var f in rawFloats) parts.Add(f.ToString());
                        valStr = "[" + string.Join(", ", parts) + "]";
                    }
                    else valStr = val.ToString();

                    sb.AppendLine($"[BellDiag]       {field.Name}: {valStr}");
                }
                catch { }
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var parts = new List<string>();
            var current = go.transform;
            while (current != null)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }
    }
}
