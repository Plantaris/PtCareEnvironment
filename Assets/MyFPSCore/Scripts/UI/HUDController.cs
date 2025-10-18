using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using MyFPSCore.Combat;    // Health
using MyFPSCore.Gameplay; // LivesAndRespawn (to read current lives once)
using HealthType = MyFPSCore.Combat.Health;

namespace MyFPSCore.UI
{
    public class HUDController : MonoBehaviour
    {
        private static HUDController _instance;

        [Header("UI Refs")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text healthText;   // shows "current/max"
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text scoreText;    // optional
        [SerializeField] private Image crosshair;

        [Header("Behavior")]
        [SerializeField] private bool hideCrosshairWhenCursorUnlocked = true;
        [SerializeField] private bool logDebug = true;

        private bool _scoreSubscribed;
        private HealthType _health;
        private LivesAndRespawn _livesComp;   // cache lives component
        private bool _subscribed;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            HookScore();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromHealth();
            UnhookScore();
        }

        void Start() => TryBindToPlayer();

        void Update()
        {
            if (!crosshair) return;
            if (!_scoreSubscribed) HookScore();

            bool show = true;
            if (hideCrosshairWhenCursorUnlocked)
                show = MyFPSCore.Gameplay.CursorLockManager.IsLocked;
            if (show && Time.timeScale <= 0f)
                show = false;

            if (crosshair.gameObject.activeSelf != show)
                crosshair.gameObject.SetActive(show);
            if (crosshair.enabled != show)
                crosshair.enabled = show;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBindToPlayer();
            HookScore();   // keep score wired after scene loads
        }

        private void TryBindToPlayer()
        {
            UnsubscribeFromHealth();

            var taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            if (taggedPlayers == null || taggedPlayers.Length == 0)
            {
                if (logDebug) Debug.LogWarning("[HUD] No GameObjects tagged 'Player' found.", this);
                return;
            }

            GameObject chosen = null;
            HealthType found = null;

            foreach (var go in taggedPlayers)
            {
                found = go.GetComponent<HealthType>() ??
                        go.GetComponentInChildren<HealthType>(true) ??
                        go.GetComponentInParent<HealthType>();
                if (found != null) { chosen = go; break; }
            }

            if (found == null)
            {
                foreach (var h in FindObjectsOfType<HealthType>(true))
                {
                    var root = h.transform.root;
                    if (root.CompareTag("Player"))
                    {
                        chosen = root.gameObject;
                        found = h;
                        break;
                    }
                }
            }

            if (found != null)
            {
                _health = found;
                _health.onHealthChanged.AddListener(OnHealthChanged);
                _health.onDied.AddListener(OnPlayerDied); // update lives when death occurs
                _subscribed = true;
                OnHealthChanged(_health.Current, _health.maxHealth);

                if (logDebug)
                    Debug.Log($"[HUD] Bound to Health on '{_health.gameObject.name}' (path: {GetPath(_health.transform)}).", this);

                // Cache Lives component and show initial count
                var rootGO = (chosen ? chosen.transform.root.gameObject : null);
                _livesComp = null;
                if (rootGO != null)
                {
                    _livesComp = rootGO.GetComponentInChildren<LivesAndRespawn>(true);
                    if (_livesComp) SetLives(_livesComp.lives);
                }
            }
            else
            {
                if (logDebug)
                {
                    var names = string.Join(", ", System.Array.ConvertAll(taggedPlayers, go => GetPath(go.transform)));
                    Debug.LogWarning($"[HUD] Found {taggedPlayers.Length} object(s) tagged Player but none had MyFPSCore.Combat.Health. Candidates: {names}", this);
                }
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        private void UnsubscribeFromHealth()
        {
            if (_health && _subscribed)
            {
                _health.onHealthChanged.RemoveListener(OnHealthChanged);
                _health.onDied.RemoveListener(OnPlayerDied);
            }
            _health = null;
            _livesComp = null;
            _subscribed = false;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (healthBar)
            {
                healthBar.minValue = 0f;
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (healthText)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // Called when player Health fires onDied
        private void OnPlayerDied(GameObject _)
        {
            // If LivesAndRespawn decrements lives in its onDied listener,
            // wait one frame so we read the updated value.
            StartCoroutine(RefreshLivesNextFrame());
        }

        private System.Collections.IEnumerator RefreshLivesNextFrame()
        {
            yield return null; // next frame
            if (_livesComp) SetLives(_livesComp.lives);
        }

        // --- optional score hookup ---
        private void HookScore()
        {
            if (_scoreSubscribed) return;
            if (ScoreManager.Instance == null) return;
            if (scoreText == null) return;

            scoreText.text = $"Score: {ScoreManager.Instance.Score}";
            ScoreManager.Instance.onScoreChanged += OnScoreChanged_Score;
            _scoreSubscribed = true;
            if (logDebug) Debug.Log("[HUD] Subscribed to ScoreManager", this);
        }

        private void UnhookScore()
        {
            if (_scoreSubscribed && ScoreManager.Instance != null)
                ScoreManager.Instance.onScoreChanged -= OnScoreChanged_Score;
            _scoreSubscribed = false;
        }

        private void OnScoreChanged_Score(int newScore)
        {
            if (scoreText) scoreText.text = $"Score: {newScore}";
        }

        // ---- Public setters (used by your weapon code) ----
        public void SetLives(int lives) { if (livesText) livesText.text = $"Lives: {lives}"; }
        public void SetAmmo(int current, int reserve)
        {
            if (!ammoText) return;
            if (current < 0 || reserve < 0) { ammoText.text = "∞"; return; }
            ammoText.text = $"{current} / {reserve}";
        }
        public void SetWeapon(string weaponName) { if (weaponText) weaponText.text = weaponName; }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!healthBar) Debug.LogWarning($"{name}: healthBar not assigned", this);
            if (!livesText) Debug.LogWarning($"{name}: livesText not assigned", this);
            if (!ammoText) Debug.LogWarning($"{name}: ammoText not assigned", this);
            if (!weaponText) Debug.LogWarning($"{name}: weaponText not assigned", this);
            if (!crosshair) Debug.LogWarning($"{name}: crosshair not assigned", this);
        }
#endif
    }
}
