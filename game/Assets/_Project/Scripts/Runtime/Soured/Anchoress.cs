using UnityEngine;

namespace MyGame
{
    // The Antediluvian Anchoress, the Drowned Village's Boss: a Laden who would not let the Ferryman go. She keeps
    // vigil at the chapel's altar, kneeling over his ferry's anchor, until the Apprentice comes near. Like a Laden she
    // guards with what she holds (the anchor, upright in front of her: frontal hits bounce off) and is open from behind,
    // where she's slow to turn. Phase one, three attacks:
    //   Anchor Drop:   raised high (a long wind-up) and brought down a stride ahead; a ring of water spreads from where
    //                  it lands. Sidestep the blow, then dash through the ring or be beyond it. Long recovery.
    //   Chain Sweep:   the anchor let out on its chain to her right, as far as you stand (up to a long reach), then
    //                  swung round in front of her to her left. Back off past it once it's let out, dash through it,
    //                  or be behind her. Hug her and she shoulders you out of the way as the swing starts.
    //   Cast and Haul: from range: hefted back and thrown at you; it lies a moment, then she hauls it home along the
    //                  ground, through anyone between. Dodge it, or perfect-block the haul: the chain snaps taut against
    //                  the oar and she's yanked off balance. While the anchor is out she has no guard.
    //   Undertow:      stay close to her too long (at her back too) and she kneels over the anchor to pray: sour
    //                  black water wells up around her, slowing you, dragging you in, hurting a little every tick.
    //                  Wade or dash out. She's open while she prays and as she rises: stay and trade, if you dare.
    // A stagger (or a perfect block) makes her drop it; she must reel it in before she can attack again. Her lantern
    // hangs from the anchor's stock (a light for the ferry to see), so in the dark you can follow it: flying, lying,
    // dragged home in a spray of sparks. While she fights, the camera leans towards her and draws back a little.
    // Phase two (Anchoress.PhaseTwo.cs): at half health she rises; her Drops break the floor; the Hook Yank.
    [DefaultExecutionOrder(-10)] // poses the anchor in LateUpdate before its swoosh samples it
    public sealed partial class Anchoress : Laden
    {
        public enum AnchorMode { Held, Swung, Flying, Lying, Hauled, Reeling }

        [Header("Anchor Drop (primary): a ring of water spreads from where it lands")]
        public float ringDamage = 16f;
        public float ringRadius = 5.5f;
        public float ringSpeed = 6f;
        public float ringWidth = 0.8f;
        public float ringKnockback = 6f;
        public Color ringTint = new Color(0.62f, 0.8f, 0.86f, 0.85f);

        [Header("Chain Sweep (sweep): let out on the chain, swung round her from right to left")]
        public float sweepRadius = 3.9f;  // the longest reach, to the middle of the anchor
        public float sweepMinRadius = 2.3f;
        public float sweepFrom = 115f;    // degrees from her facing (+ to her right)
        public float sweepTo = -115f;
        public float sweepHitRadius = 1.0f;
        [Range(0f, 1f)] public float midRangeSweepChance = 0.35f; // tried every so often when you hover just out of reach
        public float backAngle = 125f; // further round than this, you're behind her: the sweep doesn't reach
        public float shoveRadius = 1.8f;   // this close as the swing starts (inside its reach), she shoulders you away
        public float shoveDamage = 8f;
        public float shoveTo = 3.8f;       // where it sets you down: beyond the anchor's path

        [Header("Cast and Haul")]
        public SouredAttack cast = new SouredAttack
        {
            name = "Cast", windupClip = "CastWindup", strikeClip = "Cast", recoverClip = "Haul",
            telegraph = 1.0f, strike = 0.7f, recover = 1.9f, impactAt = 0.22f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 26f, reach = 10f, arc = 180f, knockback = 8f, scripted = true,
        };
        public float castMin = 4.5f, castMax = 9.5f;
        public float castCooldown = 5f;
        public float firstCastDelay = 1.2f; // after she wakes: her opening, if you hang back
        public float castFlight = 0.45f;
        public float castArc = 2.2f;
        public float castLandRadius = 1.2f;
        public float haulWait = 0.5f;  // it lies there, the chain slack: the tell that it's coming back
        public float haulTime = 0.9f;
        public float haulHitRadius = 0.9f;
        public float haulDamage = 20f;
        public float haulKnockback = 6f;
        public float reelTime = 0.45f;

