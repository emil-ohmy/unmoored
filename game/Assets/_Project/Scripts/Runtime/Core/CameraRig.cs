using UnityEngine;

namespace MyGame
{
    // Fixed angled camera that never rotates (Death's Door/Tunic style): follows the target, with screen shake.
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public Transform target;
        [Range(30f, 80f)] public float pitch = 50f;
        public float distance = 15f;
        public float smoothTime = 0.15f;
        [Range(0f, 0.5f)] public float secondaryWeight = 0.3f; // how far to lean towards a locked-on target

        Transform secondary;
        Transform listener;

        Vector3 velocity;
        Vector3 follow;
        float shake, zoom = 1f, zoomTarget = 1f;

        void Awake()
        {
            Instance = this;
            // Hear from where the Apprentice stands, not from the camera high above: torches get louder as you pass,
            // and left/right on screen stays left/right in the ears (the camera never rotates).
            listener = new GameObject("Audio Listener", typeof(AudioListener)).transform;
        }

        void Start()
        {
            if (target != null)
                follow = Desired();
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        // Frame a second subject too (the lock-on target), without ever rotating the camera.
        public void SetSecondary(Transform subject) => SetSecondary(subject, 0.3f);

        // Lean the framing towards a second subject (a locked-on target, the hook post you're aiming at).
        public void SetSecondary(Transform subject, float weight)
        {
            secondary = subject;
            secondaryWeight = weight;
        }

        // Ease in closer (a factor on the distance: 0.7 = 30% closer) for a moment worth framing (a key item held up).
        public void SetZoom(float factor) => zoomTarget = factor;
        public float Zoom => zoom;

        public void SnapToTarget()
        {
            follow = Desired();
            velocity = Vector3.zero;
        }

        Vector3 Desired()
        {
            Vector3 focus = secondary != null ? Vector3.Lerp(target.position, secondary.position, secondaryWeight) : target.position;
            return focus + Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -distance * zoom);
        }

        void LateUpdate()
        {
            if (target == null)
                return;
            // Unscaled: keeps following and shaking during hit-stop. Stepped time (tests, recordings) steps it too.
            float dt = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            zoom = Mathf.MoveTowards(zoom, zoomTarget, dt * 0.6f);
            follow = Vector3.SmoothDamp(follow, Desired(), ref velocity, smoothTime, Mathf.Infinity, dt);
            shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
            transform.SetPositionAndRotation(follow + Random.insideUnitSphere * shake, Quaternion.Euler(pitch, 0f, 0f));
            listener.SetPositionAndRotation(target.position + Vector3.up * 1.2f, Quaternion.identity);
        }
    }
}
