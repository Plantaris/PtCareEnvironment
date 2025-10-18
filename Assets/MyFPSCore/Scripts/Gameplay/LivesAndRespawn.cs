using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyFPSCore.Combat;   // Health
using MyFPSCore.Player;   // BetterFPSController
using UnityEngine.SceneManagement;

namespace MyFPSCore.Gameplay
{
    public class LivesAndRespawn : MonoBehaviour
    {
        [Header("Lives")]
        public int lives = 3;

        [Header("Respawn")]
        public Transform respawnPoint;
        public float respawnDelay = 1.0f;
        public float invulnOnRespawn = 1.5f;

        [Header("Game Over UI (simple panel)")]
        public GameObject gameOverPanel;      // assign your GameOverPanel object
        public TMP_Text titleText;            // optional
        public TMP_Text subtitleText;         // optional
        public Button restartButton;          // "New Game"
        public Button mainMenuButton;         // optional
        public string mainMenuScene = "";     // set to enable Main Menu

        [Header("Behavior")]
        public bool pauseOnGameOver = true;

        Health health;
        CharacterController cc;
        BetterFPSController fp;
        bool gameOver;

        void Awake()
        {
            health = GetComponent<Health>();
            cc = GetComponent<CharacterController>();
            fp = GetComponent<BetterFPSController>();

            if (!health) { Debug.LogError("[LivesAndRespawn] No Health found."); enabled = false; return; }
            health.destroyOnDeath = false; // reuse player object
            health.onDied.AddListener(OnDied);

            // ensure panel is hidden at start
            if (gameOverPanel) gameOverPanel.SetActive(false);
        }

        void OnDestroy() { if (health) health.onDied.RemoveListener(OnDied); }

        void OnDied(GameObject _) { if (!gameOver) StartCoroutine(RespawnRoutine()); }

        IEnumerator RespawnRoutine()
        {
            // consume life now
            lives = Mathf.Max(0, lives - 1);

            if (lives <= 0)
            {
                yield return StartCoroutine(GameOverRoutine());
                yield break;
            }

            yield return new WaitForSeconds(respawnDelay);

            // safe teleport
            if (cc) cc.enabled = false;
            if (respawnPoint) transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            if (cc) cc.enabled = true;
            if (fp) fp.ResetMotion();

            // revive to full + brief i-frames (works from 0 HP)
            health.Respawn(invulnOnRespawn);
        }

        IEnumerator GameOverRoutine()
        {
            gameOver = true;

            // freeze player control
            if (fp) fp.enabled = false;
            if (cc) cc.enabled = false;

            // unlock cursor for UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // show panel
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (titleText) titleText.text = "GAME OVER";
            if (subtitleText) subtitleText.text = "You have been permanently subdued.";

            // hook buttons
            if (restartButton)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() =>
                {
                    if (pauseOnGameOver) Time.timeScale = 1f;
                    Scene current = SceneManager.GetActiveScene();
                    SceneManager.LoadScene(current.buildIndex);
                });
            }
            if (mainMenuButton)
            {
                bool hasMenu = !string.IsNullOrEmpty(mainMenuScene);
                mainMenuButton.gameObject.SetActive(hasMenu);
                if (hasMenu)
                {
                    mainMenuButton.onClick.RemoveAllListeners();
                    mainMenuButton.onClick.AddListener(() =>
                    {
                        if (pauseOnGameOver) Time.timeScale = 1f;
                        SceneManager.LoadScene(mainMenuScene);
                    });
                }
            }

            if (pauseOnGameOver) Time.timeScale = 0f;

            // stay here until a button is pressed (UI still works while paused)
            yield break;
        }
    }
}
