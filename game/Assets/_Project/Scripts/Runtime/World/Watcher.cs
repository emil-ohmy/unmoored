using UnityEngine;

namespace MyGame
{
    // Eyes in the dark (the Hold's Shades, for now only watching): two small pale lights out among the pilings, low,
    // blinking now and then, turned towards the Apprentice. When the lantern comes near they shrink back into the dark
    // with a whisper and are gone; later, when the light has passed on, they're back (a little further off, or not).
    public sealed class Watcher : MonoBehaviour
    {
        public Transform eyeL, eyeR;
        public float withdrawAt = 4.5f;     // the lantern this near: gone (step towards them, and they're not there)
        public float returnBeyond = 9.5f;   // and back once it's further than this
        public float fade = 0.6f;
        public float gap = 0.15f;           // half the space between the eyes
        public AudioClip[] whisperSound;

        public bool Watching => shown > 0.5f; // (tests)

        Transform apprentice;
        Vector3 home;
        float shown, blinkAt, blinkUntil, goneUntil;
        bool gone;

        void Start()
        {
            home = transform.position;
            var a = FindAnyObjectByType<Apprentice>();
            apprentice = a != null ? a.transform : null;
            blinkAt = Time.time + Random.Range(1f, 5f);
            shown = 1f;
        }

        void Update()
        {
            if (apprentice == null)
                return;
            Vector3 to = apprentice.position - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (!gone && d < withdrawAt)
            {
                gone = true;
                goneUntil = Time.time + Random.Range(4f, 9f);
                Sfx.Instance?.Attack(whisperSound, transform.position, 1f, "hold_whisper"); // (quiet already, as made)
            }
            else if (gone && Time.time > goneUntil && d > returnBeyond)
            {
                gone = false;
                Vector2 r = Random.insideUnitCircle * 1.5f;
                transform.position = home + new Vector3(r.x, 0f, r.y); // (not quite where they were)
            }
            // Shrinking back as they go (drawn away from the light), fading; back slowly.
            float want = gone ? 0f : 1f;
            shown = Mathf.MoveTowards(shown, want, Time.deltaTime / (gone ? fade : fade * 3f));
            if (gone && shown > 0f)
                transform.position -= to.normalized * Time.deltaTime * 0.8f;
            // Turned towards you; a blink now and then (both at once; sometimes twice).
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 2f);
            if (Time.time > blinkAt)
            {
                blinkUntil = Time.time + 0.12f;
                blinkAt = Time.time + (Random.value < 0.25f ? 0.25f : Random.Range(2.5f, 7f));
            }
            float open = Time.time < blinkUntil ? 0.08f : 1f;
            Show(eyeL, -gap, open);
            Show(eyeR, gap, open);
        }

        void Show(Transform eye, float x, float open)
        {
            if (eye == null)
                return;
            eye.localPosition = new Vector3(x, 0f, 0f);
            float s = Mathf.SmoothStep(0f, 1f, shown);
            eye.localScale = new Vector3(0.11f * s, 0.07f * s * open, 0.03f);
            eye.gameObject.SetActive(s > 0.01f);
        }
    }
}
