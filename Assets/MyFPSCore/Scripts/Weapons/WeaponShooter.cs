using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Player;

namespace MyFPSCore.Weapons
{
    public class WeaponShooter : MonoBehaviour
    {
        [Header("Control")]
        public bool useInternalInput = true;

        [Header("Refs")]
        public BetterFPSController controller;     // drag your player
        public Transform muzzle;                   // e.g., CameraHolder or a child offset
        public Camera cam;                         // leave empty -> Camera.main

        [Header("Projectile")]
        public Projectile projectilePrefab;
        public float fireRate = 6f;                // rounds per second (e.g., 6 = ~180rpm)
        public float spreadDegrees = 0f;           // 0 for syringe/scalpel
        public float maxAimDistance = 200f;

        [Header("Fire SFX/VFX")]
        public AudioClip fireSfx;
        [Range(0f, 1f)] public float fireSfxVolume = 1f;
        public AudioSource fireAudioSource;      // optional; if null we use PlayClipAtPoint at the muzzle
        public GameObject muzzleVfxPrefab;       // optional
        public float muzzleVfxLifetime = 0.25f;  // optional
        [SerializeField] bool detachMuzzleVfx = true; // unparent so smoke/flash persists

        [Header("Viewmodel VFX")]
        [SerializeField] SimpleWeaponLunge lunge;      // drag the component from ScalpelVisualOnly(forVFX)
        [SerializeField] bool lungeOnFire = true;      // toggle if you ever want it off

        [Header("Recoil (optional)")]
        public Cinemachine.CinemachineImpulseSource impulse;
        public float impulseStrength = 0.1f;

        float nextFireTime;

        [SerializeField] bool debugVfx = true;

        void Awake()
        {
            if (!cam) cam = Camera.main;
            if (!muzzle) muzzle = controller ? controller.cameraPivot : transform;

            if (!lunge) lunge = GetComponentInChildren<SimpleWeaponLunge>(true);

        }

        void Update()
        {
            if (!useInternalInput) return;   // <-- let WeaponManager drive it

            bool wantsFire = false;

            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                wantsFire |= UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            #endif

            wantsFire |= Input.GetMouseButtonDown(0);
            wantsFire |= Input.GetButtonDown("Fire1");

            if (wantsFire) TryFire(); // centralize rate limit
        }

        static string GetPath(Transform t)
        {
            if (!t) return "<null>";
            var names = new System.Collections.Generic.List<string>();
            while (t) { names.Add(t.name); t = t.parent; }
            names.Reverse();
            return string.Join("/", names);
        }

        public void SetLunge(SimpleWeaponLunge newLunge) => lunge = newLunge;

        public bool TryFire()
        {
            if (Time.time < nextFireTime) return false;
            FireOnce();
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            return true;
        }
        void FireOnce()
        {
            if (!projectilePrefab || !cam || !muzzle) return;

            // Ray from camera to get aim point
            Vector3 targetPoint;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, maxAimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                targetPoint = hit.point;
            else
                targetPoint = cam.transform.position + cam.transform.forward * maxAimDistance;

            // Direction from muzzle to aim point (+ spread)
            Vector3 dir = (targetPoint - muzzle.position).normalized;
            if (spreadDegrees > 0f)
                dir = ApplySpread(dir, spreadDegrees);

            // Spawn & init projectile
            var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
            proj.Init(controller ? controller.gameObject : gameObject, dir);

            // Optional camera impulse
            if (impulse) impulse.GenerateImpulse(impulseStrength);

            // play fire SFX now (shot time)
            if (fireSfx)
            {
                if (fireAudioSource)
                {
                    fireAudioSource.pitch = 1f + Random.Range(-0.05f, 0.05f); // tiny variation
                    fireAudioSource.PlayOneShot(fireSfx, fireSfxVolume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(fireSfx, muzzle.position, fireSfxVolume);
                }
            }

            // spawn muzzle flash at the muzzle
            if (muzzleVfxPrefab)
            {
                var vfxRot = Quaternion.LookRotation(dir);

                // Push spawn in front of camera near-clip so it can't be clipped or inside the gun
                Vector3 camPos = cam ? cam.transform.position : muzzle.position;
                Vector3 camFwd = cam ? cam.transform.forward : muzzle.forward;
                float minDepth = (cam ? cam.nearClipPlane : 0.05f) + 0.20f; // ~20cm past near-clip
                Vector3 spawnPos = muzzle ? muzzle.position : camPos;
                float depth = Vector3.Dot(spawnPos - camPos, camFwd);
                if (depth < minDepth) spawnPos = camPos + camFwd * minDepth;

                var vfx = Instantiate(muzzleVfxPrefab, spawnPos, vfxRot);

                if (detachMuzzleVfx) vfx.transform.SetParent(null, true);
                vfx.transform.localScale = Vector3.one; // ignore tiny parent scales

                // Make sure it actually emits (even if Play On Awake was off)
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Play(true);

                if (muzzleVfxLifetime > 0f) Destroy(vfx, muzzleVfxLifetime);
            }
            // kick the viewmodel forward
            if (lungeOnFire && lunge) lunge.TriggerLunge();


        }

        Vector3 ApplySpread(Vector3 dir, float degrees)
        {
            // random yaw/pitch within cone
            Quaternion q = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up)
                          * Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.right);
            return (q * dir).normalized;
        }
    }
}
