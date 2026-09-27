using UnityEngine;

// PROTOTYPE (wayfinder ticket "Art style test"). Throwaway.
namespace MyGame.Prototypes.ArtTest
{
    // A small breathing bob so static models don't look frozen.
    public class IdleBob : MonoBehaviour
    {
        public float height = 0.02f;
        public float speed = 1.6f;
        Vector3 start;

        void Start() => start = transform.localPosition;

        void Update() => transform.localPosition = start + Vector3.up * (Mathf.Sin(Time.time * speed) * height);
    }
}
