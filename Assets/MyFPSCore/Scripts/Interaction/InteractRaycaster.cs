using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MyFPSCore.Interaction
{
    public class InteractRaycaster : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera cam;                // assign your active camera
        [SerializeField] private GameObject interactorRoot; // drag your Player root here

        [Header("Settings")]
        [SerializeField] private float range = 3f;
        [SerializeField] private LayerMask interactMask;    // set to Interactable in Inspector

        [Header("UI")]
        [SerializeField] private TMP_Text promptText;       // TMP Text element

        [SerializeField] float interactCooldown = 0.2f;
        float lastInteractTime = -999f;

        [SerializeField] private string fallbackInteractLayer = "Interactable";
        [SerializeField] private bool includeDefaultInInteract = true;
        [SerializeField] private bool warnWhenAutoSet = true;

        private void Awake()
        {
            // Prefer explicitly assigned camera; otherwise use the scene's Main Camera
            if (!cam) cam = Camera.main;
            if (!cam) cam = FindObjectOfType<Camera>(); // last resort

            if (!interactorRoot) interactorRoot = transform.root.gameObject; // fallback

            // Auto-assign interactMask if it is empty
            if (interactMask.value == 0)
            {
                int i = LayerMask.NameToLayer(fallbackInteractLayer);
                if (i >= 0) interactMask |= 1 << i;

                if (includeDefaultInInteract)
                {
                    int d = LayerMask.NameToLayer("Default");
                    if (d >= 0) interactMask |= 1 << d;
                }

                if (interactMask.value == 0)
                    interactMask = Physics.DefaultRaycastLayers; // last resort

                if (warnWhenAutoSet)
                    Debug.LogWarning($"[InteractRaycaster] interactMask was empty; auto-set to {LayerMaskToString(interactMask)}");
            }
        }


        private void Update()
        {
            if (!cam)
            {
                cam = Camera.main;
                if (!cam)
                {
                    if (promptText) { promptText.text = ""; promptText.enabled = false; }
                    return;
                }
            }

            if (!interactorRoot) interactorRoot = transform.root.gameObject;

            if (promptText) { promptText.text = ""; promptText.enabled = false; }

            // Always draw the full-length debug ray
#if UNITY_EDITOR
            Debug.DrawRay(cam.transform.position, cam.transform.forward * range, Color.yellow);
#endif

            if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                                out var hit, range, interactMask, QueryTriggerInteraction.Collide))
            {
                Debug.DrawRay(cam.transform.position, cam.transform.forward * hit.distance, Color.green);

                if (hit.collider.TryGetComponent<IInteractable>(out var target))
                {
                    if (promptText && !string.IsNullOrEmpty(target.PromptText))
                    {
                        promptText.text = $"{target.PromptText}  [E]";
                        promptText.enabled = true;
                    }

                    // Cooldown-protected interact (remove the unconditional call)
                    if (Input.GetKeyDown(KeyCode.E) && Time.time - lastInteractTime > interactCooldown)
                    {
                        lastInteractTime = Time.time;
                        target.Interact(interactorRoot);
                    }
                }
            }
        }
        static string LayerMaskToString(LayerMask mask)
        {
            var names = new List<string>();
            for (int bit = 0; bit < 32; bit++)
            {
                if ((mask.value & (1 << bit)) != 0)
                {
                    string n = LayerMask.LayerToName(bit);
                    if (!string.IsNullOrEmpty(n)) names.Add(n);
                }
            }
            return names.Count > 0 ? string.Join(", ", names) : mask.value.ToString();
        }
    }
}
