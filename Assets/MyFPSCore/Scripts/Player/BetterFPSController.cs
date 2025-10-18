using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore; // for GameSettings

namespace MyFPSCore.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class BetterFPSController : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Child at eye height (e.g., CameraHolder)")]
        public Transform cameraPivot;
        [Tooltip("Layers considered walkable")]
        public LayerMask groundMask = ~0;

        [Header("Movement")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        [Tooltip("Seconds to reach target speed on ground (smaller = snappier)")]
        public float accelerationTime = 0.08f;
        [Tooltip("Seconds to slow to zero on ground (larger = smoother stop)")]
        public float decelerationTime = 0.12f;
        [Range(0.05f, 1f)] public float airControl = 0.5f;
        [Tooltip("Hold Left Shift to sprint (or set holdToSprint = false to toggle on/off).")]
        public bool holdToSprint = true;
        public bool sprintForwardOnly = true;
        [Range(0f, 1f)] public float sprintMinForwardDot = 0.6f; // ~53°

        [Header("Gravity & Jump")]
        public float gravity = -20f;
        public float jumpHeight = 1.5f;
        [Tooltip(" Amount of time (grace period) player can still jump after leaving the ground, e.g., stepped off a platform, ledge, etc.")]
        public float coyoteTime = 0.12f;
        [Tooltip("Amount of time (grace period) to store a jump input pressed before landing, so jump will trigger the instant player lands.")]
        public float jumpBuffer = 0.12f;
        public float fallGravityMultiplier = 2.0f;
        public float jumpCutMultiplier = 2.5f;

        [Header("Grounding & Slopes")]
        [Tooltip("Match CharacterController.slopeLimit")]
        public float maxGroundAngle = 45f;
        public float groundCheckRadiusMultiplier = 0.9f;
        public float groundCheckExtraDistance = 0.1f;
        [Tooltip("Extra sideways pull on too-steep slopes")]
        public float slideGravity = 5f;
        [SerializeField] string fallbackGroundLayer = "Ground";
        [SerializeField] bool includeDefaultInGround = true;

        [Header("Look")]
        public float mouseSensitivity = 2f;
        [Tooltip("If false, script also handles pitch (useful before Cinemachine).")]
        public bool useCinemachineForPitch = true;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        // internals
        private CharacterController controller;
        private Vector3 velocity;               // y only
        private Vector3 horizontalVelocity;     // xz velocity we manage
        private Vector3 horizontalVelRef;
        private float yaw;
        private float pitch;
        private bool isGrounded;
        private Vector3 groundNormal = Vector3.up;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;
        private bool sprinting;
        public bool IsSprinting { get; private set; }

        void Awake()
        {
            // Auto-assign groundMask if the preset/user left it empty
            if (groundMask.value == 0)
            {
                int g = LayerMask.NameToLayer(fallbackGroundLayer);
                if (g >= 0) groundMask |= 1 << g;

                if (includeDefaultInGround)
                {
                    int d = LayerMask.NameToLayer("Default");
                    if (d >= 0) groundMask |= 1 << d;
                }

                if (groundMask.value == 0)
                    groundMask = Physics.DefaultRaycastLayers; // last resort

                Debug.LogWarning($"[BetterFPSController] groundMask was empty; auto-set to {LayerMaskToString(groundMask)}");
            }
        }

        static string LayerMaskToString(LayerMask mask)
        {
            var sb = new global::System.Text.StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    var n = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(n))
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(n);
                    }
                }
            }
            return sb.Length > 0 ? sb.ToString() : mask.value.ToString();
        }
        void Start()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw = transform.eulerAngles.y;
            maxGroundAngle = Mathf.Max(0.01f, maxGroundAngle);
        }

        void Update()
        {
            // ---------- LOOK ----------
            float savedSens = PlayerPrefs.GetFloat("settings.mouseSensitivity", 1.0f);
            float effectiveSens = mouseSensitivity * Mathf.Max(0.05f, savedSens);
            float mx = Input.GetAxis("Mouse X") * effectiveSens;
            float my = Input.GetAxis("Mouse Y") * effectiveSens;

            yaw += mx;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (!useCinemachineForPitch && cameraPivot != null)
            {
                pitch -= my;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // ---------- INPUT ----------
            float ix = Input.GetAxisRaw("Horizontal");
            float iz = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(ix, 0f, iz).normalized;

            // Transform input into world space
            Vector3 worldInputDir = transform.TransformDirection(inputDir);

            // Sprint toggle / hold
            if (holdToSprint) sprinting = Input.GetKey(KeyCode.LeftShift);
            else if (Input.GetKeyDown(KeyCode.LeftShift)) sprinting = !sprinting;

            // Optional: require forward-ish movement to sprint
            if (sprinting && sprintForwardOnly)
            {
                if (worldInputDir.sqrMagnitude < 0.01f ||
                    Vector3.Dot(worldInputDir.normalized, transform.forward) < sprintMinForwardDot)
                {
                    sprinting = false;
                }
            }

            IsSprinting = sprinting;

            float targetSpeed = (sprinting ? sprintSpeed : walkSpeed) * inputDir.magnitude;
            

            // ---------- GROUND CHECK ----------
            DoGroundCheck();

            Vector3 targetHorizontal = worldInputDir * targetSpeed;
            if (isGrounded)
                targetHorizontal = Vector3.ProjectOnPlane(targetHorizontal, groundNormal);

            bool accelerating = targetHorizontal.magnitude > horizontalVelocity.magnitude + 0.05f;
            float groundSmooth = accelerating ? accelerationTime : decelerationTime;
            float smoothTime = isGrounded ? groundSmooth : Mathf.Max(0.01f, accelerationTime / airControl);

            horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetHorizontal, ref horizontalVelRef, smoothTime);

            if (isGrounded)
            {
                if (velocity.y < 0f) velocity.y = -2f; // stick to ground
                float angle = Vector3.Angle(groundNormal, Vector3.up);
                if (angle > maxGroundAngle)
                {
                    Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    horizontalVelocity += slideDir * slideGravity * Time.deltaTime;
                }
                lastGroundedTime = Time.time;
            }

            // ---------- JUMP ----------
            if (Input.GetButtonDown("Jump"))
                lastJumpPressedTime = Time.time;

            bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;
            bool buffered = Time.time - lastJumpPressedTime <= jumpBuffer;

            if (buffered && (isGrounded || canCoyote))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -999f;
                isGrounded = false;
                lastGroundedTime = -999f; // <— add this line
            }

            // better falls + jump cut
            float g = gravity;
            if (velocity.y < 0f) g *= fallGravityMultiplier;
            else if (velocity.y > 0f && Input.GetButtonUp("Jump")) g *= jumpCutMultiplier;

            velocity.y += g * Time.deltaTime;

            // ---------- MOVE ----------
            Vector3 motion = horizontalVelocity;
            motion.y = velocity.y;
            controller.Move(motion * Time.deltaTime);
        }

        void DoGroundCheck()
        {
            var cc = controller;
            float r = cc.radius * groundCheckRadiusMultiplier;

            // World-space center of the CharacterController
            Vector3 centerWS = transform.TransformPoint(cc.center);

            // Compute bottom of the capsule in world space
            float bottomY = centerWS.y - (cc.height * 0.5f) + cc.radius;
            Vector3 spherePos = new Vector3(centerWS.x, bottomY + r + 0.01f, centerWS.z);

            // 1) Overlap check
            isGrounded = Physics.CheckSphere(spherePos, r, groundMask, QueryTriggerInteraction.Ignore);
            groundNormal = Vector3.up;

            // 2) Spherecast down for a solid normal (deeper reach)
            RaycastHit hit;
            if (Physics.SphereCast(spherePos + Vector3.up * 0.4f, r, Vector3.down, out hit,
                                   0.8f + groundCheckExtraDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundNormal = hit.normal;
                isGrounded = true;
            }
            else
            {
                // 3) Fallback: narrow ray
                if (Physics.Raycast(spherePos + Vector3.up * 0.05f, Vector3.down, out hit,
                                    0.3f + groundCheckExtraDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    groundNormal = hit.normal;
                    isGrounded = true;
                }
            }
        

            // DEBUG: visualize in Scene view (Play mode)
            #if UNITY_EDITOR
            Debug.DrawLine(spherePos, spherePos + Vector3.down * (0.8f + groundCheckExtraDistance), Color.green, 0f, false);
            #endif
        }

        public void ResetMotion()
        {
            // zero out all motion so we don't carry momentum
            velocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            horizontalVelRef = Vector3.zero;
        }
    }
}

