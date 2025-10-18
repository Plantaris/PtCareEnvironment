using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MyFPSCore.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float maxHealth = 100f;

        [Tooltip("If true, the GameObject is destroyed when health reaches 0.")]
        public bool destroyOnDeath = true;

        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        [Header("Damage Gating")]
        [Tooltip("Seconds of invulnerability after taking damage (0 = off).")]
        public float invulnAfterDamage = 0f;

        float _invulnerableUntil = -1f;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        [Header("Respawn")]
        [Tooltip("If true and this component is re-enabled while dead, restore to full health automatically.")]
        public bool resetOnEnableIfDead = true;

        [Tooltip("Default i-frames applied by Respawn() and the auto-reset on enable.")]
        public float respawnInvulnSeconds = 0.25f;

        // Events (unchanged)
        [global::System.Serializable] public class DamagedEvent : UnityEvent<float, GameObject, Vector3, Vector3> { } // amount, instigator, hitPoint, hitNormal
        [global::System.Serializable] public class HealthChangedEvent : UnityEvent<float, float> { }                  // current, max
        [global::System.Serializable] public class DiedEvent : UnityEvent<GameObject> { }                             // instigator

        public DamagedEvent onDamaged;
        public HealthChangedEvent onHealthChanged;
        public DiedEvent onDied;

        void OnValidate()
        {
            if (maxHealth < 1f) maxHealth = 1f;
        }

        void Awake()
        {
            // On fresh instantiation we start full.
            Current = maxHealth;
            onHealthChanged?.Invoke(Current, maxHealth);  // seed HUD
        }

        void OnEnable()
        {
            // If this object is re-enabled after "death" and not destroyed, restore automatically if desired.
            if (resetOnEnableIfDead && Current <= 0f)
            {
                Respawn(respawnInvulnSeconds);
            }
        }

        public void TakeDamage(float amount, GameObject instigator, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive || IsInvulnerable) return;
            if (amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            onDamaged?.Invoke(amount, instigator, hitPoint, hitNormal);
            onHealthChanged?.Invoke(Current, maxHealth);

            if (invulnAfterDamage > 0f)
                SetInvulnerable(invulnAfterDamage);

            if (Current <= 0f)
            {
                onDied?.Invoke(instigator);
                if (destroyOnDeath)
                    Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            if (amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            onHealthChanged?.Invoke(Current, maxHealth);
        }

        public void Kill(GameObject instigator = null)
        {
            if (!IsAlive) return;
            TakeDamage(Current, instigator, transform.position, Vector3.up);
        }

        public void SetInvulnerable(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, seconds));
        }

        /// <summary>
        /// Fully restore to maxHealth and optionally apply brief i-frames.
        /// Call this from your respawn flow if you reuse the same object.
        /// If you destroy/instantiate a new player, Awake() already resets health.
        /// </summary>
        public void Respawn(float invulnerableSeconds = 0.25f)
        {
            Current = maxHealth;
            _invulnerableUntil = (invulnerableSeconds > 0f)
                ? Time.time + invulnerableSeconds
                : -1f;

            onHealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
