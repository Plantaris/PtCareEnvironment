using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    // Generic adapter: configures the shared WeaponShooter and provides ammo + IWeapon
    public class ProjectileGunAdapter : MonoBehaviour, IWeapon, IAmmoWeapon
    {
        [Header("Identity")]
        [SerializeField] WeaponType type = WeaponType.Syringe; // set per weapon in Inspector

        [Header("Hook into shared shooter")]
        [SerializeField] WeaponShooter shooter;   // leave empty -> auto-find on Player/rig

        [Header("This weapon's config")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] Transform muzzleOverride;   // optional; leave null to use shooter's default
        [SerializeField] float fireRate = 6f;
        [SerializeField] float spreadDegrees = 0f;
        [SerializeField] float maxAimDistance = 200f;

        [Header("Ammo")]
        [SerializeField] int maxAmmo = 20;
        [SerializeField] int ammo = 10;

        [Header("Fire FX for this weapon")]
        [SerializeField] AudioClip fireSfx;
        [SerializeField, Range(0f, 1f)] float fireSfxVolume = 1f;
        [SerializeField] AudioSource fireAudioSource;      // usually on your weapon/viewmodel root
        [SerializeField] GameObject muzzleVfxPrefab;
        [SerializeField] float muzzleVfxLifetime = 0.25f;

        public WeaponType Type => type;
        public int CurrentAmmo => ammo;
        public int MaxAmmo => maxAmmo;

        void Awake()
        {
            if (!shooter) shooter = GetComponentInParent<WeaponShooter>(); // finds the shared one on Player/rig
            if (shooter) shooter.useInternalInput = false;                 // WeaponManager drives input
        }

        void Reset()
        {
            if (!shooter) shooter = GetComponentInParent<WeaponShooter>();
            if (shooter) shooter.useInternalInput = false;
        }

        public bool CanFire() => shooter && ammo > 0;

        public void Fire()
        {
            if (!shooter || ammo <= 0) return;
            if (shooter.TryFire()) ammo--;
        }

        public void OnSelected()
        {
            gameObject.SetActive(true);

            // Configure the shared shooter for THIS weapon
            if (!shooter) return;
            shooter.projectilePrefab = projectilePrefab;
            if (muzzleOverride) shooter.muzzle = muzzleOverride;
            shooter.fireRate = fireRate;
            shooter.spreadDegrees = spreadDegrees;
            shooter.maxAimDistance = maxAimDistance;
            shooter.useInternalInput = false; // keep the shooter passive
            shooter.fireSfx = fireSfx;
            shooter.fireSfxVolume = fireSfxVolume;
            shooter.fireAudioSource = fireAudioSource;
            shooter.muzzleVfxPrefab = muzzleVfxPrefab;
            shooter.muzzleVfxLifetime = muzzleVfxLifetime;
        }

        public void OnDeselected() => gameObject.SetActive(false);

        public void AddAmmo(int amount)
        {
            ammo = Mathf.Clamp(ammo + amount, 0, maxAmmo);
        }
    }
}
