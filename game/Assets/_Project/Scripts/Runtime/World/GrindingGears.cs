using UnityEngine;

namespace MyGame
{
    // The undercroft's floor gears (a trap): great brass gears lying in the floor, meshed, turning while their clockwork
    // runs (the keep's: struck at the pendulum's bob). Whoever stands on one is carried round with it, the Apprentice and
    // the Soured alike; carried into the mesh, where the teeth run in together, they're crushed and thrown out. Sparks
    // fly there while it turns. Stopped, they're only floor.
    public sealed class GrindingGears : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Gear
        {
            public Transform part;
            public float radius = 2f;   // where it carries you (to its teeth)
            public float speed = 40f;   // degrees a second about up, running (+: clockwise from above)
        }

        public Clockwork clockwork;
        public Gear[] gears = new Gear[0];
        public Vector3 mesh;            // where their teeth meet (world, on the floor)
        public Vector3 inrunning;       // from the mesh, the side the teeth run in on (world, flat)
        public float nip = 0.9f;        // this near the mesh, on that side, is crushed
        public float knock = 22f;
        public AudioClip[] grindSound;

        public TrapBlows Blows { get; } = new TrapBlows();
        public int Carried { get; private set; }  // (tests) frames the Apprentice was carried

        Apprentice apprentice;
        AudioSource grinding;
        float nextSpark;

        void Start() => grinding = TrapBlows.LoopSource(gameObject, 22f);

        void Update()
        {
            float run = clockwork != null ? UiKit.Smooth(0f, 1f, clockwork.Run) : 1f;
            TrapBlows.Keep(grinding, grindSound, 0.7f * run, 0.8f + 0.2f * run);
            if (run <= 0f)
                return;
            float dt = Time.deltaTime;
            if (apprentice == null)
                apprentice = FindAnyObjectByType<Apprentice>();
            foreach (var g in gears)
            {
                if (g.part == null)
                    continue;
                float turn = g.speed * run * dt;
                g.part.Rotate(Vector3.up, turn, Space.World);
                Vector3 centre = g.part.position;
                if (apprentice != null && apprentice.IsAlive && On(apprentice.transform.position, centre, g.radius))
                {
                    apprentice.Carry(Round(apprentice.transform.position, centre, turn));
                    Carried++;
                }
                foreach (var s in WorldState.AllSoured)
                    if (s != null && s.IsAlive && On(s.transform.position, centre, g.radius))
                        s.Carry(Round(s.transform.position, centre, turn));
            }
            if (run < 0.3f)
                return;
            if (Time.time >= nextSpark)
            {
                nextSpark = Time.time + Random.Range(0.08f, 0.2f);
                Effects.Instance?.BlockSpark(mesh + Vector3.up * 0.08f + inrunning.normalized * 0.1f, (Vector3.up + inrunning.normalized * 0.6f).normalized);
            }
            Vector3 into = new Vector3(inrunning.x, 0f, inrunning.z).normalized;
            Blows.Sweep((feet, r) =>
            {
                Vector3 d = feet - mesh;
                float up = d.y;
                d.y = 0f;
                return d.magnitude <= nip + r && Vector3.Dot(d, into) > -0.2f && up > -0.4f && up < 0.6f;
            }, feet => into * knock);
        }

        // Standing on it (its floor, within its teeth).
        static bool On(Vector3 feet, Vector3 centre, float radius)
        {
            Vector3 d = feet - centre;
            if (Mathf.Abs(d.y) > 0.35f)
                return false;
            d.y = 0f;
            return d.sqrMagnitude <= radius * radius;
        }

        // How far a point on it moves as it turns `degrees` about its centre.
        static Vector3 Round(Vector3 at, Vector3 centre, float degrees)
        {
            Vector3 d = at - centre;
            d.y = 0f;
            return Quaternion.AngleAxis(degrees, Vector3.up) * d - d;
        }
    }
}
