using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyFPSCore.Gameplay
{
    public class LevelExit : MonoBehaviour
    {
        [Header("Prototype flow")]
        [SerializeField] private bool goToMenu = true;     // keep true for prototype
        [SerializeField] private bool showPanel = true;    // show a simple overlay panel
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private float delayToAdvance = 2.0f; // seconds

        bool triggered;
        float t;

        void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player")) return;
            triggered = true;

            // Progress (same as before)
            var gm = GameManager.Instance;
            if (!gm) { SceneManager.LoadScene("MainMenu"); return; }
            gm.UnlockUpTo(gm.CurrentLevelIndex + 1);

            // Optional overlay
            if (showPanel && levelCompletePanel) levelCompletePanel.SetActive(true);

            t = delayToAdvance <= 0 ? 0.01f : delayToAdvance; // tiny buffer if zero
        }

        void Update()
        {
            if (!triggered) return;
            t -= Time.unscaledDeltaTime;
            if (t <= 0f)
            {
                var gm = GameManager.Instance;
                if (goToMenu) gm.ReturnToMenu();
                else gm.LoadLevelByIndex(gm.CurrentLevelIndex + 1); // future-proof if you add level 2
            }
        }
    }
}
