using UnityEngine;

namespace MyGame
{
    // The Clinger: a Soured that couldn't let go of a person. Fast and fragile, and always two. They circle you at a few
    // metres in quick skittering bursts, one watching while the other strikes: a crouch and a sharp hiss (the tell),
    // then a leap. Dodged, it sails past and sprawls where it lands (open); perfectly blocked, it's knocked down
    // (stunned); blocked, it glances off. Caught, it clings to your back: your stamina drains away and doesn't come back,
    // your health a little too, and you move slower, while it whispers. Dash and it's thrown off (even with no stamina
    // left), tumbling to land staggered: your window. Scatter one and the other keens, and turns frantic.
    //
    // Riding its other half (`mount`: the Unparted's Fast, on Hold's back): shielded there (Hold's shoulders take the
    // blow), it leaps down at you now and then, and after one go (caught hold or not) it scrambles back up. The Hook
    // pulls it off. Its other half (`other`) scattered: it keens and turns frantic, and between attacks it goes back
    // to where Hold fell and crouches there, weeping, open.
    public class Clinger : Soured
    {
        [Header("Clinger: circling")]
        public float circleRadius = 3.4f;
        public float circleSpeed = 2.6f;
        [Tooltip("Frantic (its other half scattered): this much faster, and it leaps again sooner")]
        public float franticSpeed = 1.3f;

        [Header("The leap")]
        public SouredAttack leap = new SouredAttack
        {
            name = "Leap", windupClip = "Crouch", strikeClip = "Leap", recoverClip = "Land",
            telegraph = 0.35f, strike = 0.35f, recover = 0.7f, impactAt = 0.8f,
            lunge = 2f, maxLunge = 4.5f, trackUntil = 0.6f, damage = 4f, reach = 1.1f, arc = 75f, knockback = 1.5f,
            scripted = true, // it catches hold (or not) by its own rules
        };
        public float leapMin = 1.6f, leapMax = 4.8f;
        public float leapCooldown = 1.8f;

        [Header("Clinging")]
        public float backOffset = 0.42f, backHeight = 0.25f, sideOffset = 0.26f;

        [Header("Thrown off")]
        public float throwDistance = 2.4f;
        public float throwTime = 0.45f;
        public float thrownStagger = 1.1f;

        [Header("Sounds")]
        public AudioClip[] skitterSound, grabSound, throwSound, keenSound, whisperSound;

        [Tooltip("Its other half (paired with the nearest Clinger if left empty)")]
        public Clinger partner;

        [Header("Riding its other half (the Unparted's Fast)")]
        public Transform mount;          // a bone on its other half's back; none: an ordinary Clinger
        public Soured other;             // that other half (not a Clinger)
        public float rideTime = 2.2f;    // at least this long on its back between leaps
        public float dismountRange = 7f; // it leaps down at you from this close
        public float mountReach = 1.5f;  // how near it must scramble to climb back up
        public float weepTime = 3.2f;    // crouched where its other half fell
        public bool Mounted { get; private set; }
        public bool Weeping { get; private set; }
        public bool Mourning { get; private set; }

        public bool Clinging { get; private set; }
        public bool Frantic { get; private set; }
        public bool BeingThrown => throwStart >= 0f;
        public int Whispers { get; private set; }

        float nextLeap, nextBurstFlip, burstSide = 1f, nextWhisper, throwStart = -1f;
        bool burst, resolved, mourned, paired, sighted;
        int slot;
        Vector3 throwFrom, throwTo;

        bool returning, leapt, mountedOnce, landStagger = true;
        float mountedAt, hopStart = -1f, nextWeep;
        Vector3 hopFrom, otherFellAt;
        const float HopTime = 0.35f;

        protected override string ChaseClip(float distance) => burst || distance > leapMax || returning ? "Skitter" : IdleClip;
        protected override bool Unhittable => Mounted; // (on Hold's back: Hold's shoulders take the blow)

