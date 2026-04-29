using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuestMod
{
    // Cluster X: in GUI tag editor. Writes back to
    // BepInEx/config/QuestMod/QuestRequirements.user.json on every change.
    public partial class QuestGUI
    {
        private Vector2 _tagsScroll;
        private string _tagInput = "";
        private string _tagsFilter = "";
        private int _tagsCategoryTab = 0;

        // Stable per-tag color. Hash the tag name to a hue, fixed S/V so the
        // pill is always readable. Same tag always renders the same color
        // across rows so the eye can group them visually.
        private static Color TagColor(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return Color.gray;
            unchecked
            {
                uint h = 2166136261;
                foreach (var c in tag) { h ^= c; h *= 16777619; }
                float hue = (h % 360u) / 360f;
                return Color.HSVToRGB(hue, 0.55f, 0.85f);
            }
        }

        private void DrawTagsTab()
        {
            if (!QuestRequirements.IsLoaded)
            {
                GUILayout.Label("Custom requirements not loaded yet.");
                return;
            }

            GUILayout.Label("Tags drive cluster I bulk presets and conditions. Edits persist to QuestRequirements.user.json.");

            // Cluster X: revert helpers. Per session revert lives at the
            // GUI close gate (closing without Save reverts edits). Here we
            // expose a permanent "wipe user overlay" path so the user can
            // drop back to embedded baseline tags entirely.
            DrawTagsResetRow();

            var allTags = QuestRequirements.GetAllUniqueTags().OrderBy(t => t).ToList();

            GUILayout.BeginHorizontal();
            GUILayout.Label("New tag:", GUILayout.Width(60));
            _tagInput = GUILayout.TextField(_tagInput ?? "", GUILayout.MinWidth(120));
            GUILayout.Label(new GUIContent("(type, then click [+ new] on any quest below)",
                "The text in this box is what the [+ new] button on each quest adds. Existing tags get a one click button per quest."));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(60));
            _tagsFilter = GUILayout.TextField(_tagsFilter ?? "", GUILayout.MinWidth(120));
            if (GUILayout.Button("Clear", GUILayout.Width(50))) _tagsFilter = "";
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{allTags.Count} unique tag(s)");
            GUILayout.EndHorizontal();

            if (QuestRegistry.Categories != null && QuestRegistry.Categories.Length > 0)
                _tagsCategoryTab = GUILayout.Toolbar(_tagsCategoryTab, QuestRegistry.Categories);

            string activeCategory = (QuestRegistry.Categories != null && _tagsCategoryTab < QuestRegistry.Categories.Length)
                ? QuestRegistry.Categories[_tagsCategoryTab] : null;

            _tagsScroll = GUILayout.BeginScrollView(_tagsScroll);

            var quests = QuestRegistry.DisplayNames.Keys
                .Where(q => activeCategory == null
                            || (QuestRegistry.QuestCategories.TryGetValue(q, out var c) && c == activeCategory))
                .Where(q => string.IsNullOrEmpty(_tagsFilter)
                            || q.IndexOf(_tagsFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(q => q)
                .ToList();

            foreach (var quest in quests) DrawQuestTagRow(quest, allTags);

            GUILayout.EndScrollView();
        }

        private void DrawQuestTagRow(string quest, List<string> allTags)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            string display = QuestModPlugin.ShowQuestDisplayNames.Value
                ? QuestAcceptance.GetDisplayName(quest) : quest;
            GUILayout.Label(display, QuestGUISkin.SectionHeader);

            HashSet<string> currentTags;
            QuestRequirements.Tags.TryGetValue(quest, out currentTags);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tags:", GUILayout.Width(40));
            if (currentTags == null || currentTags.Count == 0)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label("(none)");
                GUI.color = Color.white;
            }
            else
            {
                foreach (var tag in currentTags.OrderBy(t => t).ToList())
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = TagColor(tag);
                    if (GUILayout.Button($"{tag}  x", GUILayout.ExpandWidth(false)))
                        QuestRequirements.RemoveTag(quest, tag);
                    GUI.backgroundColor = prevBg;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Add:", GUILayout.Width(40));

            int shown = 0;
            foreach (var tag in allTags)
            {
                if (currentTags != null && currentTags.Contains(tag)) continue;
                if (shown++ >= 8) break;
                var prevBg2 = GUI.backgroundColor;
                // Dim the suggestion color so the active pills above stand out.
                GUI.backgroundColor = Color.Lerp(TagColor(tag), Color.white, 0.45f);
                if (GUILayout.Button($"+ {tag}", GUILayout.ExpandWidth(false)))
                    QuestRequirements.AddTag(quest, tag);
                GUI.backgroundColor = prevBg2;
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_tagInput)
                && (currentTags == null || !currentTags.Contains(_tagInput.Trim()));
            if (GUILayout.Button(string.IsNullOrWhiteSpace(_tagInput) ? "+ new" : $"+ new ({_tagInput.Trim()})",
                GUILayout.ExpandWidth(false)))
            {
                QuestRequirements.AddTag(quest, _tagInput.Trim());
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private float _tagsResetArmedAt = -1f;
        private const float TagsResetArmWindow = 4f;

        private void DrawTagsResetRow()
        {
            bool armed = _tagsResetArmedAt > 0f
                && (Time.realtimeSinceStartup - _tagsResetArmedAt) < TagsResetArmWindow;
            GUILayout.BeginHorizontal();
            if (!armed)
            {
                if (GUILayout.Button(new GUIContent("Reset all tag overrides...",
                    "Wipe the user overlay's tags section and reload from embedded baseline. Permanent."),
                    GUILayout.ExpandWidth(false)))
                {
                    _tagsResetArmedAt = Time.realtimeSinceStartup;
                }
            }
            else
            {
                var remaining = TagsResetArmWindow - (Time.realtimeSinceStartup - _tagsResetArmedAt);
                GUI.color = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button(string.Format("Confirm: wipe overrides [{0:0.0}s]", remaining),
                    GUILayout.ExpandWidth(false)))
                {
                    QuestRequirements.ClearAllTagOverrides();
                    _tagsResetArmedAt = -1f;
                    QuestModToast.Show("Tag overrides cleared", new Color(0.9f, 0.7f, 0.4f), 2.5f);
                }
                GUI.color = Color.white;
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false))) _tagsResetArmedAt = -1f;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }
}
