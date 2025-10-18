using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyFPSCore.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class NavAnimatorBridge : MonoBehaviour
    {
        [Header("Params")]
        [SerializeField] string speedParam = "Speed";
        [SerializeField] float dampTime = 0.1f;
        [SerializeField] float maxReportSpeed = 6f;

        Animator anim;
        NavMeshAgent agent;
        Rigidbody rb; // optional fallback if an agent isn’t present

        void Awake()
        {
            anim = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            rb = GetComponent<Rigidbody>();

            // We always drive motion via code/nav, not root motion
            anim.applyRootMotion = false;
        }

        void Update()
        {
            float speed = 0f;

            if (agent) speed = agent.velocity.magnitude;
            else if (rb) speed = rb.velocity.magnitude; // fallback
            // else remains 0

            if (speed > maxReportSpeed) speed = maxReportSpeed;

            anim.SetFloat(speedParam, speed, dampTime, Time.deltaTime);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(speedParam))
                speedParam = "Speed";
        }
#endif
    }
}

