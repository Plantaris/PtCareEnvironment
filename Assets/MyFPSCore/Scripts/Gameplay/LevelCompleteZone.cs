using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyFPSCore.Gameplay
{
    public class LevelCompleteZone : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("Tag that must be on the entering object.")]
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool destroyAfterUse = false;

        [Header("Flow")]
        [Tooltip("If true, advance automatically after 'advanceDelay' seconds. If false, wait for 'advanceKey'.")]
        [SerializeField] private bool autoAdvance = true;
        [SerializeField] private float advanceDelay = 2.5f;
        [SerializeField] private KeyCode advanceKey = KeyCode.E;

        [Header("Destination")]
        [Tooltip("If true, return to Main Menu (prototype). If false, load next level index.")]
        [SerializeField] private bool goToMenu = true;

        [Header("UI (optional)")]
        [Tooltip("Optional panel to show when level completes (e.g., a Canvas panel).")]
        [SerializeField] private GameObject levelCompletePanel;
        [Tooltip("Optional helper to set headline/subtitle copy.")]
        [SerializeField] private LevelCompleteUI uiHelper;
        [TextArea][SerializeField] private string headline = "Level Complete";
        [TextArea][SerializeField] private string subtitle = "Prototype alpha test — more levels to come.";

        [Header("Audio/FX (optional)")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private ParticleSystem fx;

        // Input locking hook (optional, if you have a central pause/input manager)
        [Header("Player Control (optional)")]
        [Tooltip("If assigned, we will disable this component on complete (e.g., your FPS controller).")]
        [SerializeField] private Behaviour playerMovementToDisable;
        [Tooltip("Also lock cursor + stop look input if applicable.")]
        [SerializeField] private bool lockCursorOnComplete = true;

        bool triggered;
        float timer;

        void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag(requiredTag)) return;

            triggered = true;

            // Optional: try to auto-detect a controller on the entering object if not wired.
            if (!playerMovementToDisable)
                playerMovementToDisable = other.GetComponentInChildren<Behaviour>();

            // Show UI
            if (levelCompletePanel) levelCompletePanel.SetActive(true);
            if (uiHelper) uiHelper.SetCopy(headline, subtitle);

            // Play FX/SFX
            if (fx) fx.Play();
            if (sfx) sfx.Play();

            // Lock player
            if (playerMovementToDisable) playerMovementToDisable.enabled = false;
            if (lockCursorOnComplete)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Progress: unlock next level
            var gm = GameManager.Instance;
            gm.UnlockUpTo(gm.CurrentLevelIndex + 1);

            // Auto advance countdown (or wait-for-key in Update)
            timer = advanceDelay;

            // Optional: prevent re-trigger spam
            if (destroyAfterUse)
            {
                // Disable collider but let script run
                var col = GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }

        void Update()
        {
            if (!triggered) return;

            if (autoAdvance)
            {
                timer -= Time.unscaledDeltaTime;
                if (timer <= 0f)
                    Advance();
            }
            else
            {
                if (advanceKey != KeyCode.None && Input.GetKeyDown(advanceKey))
                    Advance();
            }
        }

        void Advance()
        {
            // Route to menu or next level
            var gm = GameManager.Instance;
            if (goToMenu) gm.ReturnToMenu();
            else gm.LoadLevelByIndex(gm.CurrentLevelIndex + 1);
        }

        // Helpful visual in Scene view
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.35f);
            var c = GetComponent<Collider>();
            if (c is BoxCollider bc)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(bc.center, bc.size);
            }
            else if (c is SphereCollider sc)
            {
                Gizmos.DrawSphere(transform.TransformPoint(sc.center), sc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
            }
        }
    }
}
