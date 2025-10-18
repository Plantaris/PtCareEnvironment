using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    // Adapter that adds ammo/state + IWeapon over your existing WeaponShooter
    public class PistolGunAdapter : MonoBehaviour, IWeapon, IAmmoWeapon
    {
        [Header("Hook into your shooter")]
        public WeaponShooter shooter;   // drag your WeaponShooter here

        [Header("Ammo")]
        [SerializeField] private int maxAmmo = 50;
        [SerializeField] private int ammo = 8;

        public WeaponType Type => WeaponType.Pistol;
        public int CurrentAmmo => ammo;
        public int MaxAmmo => maxAmmo;

        void Reset()
        {
            if (!shooter) shooter = GetComponent<WeaponShooter>();
            if (shooter) shooter.useInternalInput = false; // manager will drive firing
        }

        public bool CanFire() => shooter && ammo > 0;

        public void Fire()
        {
            if (!shooter || ammo <= 0) return;
            if (shooter.TryFire())
                ammo--;
        }

        public void OnSelected() => gameObject.SetActive(true);
        public void OnDeselected() => gameObject.SetActive(false);

        public void AddAmmo(int amount) => ammo = Mathf.Clamp(ammo + amount, 0, maxAmmo);
    }
}
