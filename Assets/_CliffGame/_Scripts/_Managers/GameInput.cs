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
        private InputAction _toggleBuildModeAction;
        private InputAction _selectFloorAction;
        private InputAction _selectWallAction;
        private InputAction _selectRampAction;
        private InputAction _rotateBuildAction;
        private InputAction _placeBuildAction;

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
            CreateRuntimeBuildActions(_playerInput.Player.Get());
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

            _toggleBuildModeAction.started += BuildAction_OnToggleBuildMode;
            _selectFloorAction.started += BuildAction_OnSelectFloor;
            _selectWallAction.started += BuildAction_OnSelectWall;
            _selectRampAction.started += BuildAction_OnSelectRamp;
            _rotateBuildAction.started += BuildAction_OnRotateBuild;
            _placeBuildAction.started += BuildAction_OnPlaceBuild;
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

            _toggleBuildModeAction.started -= BuildAction_OnToggleBuildMode;
            _selectFloorAction.started -= BuildAction_OnSelectFloor;
            _selectWallAction.started -= BuildAction_OnSelectWall;
            _selectRampAction.started -= BuildAction_OnSelectRamp;
            _rotateBuildAction.started -= BuildAction_OnRotateBuild;
            _placeBuildAction.started -= BuildAction_OnPlaceBuild;
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

        private void GameInput_OnSprint(InputAction.CallbackContext context)
        {
            IsHoldingSprintInput = context.ReadValueAsButton();
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            
            OnMove?.Invoke(MoveInput);
        }

        private void CreateRuntimeBuildActions(InputActionMap playerMap)
        {
            _toggleBuildModeAction = playerMap.AddAction("ToggleBuildMode", InputActionType.Button, "<Keyboard>/b");
            _selectFloorAction = playerMap.AddAction("BuildFloor", InputActionType.Button, "<Keyboard>/1");
            _selectWallAction = playerMap.AddAction("BuildWall", InputActionType.Button, "<Keyboard>/2");
            _selectRampAction = playerMap.AddAction("BuildRamp", InputActionType.Button, "<Keyboard>/3");
            _rotateBuildAction = playerMap.AddAction("BuildRotate", InputActionType.Button, "<Keyboard>/r");
            _placeBuildAction = playerMap.AddAction("BuildPlace", InputActionType.Button, "<Mouse>/leftButton");
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
    }
}
