using UnityEngine;

namespace CliffGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }
        
        private PlayerInput _playerInput;
        
        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();
            
            
        }
        
        private void OnDestroy()
        {
            _playerInput.Disable();
        }
        
        
    }
}
