using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorObstacleToggleSimple : MonoBehaviour
{
    // Matches your controller
    public string openBool = "MyFPSCore_OpenDoor";
    public string closeBool = "MyFPSCore_CloseDoor";

    Animator anim;
    NavMeshObstacle obs;
    bool last;

    void Awake()
    {
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        // No child-name needed: grab the first obstacle anywhere under this door
        obs = GetComponentInChildren<NavMeshObstacle>(true);
        if (obs) last = obs.enabled;
    }

    void LateUpdate()
    {
        if (!anim || !obs) return;

        // Consider door open only when OpenDoor is true and CloseDoor is false
        bool isOpen = anim.GetBool(openBool) && !anim.GetBool(closeBool);
        bool desired = !isOpen; // closed => obstacle ON, open => OFF

        if (obs.enabled != desired)
        {
            obs.enabled = desired;
            last = desired;
            Debug.Log($"{name}: obstacle {(desired ? "ENABLED (blocked)" : "DISABLED (open)")}");
        }
    }
}
