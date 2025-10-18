using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// If your Health lives in a namespace, keep this. If not, remove it:
using MyFPSCore.Combat; // Health

[DisallowMultipleComponent]
public class DropOnDeath : MonoBehaviour
{
    [System.Serializable]
    public class DropEntry
    {
        public GameObject prefab;          // The thing to drop (ammo, syringe, weapon, etc.)
        [Min(0f)] public float weight = 1; // Higher = more likely (used in weighted pick)
        public int minCount = 1;
        public int maxCount = 1;
    }

    [Header("Drop Table (weighted random pick)")]
    [Tooltip("Pick ONE entry weighted by 'weight'. Leave 'None Weight' > 0 to allow no drop.")]
    public List<DropEntry> entries = new List<DropEntry>();

    [Tooltip("Weight for 'no drop'. Set to 0 to always drop something.")]
    [Min(0f)] public float noneWeight = 0.0f;

    [Header("Spawn Settings")]
    public float spawnRadius = 0.25f;      // random offset on the ground
    public float upwardImpulse = 0.0f;     // small pop if > 0; set 0 to place exactly
    public LayerMask groundMask = ~0;      // which layers count as 'ground' for snapping

    Health _health;

    void Awake()
    {
        _health = GetComponent<Health>();
        if (_health != null)
        {
            _health.onDied.AddListener(OnDied);
        }
        else
        {
            Debug.LogWarning($"{name}: DropOnDeath has no Health component, won't drop.", this);
        }
    }

    void OnDestroy()
    {
        if (_health != null)
            _health.onDied.RemoveListener(OnDied);
    }

    void OnDied(GameObject _)
    {
        // Weighted pick (including 'none')
        var pick = PickWeighted(entries, noneWeight);
        if (pick == null) return;

        int count = Mathf.Clamp(Random.Range(pick.minCount, pick.maxCount + 1), 1, 99);
        for (int i = 0; i < count; i++)
        {
            // --------- CHANGED: pick a spot, start slightly above so ray has room ----------
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = transform.position.y + 1.0f; // give the ray some height

            // Spawn it
            var go = Instantiate(pick.prefab, pos, Quaternion.identity);

            // --------- CHANGED: snap to ground using a downward raycast ----------
            if (Physics.Raycast(pos, Vector3.down, out var hit, 5f, groundMask, QueryTriggerInteraction.Ignore))
            {
                // Try to account for collider height so it sits ON the surface, not inside/above it
                float halfHeight = 0.02f; // small epsilon default
                Collider col = go.GetComponent<Collider>();
                if (!col) col = go.GetComponentInChildren<Collider>();
                if (col) halfHeight = Mathf.Max(0.02f, col.bounds.extents.y);

                go.transform.position = hit.point + Vector3.up * (halfHeight);
                // Optional: align to surface normal (uncomment if you want it to lie on slopes)
                // go.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * go.transform.rotation;
            }

            // --------- CHANGED: ensure sensible physics (no float to sky) ----------
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // small bounce if you want; leave upwardImpulse = 0 for no pop
                float pop = Mathf.Max(0f, upwardImpulse);
                if (pop > 0f) rb.AddForce(Vector3.up * pop, ForceMode.Impulse);
            }
        }
    }

    static DropEntry PickWeighted(List<DropEntry> list, float noneWeight)
    {
        float total = noneWeight;
        for (int i = 0; i < list.Count; i++)
            total += Mathf.Max(0f, list[i].weight);

        if (total <= 0f) return null;

        float r = Random.value * total;

        // None?
        if (r < noneWeight) return null;
        r -= noneWeight;

        // Entries
        for (int i = 0; i < list.Count; i++)
        {
            float w = Mathf.Max(0f, list[i].weight);
            if (r < w) return list[i];
            r -= w;
        }
        return null;
    }
}


