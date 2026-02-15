using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private void DrawSilkSoulTab()
        {
            if (!SilkSoulOverrides.TryResolve())
            {
                GUILayout.Label("Silk & Soul data not available (load a save first)");
                return;
            }

            int current = SilkSoulOverrides.GetCurrentPoints();
            int threshold = SilkSoulOverrides.GetThreshold();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Quest Points:", GUILayout.Width(100));
            var prevColor = GUI.color;
            GUI.color = current >= threshold ? Color.green : Color.white;
            GUILayout.Label($"{current} / {threshold}", GUILayout.Width(80));
            GUI.color = prevColor;
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Threshold:", "Number of points needed to unlock Silk & Soul quest"), GUILayout.Width(100));
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                SilkSoulOverrides.SetThreshold(threshold - 1);
                thresholdInput = (threshold - 1).ToString();
            }

            if (thresholdInput == "") thresholdInput = threshold.ToString();
            thresholdInput = GUILayout.TextField(thresholdInput, GUILayout.Width(40));

            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                SilkSoulOverrides.SetThreshold(threshold + 1);
                thresholdInput = (threshold + 1).ToString();
            }
            if (GUILayout.Button("Set", GUILayout.Width(32)))
            {
                if (int.TryParse(thresholdInput, out int val))
                    SilkSoulOverrides.SetThreshold(val);
            }
            if (GUILayout.Button("Reset", GUILayout.Width(48)))
            {
                SilkSoulOverrides.ResetThreshold();
                thresholdInput = QuestRegistry.DefaultThreshold.ToString();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (threshold != QuestRegistry.DefaultThreshold)
                GUILayout.Label($"  (default: {QuestRegistry.DefaultThreshold})");

            GUILayout.Space(4);

            if (SilkSoulOverrides.HasEntries)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset All Values", GUILayout.Width(120)))
                {
                    SilkSoulOverrides.ResetAllPointValues();
                    pointInputs.Clear();
                }
                if (GUILayout.Button("Set All to 0", GUILayout.Width(100)))
                {
                    var entries = SilkSoulOverrides.GetPointEntries();
                    foreach (var e in entries)
                    {
                        SilkSoulOverrides.SetPointValue(e.EntryIndex, 0f);
                        pointInputs[e.QuestName] = "0";
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            silkSoulScroll = GUILayout.BeginScrollView(silkSoulScroll);
            var rt = QuestDataAccess.GetRuntimeData();

            if (SilkSoulOverrides.HasEntries)
            {
                var entries = SilkSoulOverrides.GetPointEntries();
                GUILayout.Label("Quest Point Values (editable)", QuestGUISkin.SectionHeader);
                foreach (var entry in entries)
                {
                    GUILayout.BeginHorizontal();
                    bool completed = rt != null && rt.Contains(entry.QuestName) && QuestDataAccess.IsCompleted(rt[entry.QuestName]);
                    var qColor = GUI.color;
                    GUI.color = completed ? Color.green : Color.white;
                    string displayName = QuestAcceptance.GetDisplayName(entry.QuestName);
                    GUILayout.Label(completed ? "✓" : "✗", GUILayout.Width(14));
                    GUILayout.Label(displayName, GUILayout.Width(200));
                    GUI.color = qColor;

                    string key = entry.QuestName;
                    if (!pointInputs.ContainsKey(key))
                        pointInputs[key] = entry.Value.ToString("0.##");

                    pointInputs[key] = GUILayout.TextField(pointInputs[key], GUILayout.Width(40));

                    if (GUILayout.Button("Set", GUILayout.Width(32)))
                    {
                        if (float.TryParse(pointInputs[key], out float newVal))
                            SilkSoulOverrides.SetPointValue(entry.EntryIndex, newVal);
                    }

                    bool isDefault = System.Math.Abs(entry.Value - entry.DefaultValue) < 0.01f;
                    if (!isDefault)
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label($"({entry.DefaultValue:0.##})", GUILayout.Width(40));
                        GUI.color = qColor;
                    }

                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                int reqDone = 0;
                int reqTotal = QuestRegistry.SilkSoulRequiredQuests.Length;
                foreach (string quest in QuestRegistry.SilkSoulRequiredQuests)
                {
                    if (rt != null && rt.Contains(quest) && QuestDataAccess.IsCompleted(rt[quest]))
                        reqDone++;
                }

                int optDone = 0;
                int optTotal = QuestRegistry.SilkSoulPointValues.Count;
                foreach (var kvp in QuestRegistry.SilkSoulPointValues)
                {
                    if (rt != null && rt.Contains(kvp.Key) && QuestDataAccess.IsCompleted(rt[kvp.Key]))
                        optDone++;
                }

                var sumColor = GUI.color;
                GUI.color = new Color(0.70f, 0.80f, 0.95f);
                GUILayout.Label($"Required: {reqDone}/{reqTotal}  |  Optional: {optDone}/{optTotal}");
                GUI.color = sumColor;

                GUILayout.Space(3);

                GUILayout.Label("Required Quests", QuestGUISkin.SectionHeader);
                foreach (string quest in QuestRegistry.SilkSoulRequiredQuests)
                {
                    GUILayout.BeginHorizontal();
                    bool completed = rt != null && rt.Contains(quest) && QuestDataAccess.IsCompleted(rt[quest]);
                    var qColor = GUI.color;
                    GUI.color = completed ? Color.green : Color.white;
                    string displayName = QuestAcceptance.GetDisplayName(quest);
                    GUILayout.Label(completed ? "  ✓ " + displayName : "  ✗ " + displayName);
                    GUI.color = qColor;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
                GUILayout.Label("Optional Quests (contribute points)", QuestGUISkin.SectionHeader);
                foreach (var kvp in QuestRegistry.SilkSoulPointValues)
                {
                    GUILayout.BeginHorizontal();
                    bool completed = rt != null && rt.Contains(kvp.Key) && QuestDataAccess.IsCompleted(rt[kvp.Key]);
                    var qColor = GUI.color;
                    GUI.color = completed ? Color.green : Color.white;
                    string displayName = QuestAcceptance.GetDisplayName(kvp.Key);
                    string pointLabel = kvp.Value == 1f ? "1 pt" : "0.5 pt";
                    GUILayout.Label(completed ? $"  ✓ {displayName}" : $"  ✗ {displayName}", GUILayout.Width(250));
                    GUILayout.Label($"[{pointLabel}]", GUILayout.Width(50));
                    GUI.color = qColor;
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
