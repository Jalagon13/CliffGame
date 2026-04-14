using UnityEngine;

namespace CliffGame
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 150f;
        [SerializeField] private float _verticalLookClamp = 80f;
        [SerializeField] private Transform _playerCamera;

        private float _xRotation;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            float mouseX = GameInput.Instance.LookInput.x * _mouseSensitivity * Time.deltaTime;
            float mouseY = GameInput.Instance.LookInput.y * _mouseSensitivity * Time.deltaTime;

            // Horizontal look rotates the player body around the Y axis.
            transform.Rotate(Vector3.up * mouseX);

            // Vertical look rotates only the camera, clamped to prevent flipping.
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -_verticalLookClamp, _verticalLookClamp);

            if (_playerCamera != null)
            {
                _playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
        }
    }
}
