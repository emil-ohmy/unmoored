using UnityEngine;

namespace MyGame
{
    // The player character. Tuning defaults are the "Middle" pace from the combat-feel prototype (docs/brief.md).
    // Verbs: move, i-frame dash (cancels attack recovery), 2-swing light combo, heavy, block (a perfect block in its
    // first 0.2 s stuns the attacker; hits on a stunned Soured are double-damage ripostes); stamina; Healing charges;
    // Rest at Mooring posts. Falling in the water washes the Apprentice ashore with damage (no death on water).
    [RequireComponent(typeof(CharacterController))]
    public sealed class Apprentice : MonoBehaviour
    {
        public enum State { Free, Dashing, Attacking, Healing, Blocking, Staggered, Dead, Hooking, PickingUp, Ceremony, Sinking }
        enum Act { None, Dash, Light, Heavy }

        [Header("Movement")]
        public float moveSpeed = 5f; // play-test: 6 blasted through the level
        const float RunClipSpeed = 6f; // the speed the Run clip's stride was animated for
        public float turnSpeed = 1440f;

        [Header("Dash")]
        public float dashDuration = 0.38f;
        public float dashDistance = 2.8f; // short enough that the Laden can track an early dash: dodging is timing (play-test)
        public float iFrameStart = 0f; // from the first frame: a dash pressed just before the blow must work (play-test)
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
        public float lightReach = 1.8f;   // from the target's surface: about as far as the swung oar really reaches
        [Range(0f, 1f)] public float lightContact = 0.4f; // share of the active window when the oar meets its target
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
        public float heavyReach = 1.9f;   // from the target's surface, after the lunge: where the slammed blade lands
        [Range(0f, 1f)] public float heavyContact = 0.85f; // the blade comes down late in the active window (the clip)
        public float heavyArc = 55f;
        public float heavyLunge = 0.9f;
        public float heavyKnockback = 10f;
        public float heavyHitStop = 0.14f;

        [Header("Health")]
        public float maxHealth = 100f;
        public float staggerDuration = 0.35f;
        public float recoilDuration = 0.35f; // when a hit bounces off a Laden's heap
        public float recoilPush = 14f; // ~1.4 m shove: knockback decays at 10/s
        public float postHitInvulnerability = 0.5f;
        public float respawnDelay = 4.4f; // the fall, the lantern going out, the phrase, then the sink to black (Hud)
        public float deathDuration = 2.2f; // the Death clip: reel, knees, the oar dropped, the slump
        public float snuffDelay = 0.8f;    // kneeling: then the bow lantern gutters out
        public float snuffDuration = 1.1f;

        [Header("Block")]
        public float perfectWindow = 0.2f;       // a blow landing this soon after raising the block is perfect-blocked
        public float blockSpamCooldown = 0.4f;   // re-raising sooner than this after lowering gives no perfect window
        public float blockMoveFactor = 0.35f;
        public float blockChip = 0.3f;           // share of damage that gets through a normal block
        public float blockStaminaCost = 25f;
        public float guardBreakDuration = 0.8f;  // blocking with no stamina left breaks the guard
        [Range(0f, 180f)] public float blockHalfAngle = 100f;

        [Header("Healing charges")]
        public int maxHealingCharges = 3;
        public float healAmount = 45f;
        public float healDuration = 0.7f;

        [Header("Lock-on")]
        public float lockRange = 10f;
        public float lockBreakRange = 12f;
        public float lockOffScreenTime = 0.6f; // off the screen this long, and the lock lets go (the camera comes back)
        public float lockedMoveSpeedFactor = 0.85f; // a little slower while circling a target

        [Header("Hook (an Ability: pull yourself to a hook post, over water too)")]
        public float hookRange = 9f;
        public float hookAimAngle = 70f;   // posts this far either side of where you face (or steer) can be aimed at
        public float hookAimMoveFactor = 0.5f; // walking slower while aiming
        public float hookThrowTime = 0.15f; // the tether flying out
        public float hookSpeed = 13f;
        public float hookArc = 0.6f;       // the pull lifts you a little, like a leap

        [Header("Hook visuals")]
        public GameObject hookHeadModel; // the Hook's head in flight (spawned per throw)

        [Header("Taking things")]
        public float pickupDuration = 0.6f;  // the stoop to pick an item up; it's taken at the bottom
        public float ceremonyDuration = 2.4f; // a key item held up

        [Header("Feel")]
        public float inputBuffer = 0.2f;

        [Header("Water")]
        public float waterLevel = -0.35f;
        [Range(0f, 1f)] public float waterDamage = 1f / 3f; // of max health: a hard lesson at full, the end when low
        public float washAshoreStagger = 0.9f;             // coughing up the river before you can move
        public float sinkTime = 0.5f;                      // gone under, before you wash ashore (behind a dip to black)

