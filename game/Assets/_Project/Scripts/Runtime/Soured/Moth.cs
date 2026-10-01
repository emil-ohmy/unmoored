using UnityEngine;

namespace MyGame
{
    // A Moth: a Soured that couldn't let go of a memory. Pale and dusty, half a metre across, in a flock (MothFlock)
    // that circles a light, and comes to the Apprentice's. One at a time, the flock sends one in: it stalls in the
    // air, wings flung wide, its eyes flaring (the tell), then dives straight at them and through. Any blow scatters
    // one; the heavy attack sweeps through several; fire takes them like paper, and one alight sets the next alight.
    // A perfect block on a dive scatters it. When one goes, a memory is let go: a small note, as from a music box.
    public sealed class Moth : Soured
    {
        [Header("The Moth")]
        public float diveSpeed = 9f;
        public float contactRadius = 0.6f;   // how near its dive passes to strike
        public float diveDamage = 6f;
        public float flySpeed = 4.5f;        // to its place in the flock
        public AudioClip[] diveSound, releaseSound;

        public MothFlock Flock { get; set; }
        public bool Diving => CurrentState == State.Strike;
        public static event System.Action<Moth> Dived; // (tests)

        Vector3 diveDir;
        bool struck, pinned;

        // (tests) Held where it is put: no flying about, no diving.
        public void Pin(Vector3 at)
        {
            pinned = true;
            PlaceAt(at);
        }
        float nextSpread;

        public override bool Flies => true;
        protected override bool Floats => true;
        protected override bool Surface() => true;
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (it flies: Tick)
        protected override string IdleClip => "Fly";
        protected override string ChaseClip(float distance) => "Fly";
        // (it comes at you from above: anywhere it can reach is its level)
        protected override bool OnItsLevel() => target != null && Mathf.Abs(target.transform.position.y - transform.position.y) < 3.5f;

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance) =>
            !pinned && Flock != null && distance < 8f && Flock.MayDive(this) ? primary : null;

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph)
                Effects.Instance?.AlertFlare(transform.position + Vector3.up * 0.05f); // (the stall: its eyes flare)
            if (state == State.Strike && target != null)
            {
                // Straight at where they are now (their chest), and on through: a sidestep or a dash slips it.
                diveDir = (target.transform.position + Vector3.up * 0.6f - transform.position).normalized;
                struck = false;
                Sfx.Instance?.Attack(diveSound, transform.position, 0.8f, "moth_dive");
                Dived?.Invoke(this);
            }
            if (state == State.Dying)
                Sfx.Instance?.Attack(releaseSound, transform.position, 0.7f, "moth_release"); // (a memory let go)
        }

        // Caught on the Hook: too slight to pull; it's simply crushed.
        public override HookResult OnHooked(Apprentice by)
        {
            Slay(by.transform.position);
            return HookResult.Shrugged;
        }

        protected override void Tick(float dt)
        {
            if (!IsAlive)
                return;
            if (pinned)
                PlaceAt(transform.position);
            else if (CurrentState == State.Strike)
                Dive(dt);
            else if (CurrentState == State.Telegraph || CurrentState == State.Staggered || CurrentState == State.Stunned)
                Hover(dt); // (the stall: held in the air)
            else
                FlyTo(Flock != null ? Flock.PlaceFor(this) : SpawnPosition, dt);
            // Alight, it sets the ones near it alight: a flock goes up together.
            var burning = GetComponent<Burning>();
            if (burning != null && burning.Alight && Flock != null && Time.time >= nextSpread)
            {
                nextSpread = Time.time + 0.25f;
                foreach (var other in Flock.Moths)
                    if (other != null && other != this && other.IsAlive && Vector3.Distance(other.transform.position, transform.position) < 3.2f) // (the next in the ring)
                        Burning.Set(other, 1.5f, 6f);
            }
        }

        void FlyTo(Vector3 place, float dt)
        {
            Vector3 to = place - transform.position;
            Vector3 step = Vector3.ClampMagnitude(to, flySpeed * dt);
            if (!Sheltered(transform.position) && Sheltered(transform.position + step))
                return; // (a Boss's arena: never in)
            PlaceAt(transform.position + step);
            Vector3 flat = new Vector3(step.x, 0f, step.z);
            if (flat.sqrMagnitude > 1e-5f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 540f * dt);
        }

        void Hover(float dt)
        {
            PlaceAt(transform.position + Vector3.up * (Mathf.Sin(Time.time * 9f) * 0.05f * dt));
            if (target != null)
            {
                Vector3 look = target.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 720f * dt);
            }
        }

        // Its dive: straight on through, striking if it passes close; never through the ground.
        void Dive(float dt)
        {
            Vector3 next = transform.position + diveDir * diveSpeed * dt;
            if (!Sheltered(transform.position) && Sheltered(next))
            {
                diveDir = new Vector3(-diveDir.x, 0.8f, -diveDir.z).normalized; // (it sheers off at the arena's edge)
                return;
            }
            if (next.y < (target != null ? target.transform.position.y + 0.3f : 0.3f))
                diveDir = new Vector3(diveDir.x, Mathf.Max(diveDir.y, 0.4f), diveDir.z).normalized; // (pulling up off the ground)
            PlaceAt(next);
            if (struck || target == null || !target.IsAlive)
                return;
            if (Vector3.Distance(transform.position, target.transform.position + Vector3.up * 0.6f) > contactRadius)
                return;
            struck = true;
            var defence = HitTarget(diveDamage, new Vector3(diveDir.x, 0f, diveDir.z).normalized * 2f);
            if (defence == Defence.Parried)
                Slay(target.transform.position); // (turned aside: it breaks against the oar)
            else if (defence != Defence.Dodged)
                diveDir = new Vector3(-diveDir.x, 0.8f, -diveDir.z).normalized; // (it glances off, back up)
        }
    }
}
