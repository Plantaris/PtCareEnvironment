using MyFPSCore.Interaction;
using MyFPSCore.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MyFPSCore.Weapons.WeaponManager;

namespace MyFPSCore.World
{
    [RequireComponent(typeof(Collider))]
    public class WeaponPickupSmart : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponType weaponType = WeaponType.Syringe;
        [SerializeField] private string displayName = "Syringe";

        [Header("Amounts")]
        [SerializeField] private int ammoOnUnlock = 10;   // given only the first time (if weapon uses ammo)
        [SerializeField] private int ammoOnRepeat = 10;   // given on subsequent pickups (if weapon uses ammo)
        [SerializeField] private bool selectOnPickup = true;

        [Header("FX (optional)")]
        [SerializeField] private AudioSource pickupAudio;

        public string PromptText => $"Pick up {displayName}";

        public void Interact(GameObject interactorRoot)
        {
            var wm = interactorRoot.GetComponent<WeaponManager>();
            if (!wm) return;

            if (!TryGetWeapon(wm, weaponType, out var weapon, out var weaponMB))
            {
                Debug.LogWarning($"[Pickup] No weapon of type {weaponType} found on WeaponManager.", this);
                ConsumeAndDestroy();
                return;
            }

            bool alreadyUnlocked = wm.IsUnlocked(weaponType);

            if (!alreadyUnlocked)
            {
                // First time: unlock + starting ammo/charges (if applicable)
                wm.UnlockWeapon(weaponType, selectOnPickup);
                if (weapon is IAmmoWeapon ammoW && ammoOnUnlock > 0)
                    ammoW.AddAmmo(ammoOnUnlock);
            }
            else
            {
                // Refill path
                if (weapon is IAmmoWeapon ammoW && ammoOnRepeat > 0)
                    ammoW.AddAmmo(ammoOnRepeat);
                if (selectOnPickup)
                    wm.SelectWeapon(weaponType);
            }

            ConsumeAndDestroy();
        }


        private bool TryGetWeapon(WeaponManager wm, WeaponType type, out IWeapon weapon, out MonoBehaviour weaponMB)
        {
            weapon = null;
            weaponMB = null;
            foreach (var mb in wm.weaponComponents)
            {
                if (mb is IWeapon w && w.Type == type)
                {
                    weapon = w;
                    weaponMB = (MonoBehaviour)mb;
                    return true;
                }
            }
            return false;
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
