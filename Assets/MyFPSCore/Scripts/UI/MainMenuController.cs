using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MyFPSCore.Gameplay;           // we call GameManager in Gameplay
using GM = MyFPSCore.Gameplay.GameManager;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;  // only used in OnValidate
#endif

namespace MyFPSCore.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [global::System.Serializable]
        public class LevelButton
        {
            public Button button;
            public string sceneName;
            public int requiredLevelIndex = 1;
            public GameObject lockOverlay;
        }

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject gameinfoPanel;
        [SerializeField] private GameObject levelselectPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("First Selected (optional)")]
        [SerializeField] private Selectable firstSelectedMain;
        [SerializeField] private Selectable firstSelectedOptions;

        [Header("Level Select")]
        [SerializeField] private List<LevelButton> levelButtons = new List<LevelButton>();

        void OnEnable()
        {
            ShowMain();
            RefreshLevelButtons();
        }

        int GetUnlockedLevel()
        {
            if (GM.Instance != null) return GM.Instance.GetUnlockedLevel();
            return PlayerPrefs.GetInt("UnlockedLevel", 1); // fallback
        }

        void RefreshLevelButtons()
        {
            int unlocked = GetUnlockedLevel();
            foreach (var lb in levelButtons)
            {
                if (lb == null || lb.button == null) continue;

                bool isUnlocked = unlocked >= lb.requiredLevelIndex;
                lb.button.interactable = isUnlocked;
                if (lb.lockOverlay) lb.lockOverlay.SetActive(!isUnlocked);

                lb.button.onClick.RemoveAllListeners();
                if (isUnlocked)
                {
                    string target = lb.sceneName;
                    lb.button.onClick.AddListener(() => PlaySpecificLevel(target));
                }
            }
        }

        void ShowMain()
        {
            mainPanel.SetActive(true);
            creditsPanel.SetActive(false);
            gameinfoPanel.SetActive(false);
            levelselectPanel.SetActive(false);
            optionsPanel.SetActive(false);

            if (firstSelectedMain) EventSystem.current.SetSelectedGameObject(firstSelectedMain.gameObject);
        }

        // New Game
        public void Play()
        {
            var gm = GM.Instance;
            if (gm != null) gm.StartNewGame();
            else Debug.LogWarning("GameManager not found in scene.");
        }

        // Level-select buttons
        public void PlaySpecificLevel(string sceneName)
        {
            var gm = GM.Instance;
            if (gm != null) gm.LoadLevelByName(sceneName);
            else Debug.LogWarning("GameManager not found in scene.");
        }

        public void OpenOptions()
        {
            optionsPanel.SetActive(true);
            mainPanel.SetActive(false);
            gameinfoPanel.SetActive(false);
            levelselectPanel.SetActive(false);
            if (firstSelectedOptions) EventSystem.current.SetSelectedGameObject(firstSelectedOptions.gameObject);
        }

        public void OpenLevelSelect()
        {
            levelselectPanel.SetActive(true);
            mainPanel.SetActive(false);
            creditsPanel.SetActive(false);
            gameinfoPanel.SetActive(false);
            optionsPanel.SetActive(false);
            RefreshLevelButtons();
            if (firstSelectedOptions) EventSystem.current.SetSelectedGameObject(firstSelectedOptions.gameObject);
        }

        public void OpenGameInfo()
        {
            gameinfoPanel.SetActive(true);
            mainPanel.SetActive(false);
            creditsPanel.SetActive(false);
            levelselectPanel.SetActive(false);
            optionsPanel.SetActive(false);
            if (firstSelectedOptions) EventSystem.current.SetSelectedGameObject(firstSelectedOptions.gameObject);
        }

        public void OpenCredits()
        {
            creditsPanel.SetActive(true);
            mainPanel.SetActive(false);
            gameinfoPanel.SetActive(false);
            levelselectPanel.SetActive(false);
            optionsPanel.SetActive(false);
            if (firstSelectedOptions) EventSystem.current.SetSelectedGameObject(firstSelectedOptions.gameObject);
        }

        public void Back()
        {
            mainPanel.SetActive(true);
            creditsPanel.SetActive(false);
            optionsPanel.SetActive(false);
            gameinfoPanel.SetActive(false);
            levelselectPanel.SetActive(false);
            if (firstSelectedMain) EventSystem.current.SetSelectedGameObject(firstSelectedMain.gameObject);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Only run if we're in the Main Menu scene
            if (SceneManager.GetActiveScene().name != "MainMenu") return;

            if (!mainPanel) Debug.LogWarning($"{name}: mainPanel not assigned", this);
            if (!optionsPanel) Debug.LogWarning($"{name}: optionsPanel not assigned", this);
            if (!gameinfoPanel) Debug.LogWarning($"{name}: gameinfoPanel not assigned", this);
            if (!levelselectPanel) Debug.LogWarning($"{name}: levelselectPanel not assigned", this);
            if (!creditsPanel) Debug.LogWarning($"{name}: creditsPanel not assigned", this);
            if (!firstSelectedMain) Debug.LogWarning($"{name}: firstSelectedMain not assigned", this);
            if (!firstSelectedOptions) Debug.LogWarning($"{name}: firstSelectedOptions not assigned", this);
        }

        // Testing-only util
        public void ResetProgressForTesting()
        {
            var gm = GM.Instance;
            if (gm != null) gm.ResetProgress();
            else PlayerPrefs.DeleteKey("UnlockedLevel");

            PlayerPrefs.Save();
            Debug.Log("Level progress reset to default.");
            RefreshLevelButtons();
        }
#endif
    }
}
