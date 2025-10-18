using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyFPSCore.Gameplay
{
    public class LevelExitPrototype : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject levelCompletePanel;   // assign (disabled by default)

        [Header("Player Control (optional)")]
        [SerializeField] private Behaviour playerMovementToDisable;  // e.g., your FPS controller
        [SerializeField] private bool lockCursorOnComplete = true;
        [SerializeField] private bool freezeTime = false;

        bool triggered;

        void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player")) return;
            triggered = true;

            var gm = GameManager.Instance;
            if (gm) gm.UnlockUpTo(gm.CurrentLevelIndex + 1);

            if (playerMovementToDisable) playerMovementToDisable.enabled = false;
            if (lockCursorOnComplete) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (freezeTime) Time.timeScale = 0f;

            if (levelCompletePanel) levelCompletePanel.SetActive(true);
            Debug.Log("[LevelExitPrototype] Panel shown, waiting for button.");
        }

        // --- Button hooks ---
        public void OnClickMainMenu()
        {
            RestoreTime();
            var gm = GameManager.Instance;
            if (gm) gm.ReturnToMenu(); else SceneManager.LoadScene("MainMenu");
        }

        public void OnClickRestartLevel()
        {
            RestoreTime();
            var gm = GameManager.Instance;
            if (gm) gm.LoadLevelByIndex(gm.CurrentLevelIndex);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnClickQuitGame()
        {
            RestoreTime();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void RestoreTime() { if (freezeTime) Time.timeScale = 1f; }
    }
}

