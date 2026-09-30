using UnityEngine;
using UnityEngine.AI;

namespace MyGame
{
    // What the Apprentice's Hook did to a Soured it caught: glanced off, pulled it over, or tore its guard away.
    public enum HookResult { Shrugged, Yanked, Ripped }

    // A Soured: a soul the Ferryman kept too long. Idle -> Chase -> Telegraph (animated wind-up, no ground
    // markers) -> Strike -> Recover (the punish window). A hidden Stagger meter fills with hits and drains
    // when the Apprentice lets up; full = staggered, attack interrupted. Defeat only scatters it: it re-forms
    // when the Apprentice Rests or dies. Lose the Apprentice (or be drawn too far from its post) and it walks back to
    // where it stood (Returning), noticing them again on the way if they're close.
    [RequireComponent(typeof(CharacterController))]
    public class Soured : MonoBehaviour
    {
        public enum State { Idle, Chase, Telegraph, Strike, Recover, Staggered, Stunned, Dying, Scattered, Busy, Returning, Yanked }

        [Header("Health")]
        public float maxHealth = 100f;
        [Tooltip("The lab: never notices anyone (a training dummy that won't fight back)")]
        public bool passive;
        [Tooltip("The lab: never dies; a blow that would kill it fills its health again")]
        public bool immortal;
        public bool Held { get; set; } // (the lab: while the Apprentice is in a menu, nothing notices them)

        // Every blow that reaches a Soured, landed or blocked, and the damage it did (the lab's readout listens).
        public static event System.Action<Soured, HitResult, float> Struck;
        public float radius = 0.6f;
        [Range(0f, 1f)] public float knockbackResistance = 0f;

        [Header("Stagger meter")]
        public float staggerCapacity = 60f;
        public float staggerDrainDelay = 1.2f;
        public float staggerDrainPerSecond = 30f;
        public float staggerDuration = 1.0f;

        [Header("Behaviour")]
        public float aggroRadius = 9f;           // how far it sees, within its field of view
        [Range(0f, 180f)] public float viewHalfAngle = 70f;
        public float noticeCloseRadius = 1.5f;   // how close someone must get to be sensed from behind
        public float eyeHeight = 1.3f; // where the alert flare shows
        public float barHeight = 2.3f; // where the lock-on health bar floats
        public float attackRange = 2.2f;
        public float chaseSpeed = 2.4f;
        public float turnSpeed = 360f;
        public float leashRadius = 14f;          // drawn further than this from its post, it gives up and goes back
        [Tooltip("Further from it than this and it gives up the chase (0: one and a half times how far it sees)")]
        public float giveUpRange = 0f;

        [Header("Attacks (durations match the model's clips)")]
        public SouredAttack primary = new SouredAttack();

        [Header("Death: it falls, dissolves, and its fare streams into the Apprentice's lantern")]
        public float deathDuration = 0.8f;
        public float dissolveDuration = 0.9f;
        public int fareMotes = 36;
        public int obols = 40; // its fare, the Apprentice's when it falls

        [Tooltip("Its belt lantern, knocked off where it falls (optional)")]
        public DroppedLantern droppedLantern;

        [Header("Water: knocked in, it drowns (a kill, like any other)")]
        public float waterLevel = -0.35f;
        [Tooltip("How far past its own radius it feels for solid ground before stepping (never walks off an edge)")]
        public float edgeLookahead = 0.35f;

        [Header("The Hook: yanked over to the Apprentice, then reeling")]
        public float yankStagger = 1.0f;

        [Header("Perfect-blocked")]
        public float stunDuration = 1.8f;
        public float riposteMultiplier = 2f; // hits on a stunned Soured

        public State CurrentState { get; private set; }
        public float Health { get; private set; }
        public float Stagger { get; private set; }
        public bool IsAlive => CurrentState != State.Scattered && CurrentState != State.Dying;
        public float HealthFraction => Mathf.Clamp01(Health / maxHealth);
        public float StateTime => stateTime; // seconds in the current state
        public SouredAttack CurrentAttack => current;
        public bool Drowned { get; private set; }
        public int TimesDefeated { get; private set; } // it re-forms, but the world remembers it was beaten

