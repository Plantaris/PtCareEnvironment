using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentAutoGround : MonoBehaviour
{
    public NavMeshAgent agent;      // assign or auto-find
    public Transform visual;        // assign your Visual child
    public float snapRadius = 2f;   // how far to search for navmesh
    public bool alignVisualFeet = true;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!visual && transform.childCount > 0) visual = transform.GetChild(0);
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        EnsureOnNavMesh();

        // If your root is "at feet", baseOffset can be ~0
        if (agent && agent.height > 0f && Mathf.Approximately(agent.baseOffset, 0f))
            agent.baseOffset = 0f;

        if (alignVisualFeet && visual) AlignVisualToFeet();
    }

    void EnsureOnNavMesh()
    {
        if (!agent) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out var hit, snapRadius, agent.areaMask))
            agent.Warp(hit.position);
    }

    void AlignVisualToFeet()
    {
        var rends = visual.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        // Convert world min.y to local-space offset for the Visual
        float worldMinY = b.min.y;
        float delta = transform.position.y - worldMinY;  // how far to raise Visual so its min.y == root.y
        Vector3 lp = visual.localPosition;
        lp.y += delta;
        visual.localPosition = lp;
    }
}
