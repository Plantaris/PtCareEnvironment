using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyFPSCore.NPC
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BaseNPC : MonoBehaviour
    {
        [Header("Identity")]
        public NPCRole role = NPCRole.Enemy;
        public NPCSpecies species = NPCSpecies.Human;

        [Header("Tuning")]
        public float detectRadius = 12f;
        public float attackRange = 2.0f;
        public float attackCooldown = 1.2f;
        public int damage = 8;

        [Header("Refs (optional)")]
        public Transform eyes;

        [HideInInspector] public NavMeshAgent agent;
        Transform _player;
        public Transform Player => _player;

        void Reset() { agent = GetComponent<NavMeshAgent>(); }
        void OnValidate() { if (!agent) agent = GetComponent<NavMeshAgent>(); }

        void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.transform;

            if (agent)
            {
                agent.stoppingDistance = Mathf.Max(attackRange * 0.75f, 0.5f);
                agent.angularSpeed = Mathf.Max(agent.angularSpeed, 180f);
            }
        }
    }
}

