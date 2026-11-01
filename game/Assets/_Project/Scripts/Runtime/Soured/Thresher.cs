using UnityEngine;

namespace MyGame
{
    // A Thresher (the Orchard): an old threshing machine, iron and oak, its flail reel at the front, and in the seat at
    // the back the man who drove it, grown into its levers. A heavy that runs the rows: it lines up on you (its boiler
    // hissing, steam from the stack, the reel spinning up: the tell), then charges straight, barely turning. Sidestep
    // it: if it runs into something solid (a tree, a wall, a stook) it crashes and stalls a while, steaming, its driver
    // slumped. From the front the oar glances off the iron; from behind or the side it reaches the driver, and stalled,
    // from anywhere. Up close it isn't helpless: its flails sweep in front, and steam bursts from it behind. Iron doesn't
    // burn: fire only stokes its boiler, and it charges faster. The Hook wrenches its drive chain off: stalled, as if it
    // had crashed.
    public sealed class Thresher : Soured
    {
        [Header("The Thresher")]
        public SouredAttack charge = new SouredAttack(); // the run (primary: the flails' sweep; vent: steam behind)
        public SouredAttack vent = new SouredAttack();
        public float chargeSpeed = 10f;
        public float chargeNearest = 4f, chargeFurthest = 15f, lineUp = 14f, chargeCooldown = 2.2f;
        public float chargeDamage = 30f, chargeKnock = 12f;
        public float stalledTime = 3.5f;
        public float stokedTime = 8f, stokedFaster = 1.4f;
        public float ventRadius = 2.4f, ventDamage = 10f, ventKnock = 7f;
        public Transform reel;               // its flail reel's bone (spun here, faster as it revs and charges)
        public Transform stack;              // the top of its stack: steam, and sparks when it's stoked
        public AudioClip[] crashSound, chainSound, stokeSound, ventSound;

        public bool Stalled => stalled && CurrentState == State.Busy;
        public bool Charging => CurrentState == State.Strike && CurrentAttack == charge;
        public bool Stoked => Time.time < stokedUntil;
        public static event System.Action<Thresher> Crashed, ChainPulled, Charged; // (tests)

        float nextCharge, stokedUntil, reelAngle, steamDebt;
        bool stalled, struckThisCharge;

        protected override string ChaseClip(float distance) => "Idle";

        // It creeps nearer when you're far, or round whatever stands between (it only charges with a clear run);
        // otherwise it turns on the spot to line up.
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) =>
            distance > chargeFurthest * 0.7f || !ClearRun(toTarget, distance) ? PathDirection(target.transform.position) * chaseSpeed : Vector3.zero;

