using System.Collections;
using UnityEngine;
using MyFPSCore.Inventory;
using MyFPSCore.Interaction;

namespace MyFPSCore.World
{
    /// <summary>
    /// Hinge-driven door that opens on interact (with optional key) and auto-closes after a delay.
    /// Uses RotateTowards at a fixed deg/sec. Works with one or two hinges.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DoorLock_Hinge : MonoBehaviour, IInteractable
    {
        [Header("Lock Settings")]
        [SerializeField] private KeyTypeSO requiredKey;

        [Header("Hinge Settings")]
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private float openAngle = 90f;    // degrees to swing open
        [SerializeField] private float openSpeed = 180f;   // degrees per second

        [Header("Behavior")]
        [SerializeField] private float autoCloseDelay = 10f;
        [SerializeField] private bool disableFrameColliderOnOpen = true;

        private enum DoorPhase { Closed, Opening, Open, Closing }
        private DoorPhase phase = DoorPhase.Closed;

        private Quaternion leftClosed, rightClosed, leftOpen, rightOpen;
        private Collider frameCollider;
        private Coroutine autoCloseCo;

        public string PromptText => phase == DoorPhase.Open || phase == DoorPhase.Opening
            ? ""
            : (requiredKey ? $"Open ({requiredKey.displayName})" : "Open");

        private void Awake()
        {
            frameCollider = GetComponent<Collider>();

            if (leftHinge)
            {
                leftClosed = leftHinge.localRotation;
                leftOpen = leftClosed * Quaternion.Euler(0f, -openAngle, 0f);
            }
            if (rightHinge)
            {
                rightClosed = rightHinge.localRotation;
                rightOpen = rightClosed * Quaternion.Euler(0f, openAngle, 0f);
            }
        }

        public void Interact(GameObject interactorRoot)
        {
            // Only allow starting an open when we're fully closed
            if (phase != DoorPhase.Closed) return;

            bool hasKey = requiredKey == null;
            if (!hasKey)
            {
                var inv = interactorRoot.GetComponent<PlayerInventory>();
                hasKey = inv && inv.HasKey(requiredKey);
            }
            if (!hasKey)
            {
                // TODO: locked SFX/UI
                return;
            }

            phase = DoorPhase.Opening;

            if (disableFrameColliderOnOpen && frameCollider)
                frameCollider.enabled = false;
        }

        private void Update()
        {
            switch (phase)
            {
                case DoorPhase.Opening:
                    TickHingesToward(open: true);
                    if (AtTarget(open: true))
                    {
                        phase = DoorPhase.Open;
                        // (Re)start auto-close timer
                        if (autoCloseCo != null) StopCoroutine(autoCloseCo);
                        autoCloseCo = StartCoroutine(AutoCloseAfterDelay());
                    }
                    break;

                case DoorPhase.Closing:
                    TickHingesToward(open: false);
                    if (AtTarget(open: false))
                    {
                        phase = DoorPhase.Closed;
                        if (disableFrameColliderOnOpen && frameCollider)
                            frameCollider.enabled = true;
                    }
                    break;
            }
        }

        private void TickHingesToward(bool open)
        {
            float step = openSpeed * Time.deltaTime;

            if (leftHinge)
            {
                var target = open ? leftOpen : leftClosed;
                leftHinge.localRotation = Quaternion.RotateTowards(leftHinge.localRotation, target, step);
            }
            if (rightHinge)
            {
                var target = open ? rightOpen : rightClosed;
                rightHinge.localRotation = Quaternion.RotateTowards(rightHinge.localRotation, target, step);
            }
        }

        private bool AtTarget(bool open)
        {
            const float doneDeg = 0.5f; // tolerance in degrees
            bool leftOk = !leftHinge || Quaternion.Angle(leftHinge.localRotation, open ? leftOpen : leftClosed) <= doneDeg;
            bool rightOk = !rightHinge || Quaternion.Angle(rightHinge.localRotation, open ? rightOpen : rightClosed) <= doneDeg;
            return leftOk && rightOk;
        }

        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            // Only close if not already closing/closed
            if (phase == DoorPhase.Open)
                phase = DoorPhase.Closing;

            autoCloseCo = null;
        }

        /// <summary>Call this from other scripts if you ever need to force-close immediately.</summary>
        public void ForceClose()
        {
            if (autoCloseCo != null) StopCoroutine(autoCloseCo);
            phase = DoorPhase.Closing;
        }
    }
}

