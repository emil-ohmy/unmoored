using UnityEngine;

namespace MyGame
{
    // One of the Kept (the Hold, the tutorial; docs/brief.md): what the cave's living walls let go of: a small hunched figure,
    // black and soft as wet tar, two pale eyes, long thin arms. It waits in the stone at its place in a wall (unseen,
    // untouchable: carried, its collider off) until the Apprentice passes near; then it tears free (Emerge: drawn out of
    // the wall onto the floor) and shuffles after them, with the others of its group, a moment apart. Frail; one blow:
    // a clawing swipe, the long arm raised slowly first (the tell). Beaten, it sinks back into the stone, and a face
    // presses out of the wall nearby a moment after. At a Rest it's back in the wall, waiting.
    public sealed class Kept : Soured
    {
        [Header("The Kept")]
        public Vector3 inWall;              // where it waits, in the stone (its back to the wall's surface)
        public float emergeTime = 1.6f;
        public float wakeWithin = 4.5f;     // the Apprentice this near its place: it tears free
        public int group;                   // The Kept of a group come out together (each a moment after the last)
        public float groupDelay = 0.45f;
        public float stepEvery = 0.45f;     // (its shuffling, heard)
        public AudioClip[] emergeSound, stepSound, hurtSound, deathSound;

        public bool Emerged { get; private set; }
        public bool Emerging => emergeAt >= 0f && Time.time - emergeAt < emergeTime;
        public static event System.Action<Kept> TornFree; // (tests)

        public override bool Targetable => base.Targetable && Emerged && !Emerging;

        float emergeAt = -1f, nextStep;
        Vector3 outOfWall;
        Renderer[] bodies;

        protected override void Start()
        {
            base.Start();
            outOfWall = transform.position;
            bodies = GetComponentsInChildren<Renderer>(true);
            Hide();
        }

        // Back in the stone, waiting: unseen, untouchable, not noticing anyone.
        void Hide()
        {
            Emerged = false;
            emergeAt = -1f;
            passive = true;
            SetCarried(true);
            transform.position = inWall;
            Show(false);
        }

        void Show(bool on)
        {
            if (bodies == null)
                return;
            foreach (var r in bodies)
                if (r != null)
                    r.enabled = on;
        }

        // Torn free (now, or after `delay`: one of its group).
        public void Emerge(float delay = 0f)
        {
            if (Emerged || !IsAlive)
                return;
            Emerged = true;
            emergeAt = Time.time + delay;
        }

        protected override void Tick(float dt)
        {
            if (!Emerged)
            {
                if (target != null && target.IsAlive && Flat(target.transform.position - outOfWall).magnitude < wakeWithin
                    && Mathf.Abs(target.transform.position.y - outOfWall.y) < 1.5f)
                {
                    // It comes out, and the others of its group with it, a moment apart.
                    int order = 0;
                    foreach (var s in FindObjectsByType<Kept>(FindObjectsSortMode.None))
                        if (s.group == group && !s.Emerged && s.IsAlive)
                            s.Emerge(order++ * groupDelay);
                }
                return;
            }
            if (emergeAt >= 0f && Time.time >= emergeAt && Carried)
            {
                float u = (Time.time - emergeAt) / emergeTime;
                if (u < 0.02f && !shown)
                {
                    shown = true;
                    Show(true);
                    PlayThrough("Emerge", emergeTime); // (held to its end: the idle loop doesn't take over)
                    Sfx.Instance?.Attack(emergeSound, inWall + Vector3.up, 0.9f, "kept_emerge");
                    TornFree?.Invoke(this);
                }
                // Drawn out of the stone onto the floor, evenly over the clip (its hands and feet are planted for that), facing out.
                transform.position = Vector3.Lerp(inWall, outOfWall, Mathf.Clamp01(u));
                Vector3 facing = Flat(outOfWall - inWall);
                if (facing.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(facing.normalized);
                if (u >= 1f)
                {
                    SetCarried(false);
                    passive = false;
                    Alert(); // (it knows you're there: it came out for you)
                }
                return;
            }
            // Its shuffle, heard.
            if (CurrentState == State.Chase && Time.time >= nextStep)
            {
                nextStep = Time.time + stepEvery * Random.Range(0.8f, 1.3f);
                Sfx.Instance?.Attack(stepSound, transform.position, 0.45f, "kept_step");
            }
        }

        bool shown;

        protected override void OnEntered(State state)
        {
            if (state == State.Staggered || state == State.Stunned)
                Sfx.Instance?.Attack(hurtSound, transform.position + Vector3.up * 0.8f, 0.8f, "kept_hurt");
            if (state == State.Dying)
            {
                Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 0.5f, 0.9f, "kept_death");
                StartCoroutine(TakenBack());
            }
        }

        // Taken back into the stone: a face presses out of the wall nearest it, a moment after.
        System.Collections.IEnumerator TakenBack()
        {
            yield return new WaitForSeconds(deathDuration * 0.8f);
            WallPresence nearest = null;
            float best = 12f;
            foreach (var p in FindObjectsByType<WallPresence>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = p;
                }
            }
            nearest?.PressNow();
        }

        public override void Reform()
        {
            base.Reform();
            shown = false;
            Hide();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
