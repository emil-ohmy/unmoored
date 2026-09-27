using UnityEngine;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
namespace MyGame.Prototypes.CombatFeel
{
    // Fixed angled camera that never rotates (Death's Door/Tunic style), with screen shake.
    public class FixedAngleCamera : MonoBehaviour
    {
        public static FixedAngleCamera Instance { get; private set; }

        public Transform target;
        [Range(30f, 80f)] public float pitch = 50f;
        public float distance = 20f;
        public float smoothTime = 0.12f;

        Vector3 velocity;
        Vector3 followPosition;
        float shake;

        void Awake() => Instance = this;

        void Start() => followPosition = Desired();

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        Vector3 Desired() => target.position + Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -distance);

        void LateUpdate()
        {
            if (target == null)
                return;

            // Unscaled time so the camera still moves and shakes during hit-stop.
            float dt = Time.unscaledDeltaTime;
            followPosition = Vector3.SmoothDamp(followPosition, Desired(), ref velocity, smoothTime, Mathf.Infinity, dt);
            shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
            transform.SetPositionAndRotation(followPosition + Random.insideUnitSphere * shake, Quaternion.Euler(pitch, 0f, 0f));
        }
    }
}
