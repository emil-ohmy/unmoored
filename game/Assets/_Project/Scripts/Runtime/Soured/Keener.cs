using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A Keener: couldn't let go of grief. One of the mourners who stopped the Hall's clocks at the minute of a death, in
    // black crape, the stopped watch in its hand. It keeps its distance (come near and it drifts back; it never skips
    // away like a Gleaner: it's slow, and fragile), and from there it keens: it draws breath (rising, the watch lifted
    // high: the tell), then tolls a knell: three rings of sound rolling out across the floor close behind one another, a
    // band that strikes as one. It knocks you back where it passes you, unless you dash through it (or are beyond it, or
    // above it: the Vault leaps it and comes down on it); a wall between muffles it. Dashing in through the band is the
    // answer: it has little health, and two blows stagger it, cutting its knell short. Mourners near one another keen
    // in turn: while one keens the others wait, and answer it after.
    public sealed class Keener : Soured
    {
        [Header("The Keener")]
        public float keepAway = 5.5f;        // nearer than this, it drifts back
        public float comeWithin = 10f;       // further than this, it comes closer
        public float backOffSpeed = 1.3f;
        public float knellReach = 10f;       // it keens at you from within this
        public float knellEvery = 5.5f;      // from one knell's start to the next's (the breath, the toll and the spent pause in it)
        public float firstKnellDelay = 0.6f; // after it notices you
        public float chorusRadius = 14f;     // Keeners within this of one another keen in turn
        public float answerAfter = 0.6f;     // after another's knell, before it may begin its own

        [Header("The rings")]
        public float ringRadius = 9f;
        public float ringSpeed = 5.5f;
        public float ringWidth = 0.8f;
        public float ringDamage = 12f;
        public float ringKnockback = 5f;
        public Color ringTint = new Color(0.76f, 0.78f, 0.9f, 0.8f);

        [Header("Sounds")]
        public AudioClip[] knellSound; // the bell, once a knell (with its first ring)
        public AudioClip[] deathSound; // a sigh letting go, and its watch going again

        static readonly List<Keener> mourners = new List<Keener>();

        public static event System.Action<Keener, Shockwave> Rang; // (tests)
        public int Rings { get; private set; }                      // (tests) rings sent out
        public Shockwave LastRing { get; private set; }

        float nextKnell, keenedAt = -10f;
        Shockwave.Band band;

        // Drawing breath or tolling: its knell under way.
        public bool Keening => CurrentState == State.Telegraph || CurrentState == State.Strike;

        protected override void Awake()
        {
            base.Awake();
            mourners.Add(this);
        }

        void OnDestroy() => mourners.Remove(this);

        // Another near it is keening, or has only just stopped: it waits its turn.
        bool AnothersTurn()
        {
            foreach (var other in mourners)
                if (other != this && other.IsAlive && Time.time - other.keenedAt < answerAfter
                    && (other.transform.position - transform.position).sqrMagnitude < chorusRadius * chorusRadius)
                    return true;
            return false;
        }

        protected override void Tick(float dt)
        {
            if (Keening)
                keenedAt = Time.time;
        }

        protected override string ChaseClip(float distance) => distance < keepAway || distance > comeWithin ? "Walk" : "Idle";

        // Between keepAway and comeWithin it stands its ground and keens; nearer, it drifts back (facing you); further,
        // it comes.
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            if (distance < keepAway)
                return -toTarget.normalized * backOffSpeed;
            if (distance > comeWithin)
                return PathDirection(target.transform.position) * chaseSpeed;
            return Vector3.zero;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Dying)
                Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 1.2f, 1f, "keener_death");
            if (state == State.Chase && nextKnell < Time.time + firstKnellDelay && CurrentAttack == null)
                nextKnell = Time.time + firstKnellDelay; // (noticing you: a moment's look before the first breath)
        }

        // At any range within its reach, close in or not (a knell at your feet is its only defence), but only on its
        // own level: its rings roll across its floor, not the one below.
        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Time.time < nextKnell || distance > knellReach || !OnItsLevel() || AnothersTurn())
                return null;
            nextKnell = Time.time + knellEvery;
            keenedAt = Time.time; // (its turn taken at once: another choosing this same frame waits)
            return primary;
        }

        // Each ring of the knell, from where it stands; the knell's rings one band (they strike as one), the bell once.
        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            if (index == 0)
            {
                band = new Shockwave.Band();
                Sfx.Instance?.Attack(knellSound, transform.position + Vector3.up * 1.2f, 1f, "keener_knell");
            }
            var ring = Shockwave.Spread(transform.position, target, ringRadius, ringSpeed, ringWidth, ringDamage, ringKnockback, ringTint);
            ring.muffled = true;
            ring.band = band;
            LastRing = ring;
            Rings++;
            Rang?.Invoke(this, ring);
        }

        public override void Reform()
        {
            base.Reform();
            nextKnell = 0f;
            LastRing = null;
        }
    }
}
