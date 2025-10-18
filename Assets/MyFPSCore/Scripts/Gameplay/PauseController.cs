using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;                // NEW

namespace MyFPSCore.Gameplay
{
    public class PauseController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] GameObject pausePanel;

        [Header("(Optional) Buttons")]              // NEW – drag these if you prefer
        [SerializeField] Button btnResume;          // NEW
        [SerializeField] Button btnQuitToMenu;      // NEW

        [Header("Input")]
        [SerializeField] KeyCode pauseKey = KeyCode.Escape;

        bool paused;

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryFindPausePanel();
            BindButtons();                          // NEW
            if (pausePanel) pausePanel.SetActive(false);

            StartCoroutine(RebindPausePanelNextFrame());  // NEW
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            TryFindPausePanel();
            BindButtons();                          // NEW
            if (pausePanel) pausePanel.SetActive(false);

            StartCoroutine(RebindPausePanelNextFrame());  // NEW
        }

        void Update()
        {
            if (pauseKey != KeyCode.None && Input.GetKeyDown(pauseKey))
                TogglePause();
        }

        public void TogglePause() { if (paused) Resume(); else Pause(); }

        public void Pause()
        {
            Debug.Log("[PauseController] Pause()");
            paused = true;
            Time.timeScale = 0f;
            if (pausePanel)
            {
                pausePanel.SetActive(true);

                // ensure it can receive clicks
                var cg = pausePanel.GetComponent<CanvasGroup>();
                if (!cg) cg = pausePanel.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;

                // put keyboard/gamepad focus on Resume (optional but helpful)
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null)
                {
                    var resumeBtn = pausePanel.transform.Find("ResumeButton");
                    if (resumeBtn) es.SetSelectedGameObject(resumeBtn.gameObject);
                }
            }
            CursorLockManager.PauseForUI();
            AudioListener.pause = true;
        }

        public void ReturnToMainMenu()
        {
            paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CursorLockManager.PauseForUI();
            SceneManager.LoadScene("MainMenu");
        }

        public void Resume()
        {
            Debug.Log("[PauseController] Resume()");
            paused = false;
            Time.timeScale = 1f;
            if (pausePanel)
            {
                // stop the panel from intercepting raycasts once hidden
                var cg = pausePanel.GetComponent<CanvasGroup>();
                if (cg) { cg.interactable = false; cg.blocksRaycasts = false; }
                pausePanel.SetActive(false);
            }
            CursorLockManager.ResumeFromUI();
            AudioListener.pause = false;
        }

        // --- BULLETPROOF find (marker-based) + fallback-by-name (already in your script) ---
        void TryFindPausePanel()
        {
            if (pausePanel && pausePanel.scene.IsValid()) return;

            var activeScene = SceneManager.GetActiveScene();

            var markers = Resources.FindObjectsOfTypeAll<PausePanelMarker>();
            foreach (var m in markers)
            {
                if (!m) continue;
                var go = m.gameObject;
                if (!go.scene.IsValid()) continue;
                if (go.scene != activeScene) continue;
                pausePanel = go;
                return;
            }
            foreach (var root in activeScene.GetRootGameObjects())
            {
                var t = FindChildRecursive(root.transform, "PausePanel");
                if (t != null) { pausePanel = t.gameObject; return; }
            }
        }

        Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found) return found;
            }
            return null;
        }

        // NEW: rebind one frame later (handles UIs that spawn in Start)
        System.Collections.IEnumerator RebindPausePanelNextFrame()
        {
            yield return null;
            TryFindPausePanel();
            BindButtons();                          // NEW
            if (pausePanel) pausePanel.SetActive(false);
        }

        // NEW: ensure buttons always point to THIS instance
        void BindButtons()
        {
            if (!pausePanel) return;

            // If you didn’t drag them, try to find by child names:
            if (!btnResume)
                btnResume = pausePanel.transform.Find("Resume")?.GetComponent<Button>();     // rename if needed
            if (!btnQuitToMenu)
                btnQuitToMenu = pausePanel.transform.Find("QuitToMenu")?.GetComponent<Button>(); // rename if needed

            if (btnResume)
            {
                btnResume.onClick.RemoveAllListeners();
                btnResume.onClick.AddListener(Resume);
            }
            if (btnQuitToMenu)
            {
                btnQuitToMenu.onClick.RemoveAllListeners();
                btnQuitToMenu.onClick.AddListener(ReturnToMainMenu);
            }

            // Make sure the panel can receive clicks
            var cg = pausePanel.GetComponentInParent<CanvasGroup>();
            if (cg) { cg.interactable = true; cg.blocksRaycasts = true; }
        }
    }
}
