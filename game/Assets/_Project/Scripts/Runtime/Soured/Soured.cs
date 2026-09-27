using UnityEngine;

namespace MyGame
{
    // A Soured: a soul the Ferryman kept too long. Idle -> Chase -> Telegraph (animated wind-up, no ground
    // markers) -> Strike -> Recover (the punish window). A hidden Stagger meter fills with hits and drains
    // when the Apprentice lets up; full = staggered, attack interrupted. Defeat only scatters it: it re-forms
    // when the Apprentice Rests or dies.
    [RequireComponent(typeof(CharacterController))]
    public class Soured : MonoBehaviour
    {
        public enum State { Idle, Chase, Telegraph, Strike, Recover, Staggered, Scattered }

        [Header("Health")]
        public float maxHealth = 100f;
        public float radius = 0.6f;
        [Range(0f, 1f)] public float knockbackResistance = 0f;

        [Header("Stagger meter")]
        public float staggerCapacity = 60f;
        public float staggerDrainDelay = 1.2f;
        public float staggerDrainPerSecond = 30f;
        public float staggerDuration = 1.0f;

        [Header("Behaviour")]
        public float aggroRadius = 9f;
        public float attackRange = 2.2f;
        public float chaseSpeed = 2.4f;
        public float turnSpeed = 360f;

        [Header("Attack (durations match the model's clips)")]
        public float telegraphDuration = 0.9f;
        public float strikeDuration = 0.3f;
        public float recoverDuration = 1.2f;
        [Range(0f, 1f)] public float impactAt = 0.4f; // fraction of the strike when the blow lands
        public float lungeDistance = 0.6f;
        public float damage = 25f;
        public float strikeReach = 2.3f;
        public float strikeArc = 60f;
        public float playerKnockback = 7f;

        public State CurrentState { get; private set; }
        public float Health { get; private set; }
        public float Stagger { get; private set; }
        public bool IsAlive => CurrentState != State.Scattered;

        protected ClipPlayer clips;
        protected Apprentice target;
        CharacterController controller;
        Renderer[] renderers;
        MaterialPropertyBlock flash;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Vector3 lockedDirection;
        Vector3 knockback;
        float verticalVelocity;
        float stateTime;
        float lastHitTime = -10f;
        float flashUntil;
        float oneShotUntil; // a short clip (e.g. Block) is playing; don't override it with Idle/Walk
        bool struck;

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            clips = new ClipPlayer(GetComponentInChildren<Animation>());
            renderers = GetComponentsInChildren<Renderer>();
            flash = new MaterialPropertyBlock();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            Health = maxHealth;
        }

        void OnEnable() => WorldState.Register(this);
        void OnDisable() => WorldState.Unregister(this);

        void Start() => target = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            if (Time.time - lastHitTime > staggerDrainDelay)
                Stagger = Mathf.Max(0f, Stagger - staggerDrainPerSecond * dt);

            Vector3 toTarget = target != null ? Flat(target.transform.position - transform.position) : Vector3.zero;
            bool targetAlive = target != null && target.IsAlive;
            Vector3 planar = Vector3.zero;

            switch (CurrentState)
            {
                case State.Idle:
                    LoopUnlessBusy("Idle");
                    if (targetAlive && toTarget.magnitude < aggroRadius)
                        Enter(State.Chase);
                    break;

                case State.Chase:
                    if (!targetAlive || toTarget.magnitude > aggroRadius * 1.5f)
                    {
                        Enter(State.Idle);
                        break;
                    }
                    LoopUnlessBusy("Walk");
                    Face(toTarget, turnSpeed * dt);
                    if (toTarget.magnitude <= attackRange)
                    {
                        lockedDirection = toTarget.normalized; // locked now: sidestepping is always an answer
                        Enter(State.Telegraph);
                        clips.PlayOnce("Windup", telegraphDuration);
                    }
                    else
                        planar = toTarget.normalized * chaseSpeed;
                    break;

                case State.Telegraph:
                    Face(lockedDirection, turnSpeed * 2f * dt);
                    if (stateTime >= telegraphDuration)
                    {
                        struck = false;
                        Enter(State.Strike);
                        clips.PlayOnce("Strike", strikeDuration);
                    }
                    break;

                case State.Strike:
                    planar = lockedDirection * (lungeDistance / strikeDuration);
                    if (!struck && stateTime >= impactAt * strikeDuration)
                    {
                        struck = true;
                        if (targetAlive)
                            StrikeTarget(toTarget);
                    }
                    if (stateTime >= strikeDuration)
                    {
                        Enter(State.Recover);
                        clips.PlayOnce("Recover", recoverDuration);
                    }
                    break;

                case State.Recover:
                    if (stateTime >= recoverDuration)
                        Enter(State.Chase);
                    break;

                case State.Staggered:
                    if (stateTime >= staggerDuration)
                        Enter(State.Chase);
                    break;

                case State.Scattered:
                    return;
            }

            knockback *= Mathf.Exp(-8f * dt);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
            controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);
            UpdateFlash();
        }

        protected virtual void StrikeTarget(Vector3 toTarget)
        {
            if (toTarget.magnitude <= strikeReach + 0.35f && Vector3.Angle(transform.forward, toTarget) <= strikeArc)
                target.TakeHit(damage, lockedDirection * playerKnockback);
        }

        public HitResult TakeHit(Hit hit)
        {
            if (!IsAlive)
                return HitResult.Missed;
            if (BlocksHit(hit))
            {
                PlayShort("Block", 0.3f);
                return HitResult.Blocked;
            }

            Health -= hit.Damage;
            knockback = hit.Knockback * (1f - knockbackResistance);
            Stagger += hit.Stagger;
            lastHitTime = Time.time;
            flashUntil = Time.time + 0.08f;

            if (Health <= 0f)
                Scatter();
            else if (Stagger >= staggerCapacity && CurrentState != State.Staggered)
            {
                Stagger = 0f;
                Enter(State.Staggered);
                clips.PlayOnce("Stagger", staggerDuration);
            }
            else if (CurrentState == State.Idle)
                Enter(State.Chase); // hitting a sleeper wakes it
            return HitResult.Landed;
        }

        protected virtual bool BlocksHit(Hit hit) => false;

        void Scatter()
        {
            Enter(State.Scattered);
            foreach (var r in renderers)
                r.enabled = false;
            controller.enabled = false;
        }

        // Back to its spawn, whole again (the Ferryman's hoard pulls it back).
        public void Reform()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            foreach (var r in renderers)
                r.enabled = true;
            Health = maxHealth;
            Stagger = 0f;
            knockback = Vector3.zero;
            Enter(State.Idle);
            clips.Loop("Idle", 0f);
        }

        protected void PlayShort(string clip, float duration)
        {
            if (CurrentState != State.Idle && CurrentState != State.Chase)
                return; // don't interrupt attacks or staggers with a reaction clip
            clips.PlayOnce(clip, duration);
            oneShotUntil = Time.time + duration;
        }

        void LoopUnlessBusy(string clip)
        {
            if (Time.time >= oneShotUntil)
                clips.Loop(clip);
        }

        void UpdateFlash()
        {
            bool on = Time.time < flashUntil;
            foreach (var r in renderers)
            {
                if (on)
                {
                    flash.SetColor("_EmissionColor", new Color(0.7f, 0.7f, 0.7f));
                    r.SetPropertyBlock(flash);
                }
                else
                    r.SetPropertyBlock(null);
            }
        }

        void Face(Vector3 direction, float maxDegrees)
        {
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), maxDegrees);
        }

        void Enter(State state)
        {
            CurrentState = state;
            stateTime = 0f;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
