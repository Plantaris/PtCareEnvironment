using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Combat;      // Health
using MyFPSCore.Gameplay;   // ScoreManager

namespace MyFPSCore.NPC
{
    [RequireComponent(typeof(Health))]
    public class NPCScoreReporter : MonoBehaviour
    {
        [Header("Role source")]
        public bool useRoleFromBaseNPC = true;           // read BaseNPC.role if present
        public NPCRole fallbackRole = NPCRole.Enemy;     // used if no BaseNPC or toggle off

        [Header("Points")]
        public int enemyKillPoints = 100;
        public int friendlyKillPenalty = -250;
        public int neutralKillPenalty = -100;

        Health health;
        BaseNPC baseNpc;   // optional
        bool awarded;

        void Awake()
        {
            health = GetComponent<Health>();
            baseNpc = GetComponent<BaseNPC>();
            health.onDied.AddListener(OnDied);
        }

        void OnDestroy()
        {
            if (health) health.onDied.RemoveListener(OnDied);
        }

        // Health.onDied is UnityEvent<GameObject> in your project
        void OnDied(GameObject instigator)
        {
            if (awarded) return;
            awarded = true;

            var role = (useRoleFromBaseNPC && baseNpc) ? baseNpc.role : fallbackRole;
            int delta = role == NPCRole.Enemy ? enemyKillPoints
                     : role == NPCRole.Friendly ? friendlyKillPenalty
                     : neutralKillPenalty;

            var sm = ScoreManager.Instance;
            if (sm == null)
            {
                Debug.LogWarning($"[Score] No ScoreManager in scene when {name} died. Add one to Level1.");
                return;
            }

            Debug.Log($"[Score] {name} died. Role={role}, delta={delta}");
            sm.Add(delta);
        }
    }
}
