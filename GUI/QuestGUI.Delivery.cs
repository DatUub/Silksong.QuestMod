using System.Collections.Generic;
using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        // Delivery category quests + Great Gourmand (Gather quest using delivery items).
        private const string GourmandQuestId = "Great Gourmand";

        private Vector2 deliveryScroll;
        private string gourmandDecayInput = "";

        private void DrawDeliveryTab()
        {
            if (questListDirty)
            {
                cachedQuestList = QuestAcceptance.GetQuestList();
                cachedChainList = QuestAcceptance.GetChainList();
                questListDirty = false;
            }

            deliveryScroll = GUILayout.BeginScrollView(deliveryScroll);

            DrawDeliveryProtectionSection();
            GUILayout.Space(6);
            DrawGourmandTimerSection();
            GUILayout.Space(8);
            DrawDeliveryQuestList();

            GUILayout.EndScrollView();
        }

        private void DrawDeliveryProtectionSection()
        {
            GUILayout.Label("Delivery Items", QuestGUISkin.SectionHeader);
            GUILayout.BeginVertical(GUI.skin.box);

            bool prev = QuestModPlugin.QuestItemInvincible.Value;
            bool next = GUILayout.Toggle(prev, new GUIContent(
                " Quest Item Invincibility",
                "Prevent delivery quest items (Courier Supplies, etc.) from being destroyed by enemy attacks."));
            if (next != prev)
                QuestModPlugin.QuestItemInvincible.Value = next;

            GUILayout.EndVertical();
        }

        private void DrawGourmandTimerSection()
        {
            GUILayout.Label("Great Gourmand: Courier's Rasher Timer", QuestGUISkin.SectionHeader);
            GUILayout.BeginVertical(GUI.skin.box);

            bool prevStop = QuestModPlugin.GourmandStopDecay.Value;
            bool nextStop = GUILayout.Toggle(prevStop, new GUIContent(
                " Stop Rasher Decay",
                "Freeze the Courier's Rasher timer so it never decays while carried."));
            if (nextStop != prevStop)
                QuestModPlugin.GourmandStopDecay.Value = nextStop;

            GUILayout.Space(4);
            float decay = QuestModPlugin.GourmandDecaySeconds.Value;
            if (gourmandDecayInput == "") gourmandDecayInput = decay.ToString("0.#");

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Decay (s):", "Seconds per segment before a durability tick is lost (default 47)."), GUILayout.Width(90));
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                float v = Mathf.Max(1f, decay - 1f);
                QuestModPlugin.GourmandDecaySeconds.Value = v;
                gourmandDecayInput = v.ToString("0.#");
            }
            gourmandDecayInput = GUILayout.TextField(gourmandDecayInput, GUILayout.Width(50));
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                float v = Mathf.Min(600f, decay + 1f);
                QuestModPlugin.GourmandDecaySeconds.Value = v;
                gourmandDecayInput = v.ToString("0.#");
            }
            if (GUILayout.Button("Set", GUILayout.Width(36)))
            {
                if (float.TryParse(gourmandDecayInput, out float v))
                    QuestModPlugin.GourmandDecaySeconds.Value = Mathf.Clamp(v, 1f, 600f);
            }
            if (GUILayout.Button("Reset", GUILayout.Width(48)))
            {
                QuestModPlugin.GourmandDecaySeconds.Value = 47f;
                gourmandDecayInput = "47";
            }
            if (Mathf.Abs(decay - 47f) > 0.01f)
                GUILayout.Label("(default: 47)", GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Vanilla is 8 segments * decay.
            GUILayout.Label($"  Total carry budget: {(decay * 8f):0.#}s (8 × {decay:0.#}s)");

            GUILayout.EndVertical();
        }

        private void DrawDeliveryQuestList()
        {
            if (cachedQuestList == null) return;

            GUILayout.Label("Delivery Quests", QuestGUISkin.SectionHeader);

            var rt = QuestDataAccess.GetRuntimeData();
            var deliveryNames = new List<string>();
            foreach (var pair in QuestRegistry.QuestCategories)
            {
                if (pair.Value == "Delivery")
                    deliveryNames.Add(pair.Key);
            }
            deliveryNames.Add(GourmandQuestId);
            deliveryNames.Sort(System.StringComparer.OrdinalIgnoreCase);

            var lookup = new Dictionary<string, QuestInfo>();
            foreach (var q in cachedQuestList)
                lookup[q.Name] = q;

            int rendered = 0;
            foreach (var name in deliveryNames)
            {
                if (!lookup.TryGetValue(name, out var quest))
                {
                    // Quest not loaded yet -- stub row.
                    quest = new QuestInfo
                    {
                        Name = name,
                        DisplayName = QuestAcceptance.GetDisplayName(name),
                        IsAccepted = false,
                        IsCompleted = false
                    };
                }

                if (QuestModPlugin.OnlyDiscoveredQuests.Value && rt != null && !rt.Contains(name))
                    continue;

                rendered++;
                string status = quest.IsCompleted ? "✓" : (quest.IsAccepted ? "◐" : "○");
                string displayName = QuestModPlugin.ShowQuestDisplayNames.Value ? quest.DisplayName : quest.Name;

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent($"{status} {displayName}", quest.Name), GUILayout.Width(260));
                GUILayout.FlexibleSpace();

                if (!quest.IsAccepted && GUILayout.Button("Accept", GUILayout.Width(55)))
                { QuestAcceptance.AcceptQuest(quest.Name); questListDirty = true; }

                if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Drop", GUILayout.Width(45)))
                { QuestAcceptance.UnacceptQuest(quest.Name); questListDirty = true; }

                if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Complete", GUILayout.Width(65)))
                { QuestAcceptance.CompleteQuest(quest.Name); questListDirty = true; }

                if (quest.IsCompleted && GUILayout.Button("Undo", GUILayout.Width(50)))
                { QuestAcceptance.UncompleteQuest(quest.Name); questListDirty = true; }

                GUILayout.EndHorizontal();
            }

            if (rendered == 0)
                GUILayout.Label("  (no delivery quests discovered yet, visit a courier or quest board)");
        }
    }
}