        // Nothing solid between it and you (as far as a charge would carry it before it got to you).
        bool ClearRun(Vector3 toTarget, float distance)
        {
            Vector3 dir = toTarget;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return true;
            float reach = Mathf.Max(0.5f, Mathf.Min(distance, chargeFurthest) - 1.2f);
            foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * 0.75f, 0.6f, dir.normalized, reach, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController) && hit.distance > 0f)
                    return false;
            return true;
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            float angle = Vector3.Angle(transform.forward, toTarget);
            if (distance <= primary.reach && angle < 70f)
                return primary;                                       // the flails, in front
            if (distance <= ventRadius && angle > 100f)
                return vent;                                          // steam, behind
            if (Time.time >= nextCharge && distance >= chargeNearest && distance <= chargeFurthest && angle < lineUp && ClearRun(toTarget, distance))
            {
                nextCharge = Time.time + chargeCooldown;
                return charge;
            }
            return null;
        }

        protected override void OnEntered(State state)
        {
            if ((state == State.Chase || state == State.Idle) && stalled)
            {
                stalled = false;
                // Back off whatever it ran into, a little way (if there's ground behind it), before it lines up again.
                if (FootingAt(transform.position - transform.forward * (radius + 1.4f)))
                    Shove(-transform.forward * 9f);
                nextCharge = Mathf.Max(nextCharge, Time.time + 1.2f);
            }
            if (state == State.Telegraph && CurrentAttack == charge && Stoked)
                Effects.Instance?.AlertFlare(StackTop);
            if (state == State.Strike && CurrentAttack == charge)
            {
                struckThisCharge = false;
                Charged?.Invoke(this);
            }
            if (state == State.Strike && CurrentAttack == vent)
                Vent();
        }

        Vector3 StackTop => stack != null ? stack.position : transform.position + Vector3.up * 2.4f;

        protected override void Tick(float dt)
        {
            if (!IsAlive)
                return;
            if (Charging)
                Run(dt);
            // Steam from the stack: a breath at rest, billowing as it revs and runs, and sparks when it's stoked.
            bool revving = CurrentState == State.Telegraph && CurrentAttack == charge;
            steamDebt += Time.deltaTime * (Charging ? 12f : revving ? 16f : Stalled ? 8f : 2f);
            for (; steamDebt >= 1f; steamDebt -= 1f)
                Effects.Instance?.Smoke(StackTop + Random.insideUnitSphere * 0.08f, 0.55f);
            if (Stoked && Random.value < 25f * Time.deltaTime)
                Effects.Instance?.Flame(StackTop, Vector3.up * Random.Range(1.5f, 3f) + Random.insideUnitSphere * 0.5f, Random.Range(0.2f, 0.4f), Random.Range(0.08f, 0.16f));
        }

        // The run: straight on, faster when stoked; into something solid it crashes; at an edge it brakes; through you,
        // it strikes once (a perfect block stops it dead).
        void Run(float dt)
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();
            float speed = chargeSpeed * (Stoked ? stokedFaster : 1f);
            if (!FootingAt(transform.position + dir * (radius + 1.0f)))
            {
                BeBusy("Idle", 0.6f); // (the brink: it brakes)
                return;
            }
            if (Physics.SphereCast(transform.position + Vector3.up * 0.75f, 0.6f, dir, out var hit, radius - 0.3f + speed * dt, ~0, QueryTriggerInteraction.Ignore)
                && !(hit.collider is CharacterController))
            {
                Stall(hit.point, crashed: true);
                return;
            }
            if (!struckThisCharge && target != null && target.IsAlive && !target.Airborne) // (vaulted over: it drives on under)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude < radius + 0.9f && Vector3.Angle(dir, to) < 65f)
                {
                    struckThisCharge = true;
                    var side = Vector3.Cross(Vector3.up, dir) * Mathf.Sign(Vector3.Dot(to, Vector3.Cross(Vector3.up, dir)) + 0.001f);
                    if (HitTarget(chargeDamage, (dir + side).normalized * chargeKnock) == Defence.Parried)
                    {
                        Stall(transform.position + dir * radius + Vector3.up, crashed: true); // (stopped dead on the oar)
                        return;
                    }
                }
            }
            Shove(dir * speed * 1.15f); // (the controller carries it, against whatever's in the way)
        }

        // Stalled: run into something (or a perfect block, or its chain wrenched off): steaming, its driver slumped, open.
        void Stall(Vector3 at, bool crashed)
        {
            stalled = true;
            Shove(Vector3.zero);
            BeBusy("Crash", stalledTime);
            Effects.Instance?.Rubble(at, 0.4f);
            for (int i = 0; i < 4; i++)
                Effects.Instance?.BlockSpark(at + Random.insideUnitSphere * 0.3f, -transform.forward);
            for (int i = 0; i < 14; i++)
                Effects.Instance?.Smoke(StackTop + Random.insideUnitSphere * 0.4f, 0.6f);
            Sfx.Instance?.Attack(crashed ? crashSound : chainSound, at, 1f, crashed ? "thresher_crash" : "thresher_chain");
            if (target != null && Vector3.Distance(target.transform.position, transform.position) < 10f)
                CameraRig.Instance?.Shake(crashed ? 0.3f : 0.15f);
            if (crashed)
                Crashed?.Invoke(this);
            else
                ChainPulled?.Invoke(this);
        }

        // Steam, blown from it all round: whoever's at its back is scalded and thrown off.
        void Vent()
        {
            for (int i = 0; i < 40; i++)
            {
                Vector3 out_ = Random.insideUnitSphere;
                out_.y = Mathf.Abs(out_.y) * 0.4f;
                Effects.Instance?.Smoke(transform.position + Vector3.up * 0.8f + out_ * ventRadius * 0.6f, 0.7f);
            }
            Sfx.Instance?.Attack(ventSound, transform.position + Vector3.up, 1f, "thresher_vent");
            if (target == null || !target.IsAlive)
                return;
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude < ventRadius + radius * 0.5f)
                target.TakeHit(ventDamage, to.normalized * ventKnock, this, blockable: false); // (steam: no guard stops it)
        }

        // From the front the oar glances off its iron; from behind or the side (and stalled, from anywhere) it reaches the
        // driver.
        protected override bool BlocksHit(Hit hit)
        {
            if (Stalled)
                return false;
            Vector3 from = hit.From - transform.position;
            from.y = 0f;
            return Vector3.Angle(transform.forward, from) < 75f;
        }

        // Iron doesn't burn: fire only stokes its boiler (it runs faster a while).
        protected override bool Absorbs(Hit hit, bool fire)
        {
            if (!fire)
                return false;
            if (!Stoked)
                Sfx.Instance?.Attack(stokeSound, StackTop, 0.9f, "thresher_stoke");
            stokedUntil = Time.time + stokedTime;
            return true;
        }

        // The Hook: caught on its drive chain, wrenched off: it stalls where it is.
        public override HookResult OnHooked(Apprentice by)
        {
            if (!IsAlive || Stalled)
                return HookResult.Shrugged;
            Vector3 chain = transform.position + transform.right * 0.7f + Vector3.up * 0.9f;
            Stall(chain, crashed: false);
            return HookResult.Ripped;
        }

        void LateUpdate()
        {
            if (reel == null || !IsAlive)
                return;
            // The reel: idling round, spinning up as it revs, flailing as it runs; still when stalled.
            float rate = Stalled ? 0f : Charging ? 1100f
                : CurrentState == State.Telegraph && CurrentAttack == charge ? Mathf.Lerp(200f, 1100f, StateTime / Mathf.Max(0.01f, charge.telegraph))
                : 140f;
            reelAngle = (reelAngle + rate * Time.deltaTime) % 360f;
            reel.rotation = Quaternion.AngleAxis(-reelAngle, transform.right) * reel.rotation;
        }
    }
}
