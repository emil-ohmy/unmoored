using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    // Placeholder third-person movement: WASD/left stick to move (camera-relative),
    // Space/south button to jump, Shift/left stick press to sprint. Pushes rigidbodies it walks into.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintSpeed = 8f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float turnSpeed = 720f;
        [SerializeField] float pushSpeed = 2f;
        [SerializeField] float respawnBelowY = -10f;

        CharacterController controller;
        InputAction move, jump, sprint;
        float verticalVelocity;
        Vector3 spawnPosition;

        public bool IsGrounded => controller.isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Project-wide actions from Assets/InputSystem_Actions.inputactions, enabled by default.
            move = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true);
            jump = InputSystem.actions.FindAction("Player/Jump", throwIfNotFound: true);
            sprint = InputSystem.actions.FindAction("Player/Sprint", throwIfNotFound: true);
            spawnPosition = transform.position;
        }

        void Update()
        {
            Vector3 direction = CameraRelative(move.ReadValue<Vector2>());
            float speed = sprint.IsPressed() ? sprintSpeed : moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f; // keep a little downward pressure so isGrounded stays true
            if (controller.isGrounded && jump.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            if (transform.position.y < respawnBelowY)
                Respawn();
        }

        Vector3 CameraRelative(Vector2 input)
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        void Respawn()
        {
            controller.enabled = false; // CharacterController overrides direct position changes
            transform.position = spawnPosition;
            controller.enabled = true;
            verticalVelocity = 0f;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic || hit.moveDirection.y < -0.3f)
                return; // nothing to push, or we're standing on it

            var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z) * pushSpeed;
            body.linearVelocity = new Vector3(push.x, body.linearVelocity.y, push.z);
        }
    }
}
