using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }
        
        public event Action<Vector2> OnMove;
        public event Action OnJump;
        public event Action<Vector2> OnLook;

        private PlayerInput _playerInput;
        public Vector2 MoveInput { get; private set; }
        public bool IsHoldingSprintInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool IsHoldingDownJump { get; private set; }
        public Vector2 LookInput { get; private set; }

        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();
            
            _playerInput.Player.Move.started += GameInput_OnMove;
            _playerInput.Player.Move.performed += GameInput_OnMove;
            _playerInput.Player.Move.canceled += GameInput_OnMove;

            _playerInput.Player.Look.started += PlayerInput_OnLook;
            _playerInput.Player.Look.performed += PlayerInput_OnLook;
            _playerInput.Player.Look.canceled += PlayerInput_OnLook;

            _playerInput.Player.Sprint.started += GameInput_OnSprint;
            _playerInput.Player.Sprint.performed += GameInput_OnSprint;
            _playerInput.Player.Sprint.canceled += GameInput_OnSprint;
            
            _playerInput.Player.Jump.started += GameInput_OnJump;
            _playerInput.Player.Jump.canceled += GameInput_OnJump;
        }
        
        private void OnDestroy()
        {
            _playerInput.Disable();

            _playerInput.Player.Move.started -= GameInput_OnMove;
            _playerInput.Player.Move.performed -= GameInput_OnMove;
            _playerInput.Player.Move.canceled -= GameInput_OnMove;

            _playerInput.Player.Look.started -= PlayerInput_OnLook;
            _playerInput.Player.Look.performed -= PlayerInput_OnLook;
            _playerInput.Player.Look.canceled -= PlayerInput_OnLook;

            _playerInput.Player.Sprint.started += GameInput_OnSprint;
            _playerInput.Player.Sprint.performed += GameInput_OnSprint;
            _playerInput.Player.Sprint.canceled += GameInput_OnSprint;
            
            _playerInput.Player.Jump.started -= GameInput_OnJump;
            _playerInput.Player.Jump.canceled -= GameInput_OnJump;
        }

        private void PlayerInput_OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
            OnLook?.Invoke(LookInput);
        }

        private void GameInput_OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                JumpPressed = true;
            }

            IsHoldingDownJump = context.ReadValueAsButton();
            OnJump?.Invoke();
        }

        public bool ConsumeJumpPressed()
        {
            if (!JumpPressed)
            {
                return false;
            }

            JumpPressed = false;
            return true;
        }

        private void GameInput_OnSprint(InputAction.CallbackContext context)
        {
            IsHoldingSprintInput = context.ReadValue<bool>();
            Debug.Log($"IsHoldingSprintInput: {IsHoldingSprintInput}");
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            
            OnMove?.Invoke(MoveInput);
        }
    }
}
