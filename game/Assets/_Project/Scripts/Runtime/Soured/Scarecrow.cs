using UnityEngine;

namespace MyGame
{
    // A scarecrow in the Unreaped Rows: straw in an old coat on a stake, a sack for a face, keeping watch over nothing.
    // One among them isn't one (the Watchman). Struck, burnt or hooked, it bursts into flames, burning whoever's
    // near; it's gone until the Apprentice Rests. While `Follows`, it turns on its stake to watch them (once he's
    // been found and lost again, they all do).
    public sealed class Scarecrow : Soured
    {
        [Header("The scarecrow")]
        public Transform pole;          // its stake (left standing where it burst)
        public float burstRadius = 2.4f, burstDamage = 18f;
        public float catchTime = 0.4f;  // struck, it catches (a crackle, flames licking up it), then goes up: dodge clear
        public float followTurn = 50f;  // degrees a second, turning to watch them
        public AudioClip[] burstSound;
        public static event System.Action<Scarecrow> Burst; // (tests)

        public bool Follows { get; set; }
        public override bool Lockable => false; // (so many alike: lock-on would only get in the way)
        bool burst;

        protected override void Awake()
        {
            base.Awake();
            passive = true;
        }

        // Caught on the Hook: it comes apart.
        public override HookResult OnHooked(Apprentice by)
        {
            Slay(by.transform.position);
            return HookResult.Shrugged;
        }

        protected override void Tick(float dt)
        {
            if (IsAlive && Follows)
                Watchman.TurnToWatch(transform, target, followTurn * dt);
            if (CurrentState != State.Dying || burst)
                return;
            if (StateTime < catchTime)
            {
                for (int i = 0; i < 2; i++) // (catching: flames licking up it, faster and faster)
                    if (Random.value < 0.4f + StateTime / catchTime)
                        Effects.Instance?.Flame(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.7f), Random.Range(-0.3f, 0.3f)),
                            Vector3.up * Random.Range(1.2f, 2f), Random.Range(0.3f, 0.45f), Random.Range(0.16f, 0.26f));
                return;
            }
            GoUp();
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Dying)
            {
                burst = false;
                Sfx.Instance?.TarSizzle(transform.position + Vector3.up); // (it catches)
            }
        }

        // It goes up all at once: a gout of flame, and whoever's near is burnt.
        void GoUp()
        {
            burst = true;
            Vector3 at = transform.position + Vector3.up * 1.1f;
            Effects.Instance?.Straw(at, 50);
            for (int i = 0; i < 60; i++)
                Effects.Instance?.Flame(at + Random.insideUnitSphere * 0.6f, Random.insideUnitSphere * 2.5f + Vector3.up * 2.5f, Random.Range(0.35f, 0.7f), Random.Range(0.2f, 0.4f));
            Sfx.Instance?.Attack(burstSound, at, 1f, "scarecrow_burst");
            CameraRig.Instance?.Shake(0.2f);
            if (target != null && target.IsAlive)
            {
                Vector3 away = target.transform.position - transform.position;
                away.y = 0f;
                if (away.magnitude < burstRadius && target.TakeHit(burstDamage, away.normalized * 5f, this, blockable: false) == Defence.Taken) // (fire: no guard stops it)
                    target.SetAlight();
            }
            Burst?.Invoke(this);
        }

        public override void Reform()
        {
            base.Reform();
            Follows = false;
            StandPole(pole, transform);
        }

        // Moved, stake and all (in the dark, while the Watchman hides among them again).
        public void Stand(Vector3 at, Quaternion rotation)
        {
            PlaceAt(at);
            transform.rotation = rotation;
            StandPole(pole, transform);
        }

        // A stake under a scarecrow (or the Watchman, while he's one of them).
        public static void StandPole(Transform pole, Transform figure)
        {
            if (pole != null)
                pole.position = new Vector3(figure.position.x, pole.position.y, figure.position.z);
        }
    }
}
