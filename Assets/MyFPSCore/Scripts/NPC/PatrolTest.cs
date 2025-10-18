using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class PatrolTest : MonoBehaviour
{
    public Transform a, b; NavMeshAgent agent; Transform target;
    void Awake() { agent = GetComponent<NavMeshAgent>(); target = a; }
    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.3f) target = (target == a) ? b : a;
        if (target) agent.SetDestination(target.position);
    }
}