        public IApprenticeInput Input { get; set; }
        public State CurrentState { get; private set; }
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public int HealingCharges { get; private set; }
        public IInteractable Nearby { get; private set; } // what Interact would use now (its prompt shows)
        public MooringPost NearbyPost => Nearby as MooringPost;
        public Merchant NearbyMerchant => Nearby as Merchant;
        public Equipment Equipment { get; private set; }
        public Menu OpenMenu { get; private set; } // while open, the Apprentice stands still and input drives the menu
        public float DashCost => dashStaminaCost * (Equipment.IsFitted(Charm.EelSkin) ? 0.5f : 1f);
        public float DamageFactor => Equipment.IsFitted(Charm.TestersKnot) ? 4f : 1f; // (a development Charm)
        public Soured LockTarget { get; private set; }
        public string LastEvent { get; private set; } = ""; // what just happened, for debugging (not shown)
        public float LastEventTime { get; private set; } = -10f;
        public bool IsAlive => CurrentState != State.Dead;
        public bool HasHook { get; private set; }
        public HookPost HookTarget { get; private set; } // with the Hook: the post it would pull to (ringed; brighter while aiming)
        public bool HookShown => hookBone != null && hookBone.localScale.x > 0.5f; // at the hip (not before it's found, nor in flight)
        public bool AimingHook { get; private set; }
        public event System.Action<string, string> Found; // something gained for good: a title and a hint (HUD banner)
        public event System.Action Died, Woke; // woke: back at the Mooring post after dying
        public event System.Action Healed;      // a Healing charge took effect (the end of the heal)
        public int Obols { get; private set; }
        public event System.Action<int> ObolsChanged; // by how much (+ gained, - spent or dropped)
        public bool Invulnerable =>
            CurrentState == State.Dead
            || Time.time < invulnerableUntil
            || (CurrentState == State.Dashing && stateTime >= iFrameStart && stateTime <= iFrameEnd)
            || (CurrentState == State.Hooking && stateTime >= hookThrowTime) // pulled clear of harm
            || CurrentState == State.Ceremony
            || CurrentState == State.Sinking;

        // How dark the screen dips around washing ashore: to black as you go under, back as you come up (the HUD).
        public float Dip => CurrentState == State.Sinking
            ? UiKit.Smooth(0.05f, sinkTime * 0.9f, stateTime)
            : 1f - UiKit.Smooth(0.1f, 0.7f, Time.time - washedAt);