        protected ClipPlayer clips;
        SouredAttack current;
        float lungeThisStrike;
        protected virtual string IdleClip => "Idle";

        // Chase behaviour by distance to the Apprentice; subclasses change gait and speed (the Laden guards up close).
        protected virtual string ChaseClip(float distance) => "Walk";
        protected virtual float ChaseSpeed(float distance) => chaseSpeed;
        protected virtual void OnChaseDistance(float distance) { }
        // How it closes in (the default: along the way there, at ChaseSpeed; backing off, straight back); a Clinger circles.
        protected virtual Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            float speed = ChaseSpeed(distance);
            return (speed >= 0f && target != null ? PathDirection(target.transform.position) : toTarget.normalized) * speed;
        }
        protected Apprentice target;
        CharacterController controller;
        Renderer[] renderers;
        SwingTrail swingTrail;
        int[] lanternSlot; // per renderer: index of the belt lantern's material, or -1
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
        bool struck, committed;
        int loosed; // projectiles loosed so far in this ranged strike
        AudioSource tell;  // the wind-up sound playing now, cut if the attack is interrupted
        AudioClip tellClip;
        float dissolve;
        int faresReleased;
        float busyFor, staggerFor;
        Vector3 yankFrom, yankTo;
        float yankTime;

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            clips = new ClipPlayer(GetComponentInChildren<Animation>());
            renderers = GetComponentsInChildren<Renderer>();
            swingTrail = GetComponent<SwingTrail>();
            lanternSlot = System.Array.ConvertAll(renderers, r =>
                System.Array.FindIndex(r.sharedMaterials, m => m != null && m.name.StartsWith("SouredLantern_Glow")));
            flash = new MaterialPropertyBlock();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            Health = maxHealth;
        }

        void OnEnable() => WorldState.Register(this);
        void OnDisable() => WorldState.Unregister(this);

        protected virtual void Start() => target = FindAnyObjectByType<Apprentice>();

        // Whether it can be locked on to, hooked, struck (a Drowned under the water can't).
        public virtual bool Targetable => IsAlive;

        // Held up by the water (a Drowned): no gravity, no feeling for footing; it keeps the height it's given.
        protected virtual bool Floats => false;

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
                    LoopUnlessBusy(IdleClip);
                    if (targetAlive && CanSense(toTarget))
                        Notice();
                    break;

                case State.Chase:
                    if (!targetAlive || LosesTarget(toTarget.magnitude) || Flat(transform.position - Home).magnitude > leashRadius)
                    {
                        Enter(State.Returning);
                        break;
                    }
                    float distance = toTarget.magnitude;
                    OnChaseDistance(distance);
                    LoopUnlessBusy(ChaseClip(distance));
                    var attack = ChooseAttack(toTarget, distance);
                    if (attack != null && !attack.ranged && !OnItsLevel())
                        attack = null; // (never a blow through a floor, nor off a ledge at someone below)
                    if (attack == null)
                    {
                        pathTurned = false;
                        planar = ChaseStep(toTarget, distance);
                        // Going round something (the way there turns away from them): it faces where it's going.
                        Face(pathTurned && planar.sqrMagnitude > 0.01f ? planar : toTarget, turnSpeed * dt);
                    }
                    else
                    {
                        Face(toTarget, turnSpeed * dt);
                        current = attack;
                        lockedDirection = toTarget.normalized; // locked now: sidestepping is always an answer
                        Enter(State.Telegraph);
                        committed = false;
                        clips.PlayOnce(attack.windupClip, attack.telegraph);
                        tell = Sfx.Instance?.Attack(attack.windupSound, transform.position + Vector3.up, 1f, Label("windup"));
                        tellClip = tell != null ? tell.clip : null;
                    }
                    break;

                case State.Telegraph:
                    // Follow the target for most of the wind-up, then commit: dodging too early gets tracked.
                    if (stateTime < current.telegraph * current.trackUntil && toTarget.sqrMagnitude > 0.01f)
                        lockedDirection = toTarget.normalized;
                    else if (!committed)
                    {
                        committed = true;
                        OnCommitted(current); // it stops following: the aim is set
                    }
                    Face(lockedDirection, turnSpeed * 2f * dt);
                    if (stateTime >= current.telegraph)
                    {
                        struck = false;
                        // Lunge far enough to reach a target that backed off, within limits.
                        lungeThisStrike = Mathf.Clamp(toTarget.magnitude - current.reach * 0.5f, current.lunge, current.maxLunge);
                        Enter(State.Strike);
                        loosed = 0;
                        clips.PlayOnce(current.strikeClip, current.strike);
                        Sfx.Instance?.Attack(current.swingSound, transform.position + Vector3.up, 0.9f, Label("swing"), current.swingPitch);
                    }
                    break;

                case State.Strike:
                    // The whole lunge happens before the blow lands (a swing that closes the gap, then connects).
                    float lungeTime = Mathf.Max(0.01f, current.strike * current.impactAt);
                    if (stateTime < lungeTime)
                        planar = transform.forward * (lungeThisStrike / lungeTime);
                    if (current.ranged)
                        Loose(toTarget, targetAlive);
                    else if (!current.scripted && !struck && stateTime >= current.impactAt * current.strike)
                    {
                        struck = true;
                        Impact();
                        OnImpact(current);
                        if (targetAlive)
                            StrikeTarget(toTarget);
                    }
                    if (CurrentState == State.Strike && stateTime >= current.strike)
                    {
                        Enter(State.Recover);
                        clips.PlayOnce(current.recoverClip, current.recover);
                    }
                    break;

                case State.Recover:
                    if (stateTime >= current.recover)
                        Enter(State.Chase);
                    break;

                case State.Returning:
                {
                    // Back to its post; noticing the Apprentice again on the way, if they're close (not the leash).
                    Vector3 home = Flat(Home - transform.position);
                    if (targetAlive && CanSense(toTarget) && home.magnitude < leashRadius * 0.6f && !LosesTarget(toTarget.magnitude))
                    {
                        Notice();
                        break;
                    }
                    if (home.magnitude < 0.3f && ArrivedHome())
                        break;
                    if (home.magnitude < 0.3f)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, spawnRotation, turnSpeed * dt);
                        if (Quaternion.Angle(transform.rotation, spawnRotation) < 2f)
                            Enter(State.Idle);
                        LoopUnlessBusy(IdleClip);
                        break;
                    }
                    Vector3 way = PathDirection(Home);
                    Face(way.sqrMagnitude > 0.01f ? way : home, turnSpeed * dt);
                    LoopUnlessBusy(ChaseClip(float.MaxValue));
                    planar = way * Mathf.Min(chaseSpeed, home.magnitude / Mathf.Max(dt, 1e-4f));
                    break;
                }

                case State.Stunned:
                    if (stateTime >= stunDuration)
                        Enter(State.Chase);
                    break;

                case State.Staggered:
                    if (stateTime >= staggerFor)
                        Enter(State.Chase);
                    break;

                case State.Yanked:
                {
                    // Pulled over on the Hook's chain along a low arc, over water if need be, to land in front of the
                    // Apprentice; then it reels.
                    float u = Mathf.Clamp01(stateTime / yankTime);
                    float e = u * u * (3f - 2f * u);
                    controller.enabled = false;
                    transform.position = Vector3.Lerp(yankFrom, yankTo, e) + Vector3.up * (0.7f * Mathf.Sin(u * Mathf.PI));
                    if (target != null)
                        Face(toTarget, 720f * dt);
                    if (u >= 1f)
                    {
                        controller.enabled = true;
                        verticalVelocity = 0f;
                        knockback = Vector3.zero;
                        StaggerNow(yankStagger);
                    }
                    UpdateFlash();
                    return;
                }

                case State.Busy:
                    if (stateTime >= busyFor)
                        Enter(State.Chase);
                    break;

                case State.Dying:
                    // Dissolve from halfway through the fall, releasing its fare as the body goes.
                    dissolve = Mathf.Clamp01((stateTime - deathDuration * 0.45f) / dissolveDuration);
                    if (dissolve > 0f && faresReleased == 0)
                        Sfx.Instance?.SoulAbsorb(transform.position + Vector3.up);
                    while (faresReleased < Mathf.RoundToInt(fareMotes * dissolve))
                    {
                        Effects.Instance?.EmitFare(RandomPointOnBody());
                        faresReleased++;
                    }
                    if (dissolve >= 1f)
                    {
                        Scatter();
                        return;
                    }
                    break;

                case State.Scattered:
                    return;
            }
            Tick(dt);
            if (carried)
            {
                UpdateFlash(); // (carried by its own machinery: clinging to someone's back, flung off)
                return;
            }

            if (swingTrail != null) // the smear of a melee blow (a shot has none)
                swingTrail.Emitting = CurrentState == State.Strike && current != null && !current.ranged && stateTime <= current.strike * 0.9f;
            // Walking and chasing keep to solid ground. A lunge doesn't (overshooting the edge is its own mistake), nor
            // does knockback (shoving one off the edge is fair play).
            if ((CurrentState == State.Chase || CurrentState == State.Returning) && !Floats)
                planar = KeepFooting(planar);
            knockback *= Mathf.Exp(-8f * dt);
            if (Drowned)
            {
                planar = knockback = Vector3.zero;
                verticalVelocity = -0.6f; // sinks slowly while it dissolves, instead of falling forever
            }
            else if (Floats)
                verticalVelocity = 0f;
            else
                verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
            controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);
            if (IsAlive && transform.position.y < waterLevel && !Surface())
                Drown();
            UpdateFlash();
        }

        // Which attack (if any) to start now; the base Soured has one, used when the Apprentice is in range.
        protected virtual SouredAttack ChooseAttack(Vector3 toTarget, float distance) =>
            distance <= attackRange ? primary : null;

        string Label(string part) => $"{current.name.ToLowerInvariant()}_{part}";

        // The blow comes down: a slam crashes into the ground (with a little shake if the Apprentice is close),
        // whether or not it hits anyone.
        void Impact()
        {
            if (current.impactSound == null || current.impactSound.Length == 0)
                return;
            Vector3 at = transform.position + transform.forward * (current.reach * 0.7f);
            Sfx.Instance?.Attack(current.impactSound, at, 1f, Label("impact"));
            if (target != null && Vector3.Distance(target.transform.position, at) < 8f)
                CameraRig.Instance?.Shake(0.12f);
        }

        // Every frame, after the state's own work (a subclass's extra machinery: the Anchoress's anchor).
        protected virtual void Tick(float dt) { }

        // A melee blow lands (hit or miss), before it's checked against the Apprentice.
        protected virtual void OnImpact(SouredAttack attack) { }

        // Delivers a blow to the Apprentice (a scripted attack's, landing by its own rules).
        protected Defence HitTarget(float damage, Vector3 knock) =>
            target != null ? target.TakeHit(damage, knock, this) : Defence.Dodged;

        // The moment an attack stops tracking (a shot's aim is set): subclasses can mark it (the archer's glint).
        protected virtual void OnCommitted(SouredAttack attack) { }

        // A ranged attack's release, along its locked aim.
        protected virtual void Shoot(SouredAttack attack, Vector3 direction, int index) { }

        protected Vector3 LockedDirection => lockedDirection;

        // A ranged strike: its projectiles, one after another from the impact moment. Each after the first turns a
        // little towards the target (standing still is punished; moving sideways still escapes).
        void Loose(Vector3 toTarget, bool targetAlive)
        {
            int count = Mathf.Max(1, current.volley);
            while (loosed < count && stateTime >= current.impactAt * current.strike + loosed * current.volleyInterval)
            {
                if (loosed > 0 && toTarget.sqrMagnitude > 0.01f)
                {
                    lockedDirection = Vector3.RotateTowards(lockedDirection, toTarget.normalized, current.volleyTrack * Mathf.Deg2Rad, 0f);
                    transform.rotation = Quaternion.LookRotation(lockedDirection);
                }
                if (targetAlive)
                {
                    Sfx.Instance?.Attack(current.releaseSound, transform.position + Vector3.up, 0.9f, Label("release"));
                    Shoot(current, lockedDirection, loosed);
                }
                loosed++;
            }
        }

        void StrikeTarget(Vector3 toTarget)
        {
            if (toTarget.magnitude > current.reach + 0.35f || Vector3.Angle(transform.forward, toTarget) > current.arc)
                return;
            if (HitTarget(current.damage, toTarget.normalized * current.knockback) == Defence.Parried)
                Stun();
        }

        // Slain outright (a Drowned struck by its own spit, turned back): down at once, whatever its health.
        public void Slay(Vector3 from)
        {
            if (!IsAlive || immortal)
                return;
            Health = 0f;
            dealt = maxHealth;
            flashUntil = Time.time + 0.1f;
            Struck?.Invoke(this, HitResult.Landed, maxHealth);
            Die(new Hit { Damage = maxHealth, From = from, Knockback = Flat(transform.position - from).normalized * 3f });
        }

        // A perfect block turns the blow back: the Soured reels, open to a riposte.
        public void Stun()
        {
            Stagger = 0f;
            knockback = -transform.forward * 3f;
            Enter(State.Stunned);
            clips.PlayOnce("Stagger", stunDuration);
        }

        public HitResult TakeHit(Hit hit)
        {
            var result = Resolve(hit);
            if (result != HitResult.Missed)
                Struck?.Invoke(this, result, result == HitResult.Landed ? dealt : 0f);
            return result;
        }

        float dealt;

        HitResult Resolve(Hit hit)
        {
            if (!IsAlive)
                return HitResult.Missed;
            if (Unhittable)
            {
                Vector3 towards = Flat(hit.From - transform.position).normalized;
                Vector3 at = transform.position + towards * (radius + 0.25f) + Vector3.up * 1.1f;
                Effects.Instance?.BlockSpark(at, towards);
                Sfx.Instance?.BlockClang(at, 0.8f);
                return HitResult.Blocked;
            }
            bool stunned = CurrentState == State.Stunned;
            if (stunned)
                hit.Damage *= riposteMultiplier; // a riposte: double damage, straight through any guard
            else if (BlocksHit(hit))
            {
                PlayShort("Block", 0.3f);
                knockback = -hit.Knockback.normalized * 0.8f; // the heap gives a little
                Vector3 towardsAttacker = Flat(hit.From - transform.position).normalized;
                Vector3 contact = transform.position + towardsAttacker * (radius + 0.25f) + Vector3.up * 1.1f;
                Effects.Instance?.BlockSpark(contact, towardsAttacker);
                Sfx.Instance?.BlockClang(contact);
                return HitResult.Blocked;
            }

            Health -= hit.Damage;
            dealt = hit.Damage;
            if (immortal && Health <= 0f)
                Health = maxHealth; // (a dummy: a full bar again)
            Sfx.Instance?.OarHit(transform.position + Vector3.up * 1.1f, hit.Heavy || stunned);
            knockback = hit.Knockback * (1f - knockbackResistance);
            Stagger += hit.Stagger;
            lastHitTime = Time.time;
            flashUntil = Time.time + 0.08f;

            if (Health <= 0f)
                Die(hit);
            else if (!stunned && Stagger >= staggerCapacity && CurrentState != State.Staggered)
            {
                StaggerNow();
            }
            else if (CurrentState == State.Idle)
                Notice(); // hitting a sleeper wakes it
            return HitResult.Landed;
        }

        // Burning (burning pitch underfoot, the flames of an Ember-tarred oar): harm no guard stops, a little at a time;
        // no stagger, no knockback. False if there's nothing to burn.
        public bool TakeBurn(float damage, Vector3 from)
        {
            if (!IsAlive || Unhittable || damage <= 0f)
                return false;
            Health -= damage;
            dealt = damage;
            if (immortal && Health <= 0f)
                Health = maxHealth;
            flashUntil = Time.time + 0.05f;
            Struck?.Invoke(this, HitResult.Landed, damage);
            if (Health <= 0f)
                Die(new Hit { Damage = damage, From = from });
            else if (CurrentState == State.Idle)
                Notice();
            return true;
        }

        protected virtual bool BlocksHit(Hit hit) => false;

        // Whether the Apprentice is too far away to keep after (then it goes back to its post).
        protected virtual bool LosesTarget(float distance) => distance > (giveUpRange > 0f ? giveUpRange : aggroRadius * 1.5f);

        // Nothing gets through for now (a Boss mid-way between phases).
        protected virtual bool Unhittable => false;

        // Fallen into the water: true if the subclass deals with it (a Boss climbs out), false to drown.
        protected virtual bool Surface() => false;

        // A scripted moment of its own (a Boss rising into its second phase): the clip, then back to the chase.
        protected void BeBusy(string clip, float duration)
        {
            Enter(State.Busy);
            busyFor = duration;
            clips.PlayOnce(clip, duration);
        }

        protected void StaggerNow() => StaggerNow(staggerDuration);

        protected void StaggerNow(float duration)
        {
            Stagger = 0f;
            staggerFor = duration;
            Enter(State.Staggered);
            clips.PlayOnce("Stagger", duration);
        }

        // Caught by the Apprentice's Hook: yanked over to land in front of them (over water, if need be), reeling; the
        // heavier the Soured, the slower it comes (a Boss answers otherwise: see the Anchoress).
        public virtual HookResult OnHooked(Apprentice by)
        {
            if (!IsAlive || CurrentState == State.Busy || CurrentState == State.Yanked)
                return HookResult.Shrugged;
            Vector3 away = Flat(transform.position - by.transform.position);
            float distance = away.magnitude;
            yankFrom = transform.position;
            yankTo = by.transform.position + (distance > 0.01f ? away / distance : by.transform.forward) * (radius + 1.0f);
            yankTo.y = by.transform.position.y;
            yankTime = Mathf.Clamp(distance / Mathf.Lerp(16f, 10f, knockbackResistance), 0.2f, 0.7f); // (a Laden resists: slower)
            Enter(State.Yanked);
            clips.PlayOnce("Stagger", yankTime);
            return HookResult.Yanked;
        }

        public bool Yanked => CurrentState == State.Yanked;

        // (a subclass) Where the yank it's on lands it instead (a Drowned: hauled right out, onto dry ground).
        protected void RetargetYank(Vector3 to) => yankTo = to;

        protected void LoseHealth(float amount) => Health = Mathf.Max(1f, Health - amount); // (never a kill)

        protected Vector3 SpawnPosition => spawnPosition;

        // Where it keeps to (its leash is measured from here) and walks back to: where it stood, unless it belongs with
        // something that moves (the Unparted's Fast: its other half).
        protected virtual Vector3 Home => spawnPosition;
        // Back home: what then (the default: stand there, facing the way it stood).
        protected virtual bool ArrivedHome() => false;

        // Loading a save: beaten before, gone (it doesn't re-form either, if it's a Boss).
        protected void RestoreDefeated()
        {
            TimesDefeated = Mathf.Max(1, TimesDefeated);
            Scatter();
        }

        bool carried;

        // Moved by the subclass alone for a while (a Clinger on someone's back): no walking, no gravity, no collisions.
        protected void SetCarried(bool on)
        {
            carried = on;
            controller.enabled = !on;
            verticalVelocity = 0f;
            knockback = Vector3.zero;
        }

        protected bool Carried => carried;

        protected void PlaceAt(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            knockback = Vector3.zero;
            verticalVelocity = 0f;
        }

        // It sees ahead (within its field of view, out to aggroRadius) but only senses someone right behind it:
        // sneaking up from behind is how you get a free hit.
        bool CanSense(Vector3 toTarget) =>
            toTarget.magnitude < noticeCloseRadius
            || (toTarget.magnitude < aggroRadius && Vector3.Angle(transform.forward, toTarget) <= viewHalfAngle);

        // The moment it becomes aware of the Apprentice: its eyes flare, then it gives chase.
        void Notice()
        {
            if (passive || Held)
                return;
            Effects.Instance?.AlertFlare(transform.position + transform.forward * 0.35f + Vector3.up * eyeHeight);
            Enter(State.Chase);
        }

        void Die(Hit hit)
        {
            TimesDefeated++;
            Enter(State.Dying);
            dissolve = 0f;
            faresReleased = 0;
            knockback = hit.Knockback * 0.5f;
            HitStop.Trigger(0.2f);
            CameraRig.Instance?.Shake(0.3f);
            clips.PlayOnce("Death", deathDuration);
            if (target != null)
                target.GainObols(obols);
            if (droppedLantern != null && droppedLantern.carried != null)
            {
                Vector3 push = hit.Knockback * 0.25f + Random.insideUnitSphere * 0.6f;
                droppedLantern.Drop(droppedLantern.carried.transform.position, new Vector3(push.x, 0f, push.z));
                HideBeltLantern(true);
            }
        }

        // The lantern on the body vanishes the moment the dropped one appears (its material slot fully dissolved).
        void HideBeltLantern(bool hidden)
        {
            var block = new MaterialPropertyBlock();
            if (hidden)
                block.SetFloat("_Dissolve", 1f);
            for (int i = 0; i < renderers.Length; i++)
                if (lanternSlot[i] >= 0)
                    renderers[i].SetPropertyBlock(block, lanternSlot[i]);
        }

        static readonly RaycastHit[] groundHits = new RaycastHit[8];
        static readonly float[] sidesteps = { 35f, -35f, 70f, -70f };

        // Would this step take it off an edge? Then slide along the edge (angling off up to 70°), or stop at the
        // brink. It feels just past its own radius for ground above the water.
        Vector3 KeepFooting(Vector3 step)
        {
            if (step.sqrMagnitude < 1e-4f || !controller.enabled || !controller.isGrounded)
                return step;
            float probe = radius + edgeLookahead, speed = step.magnitude;
            Vector3 dir = step / speed;
            if (HasFooting(transform.position + dir * probe))
                return step;
            foreach (float angle in sidesteps)
            {
                Vector3 slide = Quaternion.Euler(0f, angle, 0f) * dir;
                if (HasFooting(transform.position + slide * probe))
                    return slide * (speed * Mathf.Cos(angle * Mathf.Deg2Rad));
            }
            return Vector3.zero;
        }

        // Solid ground about where it stands (a step down at most: walking, it never goes off a ledge).
        bool HasFooting(Vector3 at)
        {
            float lowest = Mathf.Max(waterLevel + 0.1f, transform.position.y - 0.6f);
            int count = Physics.RaycastNonAlloc(at + Vector3.up * 0.6f, Vector3.down, groundHits, 2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!(groundHits[i].collider is CharacterController) && groundHits[i].point.y > lowest)
                    return true;
            return false;
        }

        // ---- finding the way (the NavMeshBaker's: up stairs, round walls, never off a ledge)

        NavMeshPath path;
        readonly Vector3[] corners = new Vector3[32];
        int cornerCount = -1;   // -1: no way known (no navigation here): straight at it
        bool pathComplete;
        float nextPath = -1f;
        Vector3 pathGoal;
        bool pathTurned;        // the way there turns away from the straight line to it (going round something)

        // Which way to walk now to get to `goal` (flat, unit length; zero when it can get no closer).
        protected Vector3 PathDirection(Vector3 goal)
        {
            Vector3 direct = Flat(goal - transform.position);
            var baker = NavMeshBaker.Instance;
            if (baker == null || !baker.Ready || !controller.enabled)
                return direct.normalized;
            if (Time.time >= nextPath || Flat(goal - pathGoal).sqrMagnitude > 1f || Mathf.Abs(goal.y - pathGoal.y) > 1f)
                Repath(goal, baker);
            if (cornerCount < 0)
                return direct.normalized;
            for (int i = 1; i < cornerCount; i++)
            {
                Vector3 toCorner = Flat(corners[i] - transform.position);
                if (toCorner.magnitude > 0.35f)
                {
                    Vector3 dir = toCorner.normalized;
                    pathTurned = Vector3.Angle(dir, direct) > 45f;
                    return dir;
                }
            }
            return pathComplete ? direct.normalized : Vector3.zero; // (as near as it can get: it waits there)
        }

        void Repath(Vector3 goal, NavMeshBaker baker)
        {
            nextPath = Time.time + 0.25f + Random.value * 0.1f;
            pathGoal = goal;
            var filter = baker.Filter(radius);
            path ??= new NavMeshPath();
            if (!Nearest(transform.position, 1.2f, filter, out var from) || !Nearest(goal, 2.5f, filter, out var to)
                || !NavMesh.CalculatePath(from, to, filter, path))
            {
                cornerCount = -1;
                return;
            }
            cornerCount = path.GetCornersNonAlloc(corners);
            pathComplete = path.status == NavMeshPathStatus.PathComplete;
        }

        // The walkable point nearest `at`, looking close first (asked for far at once, the search can prefer another level:
        // the floor below someone standing near a wall).
        static bool Nearest(Vector3 at, float reach, NavMeshQueryFilter filter, out Vector3 point)
        {
            foreach (float r in new[] { 0.5f, 1.0f, reach })
                if (r <= reach && NavMesh.SamplePosition(at, out var hit, r, filter))
                {
                    point = hit.position;
                    return true;
                }
            point = at;
            return false;
        }

        // The Apprentice on its own level (not a floor above or below, not down off a ledge).
        protected virtual bool OnItsLevel() => target != null && Mathf.Abs(target.transform.position.y - transform.position.y) < 1.3f;

        // Knocked (or fallen) into the water: a splash, and it goes under.
        void Drown()
        {
            Drowned = true;
            Vector3 surface = new Vector3(transform.position.x, waterLevel + 0.1f, transform.position.z);
            Effects.Instance?.Splash(surface);
            Sfx.Instance?.Splash(surface);
            Die(new Hit());
        }

        Vector3 RandomPointOnBody()
        {
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position + Vector3.up, Vector3.one);
            return bounds.center + Vector3.Scale(bounds.extents * 0.7f, Random.insideUnitSphere);
        }

        void Scatter()
        {
            Enter(State.Scattered);
            foreach (var r in renderers)
                r.enabled = false;
            controller.enabled = false;
        }

        // Where it re-forms (level design; tests use it to stage a fight on open ground).
        public void SetSpawn(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        // Back to its spawn, whole again (the Ferryman's hoard pulls it back).
        public virtual void Reform()
        {
            carried = false;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            foreach (var r in renderers)
                r.enabled = true;
            if (droppedLantern != null)
            {
                droppedLantern.Return();
                HideBeltLantern(false);
            }
            Health = maxHealth;
            Stagger = 0f;
            knockback = Vector3.zero;
            verticalVelocity = 0f;
            Drowned = false;
            dissolve = 0f;
            Enter(State.Idle);
            clips.Loop(IdleClip, 0f);
        }

        // Called on every state change, so a subclass can react (the Laden raises its guard on Chase).
        protected virtual void OnEntered(State state) { }

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

        // Per-renderer overrides: the hit flash (emission) and the death dissolve. With neither, clear the block so
        // the materials' own values (glowing eyes, lantern) show.
        void UpdateFlash()
        {
            bool on = Time.time < flashUntil;
            foreach (var r in renderers)
            {
                if (!on && dissolve <= 0f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                flash.Clear();
                if (on)
                    flash.SetColor("_EmissionColor", new Color(0.35f, 0.33f, 0.3f)); // a flash, not a white statue
                if (dissolve > 0f)
                    flash.SetFloat("_Dissolve", dissolve);
                r.SetPropertyBlock(flash);
            }
        }

        void Face(Vector3 direction, float maxDegrees)
        {
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), maxDegrees);
        }

        void Enter(State state)
        {
            // A tell announces a blow: if the wind-up is cut short (staggered, stunned, killed), so is the sound.
            if (state != State.Telegraph && state != State.Strike && tell != null)
            {
                if (tell.isPlaying && tell.clip == tellClip)
                    tell.Stop();
                tell = null;
            }
            CurrentState = state;
            stateTime = 0f;
            OnEntered(state);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
