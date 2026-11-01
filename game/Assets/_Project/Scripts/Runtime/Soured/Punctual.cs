using UnityEngine;

namespace MyGame
{
    // The Punctual: couldn't let go of their duty. The Hall of the Last Hour's household, still keeping its hours: it
    // does nothing that isn't on the beat (the HallClock's, a second, the music's own). The watch in its chest ticks
    // each beat; it marches a step a beat; it only begins a blow on a beat, and every blow lands on one. Two:
    // the Count (the key raised on a beat, a jolt on the next, brought down on the third: one heavy blow, a long
    // recovery) and the Quarters (drawn back a beat, the warning, then four blows on four beats, a step into each,
    // each ringing a note of the quarter chime from its chest; the fourth overhead, the heaviest). Block one right on
    // the beat (a perfect block) or stagger it and it loses count: its hand spins, it stands listening, and does
    // nothing till a bar begins again.
    public sealed class Punctual : Soured
    {
        [Header("The Count (primary) and the Quarters")]
        public SouredAttack quarters = new SouredAttack
        {
            name = "Quarters", windupClip = "Warn", strikeClip = "Quarters", recoverClip = "QuartersRecover",
            telegraph = 0.85f, strike = 3.3f, recover = 1.4f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.9f, damage = 15f, reach = 2.3f, arc = 70f, knockback = 5f, scripted = true,
        };
        public float lastQuarterDamage = 30f, lastQuarterKnockback = 9f;
        public float quarterStep = 3.5f;             // the shove forward into each blow (about half a metre)
        public float quarterTurn = 110f;             // degrees a second it turns after you between blows
        [Range(0f, 1f)] public float quartersChance = 0.45f;
        public int countBeats = 2;                   // the Count: beats from raising the key to the blow
        public float firstQuarterAt = 0.15f;         // the Quarters: the first blow, this far into the strike (on a beat)
        public float hearing = 16f;                  // its ticking is heard this near

        [Header("Its watch")]
        public Transform watchHand;
        public float tickDegrees = 30f, lostSpin = 900f;

        [Header("Sounds")]
        public AudioClip[] tickSound, quarterSound, quarterSwingSound, lostSound, foundSound;

        public bool Lost { get; private set; }
        public int QuarterBlows { get; private set; }       // (tests) blows of the Quarters struck, all told
        public float LastBlowOffBeat { get; private set; } = -1f; // (tests) how far from a beat the last blow fell (seconds)

        long lastBeat = long.MinValue;
        int beatFrame = -10, blowsThisRun;
        float handAngle, lostAt;

        protected override string IdleClip => "Idle";
        protected override string ChaseClip(float distance) => Lost ? "Lost" : "Walk";
        // A step a beat: it moves in the middle of each, and stands on the tick.
        protected override float ChaseSpeed(float distance)
        {
            if (Lost)
                return 0f;
            float s = Mathf.Sin(Mathf.PI * HallClock.Through);
            return chaseSpeed * 2f * s * s;
        }

        bool Aware => IsAlive && CurrentState != State.Idle && CurrentState != State.Returning;

        // It only begins a blow on a beat (this frame or the last), and takes off the moment it's late by.
        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Lost || Time.frameCount - beatFrame > 1)
                return null;
            float late = HallClock.Through * HallClock.Beat;
            if (late > 0.25f)
                return null;
            if (distance <= quarters.reach && Random.value < quartersChance)
            {
                quarters.telegraph = Mathf.Max(0.05f, HallClock.Beat - firstQuarterAt - late);
                return quarters;
            }
            if (distance <= attackRange)
            {
                primary.telegraph = Mathf.Max(0.05f, countBeats * HallClock.Beat - primary.strike * primary.impactAt - late);
                return primary;
            }
            return null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Staggered || state == State.Stunned)
                LoseCount();
            if (state == State.Strike)
                blowsThisRun = 0;
        }

        void LoseCount()
        {
            if (!Lost)
                Sfx.Instance?.Attack(lostSound, WatchAt, 1f, "punctual_lost");
            Lost = true;
            lostAt = Time.time;
        }

        Vector3 WatchAt => watchHand != null ? watchHand.position : transform.position + Vector3.up * 1.3f;

        protected override void OnImpact(SouredAttack attack) => Blown();

        void Blown() => LastBlowOffBeat = Mathf.Min(HallClock.Through, 1f - HallClock.Through) * HallClock.Beat;

        protected override void Tick(float dt)
        {
            long beat = HallClock.BeatIndex;
            if (beat != lastBeat)
            {
                lastBeat = beat;
                OnBeat(beat);
            }
            if (Lost)
                handAngle += lostSpin * dt;
            if (CurrentState == State.Strike && CurrentAttack == quarters)
                Quarters(dt);
        }

        void OnBeat(long beat)
        {
            beatFrame = Time.frameCount;
            if (!IsAlive)
                return;
            if (Lost)
            {
                // It finds the beat again where a bar begins (not the very next tick after losing it).
                if (CurrentState == State.Chase && HallClock.BarStarts(beat) && Time.time - lostAt > 0.6f)
                {
                    Lost = false;
                    Sfx.Instance?.Attack(foundSound, WatchAt, 1f, "punctual_found");
                }
                return;
            }
            handAngle += tickDegrees;
            if (Aware && target != null && Vector3.Distance(target.transform.position, transform.position) < hearing)
                Sfx.Instance?.Attack(tickSound, WatchAt, 0.8f, "punctual_tick");
        }

        // The Quarters: a blow a beat, four of them, turning after you between; each rings its note.
        void Quarters(float dt)
        {
            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), quarterTurn * dt);
            }
            int due = Mathf.Min(4, Mathf.FloorToInt((StateTime - firstQuarterAt) / HallClock.Beat) + 1);
            while (blowsThisRun < due && CurrentState == State.Strike)
                Blow(blowsThisRun++);
        }

        void Blow(int k)
        {
            bool last = k == 3;
            QuarterBlows++;
            Blown();
            Shove(transform.forward * quarterStep);
            Sfx.Instance?.Attack(quarterSwingSound, transform.position + Vector3.up, 0.9f, "quarters_swing", last ? 0.85f : 1.1f);
            if (quarterSound != null && quarterSound.Length > k)
                Sfx.Instance?.Note(quarterSound[k], WatchAt, 1f, "punctual_quarter");
            if (target == null || !target.IsAlive)
                return;
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > quarters.reach + 0.35f || Vector3.Angle(transform.forward, toTarget) > quarters.arc)
                return;
            float damage = last ? lastQuarterDamage : quarters.damage;
            float knock = last ? lastQuarterKnockback : quarters.knockback;
            if (HitTarget(damage, toTarget.normalized * knock) == Defence.Parried)
                Stun(); // (blocked on the beat: the run's over, and it's lost count)
        }

        // The watch's hand, a tick a beat (clockwise, as you face it); spinning when it's lost count.
        void LateUpdate()
        {
            if (watchHand != null)
                watchHand.rotation = Quaternion.AngleAxis(-handAngle, transform.forward) * watchHand.rotation;
        }
    }
}
