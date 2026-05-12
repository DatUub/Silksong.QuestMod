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

        // Snapshot on open, revert on close without Save. Scalar compare on
        // the hot path -- JSON would look dirty every frame because Dict/Set
        // iteration order isn't deterministic.
        private string _uiOpenSnapshotSaveData;
        private string _uiOpenSnapshotTags;
        private struct SaveDataSnapshot
        {
            public AllWishesMode Mode;
            public bool AllAccepted;
            public bool Initialized;
            public bool Override;
            public string Preset;
            public bool? EnableCustom;
            public bool? EnableRC;
            public int PolicyCount;
            public int InjectedCount;
            public int CompletedCount;
            public int TargetOverrideCount;
            public int WishOverrideCount;
            public bool BypassFleatopia, BypassMandatory, BypassFaydown, BypassNeedolin, BypassBoneboard, BypassAllWalls;
        }
        private SaveDataSnapshot? _scalarSnapshot;

        private void Update()
        {
            if (QuestModPlugin.GuiToggleKey.Value.IsDown())
            {
                if (show) OnGuiClosing(); // closing
                show = !show;
                if (show)
                {
                    OnGuiOpened();
                    questListDirty = true;
                    if (tab == 1)
                        RefreshOverrides();
                }
            }
        }

        private static SaveDataSnapshot? CaptureScalarSnapshot()
        {
            var sd = QuestModPlugin.Instance?.SaveData;
            if (sd == null) return null;
            return new SaveDataSnapshot
            {
                Mode = sd.AllWishesMode,
                AllAccepted = sd.AllQuestsAccepted,
                Initialized = sd.QuestModInitialized,
                Override = sd.OverrideSafetyForThisSave,
                Preset = sd.ActiveDslPreset ?? "vanilla",
                EnableCustom = sd.EnableCustomRequirements,
                EnableRC = sd.EnableFullRemoteComplete,
                PolicyCount = sd.QuestPolicies?.Count ?? 0,
                InjectedCount = sd.InjectedQuests?.Count ?? 0,
                CompletedCount = sd.CompletedQuests?.Count ?? 0,
                TargetOverrideCount = sd.QuestTargetOverrides?.Count ?? 0,
                WishOverrideCount = sd.WishLocationOverrides?.Count ?? 0,
                BypassFleatopia = sd.Prereqs?.BypassFleatopia ?? false,
                BypassMandatory = sd.Prereqs?.BypassMandatoryWishes ?? false,
                BypassFaydown = sd.Prereqs?.BypassFaydownCloak ?? false,
                BypassNeedolin = sd.Prereqs?.BypassNeedolin ?? false,
                BypassBoneboard = sd.Prereqs?.BypassBonebottomQuestBoard ?? false,
                BypassAllWalls = sd.Prereqs?.BypassAllWishwalls ?? false,
            };
        }

        private static bool ScalarSnapshotEquals(SaveDataSnapshot a, SaveDataSnapshot b)
        {
            return a.Mode == b.Mode
                && a.AllAccepted == b.AllAccepted
                && a.Initialized == b.Initialized
                && a.Override == b.Override
                && a.Preset == b.Preset
                && a.EnableCustom == b.EnableCustom
                && a.EnableRC == b.EnableRC
                && a.PolicyCount == b.PolicyCount
                && a.InjectedCount == b.InjectedCount
                && a.CompletedCount == b.CompletedCount
                && a.TargetOverrideCount == b.TargetOverrideCount
                && a.WishOverrideCount == b.WishOverrideCount
                && a.BypassFleatopia == b.BypassFleatopia
                && a.BypassMandatory == b.BypassMandatory
                && a.BypassFaydown == b.BypassFaydown
                && a.BypassNeedolin == b.BypassNeedolin
                && a.BypassBoneboard == b.BypassBoneboard
                && a.BypassAllWalls == b.BypassAllWalls;
        }

        private void OnGuiOpened()
        {
            try
            {
                _uiOpenSnapshotSaveData = QuestModPlugin.ExportSaveDataToJson();
                _uiOpenSnapshotTags = QuestRequirements.IsLoaded ? QuestRequirements.ExportTagsJson() : null;
                _snapshotTagsVersion = QuestRequirements.TagsVersion;
                _scalarSnapshot = CaptureScalarSnapshot();
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"OnGuiOpened snapshot failed: {ex.Message}");
            }
        }

        private void OnGuiClosing()
        {
            try
            {
                bool saveDirty = ScalarDirty();
                bool tagsDirty = _uiOpenSnapshotTags != null
                    && QuestRequirements.IsLoaded
                    && !string.Equals(QuestRequirements.ExportTagsJson(), _uiOpenSnapshotTags,
                        System.StringComparison.Ordinal);
                if (saveDirty && _uiOpenSnapshotSaveData != null)
                {
                    QuestModPlugin.ImportSaveDataFromJson(_uiOpenSnapshotSaveData);
                    QuestModToast.Show("Discarded SaveData edits", new Color(0.9f, 0.7f, 0.4f), 3f);
                }
                if (tagsDirty)
                {
                    QuestRequirements.ImportTagsJson(_uiOpenSnapshotTags);
                    QuestModToast.Show("Discarded tag edits", new Color(0.9f, 0.7f, 0.4f), 3f);
                }
                if (saveDirty || tagsDirty)
                    QuestModPlugin.Log.LogInfo("UI closed without explicit save: reverted to snapshot");
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"OnGuiClosing revert failed: {ex.Message}");
            }
        }

        internal void MarkSaveExplicit()
        {
            try
            {
                _uiOpenSnapshotSaveData = QuestModPlugin.ExportSaveDataToJson();
                _uiOpenSnapshotTags = QuestRequirements.IsLoaded ? QuestRequirements.ExportTagsJson() : null;
                _snapshotTagsVersion = QuestRequirements.TagsVersion;
                _scalarSnapshot = CaptureScalarSnapshot();
            }
            catch { }
            // New baseline. Future edits revert against this.
            _safetyOverrideArmedAt = -1f;
            _resetAllArmedAt = -1f;
            _importArmedAt = -1f;
            _bulkResetArmedAt = -1f;
            _tagsResetArmedAt = -1f;
            QuestModToast.Show("Changes saved", new Color(0.5f, 0.9f, 0.5f), 2.5f);
        }

        // Re-baseline without commit. Used by Reload rules.
        internal void ReplaceOpenSnapshot()
        {
            try
            {
                _uiOpenSnapshotSaveData = QuestModPlugin.ExportSaveDataToJson();
                _uiOpenSnapshotTags = QuestRequirements.IsLoaded ? QuestRequirements.ExportTagsJson() : null;
                _snapshotTagsVersion = QuestRequirements.TagsVersion;
                _scalarSnapshot = CaptureScalarSnapshot();
            }
            catch { }
        }

        // Match = clean without re-serializing Tags.
        private int _snapshotTagsVersion = -1;

        internal bool HasUnsavedChanges()
        {
            try
            {
                // Lazy capture: panel was opened before a save loaded.
                if (!_scalarSnapshot.HasValue && QuestModPlugin.Instance?.SaveData != null)
                {
                    _uiOpenSnapshotSaveData = QuestModPlugin.ExportSaveDataToJson();
                    _uiOpenSnapshotTags = QuestRequirements.IsLoaded ? QuestRequirements.ExportTagsJson() : null;
                    _snapshotTagsVersion = QuestRequirements.TagsVersion;
                    _scalarSnapshot = CaptureScalarSnapshot();
                }
                if (ScalarDirty()) return true;
                if (_uiOpenSnapshotTags != null && QuestRequirements.IsLoaded)
                {
                    // Fast: same TagsVersion = clean.
                    if (QuestRequirements.TagsVersion == _snapshotTagsVersion) return false;
                    // Desynced: full JSON compare and re-stamp.
                    var cur = QuestRequirements.ExportTagsJson();
                    if (!string.Equals(cur, _uiOpenSnapshotTags, System.StringComparison.Ordinal))
                        return true;
                    _snapshotTagsVersion = QuestRequirements.TagsVersion;
                }
            }
            catch { }
            return false;
        }

        private bool ScalarDirty()
        {
            if (!_scalarSnapshot.HasValue) return false;
            var cur = CaptureScalarSnapshot();
            if (!cur.HasValue) return false;
            if (!ScalarSnapshotEquals(_scalarSnapshot.Value, cur.Value)) return true;
            // Count-only misses value edits on existing keys. JSON compare
            // catches the slider tweak case. Only runs when scalars match.
            if (!string.IsNullOrEmpty(_uiOpenSnapshotSaveData))
            {
                try
                {
                    var nowJson = QuestModPlugin.ExportSaveDataToJson();
                    if (!string.Equals(nowJson, _uiOpenSnapshotSaveData, System.StringComparison.Ordinal))
                        return true;
                }
                catch { }
            }
            return false;
        }

        private float GetGuiScale()
        {
            float configScale = QuestModPlugin.GuiScale.Value;
            if (configScale > 0f) return Mathf.Round(configScale * 20f) / 20f;
            float dpi = Screen.dpi;
            if (dpi <= 0f) return 1f;
            return Mathf.Round(Mathf.Clamp(dpi / 96f, 0.5f, 3f) * 20f) / 20f;
        }

        private void OnGUI()
        {
            // Toasts render even when the panel is hidden.
            QuestModToast.DrawAll();

            if (!show) return;

            float scale = GetGuiScale();
            var savedMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

            GUI.skin = QuestGUISkin.Get();
            var keyName = QuestModPlugin.GuiToggleKey.Value.MainKey.ToString();
            var version = typeof(QuestModPlugin).Assembly.GetName().Version;
            rect = GUI.Window(QuestModConstants.GuiWindowId, rect, Draw, $"Quest Manager ({keyName}) v{version.Major}.{version.Minor}.{version.Build}");

            GUI.matrix = savedMatrix;
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
            if (Event.current.type == EventType.Layout)
                GUI.tooltip = "";

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

            // Repaint-only or the tooltip sticks where the cursor last triggered it.
            if (Event.current.type == EventType.Repaint && !string.IsNullOrEmpty(GUI.tooltip))
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

        // Cache tab arrays -- Draw runs per IMGUI event, tabs change rarely.
        private bool _cachedSilkSoulTab = false;

        private void RebuildTabs()
        {
            bool silkSoul = QuestModPlugin.EnableSilkSoulTab.Value;
            if (tabs != null && silkSoul == _cachedSilkSoulTab) return;
            _cachedSilkSoulTab = silkSoul;

            var names = new System.Collections.Generic.List<string> { "Quests", "Targets", "Delivery", "Checklist" };
            var drawers = new System.Collections.Generic.List<System.Action> { DrawQuestsTab, DrawCompletionTab, DrawDeliveryTab, DrawChecklistTab };

            if (silkSoul)
            {
                names.Add("Silk & Soul");
                drawers.Add(DrawSilkSoulTab);
            }

            names.Add("Tags");
            drawers.Add(DrawTagsTab);

            names.Add("Tools");
            drawers.Add(DrawToolsTab);

            tabs = names.ToArray();
            tabDrawers = drawers.ToArray();
        }
    }
}
