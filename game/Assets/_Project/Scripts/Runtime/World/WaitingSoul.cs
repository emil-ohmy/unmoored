using UnityEngine;

namespace MyGame
{
    // One of the dead waiting in the Hub's queue to cross (soul.py): patient, out of step with the rest. It idles (its
    // breath, a sway), now and then shifts its weight or looks down at what it carries; its head turns to watch the
    // Ferryman's apprentice go by (only so far round, and only ahead of it), and as you pass close some bow a little.
    public sealed class WaitingSoul : MonoBehaviour
    {
        public Transform head;
        public float lookWithin = 4f, lookLimit = 70f, lookSpeed = 2.5f;
        public float bowWithin = 2f;
        [Range(0f, 1f)] public float bows = 0.5f;      // how likely it bows as you pass
        public Vector2 shiftEvery = new Vector2(7f, 18f);

        public bool Looking => look > 0.5f;            // (tests)
        public int Bowed { get; private set; }         // (tests)
        public float HeadTurn { get; private set; }    // degrees it's turned its head now (tests)

        Animation anim;
        ClipPlayer clips;
        Apprentice apprentice;
        float look, nextShift, busyUntil;
        bool passedClose;

        void Start()
        {
            anim = GetComponentInChildren<Animation>();
            apprentice = FindAnyObjectByType<Apprentice>();
            if (anim == null)
                return;
            clips = new ClipPlayer(anim);
            clips.Loop("Idle", 0f);
            clips.Desync();
            clips.SetLoopSpeed(Random.Range(0.85f, 1.1f));
            nextShift = Time.time + Random.Range(shiftEvery.x * 0.3f, shiftEvery.y);
        }

        void Update()
        {
            if (clips == null)
                return;
            if (Time.time >= busyUntil && clips.Current != "Idle")
                clips.Loop("Idle", 0.35f);
            if (Time.time >= nextShift && Time.time >= busyUntil)
            {
                Play("Shift", 2.5f);
                nextShift = Time.time + Random.Range(shiftEvery.x, shiftEvery.y);
            }
            if (apprentice == null)
                return;
            float near = Flat(apprentice.transform.position - transform.position).magnitude;
            if (near < bowWithin && !passedClose)
            {
                passedClose = true;
                if (Random.value < bows && Time.time >= busyUntil)
                {
                    Play("Bow", 1.6f);
                    Bowed++;
                }
            }
            else if (near > bowWithin + 4f)
                passedClose = false;
        }

        void Play(string clip, float length)
        {
            if (!clips.Has(clip))
                return;
            clips.PlayOnce(clip, length, 0.3f);
            busyUntil = Time.time + length;
        }

        // After the clip has posed it: the head turned (about the world's up) towards the Apprentice, eased in and out.
        void LateUpdate()
        {
            if (head == null || apprentice == null)
                return;
            Vector3 to = Flat(apprentice.transform.position - head.position);
            float angle = to.sqrMagnitude > 0.01f ? Vector3.SignedAngle(Flat(transform.forward), to, Vector3.up) : 0f;
            bool wants = to.magnitude < lookWithin && Mathf.Abs(angle) < 120f && apprentice.IsAlive;
            look = Mathf.MoveTowards(look, wants ? 1f : 0f, lookSpeed * Time.deltaTime * 0.5f);
            float eased = look * look * (3f - 2f * look);
            HeadTurn = Mathf.Clamp(angle, -lookLimit, lookLimit) * eased;
            head.rotation = Quaternion.AngleAxis(HeadTurn, Vector3.up) * head.rotation;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