        // Straight in from afar; nearer, round and round at a few metres in quick bursts, now and then turning back.
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            // Back to its other half, to climb up again; or (mourning) to where it fell, to weep.
            if (returning && other != null && other.IsAlive)
                return Towards(other.transform.position, mountReach, Mount);
            if (Mourning && !Weeping && Time.time >= nextWeep)
                return Towards(otherFellAt, 0.9f, Weep);
            if (Time.time >= nextBurstFlip)
            {
                burst = !burst;
                nextBurstFlip = Time.time + (burst ? Random.Range(0.35f, 0.6f) : Random.Range(0.2f, 0.45f));
                if (burst)
                {
                    Sfx.Instance?.Attack(skitterSound, transform.position, 0.5f, "clinger_skitter");
                    if (Random.value < 0.3f)
                        burstSide = -burstSide;
                }
            }
            float pace = (burst ? 1f : 0.25f) * (Frantic ? franticSpeed : 1f);
            Vector3 dir = toTarget.normalized;
            if (distance > leapMax + 1.5f)
                return dir * chaseSpeed * (Frantic ? franticSpeed : 1f);
            Vector3 round = Vector3.Cross(Vector3.up, dir) * burstSide;
            float inOut = Mathf.Clamp((distance - circleRadius) * 1.5f, -1.5f, 2.5f);
            return (round * circleSpeed + dir * inOut) * pace;
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Mounted || returning || Mourning && Time.time >= nextWeep)
                return null;
            if (Time.time < nextLeap || distance < leapMin || distance > leapMax || target == null)
                return null;
            if (partner != null && partner.IsAlive && (partner.CurrentState == State.Telegraph || partner.CurrentState == State.Strike))
                return null; // one at a time: the other watches
            if (target.ClungBy >= 2 || !Clingable(target))
                return null;
            return leap;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph)
            {
                resolved = false;
                leapt = true;
                nextLeap = Time.time + leap.telegraph + leap.strike + leap.recover + leapCooldown * (Frantic ? 0.5f : 1f);
            }
            if (state == State.Chase && !sighted)
            {
                sighted = true;
                nextLeap = Mathf.Max(nextLeap, Time.time + Random.Range(0.6f, 1.2f)); // a moment's circling first
            }
            if (state == State.Idle)
                sighted = false;
            // One go at you, then back up onto its other half.
            if (state == State.Chase && leapt && mount != null && other != null && other.IsAlive && !Mounted && !Clinging)
                returning = true;
        }

        protected override void Tick(float dt)
        {
            if (mount != null && !mountedOnce)
            {
                mountedOnce = true;
                Mount(instantly: true);
            }
            if (!paired)
                Pair();
            if (partner != null && !mourned && IsAlive && !partner.IsAlive)
                Keen();
            if (other != null && !mourned && IsAlive && !other.IsAlive)
            {
                Keen(); // its other half is gone
                Mourning = true;
                otherFellAt = other.transform.position;
                nextWeep = Time.time + 1.2f;
                returning = false;
                if (Mounted)
                    Dismount(towardsTarget: false);
            }
            if (Mounted)
                UpdateMounted();
            if (Weeping && CurrentState != State.Busy)
            {
                Weeping = false; // (done weeping, or knocked out of it)
                nextWeep = Time.time + weepTime + 3.5f; // a go at you, then back to it
            }
            if (CurrentState == State.Strike && !resolved && StateTime >= leap.strike * leap.impactAt)
            {
                resolved = true;
                TryLatch();
            }
            if (Clinging)
                UpdateCling();
            if (BeingThrown)
                UpdateThrow();
        }

        // Its other half: the nearest Clinger with none of its own.
        void Pair()
        {
            paired = true;
            if (mount != null || other != null)
                return; // (the Unparted's Fast has its other half already)
            if (partner != null)
            {
                partner.partner = this;
                partner.paired = true;
                return;
            }
            Clinger best = null;
            float bestDistance = 8f;
            foreach (var s in WorldState.AllSoured)
                if (s is Clinger other && other != this && other.partner == null)
                {
                    float d = Vector3.Distance(other.transform.position, transform.position);
                    if (d < bestDistance)
                    {
                        best = other;
                        bestDistance = d;
                    }
                }
            if (best != null)
            {
                partner = best;
                best.partner = this;
                best.paired = true;
            }
        }

        // Its other half has scattered: a keening cry, and it turns frantic.
        void Keen()
        {
            mourned = true;
            Frantic = true;
            Sfx.Instance?.Attack(keenSound, transform.position + Vector3.up, 0.9f, "clinger_keen");
            Effects.Instance?.AlertFlare(transform.position + transform.forward * 0.3f + Vector3.up * eyeHeight);
            nextLeap = Mathf.Min(nextLeap, Time.time + 0.8f);
        }

        // ---- riding its other half

        Vector3 Towards(Vector3 where, float reach, System.Action arrived)
        {
            Vector3 to = where - transform.position;
            to.y = 0f;
            if (to.magnitude <= reach)
            {
                arrived();
                return Vector3.zero;
            }
            return to.normalized * chaseSpeed * (Frantic ? franticSpeed : 1f);
        }

        void Mount() => Mount(instantly: false);

        // Up onto its other half's back (a hop), and it rides there, holding on.
        void Mount(bool instantly)
        {
            if (mount == null || other == null || !other.IsAlive)
                return;
            Mounted = true;
            returning = leapt = false;
            SetCarried(true);
            BeBusy("Cling", 3600f);
            mountedAt = Time.time;
            hopFrom = transform.position;
            hopStart = instantly ? -1f : Time.time;
        }

        void UpdateMounted()
        {
            if (!IsAlive || other == null || !other.IsAlive || CurrentState != State.Busy)
            {
                Dismount(towardsTarget: false);
                return;
            }
            Vector3 forward = other.transform.forward;
            forward.y = 0f;
            Vector3 seat = mount.position;
            float u = hopStart < 0f ? 1f : Mathf.Clamp01((Time.time - hopStart) / HopTime);
            transform.SetPositionAndRotation(Vector3.Lerp(hopFrom, seat, UiKit.Smooth(0f, 1f, u)) + Vector3.up * (0.6f * Mathf.Sin(u * Mathf.PI)),
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward));
            // Down at you: when you're near, it's had a while on its back, and its other half isn't mid-blow.
            if (u >= 1f && target != null && Clingable(target) && !passive && !Held && target.ClungBy < 2 && Time.time - mountedAt > rideTime
                && Vector3.Distance(target.transform.position, other.transform.position) < dismountRange
                && other.CurrentState != State.Telegraph && other.CurrentState != State.Strike)
            {
                Dismount(towardsTarget: true);
                nextLeap = Time.time + 0.5f;
            }
        }

        // Down off its other half's back: a hop to the ground beside it (towards you, to come at you).
        void Dismount(bool towardsTarget)
        {
            if (!Mounted)
                return;
            Mounted = false;
            Vector3 dir = towardsTarget && target != null ? target.transform.position - other.transform.position : other.transform.right;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                dir = other.transform.forward;
            throwFrom = transform.position;
            throwTo = Landing(other.transform.position + dir.normalized * (other.radius + 1.0f));
            throwStart = Time.time;
            landStagger = false;
            BeBusy("Leap", throwTime);
        }

        // Where its other half fell: it crouches there a while, weeping (open to a blow).
        void Weep()
        {
            Weeping = true;
            BeBusy("Crouch", weepTime);
            Sfx.Instance?.Attack(keenSound, transform.position + Vector3.up, 0.45f, "clinger_weep");
        }

        static bool Clingable(Apprentice a) =>
            a.IsAlive && a.CurrentState != Apprentice.State.Rowing && a.CurrentState != Apprentice.State.Sinking
            && a.CurrentState != Apprentice.State.Ceremony && a.CurrentState != Apprentice.State.Hooking;

        // The leap reaches them: dodged it sails on; parried it's knocked down; blocked it glances off; else it holds on.
        void TryLatch()
        {
            if (target == null || !Clingable(target) || target.ClungBy >= 2)
                return;
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > leap.reach + radius || Vector3.Angle(transform.forward, to) > leap.arc)
                return; // sails past
            var result = HitTarget(leap.damage, to.normalized * leap.knockback);
            if (result == Defence.Parried)
                Stun();
            else if (result == Defence.Taken && target.IsAlive && Clingable(target))
                Latch();
        }

        void Latch()
        {
            Clinging = true;
            slot = target.ClungBy;
            target.Cling(this);
            SetCarried(true);
            BeBusy("Cling", 3600f);
            Sfx.Instance?.Attack(grabSound, transform.position + Vector3.up, 0.9f, "clinger_grab");
            nextWhisper = Time.time + Random.Range(1.0f, 2.0f);
        }

        // On their back (one each side, if both are), whispering now and then.
        void UpdateCling()
        {
            if (!IsAlive || target == null || !Clingable(target) || CurrentState != State.Busy)
            {
                LetGoOf(target != null ? target.transform.forward : transform.forward, 1.2f);
                return;
            }
            var t = target.transform;
            Vector3 forward = new Vector3(t.forward.x, 0f, t.forward.z).normalized;
            Vector3 side = target.ClungBy > 1 ? t.right * (slot == 0 ? -sideOffset : sideOffset) : Vector3.zero;
            transform.SetPositionAndRotation(t.position - forward * backOffset + Vector3.up * backHeight + side, Quaternion.LookRotation(forward));
            if (Time.time >= nextWhisper)
            {
                nextWhisper = Time.time + Random.Range(2.5f, 4f);
                Whispers++;
                Sfx.Instance?.Attack(whisperSound, transform.position + Vector3.up, 0.35f, "clinger_whisper");
            }
        }

        // Dashed off (the Apprentice's dash, `direction`): flung the other way, tumbling, to land staggered.
        public void ThrowOff(Vector3 direction) => LetGoOf(direction, throwDistance, thrown: true);

        void LetGoOf(Vector3 direction, float distance, bool thrown = false)
        {
            if (!Clinging)
                return;
            Clinging = false;
            target?.LetGo(this);
            if (!IsAlive)
            {
                SetCarried(false); // (scattering: it falls where it is)
                return;
            }
            Vector3 away = -new Vector3(direction.x, 0f, direction.z);
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;
            throwFrom = transform.position;
            throwTo = Landing(throwFrom + away.normalized * distance);
            throwStart = Time.time;
            BeBusy("Thrown", throwTime);
            if (thrown)
                Sfx.Instance?.Attack(throwSound, transform.position + Vector3.up, 0.9f, "clinger_throw");
        }

        void UpdateThrow()
        {
            float u = Mathf.Clamp01((Time.time - throwStart) / throwTime);
            float e = UiKit.Smooth(0f, 1f, u);
            transform.position = Vector3.Lerp(throwFrom, throwTo, e) + Vector3.up * (0.7f * Mathf.Sin(u * Mathf.PI));
            if (u >= 1f)
            {
                throwStart = -1f;
                SetCarried(false);
                PlaceAt(throwTo);
                if (landStagger)
                    StaggerNow(thrownStagger); // (over the water it falls in, and drowns: fair play)
                else
                    BeBusy("Land", 0.25f); // (hopped down off its other half: on its feet, and at you)
                landStagger = true;
            }
        }

        // Where it comes down: the ground there, or the water's surface (it will fall in).
        Vector3 Landing(Vector3 at)
        {
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 2f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController) && hit.point.y > waterLevel)
                    return hit.point + Vector3.up * 0.05f;
            return new Vector3(at.x, waterLevel - 0.2f, at.z);
        }

        public override HookResult OnHooked(Apprentice by)
        {
            if (Clinging || BeingThrown)
                return HookResult.Shrugged;
            if (Mounted)
            {
                // Pulled off its other half's back.
                Mounted = false;
                SetCarried(false);
                PlaceAt(Landing(transform.position));
                StaggerNow(0.05f); // (no longer riding: free to be pulled)
            }
            return base.OnHooked(by);
        }

        public override void Reform()
        {
            if (Clinging)
                target?.LetGo(this);
            Clinging = false;
            throwStart = -1f;
            Frantic = mourned = sighted = false;
            Mounted = Weeping = Mourning = returning = leapt = false;
            base.Reform();
            if (mount != null)
                Mount(instantly: true);
        }

        void OnDestroy()
        {
            if (Clinging && target != null)
                target.LetGo(this);
        }
    }
}
