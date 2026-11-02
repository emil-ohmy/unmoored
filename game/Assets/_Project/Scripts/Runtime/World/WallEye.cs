using UnityEngine;

namespace MyGame
{
    // An eye in the Hold's living wall. Shut (a seam in the stone) until the Apprentice comes near on its side of the
    // wall; then it opens (one after another along a wall: each a moment apart) and turns to follow them, blinking now
    // and then; once they've passed on (or gone behind it), it shuts again. Its lids swing about its horizontal axis
    // (`axis`, in this transform's frame, whose forward is out of the wall), the top one up and the bottom one down,
    // `open` degrees each.
    public sealed class WallEye : MonoBehaviour
    {
        public Transform ball, lidTop, lidBottom;
        public Vector3 axis = Vector3.right;   // (local: the lids' hinge)
        public float open = 70f, openBottom = 70f; // degrees each lid (top, bottom) swings to open fully
        public float openWithin = 7.5f, shutBeyond = 10f;
        public float lookLimit = 45f;          // how far the ball turns from straight out
        public AudioClip[] openSound, closeSound;

        public bool Open => wanted && openness > 0.8f; // (tests)
        public float Openness => openness;

        Transform apprentice;
        Quaternion topRest, bottomRest, ballRest;
        float openness, delay, blinkAt, blinkUntil;
        bool wanted;
        float stareUntil;

        // Wide open and fixed on them for a while, unblinking (the door of eyes, tried).
        public void Stare(float seconds) { stareUntil = Time.time + seconds; blinkAt = Mathf.Max(blinkAt, stareUntil); }

        void Start()
        {
            var a = FindAnyObjectByType<Apprentice>();
            apprentice = a != null ? a.transform : null;
            if (lidTop != null) topRest = lidTop.localRotation;
            if (lidBottom != null) bottomRest = lidBottom.localRotation;
            if (ball != null) ballRest = ball.localRotation;
            blinkAt = Time.time + Random.Range(2f, 6f);
        }

        void Update()
        {
            if (apprentice == null)
                return;
            Vector3 to = apprentice.position + Vector3.up * 0.8f - transform.position;
            float d = new Vector3(to.x, 0f, to.z).magnitude;
            bool facing = Vector3.Dot(transform.forward, to) > 0f; // (its side of the wall)
            bool want = facing && (wanted ? d < shutBeyond : d < openWithin) || Time.time < stareUntil;
            if (want != wanted)
            {
                wanted = want;
                delay = Time.time + (want ? Random.Range(0.05f, 0.7f) : Random.Range(0f, 0.4f)); // (not all at once)
            }
            if (Time.time >= delay)
            {
                float was = openness;
                openness = Mathf.MoveTowards(openness, wanted ? 1f : 0f, Time.deltaTime / (wanted ? 0.45f : 0.3f));
                if (was == 0f && openness > 0f)
                    Sfx.Instance?.Attack(openSound, transform.position, 0.9f, "hold_eye_open");
                else if (was > 0.5f && openness <= 0.5f && !wanted)
                    Sfx.Instance?.Attack(closeSound, transform.position, 0.8f, "hold_eye_close");
            }
            // A blink now and then, while it's open (quick: shut and open again).
            if (openness > 0.9f && Time.time > blinkAt)
            {
                blinkUntil = Time.time + 0.14f;
                blinkAt = Time.time + Random.Range(3f, 8f);
            }
            float lids = Mathf.SmoothStep(0f, 1f, openness) * (Time.time < blinkUntil ? 0.1f : 1f);
            // (in this transform's frame, whatever way the parts themselves were turned on import)
            if (lidTop != null) lidTop.localRotation = Quaternion.AngleAxis(-open * lids, axis) * topRest;
            if (lidBottom != null) lidBottom.localRotation = Quaternion.AngleAxis(openBottom * lids, axis) * bottomRest;
            // Following them: turned towards them, within its limit (and back to straight out as it shuts).
            if (ball != null)
            {
                Vector3 local = transform.InverseTransformDirection(to.normalized);
                Quaternion look = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(Vector3.forward, local), lookLimit);
                ball.localRotation = Quaternion.Slerp(ball.localRotation, Quaternion.Slerp(Quaternion.identity, look, openness) * ballRest, Time.deltaTime * 6f);
            }
        }
    }
}
