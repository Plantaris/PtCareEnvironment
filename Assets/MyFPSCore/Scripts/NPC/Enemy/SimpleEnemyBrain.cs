using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MyFPSCore.Combat; // Health

namespace MyFPSCore.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BaseNPC))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class SimpleEnemyBrain : MonoBehaviour
    {
        [Header("Debug Gizmos")]
        public bool drawGizmos = true;
        public bool drawAlways = false;
        public Color detectColor = new Color(1f, 0f, 0f, 0.25f); // red
        public Color attackColor = new Color(1f, 1f, 0f, 0.25f); // yellow

        [Tooltip("Assign 2–4 waypoint empties placed on the navmesh.")]
        public Transform[] patrolPoints;

        [Header("Movement Speeds (m/s)")]
        [SerializeField] float patrolSpeed = 2.0f;   // matches Walk threshold
        [SerializeField] float chaseSpeed = 4.0f;   // matches Run threshold

        BaseNPC cfg;
        NavMeshAgent agent;
        int patrolIndex = -1;
        float lastAttack;

        void Awake()
        {
            cfg = GetComponent<BaseNPC>();
            agent = GetComponent<NavMeshAgent>();

            // sane agent defaults
            agent.autoBraking = true;       // helps arrive cleanly at waypoints
            agent.updateRotation = true;    // let nav rotate the body
        }

        void Update()
        {
            // No player reference yet? Just patrol.
            if (!cfg.Player) { Patrol(); return; }

            float d = Vector3.Distance(transform.position, cfg.Player.position);

            if (d <= cfg.attackRange) Attack();
            else if (d <= cfg.detectRadius) Chase();
            else Patrol();
        }

        // -------------------- STATES --------------------

        void Patrol()
        {
            if (!agent || patrolPoints == null || patrolPoints.Length == 0) return;

            // ensure walk speed while patrolling
            if (agent.speed != patrolSpeed) agent.speed = patrolSpeed;
            agent.isStopped = false;

            // consider we've "arrived" when within stopping distance (or small floor)
            float arriveThreshold = Mathf.Max(agent.stoppingDistance, 0.25f);

            // pick next point if we don't have a path or we've arrived
            if (agent.pathPending) return;

            if (!agent.hasPath || agent.remainingDistance <= arriveThreshold)
            {
                patrolIndex = (patrolIndex + 1 + patrolPoints.Length) % patrolPoints.Length;
                Vector3 target = patrolPoints[patrolIndex].position;

                // snap to nearest navmesh if slightly off
                if (NavMesh.SamplePosition(target, out var hit, 1.0f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(target);
            }
        }

        void Chase()
        {
            if (!agent || !cfg.Player) return;

            // ensure run speed while chasing
            if (agent.speed != chaseSpeed) agent.speed = chaseSpeed;

            agent.isStopped = false;
            agent.SetDestination(cfg.Player.position);
        }

        void Attack()
        {
            if (!agent || !cfg.Player) return;

            // stop movement & face the player
            agent.isStopped = true;

            Vector3 dir = cfg.Player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    10f * Time.deltaTime
                );

            if (Time.time >= lastAttack + cfg.attackCooldown)
            {
                lastAttack = Time.time;

                // Deal damage using Health.TakeDamage
                var h = cfg.Player.GetComponentInParent<Health>();
                if (h)
                {
                    Vector3 origin = cfg.eyes ? cfg.eyes.position : (transform.position + Vector3.up * 1.2f);
                    Vector3 toPlayer = (cfg.Player.position - origin);
                    Vector3 hitPoint = cfg.Player.position;
                    Vector3 hitNormal = -toPlayer.normalized;

                    h.TakeDamage(cfg.damage, gameObject, hitPoint, hitNormal);
                }
            }
        }

        // -------------------- GIZMOS --------------------

        void OnDrawGizmos()
        {
            if (drawAlways) DrawGizmoRings();
        }
        void OnDrawGizmosSelected()
        {
            if (!drawAlways) DrawGizmoRings();
        }
        void DrawGizmoRings()
        {
            var c = GetComponent<BaseNPC>(); if (!c) return;
            if (!drawGizmos) return;
            Gizmos.color = detectColor; Gizmos.DrawWireSphere(transform.position, c.detectRadius);
            Gizmos.color = attackColor; Gizmos.DrawWireSphere(transform.position, c.attackRange);
        }
    }
}
