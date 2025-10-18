using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Combat;

namespace MyFPSCore.Weapons
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Projectile : MonoBehaviour
        
    {

        [Header("Impact FX/SFX")]
        [SerializeField] GameObject hitVfxPrefab;
        [SerializeField] float hitVfxLifetime = 1.5f;
        [SerializeField] AudioClip hitSfx;
        [SerializeField][Range(0f, 1f)] float hitSfxVolume = 1f;

        public float speed = 30f;
        public float damage = 20f;
        public float lifeSeconds = 5f;
        public float gravityScale = 0f;     // 0 = straight; >0 = arcing throw
        public bool stickOnHit = true;
        public LayerMask hitMask = ~0;

        Rigidbody rb;
        Collider col;
        GameObject owner;
        float lifeTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;   // <-- add this
            rb.useGravity = gravityScale > 0f;
        }

        public void Init(GameObject ownerGO, Vector3 initialDir)
        {
            owner = ownerGO;
            lifeTimer = Time.time + lifeSeconds;

            // ignore owner collisions
            foreach (var c in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col, c, true);

            rb.velocity = initialDir.normalized * speed;
        }

        void FixedUpdate()
        {
            if (gravityScale > 0f)
                rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
        }

        void Update()
        {
            if (Time.time >= lifeTimer) Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

            var contact = collision.GetContact(0);
            var dmg = collision.collider.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage, owner, contact.point, contact.normal);

            // impact VFX/SFX
            if (hitVfxPrefab)
            {
                var fx = Instantiate(hitVfxPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, hitVfxLifetime);
            }
            if (hitSfx)
            {
                AudioSource.PlayClipAtPoint(hitSfx, contact.point, hitSfxVolume);
            }

            if (stickOnHit)
            {
                rb.isKinematic = true;
                col.enabled = false;
                transform.position = contact.point;
                transform.rotation = Quaternion.LookRotation(-contact.normal);
                transform.SetParent(collision.transform, true);
                Destroy(gameObject, 10f);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

