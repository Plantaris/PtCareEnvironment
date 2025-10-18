using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Player;

public class HeadBobCinemachine : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController cc;
    public BetterFPSController controller; // used for sprint detection & top speed

    [Header("Bob Settings")]
    [Tooltip("Total up/down magnitude in meters (keep small).")]
    public float amplitude = 0.025f;          // ~2.5 cm
    [Tooltip("Base step frequency while walking.")]
    public float frequencyWalk = 9f;          // steps per second feel
    [Tooltip("Frequency multiplier while sprinting.")]
    public float sprintFreqMultiplier = 1.25f;
    [Tooltip("Amplitude multiplier while sprinting.")]
    public float sprintAmpMultiplier = 1.2f;
    [Tooltip("How quickly the camera lerps to the target offset.")]
    public float lerpSpeed = 12f;

    [Header("Landing Dip")]
    [Tooltip("Extra downward dip when touching ground.")]
    public float landBobAmount = 0.02f;       // 2 cm
    [Tooltip("How fast the landing dip recovers.")]
    public float landBobReturnSpeed = 14f;

    Vector3 baseLocalPos;
    Vector3 bobOffset;
    float t;                  // phase
    bool wasGrounded;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (!cc) return;

        // Horizontal speed (ignore vertical)
        float horizSpeed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
        bool grounded = cc.isGrounded;
        bool moving = grounded && horizSpeed > 0.1f;

        // Frequency & amplitude scale
        float freq = frequencyWalk;
        float amp = amplitude;

        if (controller && controller.IsSprinting)
        {
            freq *= sprintFreqMultiplier;
            amp *= sprintAmpMultiplier;
        }

        // Drive phase by movement speed so small moves = slower steps
        float speed01 = 0f;
        if (controller)
            speed01 = Mathf.Clamp01(horizSpeed / Mathf.Max(0.01f, controller.sprintSpeed));
        else
            speed01 = Mathf.Clamp01(horizSpeed / 7.5f);

        if (moving)
            t += Time.deltaTime * Mathf.Lerp(0.6f, 1.2f, speed01) * freq;
        else
            t = Mathf.Lerp(t, 0f, Time.deltaTime * 5f); // relax phase when stopping

        // Basic bob: slight X sway + Y bounce (abs(sin) for up-only bounce)
        Vector3 targetOffset = Vector3.zero;
        if (moving)
        {
            float x = Mathf.Sin(t) * amp * 0.5f;
            float y = Mathf.Abs(Mathf.Sin(t * 2f)) * amp;
            targetOffset = new Vector3(x, y, 0f);
        }

        // Landing dip when becoming grounded this frame
        if (!wasGrounded && grounded)
            bobOffset.y -= landBobAmount;

        // Smooth towards target
        bobOffset = Vector3.Lerp(bobOffset, targetOffset, Time.deltaTime * lerpSpeed);
        // Recover landing dip quickly
        bobOffset.y = Mathf.Lerp(bobOffset.y, targetOffset.y, Time.deltaTime * landBobReturnSpeed);

        transform.localPosition = Vector3.Lerp(transform.localPosition, baseLocalPos + bobOffset, Time.deltaTime * lerpSpeed);

        wasGrounded = grounded;
    }
}

