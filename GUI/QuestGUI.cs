using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI : MonoBehaviour
    {
        private bool show = false;
        private Vector2 questScroll;
        private Vector2 overrideScroll;
        private Rect rect = new Rect(20, 20, 580, 580);
        private int tab = 0;
        private string[] tabs;
        private System.Action[] tabDrawers;
        private int categoryTab = 0;
        private Dictionary<string, string> countInputs = new Dictionary<string, string>();
        private Dictionary<string, List<QuestOverrideInfo>> cachedByCategory = new Dictionary<string, List<QuestOverrideInfo>>();
        private float multiplier = 1f;
        private string multiplierText = "1.0";
        private Vector2 checklistScroll;
        private List<QuestInfo> cachedQuestList;
        private bool questListDirty = true;
        private Vector2 silkSoulScroll;
        private string thresholdInput = "";
        private Dictionary<string, string> pointInputs = new Dictionary<string, string>();

        private void Update()
        {
            if (QuestModPlugin.GuiToggleKey.Value.IsDown())
            {
                show = !show;
                if (show)
                {
                    questListDirty = true;
                    if (tab == 1)
                        RefreshOverrides();
                }
            }
        }

        private void OnGUI()
        {
            if (!show) return;
            GUI.skin = QuestGUISkin.Get();
            var keyName = QuestModPlugin.GuiToggleKey.Value.MainKey.ToString();
            var version = typeof(QuestModPlugin).Assembly.GetName().Version;
            rect = GUI.Window(12345, rect, Draw, $"Quest Manager ({keyName}) v{version.Major}.{version.Minor}.{version.Build}");
        }

        private void RefreshOverrides()
        {
            cachedByCategory.Clear();
            foreach (var cat in QuestRegistry.Categories)
            {
                cachedByCategory[cat] = QuestCompletionOverrides.GetQuestsByCategory(cat);
            }
        }

        private void Draw(int id)
        {
            RebuildTabs();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                var style = (i == tab)
                    ? GUI.skin.GetStyle("ToolbarButtonActive")
                    : GUI.skin.GetStyle("ToolbarButton");
                if (GUILayout.Button(tabs[i], style))
                {
                    if (i != tab && tabs[i] == "Targets")
                        RefreshOverrides();
                    tab = i;
                }
            }
            GUILayout.EndHorizontal();

            if (tab >= tabs.Length) tab = 0;

            GUILayout.Space(6);

            tabDrawers[tab]();

            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                Vector2 mouse = Event.current.mousePosition;
                var content = new GUIContent(GUI.tooltip);
                Vector2 size = QuestGUISkin.TooltipStyle.CalcSize(content);
                size.x = Mathf.Min(size.x, 280);
                size.y = QuestGUISkin.TooltipStyle.CalcHeight(content, size.x);
                float x = Mathf.Min(mouse.x + 12, rect.width - size.x - 10);
                float y = Mathf.Min(mouse.y + 18, rect.height - size.y - 10);
                GUI.Label(new Rect(x, y, size.x, size.y), content, QuestGUISkin.TooltipStyle);
            }

            GUI.DragWindow();
        }

        private void RebuildTabs()
        {
            var names = new System.Collections.Generic.List<string> { "Quests", "Targets", "Checklist" };
            var drawers = new System.Collections.Generic.List<System.Action> { DrawQuestsTab, DrawCompletionTab, DrawChecklistTab };

            if (QuestModPlugin.EnableSilkSoulTab.Value)
            {
                names.Add("Silk & Soul");
                drawers.Add(DrawSilkSoulTab);
            }

            names.Add("⚙ Tools");
            drawers.Add(DrawToolsTab);

            tabs = names.ToArray();
            tabDrawers = drawers.ToArray();
        }
    }
}
