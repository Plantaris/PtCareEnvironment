using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.World;
using MyFPSCore.Player;

namespace MyFPSCore.Audio
{
    [RequireComponent(typeof(CharacterController))]
    public class FootstepAudio : MonoBehaviour
    {
        [Header("Refs")]
        public BetterFPSController controller;       // drag your player controller
        public CharacterController cc;               // auto-filled
        public LayerMask groundMask;                 // if 0, will copy from controller.groundMask
        public Transform rayOrigin;                  // if null, uses transform

        [Header("Timing")]
        [Tooltip("Steps per second at walk speed")]
        public float walkStepRate = 1.8f;
        [Tooltip("Steps per second at sprint speed")]
        public float sprintStepRate = 2.8f;
        [Tooltip("Minimum horizontal speed to count as moving")]
        public float moveThreshold = 0.1f;

        [Header("Volume/Pitch")]
        [Range(0f, 1f)] public float baseVolume = 0.85f;
        [Range(0.5f, 2f)] public float sprintVolumeMul = 1.15f;
        [Range(0f, 0.2f)] public float pitchJitter = 0.05f; // ±5%

        [Header("Clips (Default)")]
        public AudioClip[] defaultFootsteps;
        public AudioClip[] defaultLandThuds;

        [global::System.Serializable]
        public class SurfaceClips
        {
            public SurfaceKind kind;
            public AudioClip[] footsteps;
            public AudioClip[] landThuds;
        }
        [Header("Overrides by Surface")]
        public List<SurfaceClips> overrides = new List<SurfaceClips>();

        [Header("Landing")]
        [Tooltip("Volume scales with time in air; clamp at this max")]
        public float maxLandVolume = 1f;
        [Tooltip("Seconds airborne required to reach maxLandVolume")]
        public float landFullVolumeAirTime = 0.6f;

        AudioSource src;
        float stepTimer;
        bool wasGrounded;
        float airTime;

        void Awake()
        {
            if (!cc) cc = GetComponent<CharacterController>();
            src = GetComponent<AudioSource>();
            if (!src) src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f; // 3D
            src.playOnAwake = false;

            if (groundMask.value == 0 && controller != null)
                groundMask = controller.groundMask;

            if (!rayOrigin) rayOrigin = transform;
        }

        void Update()
        {
            // Track airborne time & landing thud
            if (!cc.isGrounded) airTime += Time.deltaTime;

            if (!wasGrounded && cc.isGrounded)
            {
                var clips = GetClipsForSurface(out _);
                if (clips.landThuds != null && clips.landThuds.Length > 0)
                {
                    float t = Mathf.Clamp01(airTime / Mathf.Max(0.01f, landFullVolumeAirTime));
                    PlayOneShot(RandomClip(clips.landThuds), Mathf.Lerp(0.4f, maxLandVolume, t));
                }
                airTime = 0f;
            }

            // Footsteps while moving on ground
            var horiz = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
            bool moving = cc.isGrounded && horiz > moveThreshold;
            if (moving)
            {
                float speed01 = controller ? Mathf.Clamp01(horiz / Mathf.Max(0.01f, controller.sprintSpeed))
                                           : Mathf.Clamp01(horiz / 7.5f);
                float rate = Mathf.Lerp(walkStepRate, sprintStepRate, speed01);
                float interval = 1f / Mathf.Max(0.01f, rate);

                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    var clips = GetClipsForSurface(out bool sprintingNow);
                    var clip = RandomClip(clips.footsteps);
                    if (clip)
                    {
                        float vol = baseVolume * (sprintingNow ? sprintVolumeMul : 1f);
                        PlayOneShot(clip, vol);
                    }
                    stepTimer = interval;
                }
            }
            else
            {
                // Reset timer so we don't instantly fire a step when resuming
                stepTimer = 0.05f;
            }

            wasGrounded = cc.isGrounded;
        }

        (AudioClip[] footsteps, AudioClip[] landThuds) GetClipsForSurface(out bool sprintingNow)
        {
            sprintingNow = controller && controller.IsSprinting;

            // Raycast down a little from feet to detect surface
            Vector3 origin = rayOrigin.position;
            // place near bottom of capsule
            float bottom = cc.bounds.min.y + 0.05f;
            origin.y = bottom + 0.05f;

            if (Physics.Raycast(origin, Vector3.down, out var hit, 0.4f, groundMask, QueryTriggerInteraction.Ignore))
            {
                var st = hit.collider.GetComponent<SurfaceType>();
                if (st != null)
                {
                    foreach (var e in overrides)
                        if (e.kind == st.kind)
                            return (e.footsteps != null && e.footsteps.Length > 0 ? e.footsteps : defaultFootsteps,
                                    e.landThuds != null && e.landThuds.Length > 0 ? e.landThuds : defaultLandThuds);
                }
            }
            return (defaultFootsteps, defaultLandThuds);
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (!clip) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        static AudioClip RandomClip(AudioClip[] arr)
        {
            if (arr == null || arr.Length == 0) return null;
            return arr[UnityEngine.Random.Range(0, arr.Length)];
        }
    }
}
