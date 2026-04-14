using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _acceleration = 20f;
        [SerializeField] private float _deceleration = 24f;
        [SerializeField] private float _airMovementMultiplier = 0.5f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = -9.81f;
        
        private CharacterController _characterController;
        private Vector3 _velocity;
        private Vector3 _horizontalVelocity;

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
            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            
            float airMultiplier = isGrounded ? 1f : _airMovementMultiplier;
            Vector3 targetHorizontalVelocity = moveDirection * (moveSpeed * airMultiplier);

            bool hasMovementInput = moveInput.sqrMagnitude > 0.0001f;
            
            float accelerationRate = hasMovementInput ? _acceleration : _deceleration;
            accelerationRate *= airMultiplier;
            
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetHorizontalVelocity, accelerationRate * Time.fixedDeltaTime);

            if (isGrounded && GameInput.Instance.ConsumeJumpPressed())
            {
                _velocity.y = Mathf.Sqrt(2f * _jumpHeight * -_gravity);
            }

            _velocity.y += _gravity * Time.fixedDeltaTime;

            Vector3 frameVelocity = _horizontalVelocity;
            frameVelocity.y = _velocity.y;
            _characterController.Move(frameVelocity * Time.fixedDeltaTime);
        }
    }
}
