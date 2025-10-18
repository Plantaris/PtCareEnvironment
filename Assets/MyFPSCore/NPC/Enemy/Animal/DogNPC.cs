using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyFPSCore.NPC
{
    public class DogNPC : MonoBehaviour
    {
        public enum Role { Friendly, Enemy }

        [Header("Role")]
        public Role role = Role.Enemy;

        [Header("Refs")]
        public Transform player;            // if null, will find by tag "Player"
        public NavMeshAgent agent;          // assign or auto-get
        public Transform eye;               // empty child ~0.45m high for LOS; if null uses transform

        [Header("Sensing")]
        [Range(30, 180)] public float fov = 120f;
        public float sightRange = 18f;      // 14f for friendly if you want
        public LayerMask losMask = ~0;      // blocks (e.g., Default + Environment)
        public float eyeHeight = 0.45f;     // used if no eye transform

        [Header("Attack (Enemy only)")]
        public float biteRange = 1.0f;
        public int biteDamage = 15;
        public float biteCooldown = 0.6f;
        public float biteActiveTime = 0.15f; // overlap window

        [Header("Follow (Friendly only)")]
        public float followDistance = 1.8f;  // behind
        public float sideOffset = 0.8f;      // keep to player’s right

        [Header("Patrol (optional)")]
        public Transform[] patrolPoints;
        public float patrolWait = 1.5f;

        float nextBiteTime;
        float biteWindowEnd;
        int patrolIndex;
        float waitUntil;
        Vector3 lastKnownPlayerPos;
        bool hasLOS;

        void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (!player)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p.transform;
            }
        }

        void Update()
        {
            if (!agent || !player) return;

            hasLOS = CanSeePlayer();

            if (role == Role.Enemy) EnemyTick();
            else
            {
                float dist = Vector3.Distance(transform.position, player.position);

                if (dist < 8f)   // within follow distance
                    FriendlyTick();
                else
                    Patrol();
            }

            if (Time.time <= biteWindowEnd)
                DoBiteOverlap();
        }

        void EnemyTick()
        {
            // Chase if seen recently or within range
            if (hasLOS)
            {
                lastKnownPlayerPos = player.position;
                agent.speed = Mathf.Max(agent.speed, 5.0f); // ensure run speed if you’re swapping speeds elsewhere
                agent.SetDestination(player.position);

                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= biteRange && Time.time >= nextBiteTime)
                {
                    StartBite();
                }
            }
            else if (lastKnownPlayerPos != Vector3.zero && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                // Reached last known spot; linger a moment then patrol
                if (Time.time > waitUntil)
                {
                    lastKnownPlayerPos = Vector3.zero;
                    waitUntil = Time.time + 0.5f;
                    GoNextPatrol();
                }
            }
            else
            {
                Patrol();
            }
        }

        void FriendlyTick()
        {
            // Stay near player with side offset
            Vector3 toPlayer = player.position - transform.position;
            float behind = followDistance;

            // offset to player's right
            Vector3 right = player.right;
            Vector3 back = -player.forward;

            Vector3 target = player.position + back * behind + right * sideOffset;
            agent.SetDestination(target);
        }

        void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (Time.time >= waitUntil)
                {
                    GoNextPatrol();
                    waitUntil = Time.time + patrolWait;
                }
            }
        }

        void GoNextPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        bool CanSeePlayer()
        {
            Vector3 origin = eye ? eye.position : (transform.position + Vector3.up * eyeHeight);
            Vector3 toPlayer = (player.position + Vector3.up * 0.9f) - origin;

            if (toPlayer.sqrMagnitude > sightRange * sightRange) return false;
            if (Vector3.Angle(transform.forward, toPlayer) > fov * 0.5f) return false;

            if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, sightRange, losMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Player")) return true;
            }
            return false;
        }

        void StartBite()
        {
            nextBiteTime = Time.time + biteCooldown;
            biteWindowEnd = Time.time + biteActiveTime;
            // (No animation—just an overlap window)
        }

        void DoBiteOverlap()
        {
            // Small sphere in front of capsule
            Vector3 mouth = transform.position + Vector3.up * 0.4f + transform.forward * 0.6f;
            float r = 0.35f;

            var hits = Physics.OverlapSphere(mouth, r, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Player")) continue;

                // Compute a sensible hit point & normal
                Vector3 hitPoint = h.ClosestPoint(mouth);
                if (hitPoint == Vector3.zero) hitPoint = mouth; // fallback
                Vector3 hitNormal = (hitPoint - mouth).sqrMagnitude > 0.0001f
                    ? (hitPoint - mouth).normalized
                    : -transform.forward;

                var health = h.GetComponentInParent<MyFPSCore.Combat.Health>();
                if (health != null)
                {
                    health.TakeDamage(biteDamage, gameObject, hitPoint, hitNormal);
                    biteWindowEnd = 0f; // only hit once per bite
                    break;
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = role == Role.Enemy ? Color.red : Color.cyan;
            // sight cone lines
            Vector3 origin = eye ? eye.position : (transform.position + Vector3.up * eyeHeight);
            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.DrawWireDisc(origin, Vector3.up, 0.15f);

            // LOS range
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, sightRange);

            // bite sphere
            Vector3 mouth = transform.position + Vector3.up * 0.4f + transform.forward * 0.6f;
            Gizmos.DrawWireSphere(mouth, 0.35f);
        }
#endif
    }
}

