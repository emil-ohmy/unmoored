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

        Vector3 velocity;
        Vector3 follow;
        float shake;

        void Awake() => Instance = this;

        void Start()
        {
            if (target != null)
                follow = Desired();
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        public void SnapToTarget()
        {
            follow = Desired();
            velocity = Vector3.zero;
        }

        Vector3 Desired() => target.position + Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -distance);

        void LateUpdate()
        {
            if (target == null)
                return;
            float dt = Time.unscaledDeltaTime; // keeps following and shaking during hit-stop
            follow = Vector3.SmoothDamp(follow, Desired(), ref velocity, smoothTime, Mathf.Infinity, dt);
            shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
            transform.SetPositionAndRotation(follow + Random.insideUnitSphere * shake, Quaternion.Euler(pitch, 0f, 0f));
        }
    }
}
