using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    public enum WeaponType { Syringe, Scalpel, Pistol, RocketLauncher }

    public interface IWeapon
    {
        WeaponType Type { get; }
        bool CanFire();
        void Fire();
        int CurrentAmmo { get; }   // -1 if N/A (e.g., melee)
        int MaxAmmo { get; }

        void OnSelected();
        void OnDeselected();
    }
}

