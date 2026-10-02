using UnityEngine;

namespace MyGame
{
    // A Gleaner (the Orchard): a farmhand who went on gleaning after the harvest failed, walking the rows at night with
    // a lantern on a long pole. It keeps its distance: come near and it backs off; close in and it skips lightly away
    // out of the oar's reach (never while it's winding up: that's the moment to catch it). Its fire: the pole drawn
    // back over its head, the lantern flaring (the tell), then one ball of flame flung at you, slower than the
    // Watchman's. Turned back with a perfect block, its own fire burns it up at once. The Hook brings it close; pitch
    // and tar burn it well.
    public sealed class Gleaner : Soured
    {
        [Header("The Gleaner")]
        public float keepAway = 6f;          // nearer than this, it backs off
        public float comeWithin = 10f;       // further than this, it comes closer
        public float backOffSpeed = 2.2f;
        public float hopNear = 3.2f;         // nearer than this (or swinging at it a little further), it skips away
        public float hopSpeed = 22f, hopTime = 0.4f, hopCooldown = 1.4f;
        public float fireCooldown = 2.8f, fireSpeed = 7f, fireDamage = 16f, fireReach = 13f;
        public Light lantern;                // its pole's lantern: where its fire comes from (it flares with the tell)
        public float lanternIntensity = 2.8f;
        public Material fireMaterial;
        public AudioClip[] hopSound, gutterSound;

        public static event System.Action<Gleaner, Fireball> Flung; // (tests)
        public static event System.Action<Gleaner> Hopped;

        float nextHop, nextFire;

        protected override string ChaseClip(float distance) => distance < keepAway || distance > comeWithin ? "Walk" : "Idle";

        Vector3 LanternPoint => lantern != null ? lantern.transform.position : transform.position + Vector3.up * 2f;

        // Between keepAway and comeWithin it stands its ground and throws; nearer, it backs off (facing you); further,
        // it comes.
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            if (distance < keepAway)
                return -toTarget.normalized * backOffSpeed;
            if (distance > comeWithin)
                return PathDirection(target.transform.position) * chaseSpeed;
            return Vector3.zero;
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Time.time < nextFire || distance > fireReach || distance < 2f)
                return null;
            nextFire = Time.time + fireCooldown;
            return primary;
        }

        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            Vector3 from = LanternPoint;
            Vector3 aim = target != null ? (target.transform.position + Vector3.up * 0.7f - from).normalized : direction;
            var ball = Fireball.Fire(from, aim, fireSpeed, 0.3f, target, this, fireMaterial);
            ball.damage = fireDamage;
            ball.gutterSound = gutterSound;
            Flung?.Invoke(this, ball);
        }

        // Its own fire, turned back: it goes up at once.
        public override void StruckByOwnFire(Vector3 from)
        {
            for (int i = 0; i < 40; i++)
                Effects.Instance?.Flame(transform.position + Vector3.up * 1.0f + Random.insideUnitSphere * 0.5f, Random.insideUnitSphere * 2f + Vector3.up * 2.2f,
                    Random.Range(0.35f, 0.7f), Random.Range(0.18f, 0.32f));
            Slay(from);
        }

        protected override void Tick(float dt)
        {
            if (!IsAlive || target == null)
                return;
            // The lantern: flaring as it draws back to throw.
            if (lantern != null)
            {
                bool drawing = CurrentState == State.Telegraph;
                float k = drawing ? 1f + 1.6f * Mathf.Clamp01(StateTime / Mathf.Max(0.01f, primary.telegraph)) : 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 6f, 0.3f);
                lantern.intensity = lanternIntensity * k;
                if (drawing && Random.value < 20f * Time.deltaTime)
                    Effects.Instance?.Flame(LanternPoint + Random.insideUnitSphere * 0.06f, Vector3.up * Random.Range(0.5f, 1.2f), Random.Range(0.2f, 0.3f),
                        Mathf.Lerp(0.07f, 0.16f, StateTime / Mathf.Max(0.01f, primary.telegraph)));
            }
            // Too near (or swung at): it skips away. Never mid-throw: winding up, it's committed.
            if (CurrentState != State.Chase || Time.time < nextHop || !target.IsAlive)
                return;
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            bool swungAt = target.CurrentState == Apprentice.State.Attacking && to.magnitude < hopNear + 0.8f;
            if (to.magnitude < hopNear || swungAt)
                Hop(-to.normalized);
        }

        // A light skip away: back from you, or off to one side if there's no ground (or a wall) behind it.
        void Hop(Vector3 away)
        {
            float reach = hopSpeed / 8f; // (how far a shove that size carries it)
            foreach (float angle in new[] { 0f, 35f, -35f, 70f, -70f, 100f, -100f })
            {
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * away;
                Vector3 land = transform.position + dir * reach;
                if (!FootingAt(land) || Physics.Raycast(transform.position + Vector3.up * 0.8f, dir, reach + radius, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                nextHop = Time.time + hopCooldown;
                BeBusy("Hop", hopTime);
                Shove(dir * hopSpeed);
                Sfx.Instance?.Attack(hopSound, transform.position + Vector3.up, 0.7f, "gleaner_hop");
                Hopped?.Invoke(this);
                return;
            }
            nextHop = Time.time + 0.5f; // (cornered: nowhere to go; it stays, for now)
        }
    }
}
