
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using GM = MyFPSCore.Gameplay.GameManager;


namespace MyFPSCore.Gameplay
{

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scenes")]
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private string[] levelSceneNames = { "Level1", "Level2", "Level3" };

        [Header("Progress")]
        [SerializeField] private string prefsKey = "UnlockedLevel";
        [SerializeField] private int defaultUnlocked = 1; // Level 1 unlocked on fresh start

        // 1-based index of the current level (1 == Level1). -1 means we’re not in a listed level.
        public int CurrentLevelIndex { get; private set; } = -1;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int idx = global::System.Array.FindIndex(levelSceneNames, n => n == scene.name);
            CurrentLevelIndex = (idx >= 0) ? idx + 1 : -1;
        }

        // ---------- Progress ----------
        public int GetUnlockedLevel() => PlayerPrefs.GetInt(prefsKey, defaultUnlocked);

        public void UnlockUpTo(int levelIndex)
        {
            int current = GetUnlockedLevel();
            if (levelIndex > current)
            {
                PlayerPrefs.SetInt(prefsKey, levelIndex);
                PlayerPrefs.Save();
            }
        }

        public void ResetProgress() => PlayerPrefs.DeleteKey(prefsKey);

        // ---------- Loading ----------
        public void StartNewGame() => LoadLevelByIndex(1);

        public void LoadLevelByIndex(int levelIndex)
        {
            if (levelIndex < 1 || levelIndex > levelSceneNames.Length)
            {
                Debug.LogWarning($"Level index {levelIndex} out of range.");
                return;
            }
            SceneManager.LoadScene(levelSceneNames[levelIndex - 1]);
        }

        public void LoadLevelByName(string sceneName)
        {
            if (!levelSceneNames.Contains(sceneName))
                Debug.LogWarning($"'{sceneName}' not listed in GameManager.levelSceneNames.");
            SceneManager.LoadScene(sceneName);
        }

        public void LoadNextLevel()
        {
            if (CurrentLevelIndex < 1)
            {
                Debug.LogWarning("LoadNextLevel called but we’re not currently in a managed level.");
                return;
            }
            int next = CurrentLevelIndex + 1;
            if (next <= levelSceneNames.Length) LoadLevelByIndex(next);
            else ReturnToMenu();
        }

        public void ReloadLevel()
        {
            if (CurrentLevelIndex < 1)
            {
                Debug.LogWarning("ReloadLevel called but we’re not currently in a managed level.");
                return;
            }
            LoadLevelByIndex(CurrentLevelIndex);
        }

        public void ReturnToMenu() => SceneManager.LoadScene(menuSceneName);
    }
}