        [Header("Undertow: too close for too long, and the water rises around her")]
        public SouredAttack undertow = new SouredAttack
        {
            name = "Undertow", windupClip = "UndertowWindup", strikeClip = "Undertow", recoverClip = "UndertowRecover",
            telegraph = 0.9f, strike = 2.6f, recover = 1.3f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0f, damage = 0f, reach = 3.4f, arc = 180f, knockback = 0f, scripted = true,
        };
        public float closeRadius = 2.8f;     // within this, it counts as close (any side)
        public float undertowAfter = 3.5f;   // seconds of it (easing off again when you step back)
        public float undertowCooldown = 9f;
        public float poolRadius = 3.4f;
        public float poolSpread = 0.45f;
        public float poolTick = 0.35f;
        public float poolDamage = 5f;
        [Range(0.1f, 1f)] public float poolWade = 0.55f; // your speed in it
        public float poolDrag = 1.0f;        // m/s towards her
        public Material poolMaterial;

        [Header("Framing the fight")]
        public float cameraLean = 0.35f;
        public float cameraZoom = 1.12f;

        [Header("The anchor and its chain (measured from the model by the scene builder)")]
        public Transform anchorBone;
        public Transform anchorRing;
        public Transform anchorCrown;
        public Transform chainStart;       // the Ferryman's hook at her waist
        public Quaternion anchorHeld = Quaternion.identity; // the Anchor bone's rotation in her hands, relative to her facing
        public Material chainMaterial;
        public AudioClip[] landSound;
        public AudioClip[] haulSound;

        const float AnchorLength = 1.5f; // ring to crown

        public AnchorMode Anchor { get; private set; }
        public bool Awakened { get; private set; }
        public Shockwave LastShockwave { get; private set; }
        public int Casts { get; private set; }
        public float CloseTime { get; private set; }
        public UndertowPool Pool { get; private set; }
        public Vector3 AnchorMiddle => lastRing - lastFrame * Vector3.up * (AnchorLength * 0.5f);

        float modeTime, nextCast, nextRoll, sweepReach, nextScrape, nextUndertow;
        bool hitThisMove, sweepCommitted, framing, shoved;
        Vector3 anchorAt, flightFrom, flightTo, haulFrom, blendRing;
        Quaternion flightFrame, blendFrame;
        Vector3 lastRing;
        Quaternion lastFrame = Quaternion.identity;
        LinkChain anchorChain;

        protected override string IdleClip => Awakened ? "Idle" : "Vigil";
        protected override string ChaseClip(float distance) => Anchor == AnchorMode.Reeling ? "Reel" : base.ChaseClip(distance);
        protected override float ChaseSpeed(float distance) => Anchor == AnchorMode.Held ? base.ChaseSpeed(distance) : 0f;
        protected override bool BlocksHit(Hit hit) => Anchor == AnchorMode.Held && base.BlocksHit(hit);

