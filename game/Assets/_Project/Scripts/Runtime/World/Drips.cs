using UnityEngine;

namespace MyGame
{
    // Water dripping from the dark overhead (the Hold, under the jetty): now and then, somewhere within `size` round it,
    // a drop falls out of the dark into the water (or onto the boards) with a plip. Only near the Apprentice (`near`):
    // the rest of the dark is quiet.
    public sealed class Drips : MonoBehaviour
    {
        public Vector3 size = new Vector3(20f, 0f, 20f); // (x and z: the area; y unused)
        public float everySecond = 0.6f;                  // on average, a drop this often
        public float from = 7f;                           // how far it falls (out of the dark)
        public float waterLevel = 0f;
        public float near = 10f;
        public AudioClip[] dripSound;

        public int Fallen { get; private set; } // (tests)

        float next;
        Transform listener;

        void Update()
        {
            if (Time.time < next || Effects.Instance == null)
                return;
            next = Time.time + Random.Range(0.3f, 1.7f) * everySecond;
            if (listener == null)
            {
                var a = FindAnyObjectByType<Apprentice>();
                listener = a != null ? a.transform : null;
            }
            // Round the Apprentice (where it can be seen and heard), within the area.
            Vector2 r = Random.insideUnitCircle * near;
            Vector3 centre = listener != null ? listener.position : transform.position;
            Vector3 at = new Vector3(centre.x + r.x, 0f, centre.z + r.y);
            Vector3 local = at - transform.position;
            if (Mathf.Abs(local.x) > size.x * 0.5f || Mathf.Abs(local.z) > size.z * 0.5f)
                return;
            // Where it lands: on whatever's there (boards, stone), or the water.
            float land = waterLevel;
            if (Physics.Raycast(new Vector3(at.x, waterLevel + from, at.z), Vector3.down, out var hit, from, ~0, QueryTriggerInteraction.Ignore))
                land = Mathf.Max(land, hit.point.y);
            at.y = land;
            float fall = Mathf.Max(0.5f, waterLevel + from - land);
            float speed = 9f;
            Effects.Instance.Streak(at + Vector3.up * fall, Vector3.down * speed, new Color(0.62f, 0.68f, 0.75f, 0.5f), fall / speed, 0.035f);
            StartCoroutine(Land(at, fall / speed));
        }

        System.Collections.IEnumerator Land(Vector3 at, float after)
        {
            yield return new WaitForSeconds(after);
            Fallen++;
            Effects.Instance?.Drip(at + Vector3.up * 0.02f);
            Sfx.Instance?.Attack(dripSound, at, Random.Range(0.35f, 0.7f), "hold_drip", Random.Range(0.85f, 1.2f));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
