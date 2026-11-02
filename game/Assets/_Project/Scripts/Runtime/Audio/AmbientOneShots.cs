using UnityEngine;

namespace MyGame
{
    // A place's sounds now and then (the Hold: the jetty creaking far overhead): one of `clips` every so often, from
    // somewhere within `spread` round it.
    public sealed class AmbientOneShots : MonoBehaviour
    {
        public AudioClip[] clips;
        public Vector2 every = new Vector2(8f, 20f); // seconds between, at least .. at most
        public float spread = 20f;
        public float volume = 0.6f;

        float next;

        void Start() => next = Time.time + Random.Range(every.x * 0.3f, every.y * 0.6f);

        void Update()
        {
            if (Time.time < next || clips == null || clips.Length == 0)
                return;
            next = Time.time + Random.Range(every.x, every.y);
            Vector2 r = Random.insideUnitCircle * spread;
            Sfx.Instance?.Attack(clips, transform.position + new Vector3(r.x, 0f, r.y), volume * Random.Range(0.7f, 1f), name, Random.Range(0.9f, 1.05f));
        }
    }
}
