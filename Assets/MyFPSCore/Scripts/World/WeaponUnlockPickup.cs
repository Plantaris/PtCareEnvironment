using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Interaction;
using MyFPSCore.Weapons;

namespace MyFPSCore.World
{
    [RequireComponent(typeof(Collider))]
    public class WeaponUnlockPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponType weaponType = WeaponType.Scalpel;
        [SerializeField] private string displayName = "Scalpel";
        [SerializeField] private AudioSource pickupAudio; // optional

        public string PromptText => $"Pick up {displayName}";

        public void Interact(GameObject interactorRoot)
        {
            var wm = interactorRoot.GetComponent<WeaponManager>();
            if (!wm) return;

            // If the weapon GO already exists as a child, it's already in weaponComponents.
            // Just ensure it's registered and select it.
            // (If you want unlock to *enable* a hidden child, you can toggle SetActive(true) here.)

            // find the weapon by type among manager.weaponComponents
            foreach (var mb in wm.weaponComponents)
            {
                if (mb is IWeapon w && w.Type == weaponType)
                {
                    wm.RegisterWeapon(w, selectAfter: true);
                    ConsumeAndDestroy();
                    return;
                }
            }

            // If weapon not present in array, you could add a fallback here (spawn or log).
            ConsumeAndDestroy();
        }

        private void ConsumeAndDestroy()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

            if (pickupAudio && pickupAudio.clip)
            {
                pickupAudio.Play();
                Destroy(gameObject, pickupAudio.clip.length);
            }
            else Destroy(gameObject);
        }
    }
}

