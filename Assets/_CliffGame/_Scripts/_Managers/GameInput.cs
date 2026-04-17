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
        public event Action OnToggleBuildMode;
        public event Action<BuildPieceType> OnBuildPieceSelected;
        public event Action OnRotateBuild;
        public event Action OnPlaceBuild;

        private PlayerInput _playerInput;

        public Vector2 MoveInput { get; private set; }
        public bool IsHoldingSprintInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool IsHoldingDownJump { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool ToggleBuildModePressed { get; private set; }
        public bool RotateBuildPressed { get; private set; }
        public bool PlaceBuildPressed { get; private set; }
        public BuildPieceType? PendingBuildPieceSelection { get; private set; }

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
            
            _playerInput.Build.ToggleBuildMode.started += BuildAction_OnToggleBuildMode;
            _playerInput.Build.RotateBuild.started += BuildAction_OnRotateBuild;
            _playerInput.Build.PlaceBuild.started += BuildAction_OnPlaceBuild;
            _playerInput.Build.SelectFloor.started += BuildAction_OnSelectFloor;
            _playerInput.Build.SelectWall.started += BuildAction_OnSelectWall;
            _playerInput.Build.SelectRamp.started += BuildAction_OnSelectRamp;
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

            _playerInput.Player.Sprint.started -= GameInput_OnSprint;
            _playerInput.Player.Sprint.performed -= GameInput_OnSprint;
            _playerInput.Player.Sprint.canceled -= GameInput_OnSprint;
            
            _playerInput.Player.Jump.started -= GameInput_OnJump;
            _playerInput.Player.Jump.canceled -= GameInput_OnJump;

            _playerInput.Build.ToggleBuildMode.started -= BuildAction_OnToggleBuildMode;
            _playerInput.Build.RotateBuild.started -= BuildAction_OnRotateBuild;
            _playerInput.Build.PlaceBuild.started -= BuildAction_OnPlaceBuild;
            _playerInput.Build.SelectFloor.started -= BuildAction_OnSelectFloor;
            _playerInput.Build.SelectWall.started -= BuildAction_OnSelectWall;
            _playerInput.Build.SelectRamp.started -= BuildAction_OnSelectRamp;
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

        private void GameInput_OnSprint(InputAction.CallbackContext context)
        {
            IsHoldingSprintInput = context.ReadValueAsButton();
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            
            OnMove?.Invoke(MoveInput);
        }

        private void BuildAction_OnToggleBuildMode(InputAction.CallbackContext _)
        {
            ToggleBuildModePressed = true;
            OnToggleBuildMode?.Invoke();
        }

        private void BuildAction_OnSelectFloor(InputAction.CallbackContext _)
        {
            PendingBuildPieceSelection = BuildPieceType.Floor;
            OnBuildPieceSelected?.Invoke(BuildPieceType.Floor);
        }

        private void BuildAction_OnSelectWall(InputAction.CallbackContext _)
        {
            PendingBuildPieceSelection = BuildPieceType.Wall;
            OnBuildPieceSelected?.Invoke(BuildPieceType.Wall);
        }

        private void BuildAction_OnSelectRamp(InputAction.CallbackContext _)
        {
            PendingBuildPieceSelection = BuildPieceType.Ramp;
            OnBuildPieceSelected?.Invoke(BuildPieceType.Ramp);
        }

        private void BuildAction_OnRotateBuild(InputAction.CallbackContext _)
        {
            RotateBuildPressed = true;
            OnRotateBuild?.Invoke();
        }

        private void BuildAction_OnPlaceBuild(InputAction.CallbackContext _)
        {
            PlaceBuildPressed = true;
            OnPlaceBuild?.Invoke();
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

        public bool ConsumeToggleBuildModePressed()
        {
            if (!ToggleBuildModePressed)
            {
                return false;
            }

            ToggleBuildModePressed = false;
            return true;
        }

        public bool ConsumeRotateBuildPressed()
        {
            if (!RotateBuildPressed)
            {
                return false;
            }

            RotateBuildPressed = false;
            return true;
        }

        public bool ConsumePlaceBuildPressed()
        {
            if (!PlaceBuildPressed)
            {
                return false;
            }

            PlaceBuildPressed = false;
            return true;
        }

        public bool TryConsumeBuildPieceSelection(out BuildPieceType selectedPiece)
        {
            if (!PendingBuildPieceSelection.HasValue)
            {
                selectedPiece = default;
                return false;
            }

            selectedPiece = PendingBuildPieceSelection.Value;
            PendingBuildPieceSelection = null;
            return true;
        }

        public void ClearBuildCommandBuffer()
        {
            ToggleBuildModePressed = false;
            RotateBuildPressed = false;
            PlaceBuildPressed = false;
            PendingBuildPieceSelection = null;
        }
    }
}
