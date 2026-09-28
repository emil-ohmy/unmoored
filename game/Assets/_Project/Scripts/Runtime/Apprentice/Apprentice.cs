using UnityEngine;

namespace MyGame
{
    // The player character. Tuning defaults are the "Middle" pace from the combat-feel prototype (docs/brief.md).
    // Verbs: move, i-frame dash (cancels attack recovery), 2-swing light combo, heavy; stamina; Healing charges;
    // Rest at Mooring posts. Falling in the water washes the Apprentice ashore with damage (no death on water).
    [RequireComponent(typeof(CharacterController))]
    public sealed class Apprentice : MonoBehaviour
    {
        public enum State { Free, Dashing, Attacking, Healing, Staggered, Dead }
        enum Act { None, Dash, Light, Heavy }

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float turnSpeed = 1440f;

        [Header("Dash")]
        public float dashDuration = 0.38f;
        public float dashDistance = 4f;
        public float iFrameStart = 0.05f;
        public float iFrameEnd = 0.28f;
        public float dashRecovery = 0.12f;
        public float dashStaminaCost = 20f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float staminaRegenPerSecond = 45f;
        public float staminaRegenDelay = 0.5f;

        [Header("Light attack")]
        public float lightWindup = 0.13f;
        public float lightActive = 0.10f;
        public float lightRecovery = 0.28f;
        public int lightComboLength = 2;
        public float lightDamage = 14f;
        public float lightStagger = 20f;
        public float lightStaminaCost = 15f;
        public float lightReach = 2.3f;
        public float lightArc = 70f;
        public float lightLunge = 0.5f;
        public float lightKnockback = 4f;
        public float lightHitStop = 0.06f;

        [Header("Heavy attack")]
        public float heavyWindup = 0.5f;
        public float heavyActive = 0.12f;
        public float heavyRecovery = 0.5f;
        public float heavyDamage = 35f;
        public float heavyStagger = 55f;
        public float heavyStaminaCost = 30f;
        public float heavyReach = 2.7f;
        public float heavyArc = 55f;
        public float heavyLunge = 0.9f;
        public float heavyKnockback = 10f;
        public float heavyHitStop = 0.14f;

        [Header("Health")]
        public float maxHealth = 100f;
        public float staggerDuration = 0.35f;
        public float recoilDuration = 0.3f; // when a hit bounces off a Laden's heap
        public float postHitInvulnerability = 0.5f;
        public float respawnDelay = 2.5f;

        [Header("Healing charges")]
        public int maxHealingCharges = 3;
        public float healAmount = 45f;
        public float healDuration = 0.7f;

        [Header("Feel")]
        public float inputBuffer = 0.2f;

        [Header("Water")]
        public float waterLevel = -0.35f;
        public float waterDamage = 15f;

        public IApprenticeInput Input { get; set; }
        public State CurrentState { get; private set; }
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public int HealingCharges { get; private set; }
        public MooringPost NearbyPost { get; private set; }
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10f;
        public bool IsAlive => CurrentState != State.Dead;
        public bool Invulnerable =>
            CurrentState == State.Dead
            || Time.time < invulnerableUntil
            || (CurrentState == State.Dashing && stateTime >= iFrameStart && stateTime <= iFrameEnd);

        CharacterController controller;
        ClipPlayer clips;
        GameInput ownInput;
        State stateAfterStagger;
        Act buffered;
        float bufferedAt;
        float stateTime;
        float stateDuration;
        float lastStaminaUse = -10f;
        float invulnerableUntil;
        float verticalVelocity;
        Vector3 knockback;
        Vector3 dashDirection;
        bool isHeavy;
        int comboStep;
        bool hitLanded;
        Vector3 lastSafeGround;
        float safeGroundTimer;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            clips = new ClipPlayer(GetComponentInChildren<Animation>());
            Health = maxHealth;
            Stamina = maxStamina;
            HealingCharges = maxHealingCharges;
            lastSafeGround = transform.position;
        }

        void Start()
        {
            if (Input == null)
                Input = ownInput = new GameInput();
        }

        void OnDestroy() => ownInput?.Dispose();

        void Update()
        {
            float dt = Time.deltaTime;
            ReadInput();
            stateTime += dt;
            Vector3 planar = Vector3.zero;

            switch (CurrentState)
            {
                case State.Free:
                    Vector3 move = MoveDirection();
                    planar = move * moveSpeed;
                    if (move.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(move), turnSpeed * dt);
                        clips.Loop("Run", 0.1f);
                    }
                    else
                        clips.Loop("Idle");
                    NearbyPost = MooringPost.Nearest(transform.position);
                    if (NearbyPost != null && Input.InteractPressed)
                    {
                        NearbyPost.Rest(this);
                        break;
                    }
                    if (Input.HealPressed)
                        TryHeal();
                    TryConsumeBuffer();
                    break;

                case State.Dashing:
                    if (stateTime < dashDuration)
                        planar = dashDirection * (dashDistance / dashDuration);
                    if (stateTime >= dashDuration + dashRecovery)
                        ToFreeAndConsume();
                    break;

                case State.Attacking:
                    planar = UpdateAttack();
                    break;

                case State.Healing:
                    if (stateTime >= healDuration)
                    {
                        Health = Mathf.Min(maxHealth, Health + healAmount);
                        Announce("healed");
                        ToFreeAndConsume();
                    }
                    break;

                case State.Staggered:
                    if (stateTime >= stateDuration)
                        Enter(stateAfterStagger);
                    break;

                case State.Dead:
                    if (stateTime >= respawnDelay)
                        Respawn();
                    break;
            }
            if (CurrentState != State.Free)
                NearbyPost = null;

