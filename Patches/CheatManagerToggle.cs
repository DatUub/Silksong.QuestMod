using UnityEngine.SceneManagement;

namespace QuestMod
{
    public static class CheatManagerToggle
    {
        public static void Initialize()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            QuestModPlugin.Log.LogInfo("CheatManagerToggle initialized");
        }

        private static void OnSceneChanged(Scene from, Scene to)
        {
            SyncFlags();
        }

        public static void SyncFlags()
        {
            if (QuestModPlugin.AllQuestsAvailable)
            {
                CheatManager.ShowAllQuestBoardQuest = true;
                CheatManager.ShowAllCompletionIcons = true;
            }
            else
            {
                CheatManager.ShowAllQuestBoardQuest = false;
                CheatManager.ShowAllCompletionIcons = false;
            }
        }
    }
}
