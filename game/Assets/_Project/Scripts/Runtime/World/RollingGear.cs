using UnityEngine;

namespace MyGame
{
    // The Gear Run's gear (a trap): a great gear as broad as the run, let go from its head to roll down it faster and
    // faster, crash into the buffers at its foot, be drawn back up by its chain, held a while at the head (a clunk as
    // the pawl lifts: it's coming), and let go again; for as long as its Clockwork runs (the Hall's time going: for
    // good; the Stroke, at the gear itself: a while). Stopped, it stops wherever it is: the clock stopped as it rolled.
    // Rolling, it flattens what's in the lane (step into an alcove); held or drawn up, it's solid. The run's floor is
    // flat at its foot (lowU), rising evenly from lowN to highN, flat again at its head (highU); it runs south, down.
    public sealed class RollingGear : MonoBehaviour
    {
        public enum Phase { Held, Rolling, Crashed, Drawn }

        public Clockwork clockwork;
        public Transform strikePoint;          // the Stroke's aimed at the gear (kept on the floor under it)
        public float radius = 1.45f;           // over its teeth: what it rolls on
        public float width = 1.7f;             // across its face
        public float laneE;                    // the lane's middle (e)
        public float lowN, highN, lowU, highU; // the run's floor
        public float headN, footN;             // its middle where it's held, and where the buffers stop it
        public float gravity = 6.5f;           // down a slope: g sin θ (a rolling wheel's two thirds of it)
        public float holdTime = 2.6f, crashTime = 1.2f, drawSpeed = 1.8f;
        public float tell = 0.7f;              // the pawl's clunk this long before it's let go
        public Phase startPhase = Phase.Rolling;
        public float startN, startSpeed = 4f;  // where the clock stopped it, and how fast it was going
        public float harmfulSpeed = 1.5f, knock = 28f;
        public AudioClip[] releaseSound, rollSound, crashSound, drawSound;

        public Phase Current { get; private set; }
        public float N { get; private set; }       // its middle's n
        public float Speed { get; private set; }   // m/s along the run (rolling down)
        public int Crashes { get; private set; }   // (tests)
        public int Releases { get; private set; }  // (tests)
        public TrapBlows Blows { get; } = new TrapBlows { harm = 40f, souredHarm = 60f, every = 1.2f };

        float phaseTime, turned;
        bool told;
        Quaternion rest;
        Collider[] solids;
        AudioSource rolling, drawing;

        void Start()
        {
            rest = transform.rotation;
            solids = GetComponentsInChildren<Collider>();
            Current = startPhase;
            N = startN;
            Speed = Current == Phase.Rolling ? startSpeed : 0f;
            rolling = TrapBlows.LoopSource(gameObject, 30f);
            drawing = TrapBlows.LoopSource(gameObject, 22f);
            Place();
        }

        // The run's floor at n.
        public float Ground(float n)
        {
            if (n <= lowN)
                return lowU;
            if (n >= highN)
                return highU;
            return Mathf.Lerp(lowU, highU, (n - lowN) / (highN - lowN));
        }

        // Its middle's height at n: resting on the floor (over the bends, on their edges).
        public float Centre(float n)
        {
            float best = float.MinValue;
            for (int i = 0; i <= 16; i++)
            {
                float x = n - radius + 2f * radius * i / 16f;
                float dx = x - n;
                best = Mathf.Max(best, Ground(x) + Mathf.Sqrt(Mathf.Max(0f, radius * radius - dx * dx)));
            }
            return best;
        }

        public Vector3 Middle => new Vector3(laneE, Centre(N), N);

        void Update()
        {
            float run = clockwork != null ? UiKit.Smooth(0f, 1f, clockwork.Run) : 1f;
            float dt = Time.deltaTime * run; // (time slowing to a stop with its clockwork)
            if (dt > 0f)
                Step(dt);
            Place();
            bool flattening = Current == Phase.Rolling && Speed * run >= harmfulSpeed;
            foreach (var c in solids) // (rolling, it passes through what it flattens)
                if (c != null)
                    c.enabled = !flattening;
            TrapBlows.Keep(rolling, rollSound, Current == Phase.Rolling ? Mathf.Clamp01(Speed / 7f) * run : 0f, 0.7f + 0.5f * Mathf.Clamp01(Speed / 7f));
            TrapBlows.Keep(drawing, drawSound, Current == Phase.Drawn ? 0.55f * run : 0f);
            if (flattening)
            {
                Vector3 m = Middle;
                Blows.Sweep((feet, r) => Mathf.Abs(feet.x - laneE) <= width * 0.5f + r
                                         && Mathf.Abs(feet.z - m.z) <= radius * 0.8f + r
                                         && feet.y <= m.y + radius && feet.y + TrapBlows.Height >= m.y - radius,
                            feet => Vector3.back * knock + Vector3.right * Mathf.Sign(feet.x - laneE + 0.001f) * 4f);
            }
        }

        void Step(float dt)
        {
            phaseTime += dt;
            float slope = (Centre(N + 0.05f) - Centre(N - 0.05f)) / 0.1f; // (rise per metre north)
            float along = Mathf.Sqrt(1f + slope * slope);                    // (path per metre of n)
            switch (Current)
            {
                case Phase.Held:
                    if (!told && phaseTime >= holdTime - tell)
                    {
                        told = true;
                        Sfx.Instance?.Attack(releaseSound, Middle + Vector3.up * radius, 1f, "trap_gear_release");
                    }
                    if (phaseTime >= holdTime)
                    {
                        Enter(Phase.Rolling);
                        Releases++;
                    }
                    break;
                case Phase.Rolling:
                    Speed = Mathf.Max(0.3f, Speed + (gravity * slope / along - 0.25f) * dt);
                    float dn = Speed * dt / along;
                    N -= dn;
                    turned += dn * along / radius;
                    if (N <= footN)
                    {
                        N = footN;
                        Crash();
                    }
                    break;
                case Phase.Crashed:
                    if (phaseTime >= crashTime)
                        Enter(Phase.Drawn);
                    break;
                case Phase.Drawn:
                    float up = drawSpeed * dt / along;
                    N = Mathf.Min(headN, N + up);
                    turned -= up * along / radius;
                    if (N >= headN)
                        Enter(Phase.Held);
                    break;
            }
        }

        void Enter(Phase phase)
        {
            Current = phase;
            phaseTime = 0f;
            told = false;
            if (phase != Phase.Rolling)
                Speed = 0f;
        }

        // Into the buffers: a crash that shakes the run.
        void Crash()
        {
            Crashes++;
            float hard = Mathf.Clamp01(Speed / 7f);
            Enter(Phase.Crashed);
            Vector3 front = Middle + Vector3.back * radius;
            Sfx.Instance?.Attack(crashSound, front, 0.6f + 0.4f * hard, "trap_gear_crash");
            Effects.Instance?.Rubble(front + Vector3.down * radius * 0.5f, 1.2f);
            var apprentice = FindAnyObjectByType<Apprentice>();
            if (apprentice != null && (apprentice.transform.position - front).sqrMagnitude < 14f * 14f)
                CameraRig.Instance?.Shake(0.25f * hard);
        }

        void Place()
        {
            transform.SetPositionAndRotation(Middle, Quaternion.AngleAxis(-turned * Mathf.Rad2Deg, Vector3.right) * rest);
            if (strikePoint != null)
                strikePoint.position = new Vector3(laneE, Ground(N), N);
        }
    }
}
