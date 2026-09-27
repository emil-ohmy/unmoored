using System;
using System.Collections.Generic;
using UnityEngine;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
namespace MyGame.Prototypes.CombatFeel
{
    [Serializable]
    public class PacePreset
    {
        public string name;

        [Header("Movement")]
        public float moveSpeed;

        [Header("Roll")]
        public float rollDuration;
        public float rollDistance;
        [Tooltip("Invulnerable from this many seconds into the roll...")] public float iFrameStart;
        [Tooltip("...until this many seconds into the roll.")] public float iFrameEnd;
        public float rollRecovery;
        public float rollStaminaCost;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float staminaRegenPerSec;
        public float staminaRegenDelay;

        [Header("Light attack (oar sweep)")]
        public float lightWindup;
        public float lightActive;
        public float lightRecovery;
        public int lightComboLength;
        public float lightDamage;
        public float lightStaminaCost;
        public float lightReach;
        public float lightLunge;

        [Header("Heavy attack (oar swing)")]
        public float heavyWindup;
        public float heavyActive;
        public float heavyRecovery;
        public float heavyDamage;
        public float heavyStaminaCost;
        public float heavyReach;
        public float heavyLunge;

        [Header("Feel")]
        public float hitStopLight;
        public float hitStopHeavy;
        public float knockbackLight;
        public float knockbackHeavy;
        [Tooltip("How long a button press is remembered while you're busy.")] public float inputBuffer;
        [Tooltip("Can a roll interrupt an attack's recovery?")] public bool rollCancelsRecovery;

        [Header("Soured enemy")]
        public float enemyTelegraph;
        public float enemyLungeDistance;
        public float enemyRecovery;
        public float enemyDamage;
        public float enemyChaseSpeed;
    }

    // Holds the pace presets (editable in the Inspector during Play) and global hit-stop.
    public class CombatTuning : MonoBehaviour
    {
        public static CombatTuning Instance { get; private set; }

        public List<PacePreset> presets = new List<PacePreset>();
        public int current = 1;

        float hitStopUntil;

        public PacePreset P => presets[current];

        void Awake()
        {
            Instance = this;
            if (presets.Count == 0)
                presets = DefaultPresets();
        }

        void OnDestroy() => Time.timeScale = 1f;

        void Update() => Time.timeScale = Time.unscaledTime < hitStopUntil ? 0.02f : 1f;

        public void Select(int index)
        {
            current = (index % presets.Count + presets.Count) % presets.Count;
            Debug.Log($"[CombatFeel] Pace preset: {P.name}");
        }

        public void HitStop(float seconds) => hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);

        public static List<PacePreset> DefaultPresets() => new List<PacePreset>
        {
            new PacePreset
            {
                name = "Snappy (Death's Door-ish)",
                moveSpeed = 7f,
                rollDuration = 0.28f, rollDistance = 4.5f, iFrameStart = 0.02f, iFrameEnd = 0.24f, rollRecovery = 0.05f, rollStaminaCost = 10f,
                maxStamina = 100f, staminaRegenPerSec = 70f, staminaRegenDelay = 0.3f,
                lightWindup = 0.07f, lightActive = 0.08f, lightRecovery = 0.18f, lightComboLength = 3, lightDamage = 10f, lightStaminaCost = 5f, lightReach = 2.3f, lightLunge = 0.6f,
                heavyWindup = 0.35f, heavyActive = 0.12f, heavyRecovery = 0.35f, heavyDamage = 30f, heavyStaminaCost = 20f, heavyReach = 2.7f, heavyLunge = 1.0f,
                hitStopLight = 0.05f, hitStopHeavy = 0.12f, knockbackLight = 5f, knockbackHeavy = 11f, inputBuffer = 0.25f, rollCancelsRecovery = true,
                enemyTelegraph = 0.5f, enemyLungeDistance = 4f, enemyRecovery = 0.6f, enemyDamage = 20f, enemyChaseSpeed = 3.8f,
            },
            new PacePreset
            {
                name = "Middle",
                moveSpeed = 6f,
                rollDuration = 0.38f, rollDistance = 4f, iFrameStart = 0.05f, iFrameEnd = 0.28f, rollRecovery = 0.12f, rollStaminaCost = 20f,
                maxStamina = 100f, staminaRegenPerSec = 45f, staminaRegenDelay = 0.5f,
                lightWindup = 0.13f, lightActive = 0.1f, lightRecovery = 0.28f, lightComboLength = 2, lightDamage = 14f, lightStaminaCost = 15f, lightReach = 2.3f, lightLunge = 0.5f,
                heavyWindup = 0.5f, heavyActive = 0.12f, heavyRecovery = 0.5f, heavyDamage = 35f, heavyStaminaCost = 30f, heavyReach = 2.7f, heavyLunge = 0.9f,
                hitStopLight = 0.06f, hitStopHeavy = 0.14f, knockbackLight = 4f, knockbackHeavy = 10f, inputBuffer = 0.2f, rollCancelsRecovery = true,
                enemyTelegraph = 0.7f, enemyLungeDistance = 4f, enemyRecovery = 0.8f, enemyDamage = 25f, enemyChaseSpeed = 3.5f,
            },
            new PacePreset
            {
                name = "Heavy (Tunic/Souls-ish)",
                moveSpeed = 5f,
                rollDuration = 0.5f, rollDistance = 3.5f, iFrameStart = 0.08f, iFrameEnd = 0.32f, rollRecovery = 0.2f, rollStaminaCost = 25f,
                maxStamina = 100f, staminaRegenPerSec = 35f, staminaRegenDelay = 0.8f,
                lightWindup = 0.2f, lightActive = 0.1f, lightRecovery = 0.4f, lightComboLength = 2, lightDamage = 18f, lightStaminaCost = 20f, lightReach = 2.3f, lightLunge = 0.4f,
                heavyWindup = 0.7f, heavyActive = 0.14f, heavyRecovery = 0.7f, heavyDamage = 40f, heavyStaminaCost = 35f, heavyReach = 2.8f, heavyLunge = 0.8f,
                hitStopLight = 0.07f, hitStopHeavy = 0.16f, knockbackLight = 3f, knockbackHeavy = 9f, inputBuffer = 0.15f, rollCancelsRecovery = false,
                enemyTelegraph = 0.9f, enemyLungeDistance = 4f, enemyRecovery = 1.0f, enemyDamage = 30f, enemyChaseSpeed = 3.2f,
            },
        };
    }
}
