using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    public interface IAmmoWeapon : IWeapon
    {
        void AddAmmo(int amount);
    }
}
