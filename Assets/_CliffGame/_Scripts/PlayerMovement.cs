using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = -9.81f;
        
        private CharacterController _characterController;
        private Vector3 _velocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = GameInput.Instance.MoveInput;
            bool isGrounded = _characterController.isGrounded;

            // Keep the controller snapped to the ground so it does not accumulate downward velocity.
            if (isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            float moveSpeed = GameInput.Instance.IsHoldingSprintInput ? _sprintSpeed : _walkSpeed;
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

            _characterController.Move(move * moveSpeed * Time.fixedDeltaTime);

            if (isGrounded && GameInput.Instance.ConsumeJumpPressed())
            {
                _velocity.y = Mathf.Sqrt(2f * _jumpHeight * -_gravity);
            }

            _velocity.y += _gravity * Time.fixedDeltaTime;
            _characterController.Move(_velocity * Time.fixedDeltaTime);
        }
    }
}
