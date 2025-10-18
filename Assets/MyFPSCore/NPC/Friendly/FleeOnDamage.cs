using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MyFPSCore.Combat; // for Health

namespace MyFPSCore.NPC
{
    [RequireComponent(typeof(BaseNPC))]
    public class FleeOnDamage : MonoBehaviour
    {
        [Header("Flee Behavior")]
        public float fleeDuration = 4f;           // how long to keep fleeing after a hit
        public float fleeDistance = 12f;          // where we aim each hop
        public float safeDistance = 14f;          // end early if we get this far from the threat
        public float fleeSpeedMultiplier = 1.35f; // run faster than normal when fleeing
        public bool disablePatrolWhileFleeing = true;

        [Header("Debug")]
        public bool drawGizmos;
        public Color fleeColor = new Color(0.2f, 0.8f, 1f, 0.5f);

        BaseNPC cfg;
        NavMeshAgent agent;
        Health hp;
        SimplePatrol patrol; // optional, only if present
        Transform threat;
        float fleeUntil;
        float baseSpeed;
        float repathEvery = 0.5f;
        float nextRepath;

        void Awake()
        {
            cfg = GetComponent<BaseNPC>();
            agent = GetComponent<NavMeshAgent>();
            hp = GetComponent<Health>();
            patrol = GetComponent<SimplePatrol>();
            baseSpeed = agent ? agent.speed : 3.5f;
        }

        void OnEnable()
        {
            if (hp != null) hp.onDamaged.AddListener(OnDamaged);
        }

        void OnDisable()
        {
            if (hp != null) hp.onDamaged.RemoveListener(OnDamaged);
        }

        void OnDamaged(float amount, GameObject instigator, Vector3 hitPoint, Vector3 hitNormal)
        {
            threat = instigator ? instigator.transform : (cfg.Player ? cfg.Player : null);
            if (!agent) return;

            fleeUntil = Time.time + Mathf.Max(0.5f, fleeDuration);
            agent.isStopped = false;
            baseSpeed = agent.speed;
            agent.speed = baseSpeed * fleeSpeedMultiplier;

            if (disablePatrolWhileFleeing && patrol) patrol.enabled = false;

            SetFleeDestination(true);
        }

        void Update()
        {
            if (!agent) return;

            if (Time.time <= fleeUntil && threat)
            {
                float dist = Vector3.Distance(transform.position, threat.position);

                // repath occasionally or when we reach our hop
                if (Time.time >= nextRepath || agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.3f))
                    SetFleeDestination(false);

                // end early if we got far enough
                if (dist >= safeDistance) EndFlee();
            }
            else if (fleeUntil > 0f)
            {
                EndFlee();
            }
        }

        void SetFleeDestination(bool immediate)
        {
            if (!threat) return;

            Vector3 fromThreat = (transform.position - threat.position);
            if (fromThreat.sqrMagnitude < 0.01f) fromThreat = transform.forward; // degenerate case
            Vector3 awayDir = fromThreat.normalized;

            // Try a few angled options if straight back isn't valid
            Vector3 best = transform.position;
            bool found = false;
            float[] angles = { 0f, 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f };
            for (int i = 0; i < angles.Length && !found; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * awayDir;
                Vector3 target = transform.position + dir * fleeDistance;
                if (NavMesh.SamplePosition(target, out var hit, 2.5f, NavMesh.AllAreas))
                {
                    best = hit.position;
                    found = true;
                }
            }

            if (!found)
            {
                // last resort: step sideways
                Vector3 side = Vector3.Cross(Vector3.up, awayDir).normalized;
                Vector3 target = transform.position + side * (fleeDistance * 0.6f);
                if (NavMesh.SamplePosition(target, out var hit2, 2.5f, NavMesh.AllAreas))
                    best = hit2.position;
            }

            agent.SetDestination(best);
            nextRepath = Time.time + (immediate ? 0.25f : repathEvery);
        }

        void EndFlee()
        {
            fleeUntil = 0f;
            threat = null;
            if (agent) agent.speed = baseSpeed;
            if (disablePatrolWhileFleeing && patrol) patrol.enabled = true;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !threat) return;
            Gizmos.color = fleeColor;
            Gizmos.DrawLine(transform.position, threat.position);
        }
    }
}

