using UnityEngine;

namespace MyGame
{
    // A Keener: couldn't let go of grief. One of the mourners who stopped the Hall's clocks at the minute of a death, in
    // black crape, the stopped watch in its hand. It keeps its distance (come near and it drifts back; it never skips
    // away like a Gleaner: it's slow, and fragile), and from there it keens: it draws breath (rising, the watch lifted
    // high: the tell), then tolls a knell, three rings of sound rolling out across the floor half a second apart. Each
    // ring knocks you back where its crest passes you, unless you dash through it (or are beyond it, or above it: the
    // Vault leaps them and comes down on it); a wall between muffles it. Dashing in through the rings is the answer: it
    // has little health, and two blows stagger it, cutting its knell short.
    public sealed class Keener : Soured
    {
        [Header("The Keener")]
        public float keepAway = 5.5f;        // nearer than this, it drifts back
        public float comeWithin = 10f;       // further than this, it comes closer
        public float backOffSpeed = 1.3f;
        public float knellReach = 10f;       // it keens at you from within this
        public float knellEvery = 5.5f;      // from one knell's start to the next's (the breath, the toll and the spent pause in it)
        public float firstKnellDelay = 0.6f; // after it notices you

        [Header("The rings")]
        public float ringRadius = 9f;
        public float ringSpeed = 5.5f;
        public float ringWidth = 0.8f;
        public float ringDamage = 12f;
        public float ringKnockback = 5f;
        public Color ringTint = new Color(0.76f, 0.78f, 0.9f, 0.8f);

        [Header("Sounds")]
        public AudioClip[] deathSound; // a sigh letting go, and its watch going again

        public static event System.Action<Keener, Shockwave> Rang; // (tests)
        public int Rings { get; private set; }                      // (tests) rings sent out
        public Shockwave LastRing { get; private set; }

        float nextKnell;

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
            if (Time.time < nextKnell || distance > knellReach || !OnItsLevel())
                return null;
            nextKnell = Time.time + knellEvery;
            return primary;
        }

        // Each ring of the knell, from where it stands.
        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            var ring = Shockwave.Spread(transform.position, target, ringRadius, ringSpeed, ringWidth, ringDamage, ringKnockback, ringTint);
            ring.muffled = true;
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
