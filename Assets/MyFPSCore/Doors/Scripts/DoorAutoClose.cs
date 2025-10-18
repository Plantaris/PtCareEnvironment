using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.World
{
    public class DoorAutoClose : MonoBehaviour
    {
        [Header("Door Settings")]
        public Animator animator;
        public float autoCloseDelay = 10f;

        private bool playerInRange;
        private bool doorOpen;
        private float timer;

        private readonly int openTrigger = Animator.StringToHash("MyFPSCore_OpenDoor");
        private readonly int closeTrigger = Animator.StringToHash("MyFPSCore_CloseDoor");

        void Update()
        {
            if (playerInRange && !doorOpen && Input.GetKeyDown(KeyCode.E))
            {
                animator.SetTrigger(openTrigger);
                doorOpen = true;
                timer = autoCloseDelay;
            }

            if (doorOpen)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    animator.SetTrigger(closeTrigger);
                    doorOpen = false;
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                playerInRange = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                playerInRange = false;
        }
    }
}
