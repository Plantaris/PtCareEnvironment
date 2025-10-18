using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Weapons
{
    // Now has durability/charges and can be refilled by pickups
    public class ScalpelWeapon : MonoBehaviour, IWeapon, IAmmoWeapon
    {
        [Header("Melee")]
        public Camera aimCamera;           // assign player camera (auto-fills in Awake)
        public Transform ownerRoot;        // drag Player root (optional; auto-fills)

        public float range = 2f;
        public float damage = 25f;
        public float swingCooldown = 0.4f;

        [Header("Durability / Charges")]
        [SerializeField] private int maxCharges = 40;
        [SerializeField] private int charges = 40;

        [Header("Raycast filters (optional)")]
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] QueryTriggerInteraction triggerMode = QueryTriggerInteraction.Ignore;
        [SerializeField] bool excludePlayerLayer = true;   // strip Player from hitMask in Awake
        [SerializeField] bool skipSelfRoot = true;         // ignore any colliders under ownerRoot when raycasting

        [Header("Viewmodel VFX")]
        [SerializeField] SimpleWeaponLunge lunge;      // drag the component from ScalpelVisualOnly(forVFX)
        [SerializeField] bool lungeOnFire = true;      // toggle if you ever want it off

        [Header("FX (optional)")]
        public AudioSource swingSfx;
        public AudioSource breakSfx;
        public GameObject breakVfx;
        [SerializeField] GameObject hitVfxPrefab;

        [Header("Anchors")]
        [SerializeField] Transform vfxAnchorTip;       // assign VFXAnchor_Tip at blade tip
        [SerializeField] Vector3 tipLocalOffset;       // fallback if no anchor (e.g., 0,0,0.15)

        // cache last swing hit point to spawn VFX where you just struck
        private Vector3 lastHitPoint;
        private bool hasLastHitPoint;

        private float nextSwing;

        public WeaponType Type => WeaponType.Scalpel;
        public int CurrentAmmo => charges;
        public int MaxAmmo => maxCharges;

        void Awake()
        {
            if (!aimCamera) aimCamera = Camera.main;
            if (!ownerRoot && aimCamera) ownerRoot = aimCamera.transform.root;
            if (!ownerRoot) ownerRoot = transform.root;
            if (!lunge) lunge = GetComponentInChildren<SimpleWeaponLunge>(true);

            // ensure we never hit Player layer even if the inspector mask is wrong
            if (excludePlayerLayer)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0) hitMask &= ~(1 << playerLayer);
            }
        }

        public bool CanFire() => Time.time >= nextSwing && charges > 0;

        public void Fire()
        {
            if (!CanFire() || !aimCamera) return;
            nextSwing = Time.time + swingCooldown;

            // define origin/dir (these were missing)
            var origin = aimCamera.transform.position;
            var dir = aimCamera.transform.forward;

            // use RaycastAll so we can skip our own root reliably, then hit the first valid target
            hasLastHitPoint = false;

            var hits = Physics.RaycastAll(origin, dir, range, hitMask, triggerMode);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (skipSelfRoot && ownerRoot && hit.collider.transform.root == ownerRoot)
                        continue;

                    var h = hit.collider.GetComponentInParent<MyFPSCore.Combat.Health>();
                    if (h) h.TakeDamage(damage, ownerRoot ? ownerRoot.gameObject : gameObject, hit.point, hit.normal);

                    if (hitVfxPrefab)
                    {
                        var fx = Instantiate(hitVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(fx, 1.5f);
                    }

                    // remember where we hit for potential break VFX
                    lastHitPoint = hit.point;
                    hasLastHitPoint = true;
                    break;
                }
            }

            if (swingSfx) swingSfx.Play();

            // kick the viewmodel forward
            if (lungeOnFire && lunge) lunge.TriggerLunge();

            // consume a charge
            charges = Mathf.Clamp(charges - 1, 0, maxCharges);
            if (charges == 0) BreakScalpel();
        }

        public void AddAmmo(int amount) // "repair" or "replace" with fresh charges
        {
            charges = Mathf.Clamp(charges + amount, 0, maxCharges);
        }

        public void OnSelected() => gameObject.SetActive(true);
        public void OnDeselected() => gameObject.SetActive(false);

        private void BreakScalpel()
        {
            // Play sound in world so it won’t cut off when we deactivate
            if (breakSfx && breakSfx.clip)
                AudioSource.PlayClipAtPoint(breakSfx.clip, GetTipWorldPos(), breakSfx.volume);

            // Spawn VFX at the tip (or last hit) and make it simulate in World space
            if (breakVfx)
            {
                Vector3 spawnPos = hasLastHitPoint ? lastHitPoint : GetTipWorldPos();
                Quaternion spawnRot = hasLastHitPoint ? Quaternion.identity : transform.rotation;

                var vfxObj = Instantiate(breakVfx, spawnPos, spawnRot);
                var ps = vfxObj.GetComponent<ParticleSystem>();
                if (ps)
                {
                    var main = ps.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World; // survives disable
                }
                Destroy(vfxObj, 2f); // safety cleanup
            }

            // Disable this weapon
            gameObject.SetActive(false);

            // Fallback to Syringe
            var wm = ownerRoot ? ownerRoot.GetComponent<WeaponManager>() : GetComponentInParent<WeaponManager>();
            if (wm != null && wm.HasWeapon(WeaponType.Syringe))
                wm.SelectWeapon(WeaponType.Syringe);
        }

        // Prefer tip anchor → else local offset → else transform position
        private Vector3 GetTipWorldPos()
        {
            if (vfxAnchorTip) return vfxAnchorTip.position;
            if (tipLocalOffset != Vector3.zero) return transform.TransformPoint(tipLocalOffset);
            return transform.position;
        }
    }
}
