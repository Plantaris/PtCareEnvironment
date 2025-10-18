using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyFPSCore.NPC
{
    [RequireComponent(typeof(BaseNPC))]
    public class SimplePatrol : MonoBehaviour
    {
        public Transform[] points;
        BaseNPC cfg; NavMeshAgent agent; int i;

        void Awake() { cfg = GetComponent<BaseNPC>(); agent = cfg.agent; }

        void Update()
        {
            if (points == null || points.Length == 0) return;
            if (!agent || !agent.isOnNavMesh) return;

            // Respect external pause (InteractToHeal, cutscenes, etc.)
            if (agent.isStopped) return;   // ← NEW

            float arrive = Mathf.Max(agent.stoppingDistance, 0.25f);
            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrive))
            {
                i = (i + 1) % points.Length;
                var p = points[i].position;
                if (NavMesh.SamplePosition(p, out var hit, 1f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(p);
            }
        }
    }
}