        protected override void Awake()
        {
            base.Awake();
            if (anchorRing != null)
                lastRing = anchorRing.position;
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Anchor != AnchorMode.Held)
                return null; // she needs it in her hands
            float angle = Vector3.Angle(transform.forward, toTarget);
            if (FollowUp(distance, angle) is SouredAttack follow)
                return follow;
            if (CloseTime >= undertowAfter && Time.time >= nextUndertow)
                return undertow; // you've stayed too close too long, wherever you stand
            if (angle > backAngle)
                return null; // behind her: she has to turn first
            if (distance <= sweepRadius + 0.4f && angle > flankAngle)
                return sweep; // at her side: the chain comes round
            if (distance <= attackRange)
                return Random.value < sweepChanceInFront ? sweep : primary;
            bool castReady = distance >= castMin && distance <= castMax && Time.time >= nextCast;
            if (YankReady(distance) && (!castReady || Random.value < 0.5f))
                return yank;
            if (castReady)
                return cast;
            if (distance <= sweepRadius + 0.3f && Time.time >= nextRoll)
            {
                nextRoll = Time.time + 0.8f;
                if (Random.value < midRangeSweepChance)
                    return sweep;
            }
            return null;
        }

        protected override void OnEntered(State state)
        {
            base.OnEntered(state);
            if (state == State.Chase && !Awakened)
            {
                Awakened = true;
                nextCast = Time.time + firstCastDelay;
            }
            if (state == State.Telegraph)
                hitThisMove = sweepCommitted = shoved = false;
            if (state == State.Telegraph && CurrentAttack == undertow)
            {
                CloseTime = 0f;
                nextUndertow = Time.time + undertow.telegraph + undertow.strike + undertow.recover + undertowCooldown;
            }
            if (state == State.Strike && CurrentAttack == undertow)
                Pool = UndertowPool.Well(transform, target, poolMaterial, poolRadius, poolSpread, poolTick, poolDamage, poolWade, poolDrag);
            else if (Pool != null && state != State.Strike)
            {
                Pool.Recede(state == State.Recover ? 0.7f : 0.35f); // she rises (or is knocked out of her prayer)
                Pool = null;
            }
            if (state == State.Staggered || state == State.Stunned || state == State.Dying)
                DropAnchor(); // whatever it was doing out there, it falls where it is
            if (state == State.Recover && CurrentAttack == sweep && Anchor == AnchorMode.Swung)
                StartReel();
            if (state == State.Recover && CurrentAttack == cast)
                nextCast = Time.time + cast.recover + castCooldown;
            PhaseTwoEntered(state);
        }

        // The sweep's reach is set with its aim: as far out as you stood.
        protected override void OnCommitted(SouredAttack attack)
        {
            if (attack != sweep)
                return;
            sweepReach = SweepReach();
            sweepCommitted = true;
        }

        float SweepReach()
        {
            if (sweepCommitted)
                return sweepReach;
            float distance = target != null ? Flat(target.transform.position - transform.position).magnitude : sweepRadius;
            return Mathf.Clamp(distance + 0.2f, sweepMinRadius, sweepRadius);
        }

        // The Anchor Drop lands: the ring of water spreads from the crown.
        protected override void OnImpact(SouredAttack attack)
        {
            if (!IsDrop(attack))
                return;
            Vector3 at = anchorCrown != null ? anchorCrown.position : transform.position + transform.forward * attack.reach;
            at.y = transform.position.y;
            LastShockwave = Shockwave.Spread(at, target, ringRadius, ringSpeed, ringWidth, ringDamage, ringKnockback, ringTint);
            Effects.Instance?.Splash(at + Vector3.up * 0.1f);
            if (target != null && Vector3.Distance(target.transform.position, at) < 9f)
                CameraRig.Instance?.Shake(PhaseTwo ? 0.38f : 0.22f); // (heavier once the floor breaks under it)
            if (PhaseTwo)
                floor?.BreakAt(at); // the floor gives way where it lands
        }