        CharacterController controller;
        ClipPlayer clips;
        GameInput ownInput;
        State stateAfterStagger;
        Act buffered;
        float bufferedAt;
        float stateTime;
        float stateDuration;
        bool snuffSounded;
        float lastStaminaUse = -10f;
        float invulnerableUntil;
        float verticalVelocity;
        Vector3 knockback;
        Vector3 dashDirection;
        bool isHeavy;
        int comboStep;
        bool hitLanded;
        bool swingSounded;
        float blockStartedAt;
        float lastBlockRelease = -10f;
        bool perfectArmed;
        float blockClipUntil;
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
            Equipment = GetComponent<Equipment>();
            if (Equipment == null)
                Equipment = gameObject.AddComponent<Equipment>();
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name == "Hook")
                    hookBone = t; // the rig's Hook bone (the Hook at the hip, whirled, held up)
            swingTrail = GetComponent<SwingTrail>();
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (bodyRenderer != null)
                hookSlot = System.Array.FindIndex(bodyRenderer.sharedMaterials, m => m != null && m.name.StartsWith("HookIron"));
            hookGlow = new MaterialPropertyBlock();
        }

        public void ShowMenu(Menu menu)
        {
            OpenMenu?.Close();
            OpenMenu = menu;
        }

        void CloseMenu()
        {
            OpenMenu?.Close();
            OpenMenu = null;
        }

        void Start()
        {
            if (Input == null)
                Input = ownInput = new GameInput();
        }

        void OnDestroy() => ownInput?.Dispose();

        void Update()
        {
            if (Pause.IsPaused)
                return; // (the pause menu has the controls)
            float dt = Time.deltaTime;
            if (OpenMenu != null && (!OpenMenu.IsOpen || CurrentState != State.Free))
                CloseMenu();
            bool inMenu = OpenMenu != null;
            if (!inMenu)
            {
                ReadInput();
                UpdateLockOn();
            }
            stateTime += dt;
            Vector3 planar = Vector3.zero;

            switch (CurrentState)
            {
                case State.Free when inMenu:
                    clips.Loop("Idle", 0.1f);
                    OpenMenu.Tick(Input);
                    if (!OpenMenu.IsOpen)
                        OpenMenu = null;
                    break;

                case State.Free:
                    Vector3 move = MoveDirection();
                    bool wasAiming = AimingHook;
                    AimingHook = HasHook && Input.HookHeld;
                    planar = move * moveSpeed * (LockTarget != null ? lockedMoveSpeedFactor : 1f) * (AimingHook ? hookAimMoveFactor : 1f);
                    Vector3 face = LockTarget != null ? ToLockTarget() : move; // locked: strafe, always facing it
                    if (face.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), turnSpeed * dt);
                    if (AimingHook)
                        clips.Loop("HookAim", 0.1f); // the Hook whirled on its chain
                    else
                    {
                        clips.Loop(move.sqrMagnitude > 0.001f ? "Run" : "Idle", 0.1f);
                        if (move.sqrMagnitude > 0.001f)
                            clips.SetLoopSpeed(Mathf.Max(0.35f, planar.magnitude / RunClipSpeed)); // no foot-sliding
                    }
                    Nearby = Interactables.Nearest(transform.position);
                    if (Nearby != null && Input.InteractPressed)
                    {
                        Nearby.Interact(this);
                        break;
                    }
                    // With the Hook, the post in reach ahead (on other ground) is ringed; hold to aim, let go to throw
                    // (a quick tap too).
                    if (wasAiming && !AimingHook && HookTarget != null)
                    {
                        StartHook(HookTarget);
                        break;
                    }
                    HookTarget = HasHook ? HookPost.Best(transform.position, move.sqrMagnitude > 0.01f ? move : transform.forward, hookRange, hookAimAngle) : null;
                    if (Input.HealPressed)
                        TryHeal();
                    else if (Input.BlockHeld)
                        StartBlock();
                    TryConsumeBuffer();
                    break;

                case State.Blocking:
                    if (!Input.BlockHeld)
                    {
                        lastBlockRelease = Time.time;
                        Enter(State.Free);
                        break;
                    }
                    Vector3 step = MoveDirection();
                    planar = step * moveSpeed * blockMoveFactor;
                    Vector3 faceBlock = LockTarget != null ? ToLockTarget() : step;
                    if (faceBlock.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(faceBlock), turnSpeed * 0.5f * dt);
                    if (Time.time >= blockClipUntil)
                        clips.Loop("Block", 0.05f);
                    TryConsumeBuffer(); // attacking or dashing lowers the block
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
                        Healed?.Invoke();
                        ToFreeAndConsume();
                    }
                    break;

                case State.Staggered:
                    if (stateTime >= stateDuration)
                        Enter(stateAfterStagger);
                    break;

                case State.Sinking:
                    if (stateTime >= sinkTime)
                        WashAshore(); // behind the black
                    break;

                case State.Dead:
                    if (!snuffSounded && stateTime >= snuffDelay)
                    {
                        snuffSounded = true;
                        Sfx.Instance?.LanternSnuff(transform.position + Vector3.up * 0.6f);
                    }
                    if (stateTime >= respawnDelay)
                        Respawn();
                    break;

                case State.Hooking:
                    UpdateHook();
                    break;

                case State.PickingUp:
                    if (pendingTake != null && stateTime >= pickupDuration * 0.5f)
                    {
                        var take = pendingTake;
                        pendingTake = null;
                        Sfx.Instance?.ItemTake(transform.position + Vector3.up * 0.4f);
                        take();
                    }
                    if (stateTime >= pickupDuration)
                        ToFreeAndConsume();
                    break;

                case State.Ceremony:
                    UpdateCeremony();
                    break;
            }
            if (CurrentState != State.Free || inMenu)
            {
                Nearby = null;
                if (CurrentState != State.Hooking)
                {
                    HookTarget = null;
                    AimingHook = false;
                }
            }
            if (CurrentState != State.Hooking)
            {
                if (tether != null)
                    tether.enabled = false;
                if (hookHead != null)
                    Destroy(hookHead); // (a throw cut short by a blow)
                hookInFlight = false;
            }
            UpdateWhirl();
            if (swingTrail != null)
            {
                // The smear shows through the swing's active part (a touch either side, so it reads as one arc).
                float windup = isHeavy ? heavyWindup : lightWindup, active = isHeavy ? heavyActive : lightActive;
                swingTrail.Emitting = CurrentState == State.Attacking && stateTime >= windup * 0.8f && stateTime <= windup + active + 0.05f;
                swingTrail.lifetime = isHeavy ? 0.18f : 0.16f;
            }
            FrameHookTarget();

            RegenerateStamina(dt);
            knockback *= Mathf.Exp(-10f * dt);
            if (CurrentState == State.Hooking)
            {
                verticalVelocity = 0f;
                controller.Move(hookStep); // the pull sets the path; collisions still apply
                hookStep = Vector3.zero;
            }
            else
            {
                verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
                if (CurrentState == State.Sinking)
                {
                    planar = knockback = Vector3.zero; // going under, slowly
                    verticalVelocity = -1.2f;
                }
                if (Wading && CurrentState != State.Dashing && CurrentState != State.Dead)
                    planar = planar * wadeFactor + wadeDrift; // (a dash cuts through it)
                controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);
            }
            TrackSafeGround(dt);
            if (transform.position.y < waterLevel && IsAlive && CurrentState != State.Hooking && CurrentState != State.Sinking)
                GoUnder();
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
                if (!swingSounded)
                {
                    swingSounded = true; // the whoosh starts as the oar starts to move
                    Sfx.Instance?.OarSwing(transform.position + Vector3.up, isHeavy);
                }
                // The blow lands at the clip's contact moment (after most of the lunge), not the first active frame.
                if (stateTime >= windup + active * (isHeavy ? heavyContact : lightContact))
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
                bool riposte = soured.CurrentState == Soured.State.Stunned;
                var result = soured.TakeHit(new Hit
                {
                    Damage = (isHeavy ? heavyDamage : lightDamage) * DamageFactor,
                    Stagger = isHeavy ? heavyStagger : lightStagger,
                    Knockback = to.normalized * (isHeavy ? heavyKnockback : lightKnockback),
                    From = transform.position,
                    Heavy = isHeavy,
                });
                if (result == HitResult.Blocked)
                    Recoil(to.normalized);
                else
                {
                    HitStop.Trigger(riposte ? 0.16f : isHeavy ? heavyHitStop : lightHitStop);
                    CameraRig.Instance?.Shake(riposte || isHeavy ? 0.25f : 0.1f);
                    Announce(riposte ? "RIPOSTE" : isHeavy ? "HEAVY HIT" : "hit");
                }
                return; // one target per swing
            }
        }

        void Recoil(Vector3 towardsBlocker)
        {
            knockback = -towardsBlocker * recoilPush;
            HitStop.Trigger(0.12f);
            CameraRig.Instance?.Shake(0.22f);
            Announce("blocked");
            EnterStagger(recoilDuration, isHeavy ? "RecoilHeavy" : "RecoilLight"); // from the contact pose: seen to land, then bounce
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
            Vector3 aim = LockTarget != null ? ToLockTarget() : MoveDirection(); // locked: every swing goes at the target
            if (aim.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(aim); // otherwise attacks go where the stick points
            isHeavy = heavy;
            comboStep = step;
            hitLanded = false;
            swingSounded = false;
            Enter(State.Attacking);
            float total = heavy ? heavyWindup + heavyActive + heavyRecovery : lightWindup + lightActive + lightRecovery;
            clips.PlayOnce(heavy ? "Heavy" : (step % 2 == 0 ? "Light1" : "Light2"), total);
        }

        // ------------------------------------------------------------ lock-on

        void UpdateLockOn()
        {
            if (!IsAlive)
            {
                SetLockTarget(null);
                return;
            }
            if (Input.LockPressed)
                SetLockTarget(LockTarget != null ? null : BestTarget(null));

            int direction = Input.SwitchTarget;
            if (direction != 0 && LockTarget != null)
                SetLockTarget(Neighbour(direction) ?? LockTarget);

            if (LockTarget != null)
            {
                if (!LockTarget.IsAlive || !LockTarget.isActiveAndEnabled)
                    SetLockTarget(BestTarget(LockTarget)); // it scattered: move on to the next one nearby, if any
                else if (ToLockTarget().magnitude > lockBreakRange || OffScreenTooLong())
                    SetLockTarget(null);
            }
        }

        float offScreenFor;

        // A lock on something that has left the screen lets go after a moment, so the camera comes back to you.
        bool OffScreenTooLong()
        {
            var cam = Camera.main;
            if (cam == null || LockTarget == null)
                return false;
            Vector3 v = cam.WorldToViewportPoint(LockTarget.transform.position + Vector3.up);
            bool off = v.z <= 0f || v.x < -0.02f || v.x > 1.02f || v.y < -0.02f || v.y > 1.02f;
            offScreenFor = off ? offScreenFor + Time.deltaTime : 0f;
            if (offScreenFor < lockOffScreenTime)
                return false;
            offScreenFor = 0f;
            return true;
        }

        void SetLockTarget(Soured target)
        {
            LockTarget = target;
            framedHook = null; // lock-on framing takes over (and hands back to the Hook's when released)
            CameraRig.Instance?.SetSecondary(target != null ? target.transform : null);
        }

        Vector3 ToLockTarget()
        {
            Vector3 to = LockTarget.transform.position - transform.position;
            to.y = 0f;
            return to;
        }

        // The Soured most likely meant: close, and in the direction the Apprentice is heading or facing.
        Soured BestTarget(Soured exclude)
        {
            Vector3 heading = MoveDirection().sqrMagnitude > 0.001f ? MoveDirection() : transform.forward;
            Soured best = null;
            float bestScore = float.MaxValue;
            foreach (var s in WorldState.AllSoured)
            {
                if (s == exclude || !s.IsAlive || !s.isActiveAndEnabled)
                    continue;
                Vector3 to = s.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > lockRange)
                    continue;
                float score = to.magnitude + Vector3.Angle(heading, to) / 45f;
                if (score < bestScore)
                {
                    best = s;
                    bestScore = score;
                }
            }
            return best;
        }

        // The next target clockwise (+1, to the right on screen) or anticlockwise (-1) from the current one.
        Soured Neighbour(int direction)
        {
            Vector3 current = ToLockTarget();
            Soured best = null;
            float bestAngle = float.MaxValue;
            foreach (var s in WorldState.AllSoured)
            {
                if (s == LockTarget || !s.IsAlive || !s.isActiveAndEnabled)
                    continue;
                Vector3 to = s.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > lockRange)
                    continue;
                float angle = Vector3.SignedAngle(current, to, Vector3.up) * direction;
                if (angle > 0f && angle < bestAngle)
                {
                    best = s;
                    bestAngle = angle;
                }
            }
            return best;
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
                    if (!CanSpend(DashCost))
                    {
                        Announce("no stamina");
                        return;
                    }
                    Spend(DashCost);
                    Vector3 move = MoveDirection();
                    if (move.sqrMagnitude > 0.001f)
                        dashDirection = move.normalized;
                    else
                        dashDirection = LockTarget != null ? -ToLockTarget().normalized : transform.forward; // locked: backstep
                    transform.rotation = Quaternion.LookRotation(dashDirection);
                    Enter(State.Dashing);
                    Sfx.Instance?.Dash(transform.position);
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
            float rate = CurrentState == State.Blocking ? 0.4f : 1f; // holding a block slows recovery
            if (!busy && Time.time - lastStaminaUse >= staminaRegenDelay)
                Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * rate * dt);
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
            Sfx.Instance?.Heal(transform.position + Vector3.up * 0.7f);
        }

        // ------------------------------------------------------------ damage, death, rest

        float wadeFactor = 1f;
        Vector3 wadeDrift;
        int wadeFrame = -10;
        bool Wading => Time.frameCount - wadeFrame <= 1;

        // Wading through something that drags at you (the Anchoress's Undertow), for this frame: slower, and pulled.
        public void Wade(float factor, Vector3 drift)
        {
            wadeFactor = factor;
            wadeDrift = drift;
            wadeFrame = Time.frameCount;
        }

        // Harm over time (standing in something that hurts): no stagger, no knockback, and no blocking it; only a
        // dash's i-frames slip it. True if it hurt.
        public bool TakeTickDamage(float damage)
        {
            if (!IsAlive || CurrentState == State.Ceremony
                || (CurrentState == State.Dashing && stateTime >= iFrameStart && stateTime <= iFrameEnd))
                return false;
            Health = Mathf.Max(0f, Health - damage);
            Sfx.Instance?.Hurt(transform.position + Vector3.up, 0.4f);
            CameraRig.Instance?.Shake(0.05f);
            Announce($"-{damage:0} (over time)");
            if (Health <= 0f)
                Die();
            return true;
        }

        public Defence TakeHit(float damage, Vector3 knock, Soured attacker = null)
        {
            if (Invulnerable)
            {
                if (CurrentState == State.Dashing)
                    Announce("dodged");
                return Defence.Dodged;
            }
            if (CurrentState == State.Blocking && Facing(attacker != null ? attacker.transform.position : transform.position - knock))
                return Block(damage, knock, attacker);
            Health = Mathf.Max(0f, Health - damage);
            CloseMenu(); // a blow ends any browsing
            Sfx.Instance?.Hurt(transform.position + Vector3.up);
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
            return Defence.Taken;
        }

        void StartBlock()
        {
            Enter(State.Blocking);
            blockStartedAt = Time.time;
            perfectArmed = Time.time - lastBlockRelease >= blockSpamCooldown; // no mashing your way to a parry
            clips.Loop("Block", 0.05f);
        }

        bool Facing(Vector3 from)
        {
            Vector3 to = from - transform.position;
            to.y = 0f;
            return to.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, to) <= blockHalfAngle;
        }

        Defence Block(float damage, Vector3 knock, Soured attacker)
        {
            Vector3 towards = attacker != null ? attacker.transform.position - transform.position : -knock;
            towards.y = 0f;
            Vector3 contact = transform.position + towards.normalized * 0.6f + Vector3.up * 0.9f;
            if (perfectArmed && Time.time - blockStartedAt <= perfectWindow)
            {
                HitStop.Trigger(0.18f);
                CameraRig.Instance?.Shake(0.25f);
                Effects.Instance?.ParryFlash(contact, towards.normalized);
                Sfx.Instance?.Parry(contact);
                clips.PlayOnce("Parry", 0.3f);
                blockClipUntil = Time.time + 0.3f;
                invulnerableUntil = Time.time + 0.2f;
                Announce("PERFECT BLOCK");
                return Defence.Parried; // the attacker is stunned by the caller
            }

            Stamina = Mathf.Max(0f, Stamina - blockStaminaCost);
            lastStaminaUse = Time.time;
            Health = Mathf.Max(0f, Health - damage * blockChip);
            knockback = knock * 0.6f;
            HitStop.Trigger(0.06f);
            CameraRig.Instance?.Shake(0.12f);
            Effects.Instance?.BlockSpark(contact, towards.normalized);
            Sfx.Instance?.BlockClang(contact, 0.7f);
            if (Health <= 0f)
                Die();
            else if (Stamina <= 0f)
            {
                Announce("guard broken");
                EnterStagger(guardBreakDuration, "Hit");
            }
            else
                Announce("blocked");
            return Defence.Blocked;
        }

        void EnterStagger(float duration, string clip)
        {
            stateAfterStagger = State.Free;
            Enter(State.Staggered);
            stateDuration = duration;
            clips.PlayOnce(clip, duration);
        }

        // The fall: the killing blow freezes, then time slows as the Apprentice reels, drops to their knees and lets the
        // oar go; the camera closes in on them alone; the bow lantern flares, gutters and goes out, its light drifting
        // away; they slump, and their Shade rises from the body. Then the phrase, and the dark.
        void Die()
        {
            Enter(State.Dead);
            snuffSounded = false;
            clips.PlayOnce("Death", deathDuration);
            Sfx.Instance?.DeathToll(transform.position);
            HitStop.Trigger(0.22f);
            HitStop.Slow(0.3f, 1.6f);
            CameraRig.Instance?.Mourn(true);
            CameraRig.Instance?.Shake(0.4f);
            GetComponent<BowLantern>()?.Snuff(snuffDelay, snuffDuration);
            // Every Obol stays behind with the Shade (and an older Shade, never reached, is lost).
            int carried = Obols;
            Obols = 0;
            var ghosts = GetComponent<DashGhosts>();
            Shade.Leave(this, carried, ghosts != null ? ghosts.ghostMaterial : null, deathDuration);
            if (carried > 0)
                ObolsChanged?.Invoke(-carried);
            Announce("the Apprentice fell");
            Died?.Invoke();
        }

        void Respawn()
        {
            Vector3 point = WorldState.Instance != null ? WorldState.Instance.RespawnPosition : lastSafeGround;
            Teleport(point);
            RestoreFully();
            CameraRig.Instance?.Mourn(false);
            GetComponent<BowLantern>()?.Relight();
            WorldState.Instance?.ReformAllSoured();
            Announce("woke at the Mooring post");
            Woke?.Invoke();
        }

        // ------------------------------------------------------------ the Hook

        HookPost hookPost;
        Vector3 hookFrom, hookStep;
        float hookDuration;
        LineRenderer tether;

        public const string HookHint = "Hold {hook} to aim at a hook post, let go to pull yourself across";

        public void GrantHook()
        {
            GiveHook();
            Found?.Invoke("The Hook", HookHint);
        }

        public void GiveHook() => HasHook = true; // (no fanfare: see ReceiveKeyItem)

        // ------------------------------------------------------------ taking things

        System.Action pendingTake;
        string ceremonyTitle, ceremonyHint;
        bool ceremonyAnnounced, ceremonyFlashed;
        Light ceremonyLight;

        // Stoop to pick an item up; it's taken (`take`) at the bottom of the stoop. A blow before then leaves it there.
        public void PickUp(Transform item, System.Action take)
        {
            Vector3 to = item.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(to);
            pendingTake = take;
            buffered = Act.None;
            Enter(State.PickingUp);
            clips.PlayOnce("Pickup", pickupDuration);
        }

        // A key item (an Ability): held up high for a moment, the world dimming and the camera easing in, a cold burst
        // of light and its motif; then its banner. Untouchable meanwhile.
        public void ReceiveKeyItem(string title, string hint, System.Action grant)
        {
            grant();
            ceremonyTitle = title;
            ceremonyHint = hint;
            ceremonyAnnounced = ceremonyFlashed = false;
            buffered = Act.None;
            SetLockTarget(null);
            Enter(State.Ceremony);
            clips.PlayOnce("RaiseItem", ceremonyDuration);
            Sfx.Instance?.KeyItem(transform.position + Vector3.up);
            CameraRig.Instance?.SetZoom(0.72f);
            if (ceremonyLight == null)
            {
                ceremonyLight = new GameObject("Key Item Light").AddComponent<Light>();
                ceremonyLight.transform.SetParent(transform, false);
                ceremonyLight.type = LightType.Point;
                ceremonyLight.color = new Color(0.62f, 0.77f, 1f);
                ceremonyLight.range = 6f;
                ceremonyLight.shadows = LightShadows.None;
            }
            ceremonyLight.intensity = 0f;
            ceremonyLight.enabled = true;
        }

        void UpdateCeremony()
        {
            float t = stateTime / ceremonyDuration;
            Vector3 held = hookBone != null ? hookBone.position : transform.position + Vector3.up * 1.4f;
            ceremonyLight.transform.position = held + Vector3.up * 0.2f;
            // The light swells as it's lifted, flares at the top, and eases away as it's lowered.
            ceremonyLight.intensity = t < 0.3f ? 6f * (t / 0.3f) * (t / 0.3f) : 6f * Mathf.Clamp01((0.95f - t) / 0.65f);
            if (!ceremonyFlashed && t >= 0.28f)
            {
                ceremonyFlashed = true;
                Effects.Instance?.KeyItemRing(held);
            }
            if (!ceremonyAnnounced && t >= 0.4f)
            {
                ceremonyAnnounced = true;
                Found?.Invoke(ceremonyTitle, ceremonyHint);
            }
            if (t >= 1f)
            {
                ceremonyLight.enabled = false;
                CameraRig.Instance?.SetZoom(1f);
                ToFreeAndConsume();
            }
        }

        // Something found for good, announced (the HUD's banner): a title and a hint.
        public void AnnounceFind(string title, string hint) => Found?.Invoke(title, hint);

        // Loading a save: the Obols carried, the most Healing charges they can carry, the Hook; health and charges full.
        public void Restore(int obols, int healingChargeMax, bool hook)
        {
            Obols = Mathf.Max(0, obols);
            maxHealingCharges = Mathf.Max(1, healingChargeMax);
            HasHook = hook;
            RestoreFully();
        }

        public void AddHealingCharge()
        {
            maxHealingCharges++;
            HealingCharges = Mathf.Min(HealingCharges + 1, maxHealingCharges);
            Found?.Invoke("A Healing Charge", "One more flame to heal with");
        }

        void StartHook(HookPost post)
        {
            hookPost = post;
            hookFrom = transform.position;
            hookDuration = Mathf.Max(0.2f, Vector3.Distance(hookFrom, post.LandingPosition) / hookSpeed);
            Vector3 to = post.LandingPosition - hookFrom;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(to);
            buffered = Act.None;
            Enter(State.Hooking);
            clips.PlayOnce("HookThrow", hookThrowTime);
            Sfx.Instance?.HookThrow(transform.position + Vector3.up);
            if (tether == null)
                tether = BuildTether();
            tether.enabled = true;
            // The Hook itself flies out along the tether (the one at the hip hides meanwhile).
            if (hookHeadModel != null)
            {
                hookHead = Instantiate(hookHeadModel);
                foreach (var c in hookHead.GetComponentsInChildren<Collider>())
                    Destroy(c);
                hookHeadRest = hookHeadModel.transform.localRotation;
            }
            hookInFlight = true;
        }

        // The tether flies out to the ring, catches, and pulls the Apprentice along a low arc to the post's platform.
        void UpdateHook()
        {
            Vector3 hand = hookBone != null ? hookBone.position : transform.position + Vector3.up * 0.75f + transform.right * 0.3f;
            Vector3 ring = hookPost.RingPosition;
            if (stateTime < hookThrowTime)
            {
                Vector3 end = Vector3.Lerp(hand, ring, stateTime / hookThrowTime);
                tether.SetPosition(0, hand);
                tether.SetPosition(1, end);
                PlaceHookHead(end, ring - hand);
                return;
            }
            float before = stateTime - Time.deltaTime;
            if (before < hookThrowTime)
            {
                Sfx.Instance?.HookCatch(ring); // it catches: the pull starts
                clips.Loop("HookPull", 0.05f);
            }
            PlaceHookHead(ring, ring - hand);
            float u = Mathf.Clamp01((stateTime - hookThrowTime) / hookDuration);
            float eased = u * u * (3f - 2f * u);
            Vector3 target = Vector3.Lerp(hookFrom, hookPost.LandingPosition, eased) + Vector3.up * (hookArc * Mathf.Sin(u * Mathf.PI));
            hookStep = target - transform.position;
            tether.SetPosition(0, hand);
            tether.SetPosition(1, ring);
            if (u >= 1f)
            {
                lastSafeGround = hookPost.LandingPosition;
                ToFreeAndConsume();
            }
        }

        GameObject hookHead;
        Quaternion hookHeadRest;
        bool hookInFlight;
        Transform hookBone;
        AudioSource whirl;

        void PlaceHookHead(Vector3 at, Vector3 towards)
        {
            if (hookHead == null)
                return;
            towards.y = 0f;
            hookHead.transform.SetPositionAndRotation(at,
                (towards.sqrMagnitude > 0.001f ? Quaternion.LookRotation(towards) : transform.rotation) * hookHeadRest);
        }

        SwingTrail swingTrail;
        SkinnedMeshRenderer bodyRenderer;
        int hookSlot = -1;
        MaterialPropertyBlock hookGlow;
        static readonly Color HookColour = new Color(0.62f, 0.77f, 1f);

        // The Hook at the hip: shown once it's been found, hidden while it's out on the tether. Whenever it's out
        // (whirled, flying, held up) its dark iron glows faint cold blue so it reads at night; brightly when held up.
        void LateUpdate()
        {
            if (hookBone != null)
                hookBone.localScale = HasHook && !hookInFlight ? Vector3.one : Vector3.one * 0.0001f;
            float glow = CurrentState == State.Ceremony ? 1.6f + 0.5f * Mathf.Sin(Time.time * 6f)
                       : AimingHook && CurrentState == State.Free || CurrentState == State.Hooking ? 0.7f : 0f;
            if (hookSlot >= 0)
            {
                hookGlow.SetColor("_EmissionColor", HookColour * glow);
                bodyRenderer.SetPropertyBlock(hookGlow, hookSlot);
            }
            if (hookHead != null)
            {
                hookGlow.SetColor("_EmissionColor", HookColour * 0.9f);
                foreach (var r in hookHead.GetComponentsInChildren<Renderer>())
                    r.SetPropertyBlock(hookGlow);
            }
        }

        // The chain's whoosh while the Hook is whirled (aiming).
        void UpdateWhirl()
        {
            bool whirling = AimingHook && CurrentState == State.Free;
            if (whirl == null)
            {
                if (!whirling || Sfx.Instance == null || Sfx.Instance.hookWhirl == null || Sfx.Instance.hookWhirl.Length == 0)
                    return;
                whirl = gameObject.AddComponent<AudioSource>();
                whirl.clip = Sfx.Instance.hookWhirl[0];
                whirl.loop = true;
                whirl.playOnAwake = false;
                whirl.spatialBlend = 0.7f;
                whirl.volume = 0.5f;
            }
            if (whirling && !whirl.isPlaying)
                whirl.Play();
            else if (!whirling && whirl.isPlaying)
                whirl.Stop();
        }

        Transform framedHook;

        // While aiming the Hook: the camera leans towards where the post leads, so you see what it would reach
        // (lock-on framing takes precedence).
        void FrameHookTarget()
        {
            Transform frame = LockTarget == null && AimingHook && HookTarget != null
                ? (HookTarget.landing != null ? HookTarget.landing : HookTarget.transform) : null;
            if (frame == framedHook)
                return;
            framedHook = frame;
            if (LockTarget == null)
                CameraRig.Instance?.SetSecondary(frame, 0.45f);
        }

        LineRenderer BuildTether()
        {
            var line = new GameObject("Hook Tether").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.positionCount = 2;
            line.widthMultiplier = 0.06f;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            var colour = new Color(0.62f, 0.77f, 1f);
            line.startColor = colour;
            line.endColor = new Color(0.85f, 0.92f, 1f);
            line.enabled = false;
            return line;
        }

        public void GainObols(int amount)
        {
            if (amount <= 0)
                return;
            Obols += amount;
            ObolsChanged?.Invoke(amount);
        }

        public bool SpendObols(int amount)
        {
            if (amount > Obols)
                return false;
            Obols -= amount;
            ObolsChanged?.Invoke(-amount);
            return true;
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

        // Into the water: a splash, and you go under while the screen dips to black; then you wash ashore.
        void GoUnder()
        {
            Vector3 surface = new Vector3(transform.position.x, waterLevel + 0.1f, transform.position.z);
            Effects.Instance?.Splash(surface);
            Sfx.Instance?.Splash(surface);
            SetLockTarget(null);
            CloseMenu();
            Enter(State.Sinking);
            clips.PlayOnce("Hit", sinkTime);
        }

        float washedAt = -100f;

        // Behind the black: on solid ground near where you fell, coughing, a third of your health gone.
        void WashAshore()
        {
            Health = Mathf.Max(0f, Health - maxHealth * waterDamage);
            washedAt = Time.time;
            Teleport(SafeGroundNear(lastSafeGround));
            Sfx.Instance?.Hurt(transform.position + Vector3.up);
            Announce("washed ashore");
            if (Health <= 0f)
                Die();
            else
                EnterStagger(washAshoreStagger, "Hit");
        }

        // The remembered safe ground, if it's still there (a floor can break away under it); otherwise the nearest solid
        // ground around it, clear of any edge, or the Mooring post.
        Vector3 SafeGroundNear(Vector3 point)
        {
            if (Solid(point))
                return point;
            for (float r = 1f; r <= 9f; r += 0.75f)
                for (int i = 0; i < 16; i++)
                {
                    Vector3 p = point + Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward * r;
                    if (Solid(p))
                        return p;
                }
            return WorldState.Instance != null ? WorldState.Instance.RespawnPosition : point;
        }

        static bool Solid(Vector3 point)
        {
            foreach (var offset in new[] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                if (!Physics.Raycast(point + Vector3.up * 0.5f + offset * 0.5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                    return false;
            return true;
        }

        // Remember solid ground well away from any edge, so washing ashore never lands on the brink.
        void TrackSafeGround(float dt)
        {
            safeGroundTimer -= dt;
            if (safeGroundTimer > 0f || !controller.isGrounded || CurrentState != State.Free)
                return;
            safeGroundTimer = 0.25f;
            Vector3 p = transform.position + Vector3.up * 0.5f;
            // 0.5 m keeps the point clear of the brink while still fitting the 1.7 m boardwalk.
            foreach (var offset in new[] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                if (!Physics.Raycast(p + offset * 0.5f, Vector3.down, 1.5f))
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
