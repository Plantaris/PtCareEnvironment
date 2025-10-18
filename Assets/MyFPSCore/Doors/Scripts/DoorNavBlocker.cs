using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyFPSCore.Doors
{
    public class DoorNavBlocker : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Animator doorAnimator;
        [SerializeField] NavMeshObstacle blocker;

        [Header("Animator bools")]
        [SerializeField] string openBool = "MyFPSCore_OpenDoor";
        [SerializeField] string closeBool = "MyFPSCore_CloseDoor";

        void Reset()
        {
            if (!doorAnimator) doorAnimator = GetComponentInChildren<Animator>();
            if (!blocker) blocker = GetComponentInChildren<NavMeshObstacle>();
        }

        void Update()
        {
            if (!doorAnimator || !blocker) return;

            // Consider the door "open" only when OpenDoor is true and CloseDoor is false
            bool isOpen = doorAnimator.GetBool(openBool) && !doorAnimator.GetBool(closeBool);
            blocker.enabled = !isOpen; // closed => carve; open => free
        }

        // Optional if you ever add events later:
        public void OnDoorOpened() { if (blocker) blocker.enabled = false; }
        public void OnDoorClosed() { if (blocker) blocker.enabled = true; }
    }
}

