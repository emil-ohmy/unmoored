using UnityEngine;

namespace MyGame
{
    // Something loose the Stroke's wave shoves: a barrel or a crate. On the ground it slides and soon stops (scraping);
    // off an edge it drops into the water and floats there, bobbing, drifting a long way before it slows. Walls (and
    // whoever's in the way) stop it, and it glances off.
    public sealed class Shovable : MonoBehaviour, IWaveable
    {
        public float radius = 0.4f;
        public float draft = 0.45f;       // afloat: how deep it sits (from its base)
        public float heaviness = 1f;      // the shove divided by this
        public float waterLevel = 0f;     // the water's surface
        public AudioClip[] scrapeSound;

        public Vector3 Velocity => velocity;
        public bool Afloat { get; private set; }

        Vector3 velocity;
        float spin, phase, groundY;
        Collider[] own;
        static readonly RaycastHit[] hits = new RaycastHit[8];

        void Awake()
        {
            own = GetComponentsInChildren<Collider>();
            phase = Random.value * 10f;
        }

        void OnEnable() => Waveables.Register(this);
        void OnDisable() => Waveables.Unregister(this);

        public Vector3 WavePoint => transform.position + Vector3.up * 0.3f;
        public float WaveRadius => radius;

        public void TakeWave(StrokeWave wave, Vector3 push)
        {
            velocity += new Vector3(push.x, 0f, push.z) * ((Afloat ? 0.5f : 0.32f) / heaviness);
            spin += Random.Range(-120f, 120f);
            if (!Afloat)
                Sfx.Instance?.Attack(scrapeSound, transform.position, 0.7f, "shove_scrape");
        }

        void Start() => Settle(true);

        void Update()
        {
            float dt = Time.deltaTime;
            if (velocity.sqrMagnitude > 0.0004f)
                Move(velocity * dt);
            velocity *= Mathf.Exp(-(Afloat ? 0.9f : 5f) * dt); // (the water carries it on; the ground stops it)
            spin *= Mathf.Exp(-(Afloat ? 0.7f : 6f) * dt);
            transform.Rotate(0f, spin * dt, 0f, Space.World);
            Settle(false);
        }

        // Moves it, unless something stands in the way: then it glances off, slowed.
        void Move(Vector3 step)
        {
            Vector3 centre = transform.position + Vector3.up * (radius + 0.05f);
            int n = Physics.SphereCastNonAlloc(centre, radius * 0.9f, step.normalized, hits, step.magnitude + 0.02f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (IsOwn(hits[i].collider) || hits[i].distance <= 0f)
                    continue;
                Vector3 normal = new Vector3(hits[i].normal.x, 0f, hits[i].normal.z).normalized;
                if (normal.sqrMagnitude < 0.01f)
                    continue; // (a floor, not a wall)
                velocity = Vector3.Reflect(velocity, normal) * 0.3f;
                return;
            }
            transform.position += step;
        }

        // On the ground under it (dropping to it), or afloat on the water (bobbing), whichever is here.
        void Settle(bool snap)
        {
            Vector3 p = transform.position;
            float ground = float.NegativeInfinity;
            int n = Physics.RaycastNonAlloc(p + Vector3.up * 1.2f, Vector3.down, hits, 3f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!IsOwn(hits[i].collider) && !(hits[i].collider is CharacterController) && hits[i].point.y > waterLevel - draft + 0.02f)
                    ground = Mathf.Max(ground, hits[i].point.y);
            bool wasAfloat = Afloat;
            Afloat = float.IsNegativeInfinity(ground);
            float y = Afloat ? waterLevel - draft + 0.04f * Mathf.Sin(Time.time * 1.6f + phase) : ground;
            if (Afloat && !wasAfloat && !snap)
            {
                Effects.Instance?.Splash(new Vector3(p.x, waterLevel, p.z));
                Sfx.Instance?.Splash(new Vector3(p.x, waterLevel, p.z));
            }
            groundY = snap || y > p.y ? y : Mathf.MoveTowards(p.y, y, 6f * Time.deltaTime); // (it drops, it doesn't jump)
            transform.position = new Vector3(p.x, groundY, p.z);
        }

        bool IsOwn(Collider c)
        {
            foreach (var o in own)
                if (o == c)
                    return true;
            return false;
        }
    }
}