        protected override void Tick(float dt)
        {
            modeTime += dt;
            Frame();
            // How long the Apprentice has stayed close (easing off twice as fast once they step back).
            bool close = Awakened && target != null && target.IsAlive
                && Flat(target.transform.position - transform.position).magnitude <= closeRadius;
            CloseTime = close ? CloseTime + dt : Mathf.Max(0f, CloseTime - 2f * dt);
            PhaseTwoTick(dt);
            var attack = CurrentAttack;
            if (CurrentState == State.Telegraph && attack == sweep && Anchor == AnchorMode.Held && StateTime >= attack.telegraph * 0.55f)
                SetMode(AnchorMode.Swung); // let out on the chain to her right: the tell
            else if (CurrentState == State.Strike && attack == sweep && Anchor == AnchorMode.Swung)
            {
                if (!shoved)
                    Shove();
                StrikeWith(sweepHitRadius, sweep.damage, SweepKnock(), stunOnParry: true);
            }
            else if (CurrentState == State.Strike && attack == cast && Anchor == AnchorMode.Held && StateTime >= attack.strike * attack.impactAt)
                Release();

            switch (Anchor)
            {
                case AnchorMode.Flying:
                    if (modeTime >= castFlight)
                        Land();
                    break;
                case AnchorMode.Lying:
                    bool hauling = CurrentState == State.Recover && CurrentAttack == cast;
                    if (hauling && modeTime >= haulWait)
                        StartHaul();
                    else if (!hauling && (CurrentState == State.Chase || CurrentState == State.Idle))
                        StartReel(); // back on her feet: she pulls it home before anything else
                    break;
                case AnchorMode.Hauled:
                    Vector3 home = HaulTarget();
                    if (Time.time >= nextScrape)
                    {
                        nextScrape = Time.time + 0.05f;
                        Effects.Instance?.Scrape(AnchorMiddle - Vector3.up * 0.05f, AnchorMiddle - home);
                    }
                    StrikeWith(haulHitRadius, haulDamage, Flat(home - AnchorMiddle).normalized * haulKnockback, stunOnParry: true);
                    if (modeTime >= haulTime)
                        StartReel();
                    break;
                case AnchorMode.Reeling:
                    if (modeTime >= reelTime)
                        SetMode(AnchorMode.Held);
                    break;
            }
        }

        // The anchor (while it's out) reaches the Apprentice: one blow per attack. A perfect block against it snaps the
        // chain taut and yanks her off balance; the anchor stays where it stopped.
        void StrikeWith(float hitRadius, float damage, Vector3 knock, bool stunOnParry)
        {
            if (hitThisMove || target == null || !target.IsAlive)
                return;
            Vector3 to = target.transform.position - AnchorMiddle;
            if (Mathf.Abs(to.y) > 1.2f || Flat(to).magnitude > hitRadius)
                return;
            hitThisMove = true;
            if (HitTarget(damage, knock) == Defence.Parried && stunOnParry)
                Stun();
        }

