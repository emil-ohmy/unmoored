using UnityEngine;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
namespace MyGame.Prototypes.CombatFeel
{
    // One Soured with a single, readable attack: it telegraphs (red line grows on the ground,
    // body turns red), lunges along that locked line, then recovers (your punish window).
    // Light hits don't interrupt it; heavy hits stagger it.
    [RequireComponent(typeof(CharacterController))]
    public class ProtoSoured : MonoBehaviour
    {
        public enum State { Idle, Chase, Telegraph, Lunge, Recover, Staggered, Scattered }

        public Renderer body;
        public Transform indicator;
        public float maxHealth = 100f;
        public float attackRange = 3.5f;
        public float lungeSpeed = 14f;
        public float radius = 0.65f;
        public float staggerDuration = 0.7f;
        public float reformDelay = 3f;

        public State CurrentState { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => CurrentState != State.Scattered;
        public float StateTime => stateTime;

        PacePreset P => CombatTuning.Instance.P;

        CharacterController controller;
        ProtoPlayer player;
        Material material;
        Color baseColor;
        Color flashColor;
        float flashUntil;
        float stateTime;
        float idleFor;
        Vector3 lockedDirection;
        Vector3 lungeStart;
        bool lungeHit;
        Vector3 knockback;
        float verticalVelocity;
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        void Start()
        {
            player = FindAnyObjectByType<ProtoPlayer>();
            material = body.material;
            baseColor = material.GetColor("_BaseColor");
            ResetEnemy();
        }

        public void ResetEnemy()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            body.enabled = true;
            Health = maxHealth;
            knockback = Vector3.zero;
            idleFor = 1f;
            EnterState(State.Idle);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            Vector3 planar = Vector3.zero;
            Color color = baseColor;
            bool playerAlive = player.CurrentState != ProtoPlayer.State.Dead;

            switch (CurrentState)
            {
                case State.Idle:
                    if (stateTime >= idleFor && playerAlive)
                        EnterState(State.Chase);
                    break;

                case State.Chase:
                    if (!playerAlive) { EnterState(State.Idle); break; }
                    Face(toPlayer, 540f * dt);
                    if (toPlayer.magnitude <= attackRange)
                    {
                        lockedDirection = toPlayer.normalized; // locked here: dodging sideways is always an answer
                        lungeStart = transform.position;
                        EnterState(State.Telegraph);
                    }
                    else
                        planar = toPlayer.normalized * P.enemyChaseSpeed;
                    break;

                case State.Telegraph:
                    Face(lockedDirection, 1080f * dt);
                    float charge = Mathf.Clamp01(stateTime / P.enemyTelegraph);
                    color = Color.Lerp(baseColor, new Color(1f, 0.15f, 0.1f), charge);
                    ShowIndicator(charge);
                    if (stateTime >= P.enemyTelegraph)
                    {
                        lungeHit = false;
                        EnterState(State.Lunge);
                    }
                    break;

                case State.Lunge:
                    color = new Color(1f, 0.5f, 0.4f);
                    ShowIndicator(1f);
                    planar = lockedDirection * lungeSpeed;
                    if (!lungeHit && toPlayer.magnitude <= radius + 0.5f + 0.35f)
                        lungeHit = player.TakeHit(P.enemyDamage, lockedDirection * 8f);
                    if (Vector3.Distance(lungeStart, transform.position) >= P.enemyLungeDistance || stateTime > 1f)
                        EnterState(State.Recover);
                    break;

                case State.Recover:
                    color = Color.Lerp(baseColor, Color.gray, 0.5f);
                    if (stateTime >= P.enemyRecovery)
                    {
                        idleFor = Random.Range(0.3f, 0.8f);
                        EnterState(State.Idle);
                    }
                    break;

                case State.Staggered:
                    color = Color.Lerp(baseColor, Color.yellow, 0.4f);
                    if (stateTime >= staggerDuration)
                    {
                        idleFor = 0.2f;
                        EnterState(State.Idle);
                    }
                    break;

                case State.Scattered:
                    if (stateTime >= reformDelay)
                        ResetEnemy();
                    break;
            }

            if (CurrentState != State.Telegraph && CurrentState != State.Lunge)
                indicator.gameObject.SetActive(false);

            knockback *= Mathf.Exp(-10f * dt);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
            if (controller.enabled)
                controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);

            material.SetColor("_BaseColor", Time.time < flashUntil ? flashColor : color);
        }

        public void TakeHit(float damage, Vector3 knock, bool heavy)
        {
            if (!IsAlive)
                return;

            Health -= damage;
            knockback = knock;
            flashColor = Color.white;
            flashUntil = Time.time + 0.08f;

            if (Health <= 0f)
            {
                body.enabled = false;
                indicator.gameObject.SetActive(false);
                EnterState(State.Scattered); // scattered, not freed: it re-forms, as the Soured do
            }
            else if (heavy && CurrentState != State.Lunge)
                EnterState(State.Staggered); // heavy hits break a telegraph; light hits don't
        }

        void ShowIndicator(float fill)
        {
            float length = P.enemyLungeDistance + radius;
            float current = Mathf.Max(0.05f, length * fill);
            indicator.gameObject.SetActive(true);
            indicator.SetPositionAndRotation(
                lungeStart + lockedDirection * (current / 2f) + Vector3.up * (0.02f - lungeStart.y),
                Quaternion.LookRotation(lockedDirection));
            indicator.localScale = new Vector3(radius * 2f, 0.02f, current);
        }

        void Face(Vector3 direction, float maxDegrees)
        {
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), maxDegrees);
        }

        void EnterState(State state)
        {
            CurrentState = state;
            stateTime = 0f;
        }
    }
}
