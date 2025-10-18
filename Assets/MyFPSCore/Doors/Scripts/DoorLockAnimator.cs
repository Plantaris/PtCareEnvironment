using System.Collections; // for IEnumerator and coroutines
using UnityEngine;
using MyFPSCore.Inventory;
using MyFPSCore.Interaction;

namespace MyFPSCore.World
{
    [RequireComponent(typeof(Collider))]
    public class DoorLockAnimator : MonoBehaviour, IInteractable
    {
        [Header("Lock")]
        [SerializeField] private KeyTypeSO requiredKey;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float autoCloseDelay = 10f;
        [SerializeField] private bool disableFrameColliderOnOpen = true;

        [Header("Feedback")]
        [SerializeField] private AudioSource lockedAudio;   // optional "locked" sound
        [SerializeField] private AudioSource unlockedAudio; // optional "unlocked" sound

        // Animator parameters
        private static readonly int OpenTrig = Animator.StringToHash("MyFPSCore_OpenDoor");
        private static readonly int CloseTrig = Animator.StringToHash("MyFPSCore_CloseDoor");

        private bool isOpen;
        private Collider frameCollider;
        private Coroutine closeRoutine;

        // Updated prompt logic
        public string PromptText
        {
            get
            {
                if (isOpen) return "";

                if (requiredKey != null)
                {
                    var playerInv = Object.FindObjectOfType<PlayerInventory>();
                    if (playerInv != null && !playerInv.HasKey(requiredKey))
                    {
                        return $"Locked - Need {requiredKey.displayName}";
                    }
                    return $"Open ({requiredKey.displayName})";
                }

                return "Open";
            }
        }

        void Awake()
        {
            frameCollider = GetComponent<Collider>();
            if (!animator) animator = GetComponent<Animator>();
        }

        public void Interact(GameObject interactorRoot)
        {
            if (isOpen) return;

            var hasKey = requiredKey == null;
            if (!hasKey)
            {
                var inv = interactorRoot.GetComponent<PlayerInventory>();
                hasKey = inv && inv.HasKey(requiredKey);
            }

            if (!hasKey)
            {
                // Locked feedback
                if (lockedAudio != null)
                    lockedAudio.Play();
                return;
            }

            // Play unlocked sound before opening
            if (unlockedAudio != null)
                unlockedAudio.Play();

            // Open
            isOpen = true;
            animator.SetTrigger(OpenTrig);

            if (disableFrameColliderOnOpen && frameCollider)
                frameCollider.enabled = false;

            // (Re)start auto close
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(AutoCloseAfterDelay());
        }

        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            animator.SetTrigger(CloseTrig);
            isOpen = false;
            if (disableFrameColliderOnOpen && frameCollider)
                frameCollider.enabled = true;
            closeRoutine = null;
        }
    }
}
