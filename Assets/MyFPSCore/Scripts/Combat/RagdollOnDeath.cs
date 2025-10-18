using System.Collections;
using UnityEngine;
using MyFPSCore.Combat;

public class RagdollOnDeath : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;               // assign your character Animator
    public Transform ragdollRoot;           // set to the hips/spine root (optional: if null, uses this.transform)

    [Header("Behavior")]
    public float freezeAfterSeconds = 4f;   // freeze physics after settle

    Rigidbody[] _rbs;
    Collider[] _cols;
    bool _done;

    // Optional hit impulse
    Vector3 _lastHitPoint, _lastHitForce;

    void Awake()
    {
        var h = GetComponent<Health>();
        if (h)
        {
            // capture last hit to add an impulse on ragdoll
            h.onDamaged.AddListener((amt, inst, pt, nrm) => {
                _lastHitPoint = pt;
                _lastHitForce = (inst ? (pt - inst.transform.position).normalized : -nrm) * Mathf.Clamp(amt * 5f, 5f, 50f);
            });
            h.onDied.AddListener(_ => GoRagdoll());
        }

        if (!ragdollRoot) ragdollRoot = transform;

        _rbs = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        _cols = ragdollRoot.GetComponentsInChildren<Collider>(true);

        // Ensure bones start under animator control
        foreach (var rb in _rbs) rb.isKinematic = true;
        // Many rigs leave bone colliders enabled while isKinematic=true; keep as-is unless you had them disabled.
        // If your rig starts with them disabled, you can enable them in GoRagdoll.
    }

    void GoRagdoll()
    {
        if (_done) return;
        _done = true;

        if (animator) animator.enabled = false;

        // If the root has a CharacterController, disable it so physics doesn't fight it
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        foreach (var rb in _rbs) rb.isKinematic = false;
        foreach (var col in _cols) col.enabled = true; // safe even if already enabled

        // Add a little impulse near the hit point
        Rigidbody nearest = null; float best = float.MaxValue;
        foreach (var rb in _rbs)
        {
            float d = (_lastHitPoint - rb.worldCenterOfMass).sqrMagnitude;
            if (d < best) { best = d; nearest = rb; }
        }
        if (nearest != null)
            nearest.AddForceAtPosition(_lastHitForce, _lastHitPoint, ForceMode.Impulse);

        if (freezeAfterSeconds > 0f)
            StartCoroutine(SettleAndFreeze(freezeAfterSeconds));
    }

    IEnumerator SettleAndFreeze(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var rb in _rbs)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // freezes pose where it landed
        }
    }
}

