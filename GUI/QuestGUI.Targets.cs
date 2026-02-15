using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private void DrawCompletionTab()
        {
            bool overridesEnabled = QuestModPlugin.EnableCompletionOverrides.Value;

            if (!QuestCompletionOverrides.IsInitialized)
            {
                GUILayout.Label("Not initialized - load a save first");
                return;
            }

            var oldCatTab = categoryTab;
            categoryTab = GUILayout.Toolbar(categoryTab, QuestRegistry.Categories);
            if (categoryTab != oldCatTab)
                multiplierText = multiplier.ToString("0.0");

            GUILayout.Space(4);

            GUI.enabled = overridesEnabled;

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Multiplier:", "Scale all targets in this category"), GUILayout.Width(68));
            multiplier = GUILayout.HorizontalSlider(multiplier, 0f, 1f, GUILayout.Width(150));
            multiplier = Mathf.Round(multiplier * 20f) / 20f;

            string newMultText = multiplier.ToString("0.0");
            if (newMultText != multiplierText)
                multiplierText = newMultText;

            multiplierText = GUILayout.TextField(multiplierText, GUILayout.Width(35));

            if (GUILayout.Button("Apply", GUILayout.Width(48)))
            {
                if (float.TryParse(multiplierText, out float parsed))
                {
                    multiplier = parsed;
                    ApplyMultiplier(parsed);
                }
            }

            if (GUILayout.Button("1x", GUILayout.Width(28)))
            {
                multiplier = 1f;
                multiplierText = "1.0";
                ApplyMultiplier(1f);
            }

            if (GUILayout.Button(new GUIContent("R", "Reset this category to original values"), GUILayout.Width(24)))
            {
                ResetCategory();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(50));
            if (GUILayout.Button("Set to 1", GUILayout.Width(60)))
            {
                QuestCompletionOverrides.ApplyPresetAll("set1");
                countInputs.Clear();
                RefreshOverrides();
            }
            if (GUILayout.Button("QoL Half", GUILayout.Width(65)))
            {
                QuestCompletionOverrides.ApplyPresetAll("qol");
                countInputs.Clear();
                RefreshOverrides();
            }
            if (GUILayout.Button("Default", GUILayout.Width(55)))
            {
                QuestCompletionOverrides.ApplyPresetAll("default");
                countInputs.Clear();
                RefreshOverrides();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.enabled = true;

            if (!overridesEnabled)
                GUILayout.Label("Overrides disabled in config — counts are read-only");
            else if (QuestModPlugin.DevRemoveLimits.Value)
                GUILayout.Label("⚠ DevRemoveLimits — no count limits (may break quest state)");

            GUILayout.Space(4);

            string currentCategory = QuestRegistry.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory) || cachedByCategory[currentCategory].Count == 0)
            {
                GUILayout.Label("No quests found in this category.");
                return;
            }

            int modifiedCount = 0;
            int totalTargets = 0;
            foreach (var q in cachedByCategory[currentCategory])
            {
                foreach (var t in q.Targets)
                {
                    totalTargets++;
                    if (t.CurrentCount != t.OriginalCount) modifiedCount++;
                }
            }
            if (modifiedCount > 0)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.4f);
                GUILayout.Label($"{modifiedCount} of {totalTargets} targets modified");
                GUI.color = prev;
            }

            overrideScroll = GUILayout.BeginScrollView(overrideScroll);

            bool needsRefresh = false;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();
                int totalTarget = 0;
                foreach (var t in quest.Targets)
                    totalTarget += t.CurrentCount;
                string progressText = $"[{quest.CompletedCount}/{totalTarget}]";
                GUILayout.Label(new GUIContent(quest.QuestName, quest.QuestName), GUILayout.Width(240));
                var prevColor = GUI.color;
                GUI.color = quest.CompletedCount >= totalTarget ? Color.green : Color.white;
                GUILayout.Label(progressText, GUILayout.Width(60));
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                if (overridesEnabled && GUILayout.Button("Reset", GUILayout.Width(48)))
                {
                    QuestCompletionOverrides.ResetToOriginal(quest.QuestName);
                    foreach (var target in quest.Targets)
                    {
                        string key = $"{quest.QuestName}_{target.TargetIndex}";
                        countInputs.Remove(key);
                    }
                    needsRefresh = true;
                }
                GUILayout.EndHorizontal();

                foreach (var target in quest.Targets)
                {
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    if (!countInputs.ContainsKey(key))
                        countInputs[key] = target.CurrentCount.ToString();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);

                    bool isModified = target.CurrentCount != target.OriginalCount;
                    string label = isModified
                        ? $"{target.DisplayName} ({target.OriginalCount}→{target.CurrentCount})"
                        : $"{target.DisplayName} ({target.OriginalCount})";

                    int maxCap = QuestCompletionOverrides.GetMaxCap(quest.QuestName, target.TargetIndex);
                    if (maxCap > 0)
                        label += $" [max {maxCap}]";

                    GUILayout.Label(label, GUILayout.Width(210));

                    if (!overridesEnabled)
                    {
                        GUILayout.EndHorizontal();
                        continue;
                    }

                    if (quest.Targets.Count > 1)
                    {
                        bool wasEnabled = target.CurrentCount > 0;
                        bool nowEnabled = GUILayout.Toggle(wasEnabled, "", GUILayout.Width(18));
                        if (nowEnabled != wasEnabled)
                        {
                            int newVal = nowEnabled ? target.OriginalCount : 0;
                            QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, newVal);
                            countInputs[key] = newVal.ToString();
                            needsRefresh = true;
                        }
                    }

                    if (GUILayout.Button("−", GUILayout.Width(22)))
                    {
                        int dec = QuestCompletionOverrides.ClampCount(quest.QuestName, target.TargetIndex, target.CurrentCount - 1);
                        QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, dec);
                        countInputs[key] = dec.ToString();
                        needsRefresh = true;
                    }

                    countInputs[key] = GUILayout.TextField(countInputs[key], GUILayout.Width(38));

                    if (GUILayout.Button("+", GUILayout.Width(22)))
                    {
                        int inc = QuestCompletionOverrides.ClampCount(quest.QuestName, target.TargetIndex, target.CurrentCount + 1);
                        QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, inc);
                        countInputs[key] = inc.ToString();
                        needsRefresh = true;
                    }

                    if (GUILayout.Button("Set", GUILayout.Width(32)))
                    {
                        if (int.TryParse(countInputs[key], out int newCount))
                        {
                            QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, newCount);
                            needsRefresh = true;
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();

            if (needsRefresh)
                RefreshOverrides();
        }

        private void ApplyMultiplier(float mult)
        {
            string currentCategory = QuestRegistry.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory)) return;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                foreach (var target in quest.Targets)
                {
                    int raw = Mathf.Max(1, Mathf.RoundToInt(target.OriginalCount * mult));
                    int scaled = raw <= 1 ? 1 : (raw % 2 == 0 ? raw : raw + 1);
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    countInputs[key] = scaled.ToString();
                    QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, scaled);
                }
            }

            RefreshOverrides();
        }

        private void ResetCategory()
        {
            string currentCategory = QuestRegistry.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory)) return;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                QuestCompletionOverrides.ResetToOriginal(quest.QuestName);
                foreach (var target in quest.Targets)
                {
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    countInputs.Remove(key);
                }
            }

            multiplier = 1f;
            multiplierText = "1.0";
            RefreshOverrides();
        }
    }
}
