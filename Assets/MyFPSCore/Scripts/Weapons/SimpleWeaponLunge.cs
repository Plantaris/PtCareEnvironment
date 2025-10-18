using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWeaponLunge : MonoBehaviour
{
    [Header("What to move")]
    [SerializeField] Transform weaponToMove;   // leave empty to use this.transform

    [Header("Poses")]
    [SerializeField] Transform poseRest;
    [SerializeField] Transform poseAttack;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] float forwardTime = 0.06f;
    [SerializeField, Min(0f)] float holdTime = 0.04f;
    [SerializeField, Min(0f)] float returnTime = 0.08f;

    [Header("Easing")]
    [SerializeField] AnimationCurve easeForward = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] AnimationCurve easeBack = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Coroutine lungeRoutine;
    Transform t;

    void Awake()
    {
        t = weaponToMove ? weaponToMove : transform;
        if (poseRest) SnapTo(poseRest);
    }

    void OnEnable()
    {
        if (poseRest) SnapTo(poseRest);
    }

    public void TriggerLunge()
    {
        if (!poseRest || !poseAttack) { Debug.LogWarning($"{name}: Lunge poses not assigned."); return; }

        if (lungeRoutine != null) StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(Lunge());
    }

    IEnumerator Lunge()
    {
        // forward
        yield return MoveOverTime(poseRest, poseAttack, forwardTime, easeForward);
        // brief hold
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);
        // back
        yield return MoveOverTime(poseAttack, poseRest, returnTime, easeBack);
        lungeRoutine = null;
    }

    IEnumerator MoveOverTime(Transform from, Transform to, float duration, AnimationCurve curve)
    {
        if (duration <= 0f) { SnapTo(to); yield break; }

        Vector3 p0 = from.localPosition; Quaternion r0 = from.localRotation;
        Vector3 p1 = to.localPosition; Quaternion r1 = to.localRotation;

        float tElapsed = 0f;
        while (tElapsed < duration)
        {
            float u = tElapsed / duration;
            float k = curve != null ? curve.Evaluate(u) : u;
            t.localPosition = Vector3.LerpUnclamped(p0, p1, k);
            t.localRotation = Quaternion.SlerpUnclamped(r0, r1, k);
            tElapsed += Time.deltaTime;          // obeys Time.timeScale (pauses when your game is paused)
            yield return null;
        }
        SnapTo(to);
    }

    void SnapTo(Transform pose)
    {
        t.localPosition = pose.localPosition;
        t.localRotation = pose.localRotation;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        var target = weaponToMove ? weaponToMove : transform;
        if (!poseRest || !poseAttack || !target) return;

        // All three should share the same parent for local-space motion to match
        var p = target.parent;
        if (poseRest.parent != p || poseAttack.parent != p)
            Debug.LogWarning($"{name}: poseRest, poseAttack, and the moved object must share the SAME parent.", this);
    }
#endif
}
