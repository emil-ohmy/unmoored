using System.Collections;
using UnityEngine;

namespace MyGame
{
    // The Hall of the Last Hour's central engine: the clock's heart in the belfry at the keep's top (the escapement, the
    // winding drum, the hour bell), reached over the belfry's broken floor (the Vault). The Stroke at its plate sets it
    // going, and with it the Hall's time, for good: the hour bell tolls, the clock's hands sweep round to the hour,
    // every machine of the Hall runs from then on, the colour comes back in a wave out from here (HallTime), and the
    // camera draws back a moment to see it go out over the castle. A save that had it going loads it going (Settle).
    public sealed class CentralEngine : MonoBehaviour
    {
        public static CentralEngine Instance { get; private set; }

        public Clockwork clockwork;          // its own works (struck at its plate)
        public Transform bell;               // the hour bell: it swings as it tolls
        public Transform hourHand, minuteHand;
        public float hourTurn = 22.5f, minuteTurn = 270f; // a quarter past nine round to ten (clockwise from the south)
        public float sweepTime = 3.2f;
        public float pullBack = 2.4f, pullBackTime = 3.6f; // the camera drawn back (zoom), this long
        public AudioClip[] tollSound;

        public bool Set { get; private set; }  // (tests) time set going by it (or loaded so)
        public event System.Action Going;

        Quaternion hourRest, minuteRest, bellRest;
        bool ready;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Where the hands and the bell rest (stopped): once.
        void Ready()
        {
            if (ready)
                return;
            ready = true;
            if (hourHand != null)
                hourRest = hourHand.rotation;
            if (minuteHand != null)
                minuteRest = minuteHand.rotation;
            if (bell != null)
                bellRest = bell.rotation;
        }

        void Start()
        {
            Ready();
            if (clockwork != null)
                clockwork.Started += () =>
                {
                    if (!Set && (HallTime.Instance == null || !HallTime.Instance.Running))
                        StartCoroutine(SetGoing());
                };
        }

        // Struck: the moment time starts again.
        IEnumerator SetGoing()
        {
            Set = true;
            Going?.Invoke();
            HallTime.Instance?.Run();
            foreach (var c in FindObjectsByType<Clockwork>(FindObjectsSortMode.None))
                c.RunForever();
            Sfx.Instance?.Attack(tollSound, bell != null ? bell.position : transform.position, 1f, "last_hour_toll");
            CameraRig.Instance?.Shake(0.25f);
            CameraRig.Instance?.SetZoom(pullBack);
            float t = 0f;
            while (t < Mathf.Max(sweepTime, pullBackTime))
            {
                t += Time.deltaTime;
                float u = UiKit.Smooth(0f, 1f, Mathf.Clamp01(t / sweepTime));
                Hands(u);
                if (bell != null) // (swinging as it tolls, dying away)
                    bell.rotation = Quaternion.AngleAxis(9f * Mathf.Sin(t * 3.2f) * Mathf.Exp(-t * 0.6f), Vector3.right) * bellRest;
                yield return null;
            }
            Hands(1f);
            CameraRig.Instance?.SetZoom(1f);
            while (bell != null && t < 9f)
            {
                t += Time.deltaTime;
                bell.rotation = Quaternion.AngleAxis(9f * Mathf.Sin(t * 3.2f) * Mathf.Exp(-t * 0.6f), Vector3.right) * bellRest;
                yield return null;
            }
        }

        // The hands, `u` of the way from a quarter past nine round to ten.
        void Hands(float u)
        {
            if (hourHand != null)
                hourHand.rotation = Quaternion.AngleAxis(hourTurn * u, Vector3.forward) * hourRest;
            if (minuteHand != null)
                minuteHand.rotation = Quaternion.AngleAxis(minuteTurn * u, Vector3.forward) * minuteRest;
        }

        // A save that had the Hall's time going: going, at once (no toll, no wave; the hands at the hour).
        public void Settle()
        {
            if (Set)
                return;
            Set = true;
            Ready();
            Hands(1f);
            HallTime.Instance?.RunAtOnce();
            foreach (var c in FindObjectsByType<Clockwork>(FindObjectsSortMode.None))
                c.RunForever(atOnce: true);
        }
    }
}
