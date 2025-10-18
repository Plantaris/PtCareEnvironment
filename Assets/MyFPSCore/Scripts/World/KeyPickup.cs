using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Inventory;
using MyFPSCore.Interaction;

namespace MyFPSCore.World
{
    [RequireComponent(typeof(Collider))]
    public class KeyPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private KeyTypeSO keyType;
        [SerializeField] private AudioSource keyPickupAudio; // optional "Key Pickup" sound

        public string PromptText => keyType ? $"Pick up {keyType.displayName}" : "Pick up key";

        public void Interact(GameObject interactorRoot)
        {
            var inv = interactorRoot.GetComponent<PlayerInventory>();
            if (inv != null && keyType != null)
            {
                inv.AddKey(keyType);

                // hide & prevent double-pickup immediately
                if (TryGetComponent<Collider>(out var col)) col.enabled = false;
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

                // play then destroy after the clip finishes
                if (keyPickupAudio != null && keyPickupAudio.clip != null)
                {
                    keyPickupAudio.Play();
                    Destroy(gameObject, keyPickupAudio.clip.length);
                }
                else
                {
                    Destroy(gameObject); // no clip assigned, just nuke it
                }
            }
        }
    }
}