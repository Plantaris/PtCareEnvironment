using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorObstacleToggleByTag : MonoBehaviour
{
    public int layer = 0;
    public string openTag = "Open";   // tag you set on the open state(s)

    Animator anim;
    NavMeshObstacle obs;
    bool last;

    void Awake()
    {
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        // auto-find the first obstacle under this door (no child name needed)
        obs = GetComponentInChildren<NavMeshObstacle>(true);
        if (obs) last = obs.enabled;
    }

    void LateUpdate()
    {
        if (!anim || !obs) return;

        var st = anim.GetCurrentAnimatorStateInfo(layer);
        bool isOpen = !anim.IsInTransition(layer) && st.IsTag(openTag);

        bool desired = !isOpen; // closed => obstacle ON, open => OFF
        if (obs.enabled != desired)
        {
            obs.enabled = desired;
            last = desired;
            Debug.Log($"{name}: obstacle {(desired ? "ENABLED (blocked)" : "DISABLED (open)")}");
        }
    }
}