            RegenerateStamina(dt);
            knockback *= Mathf.Exp(-10f * dt);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
            controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);
            TrackSafeGround(dt);
            if (transform.position.y < waterLevel && IsAlive)
                WashAshore();
        }

        void ReadInput()
        {
            if (Input.DashPressed) Buffer(Act.Dash);
            if (Input.LightPressed) Buffer(Act.Light);
            if (Input.HeavyPressed) Buffer(Act.Heavy);
        }

        Vector3 MoveDirection()
        {
            // The camera never rotates and looks along +Z, so stick directions map straight onto the world.
            Vector2 v = Input.Move;
            return Vector3.ClampMagnitude(new Vector3(v.x, 0f, v.y), 1f);
        }

        // ------------------------------------------------------------ attacks

        Vector3 UpdateAttack()
        {
            float windup = isHeavy ? heavyWindup : lightWindup;
            float active = isHeavy ? heavyActive : lightActive;
            float recovery = isHeavy ? heavyRecovery : lightRecovery;

            if (stateTime >= windup && stateTime < windup + active)
            {
                CheckHit();
                return transform.forward * ((isHeavy ? heavyLunge : lightLunge) / active);
            }
            if (stateTime >= windup + active && stateTime < windup + active + recovery)
            {
                bool comboAvailable = !isHeavy && comboStep < lightComboLength - 1;
                if (HasBuffered(Act.Light) && comboAvailable)
                    StartAttack(false, comboStep + 1);
                else if (HasBuffered(Act.Dash))
                    TryConsumeBuffer(); // a dash may cancel recovery
            }
            else if (stateTime >= windup + active + recovery)
                ToFreeAndConsume();
            return Vector3.zero;
        }

        void CheckHit()
        {
            if (hitLanded)
                return;
            float reach = isHeavy ? heavyReach : lightReach;
            float arc = isHeavy ? heavyArc : lightArc;
            foreach (var collider in Physics.OverlapSphere(transform.position, reach + 1f))
            {
                if (!collider.TryGetComponent(out Soured soured) || !soured.IsAlive)
                    continue;
                Vector3 to = soured.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > reach + soured.radius || Vector3.Angle(transform.forward, to) > arc)
                    continue;

                hitLanded = true;
                var result = soured.TakeHit(new Hit
                {
                    Damage = isHeavy ? heavyDamage : lightDamage,
                    Stagger = isHeavy ? heavyStagger : lightStagger,
                    Knockback = to.normalized * (isHeavy ? heavyKnockback : lightKnockback),
                    From = transform.position,
                    Heavy = isHeavy,
                });
                if (result == HitResult.Blocked)
                    Recoil(to.normalized);
                else
                {
                    HitStop.Trigger(isHeavy ? heavyHitStop : lightHitStop);
                    CameraRig.Instance?.Shake(isHeavy ? 0.25f : 0.1f);
                    Announce(isHeavy ? "HEAVY HIT" : "hit");
                }
                return; // one target per swing
            }
        }

        void Recoil(Vector3 towardsBlocker)
        {
            knockback = -towardsBlocker * 5f;
            HitStop.Trigger(0.1f);
            CameraRig.Instance?.Shake(0.15f);
            Announce("blocked");
            EnterStagger(recoilDuration, "Hit");
        }

        void StartAttack(bool heavy, int step)
        {
            buffered = Act.None;
            float cost = heavy ? heavyStaminaCost : lightStaminaCost;
            if (!CanSpend(cost))
            {
                Announce("no stamina");
                return;
            }
            Spend(cost);
            Vector3 move = MoveDirection();
            if (move.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(move); // attacks go where the stick points
            isHeavy = heavy;
            comboStep = step;
            hitLanded = false;
            Enter(State.Attacking);
            float total = heavy ? heavyWindup + heavyActive + heavyRecovery : lightWindup + lightActive + lightRecovery;
            clips.PlayOnce(heavy ? "Heavy" : (step % 2 == 0 ? "Light1" : "Light2"), total);
        }

        // ------------------------------------------------------------ buffer, stamina, healing

        void Buffer(Act act)
        {
            buffered = act;
            bufferedAt = Time.time;
        }

        bool HasBuffered(Act act) => buffered == act && Time.time - bufferedAt <= inputBuffer;

        void ToFreeAndConsume()
        {
            Enter(State.Free);
            TryConsumeBuffer();
        }

        void TryConsumeBuffer()
        {
            if (buffered == Act.None || Time.time - bufferedAt > inputBuffer)
                return;
            Act act = buffered;
            buffered = Act.None;
            switch (act)
            {
                case Act.Dash:
                    if (!CanSpend(dashStaminaCost))
                    {
                        Announce("no stamina");
                        return;
                    }
                    Spend(dashStaminaCost);
                    Vector3 move = MoveDirection();
                    dashDirection = move.sqrMagnitude > 0.001f ? move.normalized : transform.forward;
                    transform.rotation = Quaternion.LookRotation(dashDirection);
                    Enter(State.Dashing);
                    clips.PlayOnce("Dash", dashDuration);
                    break;
                case Act.Light:
                    StartAttack(false, 0);
                    break;
                case Act.Heavy:
                    StartAttack(true, 0);
                    break;
            }
        }

        // Souls rule: any stamina left lets you act; the cost may take you down to zero.
        bool CanSpend(float cost) => cost <= 0f || Stamina > 0f;

        void Spend(float cost)
        {
            Stamina = Mathf.Max(0f, Stamina - cost);
            lastStaminaUse = Time.time;
        }

        void RegenerateStamina(float dt)
        {
            bool busy = CurrentState == State.Dashing || CurrentState == State.Attacking;
            if (!busy && Time.time - lastStaminaUse >= staminaRegenDelay)
                Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * dt);
        }

        void TryHeal()
        {
            if (HealingCharges <= 0)
            {
                Announce("no Healing charges");
                return;
            }
            HealingCharges--;
            Enter(State.Healing);
            clips.PlayOnce("Heal", healDuration);
        }

        // ------------------------------------------------------------ damage, death, rest

        public bool TakeHit(float damage, Vector3 knock)
        {
            if (Invulnerable)
            {
                if (CurrentState == State.Dashing)
                    Announce("dodged");
                return false;
            }
            Health = Mathf.Max(0f, Health - damage);
            knockback = knock;
            invulnerableUntil = Time.time + postHitInvulnerability;
            buffered = Act.None;
            HitStop.Trigger(0.08f);
            CameraRig.Instance?.Shake(0.3f);
            Announce($"-{damage:0}");
            if (Health <= 0f)
                Die();
            else
                EnterStagger(staggerDuration, "Hit");
            return true;
        }

        void EnterStagger(float duration, string clip)
        {
            stateAfterStagger = State.Free;
            Enter(State.Staggered);
            stateDuration = duration;
            clips.PlayOnce(clip, duration);
        }

        void Die()
        {
            Enter(State.Dead);
            clips.PlayOnce("Death", 1.0f);
            Announce("the Apprentice fell");
        }

        void Respawn()
        {
            Vector3 point = WorldState.Instance != null ? WorldState.Instance.RespawnPosition : lastSafeGround;
            Teleport(point);
            RestoreFully();
            WorldState.Instance?.ReformAllSoured();
            Announce("woke at the Mooring post");
        }

        public void RestoreFully()
        {
            Health = maxHealth;
            Stamina = maxStamina;
            HealingCharges = maxHealingCharges;
            knockback = Vector3.zero;
            buffered = Act.None;
            Enter(State.Free);
        }

        // Moves the Apprentice instantly. `onSafeGround`: the spot is solid, so washing ashore may return here.
        public void Teleport(Vector3 position, bool onSafeGround = true)
        {
            controller.enabled = false; // CharacterController overrides direct position changes
            transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
            if (onSafeGround)
                lastSafeGround = position;
            CameraRig.Instance?.SnapToTarget();
        }

        void WashAshore()
        {
            Health = Mathf.Max(0f, Health - waterDamage);
            Teleport(lastSafeGround);
            Announce("washed ashore");
            if (Health <= 0f)
                Die();
            else
                EnterStagger(staggerDuration, "Hit");
        }

        // Remember solid ground well away from any edge, so washing ashore never lands on the brink.
        void TrackSafeGround(float dt)
        {
            safeGroundTimer -= dt;
            if (safeGroundTimer > 0f || !controller.isGrounded || CurrentState != State.Free)
                return;
            safeGroundTimer = 0.25f;
            Vector3 p = transform.position + Vector3.up * 0.5f;
            foreach (var offset in new[] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                if (!Physics.Raycast(p + offset * 0.8f, Vector3.down, 1.5f))
                    return;
            lastSafeGround = transform.position;
        }

        void Enter(State state)
        {
            CurrentState = state;
            stateTime = 0f;
        }

        void Announce(string text)
        {
            LastEvent = text;
            LastEventTime = Time.unscaledTime;
        }
    }
}
