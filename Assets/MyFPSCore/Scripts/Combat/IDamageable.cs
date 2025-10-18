using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, UnityEngine.GameObject instigator,
                        UnityEngine.Vector3 hitPoint, UnityEngine.Vector3 hitNormal);
    

    // Start is called before the first frame update
    void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
