using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Combat;
using UnityEngine.AI;

namespace MyFPSCore.NPC
{
    public class InteractToHeal : MonoBehaviour
    {
        [Header("Interact")]
        public KeyCode key = KeyCode.E;
        public float interactRange = 2.2f;
        public int healAmount = 15;
        public float cooldownSeconds = 3f;

        [Header("Usage Limits")]
        public int maxUses = 2;
        public bool disableOnDepleted = false;

        [Header("Behavior")]
        public bool pauseNPCWhilePlayerNear = true;   // NEW
        public float stopRangePadding = 0.4f;         // NEW (so we stop a bit before interactRange)

        [Header("Prompt (OnGUI)")]
        public bool showOnGuiPrompt = true;
        public string promptText = "Press E to receive treatment";
        public string depletedText = "Out of supplies";
        public bool showRemainingCount = true;

        Transform player;
        Health playerHealth;
        NavMeshAgent agent;                           // NEW
        float nextAllowedTime;
        int usesRemaining;
        bool playerClose;                             // NEW

        void Awake()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) { player = p.transform; playerHealth = p.GetComponentInParent<Health>(); }

            agent = GetComponent<NavMeshAgent>();     // NEW (patient root has the agent)
            usesRemaining = Mathf.Max(0, maxUses);
        }

        void Update()
        {
            if (!player || !playerHealth) return;

            if (usesRemaining <= 0 && disableOnDepleted)
            {
                // resume patrol if we had paused, then disable
                if (agent && playerClose) agent.isStopped = false;
                enabled = false;
                return;
            }

            float d = Vector3.Distance(transform.position, player.position);
            float stopRange = interactRange - 0.1f + Mathf.Max(0f, stopRangePadding);

            // --- Pause/resume the NavMeshAgent when near ---
            bool nowClose = d <= stopRange;
            if (pauseNPCWhilePlayerNear && agent)
            {
                if (nowClose && !playerClose) agent.isStopped = true;      // just entered range -> stop
                else if (!nowClose && playerClose) agent.isStopped = false; // just exited -> resume
            }
            playerClose = nowClose;

            // --- Face the player while close (optional: replace your separate script) ---
            if (playerClose)
            {
                Vector3 dir = player.position - transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
            }

            // --- Handle the heal interaction ---
            if (d > interactRange) return;
            if (Time.time < nextAllowedTime) return;
            if (usesRemaining <= 0) return;

            if (Input.GetKeyDown(key))
            {
                playerHealth.Heal(healAmount);
                nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
                if (usesRemaining > 0) usesRemaining--;

                // optional: keep them stopped during cooldown; remove if you prefer immediate resume
                // if (agent) agent.isStopped = true;
            }
        }

        void OnGUI()
        {
            if (!showOnGuiPrompt || !player) return;

            float d = Vector3.Distance(transform.position, player.position);
            if (d > interactRange) return;

            string text;
            if (usesRemaining <= 0) text = depletedText;
            else if (Time.time < nextAllowedTime) text = "Preparing supplies...";
            else
            {
                text = promptText;
                if (showRemainingCount) text += $" ({usesRemaining} left)";
            }

            var rect = new Rect(Screen.width / 2f - 160f, Screen.height - 100f, 320f, 26f);
            GUI.Label(rect, text);
        }

        public void ResetUses(int to = -1)
        {
            usesRemaining = (to >= 0) ? to : maxUses;
        }
    }
}