        // The swing starts with anyone pressed up against her inside its reach: she shoulders them out past it (a
        // small hit, a big shove). Dash through it, or perfect-block it to stun her.
        void Shove()
        {
            shoved = true;
            if (target == null || !target.IsAlive)
                return;
            Vector3 away = Flat(target.transform.position - transform.position);
            if (away.magnitude > shoveRadius || Vector3.Angle(transform.forward, away) > backAngle)
                return;
            var push = (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * (10f * Mathf.Max(0f, shoveTo - away.magnitude));
            var result = HitTarget(shoveDamage, push); // (knockback eases off at 10/s: this much sets you down about shoveTo out)
            if (result == Defence.Dodged)
                return;
            hitThisMove = true; // out past the anchor: the swing itself won't catch you too
            CameraRig.Instance?.Shake(0.2f);
            if (result == Defence.Parried)
                Stun();
        }

        Vector3 SweepKnock()
        {
            Vector3 radial = Flat(AnchorMiddle - transform.position).normalized;
            return (radial * 0.5f + Quaternion.Euler(0f, -90f, 0f) * radial).normalized * sweep.knockback; // with the swing
        }

        // Thrown: along the locked aim, as far as the Apprentice is now (within limits), in a high arc.
        void Release()
        {
            Casts++;
            Vector3 dir = LockedDirection.sqrMagnitude > 0.01f ? LockedDirection : transform.forward;
            float distance = target != null ? Flat(target.transform.position - transform.position).magnitude : castMin;
            distance = Mathf.Clamp(distance, castMin - 1f, castMax);
            Vector3 middle = transform.position + dir * distance;
            flightFrom = lastRing;
            flightFrame = lastFrame;
            flightTo = middle - dir * (AnchorLength * 0.5f) + Vector3.up * 0.12f;
            SetMode(AnchorMode.Flying);
        }

        void Land()
        {
            anchorAt = flightTo;
            SetMode(AnchorMode.Lying);
            Vector3 middle = anchorAt + Flat(anchorAt - transform.position).normalized * (AnchorLength * 0.5f);
            Sfx.Instance?.Attack(landSound, middle, 1f, "cast_land");
            Effects.Instance?.Splash(middle + Vector3.up * 0.1f);
            if (target == null || !target.IsAlive)
                return;
            Vector3 to = Flat(target.transform.position - middle);
            if (to.magnitude < 9f)
                CameraRig.Instance?.Shake(0.15f);
            if (to.magnitude <= castLandRadius)
                HitTarget(cast.damage, (to.sqrMagnitude > 0.01f ? to.normalized : transform.forward) * cast.knockback); // (a perfect block only turns it aside)
        }

        void StartHaul()
        {
            haulFrom = anchorAt;
            hitThisMove = false;
            SetMode(AnchorMode.Hauled);
            clips.Loop("Haul", 0.1f);
            Sfx.Instance?.Attack(haulSound, AnchorMiddle, 0.9f, "cast_haul");
        }

        // It stops where it is and drops to the floor.
        void DropAnchor()
        {
            if (Anchor == AnchorMode.Held || Anchor == AnchorMode.Flying || Anchor == AnchorMode.Lying)
                return; // in her hands the Stagger clip drops it; in flight it lands anyway
            anchorAt = new Vector3(lastRing.x, transform.position.y + 0.12f, lastRing.z);
            SetMode(AnchorMode.Lying);
        }

        void StartReel()
        {
            blendRing = lastRing;
            blendFrame = lastFrame;
            SetMode(AnchorMode.Reeling);
        }

        void SetMode(AnchorMode mode)
        {
            if (mode == AnchorMode.Swung)
            {
                blendRing = lastRing;
                blendFrame = lastFrame;
            }
            Anchor = mode;
            modeTime = 0f;
        }

        // While the fight is on (she's awake, and you're in the chapel), the camera takes in more of it.
        void Frame()
        {
            bool fighting = Awakened && IsAlive && target != null && target.IsAlive
                && Flat(target.transform.position - transform.position).magnitude < aggroRadius * 1.6f;
            if (fighting == framing)
                return;
            framing = fighting;
            CameraRig.Instance?.SetFightFocus(fighting ? transform : null, cameraLean);
            CameraRig.Instance?.SetZoom(fighting ? cameraZoom : 1f);
        }

        Vector3 HaulTarget() => transform.position + Flat(anchorAt - transform.position).normalized * 1.3f + Vector3.up * 0.12f;

        public override void Reform()
        {
            floor?.Restore();
            if (TimesDefeated > 0)
                return; // a Boss stays beaten
            ResetPhaseTwo();
            Awakened = false;
            hitThisMove = false;
            CloseTime = 0f;
            nextUndertow = 0f;
            if (Pool != null)
                Destroy(Pool.gameObject);
            Pool = null;
            Frame();
            SetMode(AnchorMode.Held);
            base.Reform();
        }

        // ------------------------------------------------------------ the anchor, posed after the animation

        void LateUpdate()
        {
            if (anchorBone == null || anchorRing == null)
                return;
            // The animation has put it in her hands; while it's out, it goes where the fight says instead.
            Vector3 heldRing = anchorRing.position;
            Quaternion heldFrame = anchorBone.rotation * Quaternion.Inverse(anchorHeld);
            if (Anchor != AnchorMode.Held)
            {
                OutPose(heldRing, heldFrame, out Vector3 ring, out Quaternion frame);
                anchorBone.rotation = frame * anchorHeld;
                anchorBone.position += ring - anchorRing.position;
            }
            lastRing = anchorRing.position;
            lastFrame = anchorBone.rotation * Quaternion.Inverse(anchorHeld);
            DrawChain();
            PoseHook();
        }

        // Where the anchor's ring is and how it lies (its frame: forward = the way its face looks, up = from crown to
        // ring), for every mode but Held.
        void OutPose(Vector3 heldRing, Quaternion heldFrame, out Vector3 ring, out Quaternion frame)
        {
            Vector3 floor = Vector3.up * transform.position.y;
            switch (Anchor)
            {
                case AnchorMode.Swung:
                {
                    float angle = sweepFrom + 6f * Mathf.Sin(modeTime * 7f); // let out to her right, swinging
                    if (CurrentState == State.Strike && CurrentAttack == sweep)
                        angle = Mathf.Lerp(sweepFrom, sweepTo, UiKit.Smooth(0f, 1f, StateTime / sweep.strike));
                    Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Flat(transform.forward).normalized;
                    ring = transform.position + radial * (SweepReach() - AnchorLength * 0.5f) + Vector3.up * 0.9f;
                    frame = Quaternion.LookRotation(Vector3.up, -radial);
                    float blend = UiKit.Smooth(0f, 1f, modeTime / 0.3f); // out of her hands
                    ring = Vector3.Lerp(blendRing, ring, blend);
                    frame = Quaternion.Slerp(blendFrame, frame, blend);
                    return;
                }
                case AnchorMode.Flying:
                {
                    float t = Mathf.Clamp01(modeTime / castFlight);
                    ring = Vector3.Lerp(flightFrom, flightTo, t) + Vector3.up * (castArc * 4f * t * (1f - t));
                    frame = Quaternion.Slerp(flightFrame, Lying(flightTo), t);
                    return;
                }
                case AnchorMode.Hauled:
                {
                    float t = Mathf.Clamp01(modeTime / haulTime);
                    anchorAt = Vector3.Lerp(haulFrom, HaulTarget(), t * t); // it gathers speed as she hauls
                    ring = anchorAt;
                    frame = Lying(anchorAt);
                    return;
                }
                case AnchorMode.Reeling:
                {
                    float t = UiKit.Smooth(0f, 1f, modeTime / reelTime);
                    ring = Vector3.Lerp(blendRing, heldRing, t);
                    frame = Quaternion.Slerp(blendFrame, heldFrame, t);
                    return;
                }
                default: // Lying
                    ring = new Vector3(anchorAt.x, floor.y + 0.12f, anchorAt.z);
                    frame = Lying(anchorAt);
                    return;
            }
        }

        // Flat on the floor, face up, its ring towards her (the chain pulls it that way).
        Quaternion Lying(Vector3 ringAt)
        {
            Vector3 towardsHer = Flat(transform.position - ringAt);
            return Quaternion.LookRotation(Vector3.up, towardsHer.sqrMagnitude > 0.001f ? towardsHer.normalized : -transform.forward);
        }

        // The chain from the hook at her waist to the anchor's ring: slack in her hands, taut when it's far out.
        void DrawChain()
        {
            if (chainStart == null)
                return;
            anchorChain ??= new LinkChain("Anchor Chain", chainMaterial, 96, new Vector3(0.05f, 0.1f, 0.18f), 0.15f);
            bool shown = IsAlive || CurrentState == State.Dying && StateTime < deathDuration * 0.45f;
            float distance = Vector3.Distance(chainStart.position, lastRing);
            anchorChain.Draw(chainStart.position, lastRing, Mathf.Lerp(0.45f, 0.08f, distance / 6f), transform.position.y + 0.04f, shown);
        }

        void OnDestroy()
        {
            anchorChain?.Destroy();
            hookLine?.Destroy();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
