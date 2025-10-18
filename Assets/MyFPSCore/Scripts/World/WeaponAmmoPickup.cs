using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Interaction;
using MyFPSCore.Weapons;

namespace MyFPSCore.World
{
    [RequireComponent(typeof(Collider))]
    public class WeaponAmmoPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponType weaponType = WeaponType.Syringe;
        [SerializeField] private int amount = 10;
        [SerializeField] private string displayName = "Syringe Ammo";
        [SerializeField] private AudioSource pickupAudio; // optional

        public string PromptText => $"Pick up {displayName} (+{amount})";

        public void Interact(GameObject interactorRoot)
        {
            var wm = interactorRoot.GetComponent<WeaponManager>();
            if (!wm) return;

            wm.AddAmmo(weaponType, amount);
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

