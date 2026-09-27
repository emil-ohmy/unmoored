using UnityEngine;

namespace MyGame
{
    // Placeholder camera: follows the target from a fixed offset and looks at it.
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 6f, -8f);
        [SerializeField] float smoothTime = 0.15f;

        Vector3 velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.up);
        }
    }
}
