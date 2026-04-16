using UnityEngine;

namespace CliffGame
{
    public class BuildGhostController : MonoBehaviour
    {
        [Header("Ghost Prefabs")]
        [SerializeField] private GameObject _floorGhostPrefab;
        [SerializeField] private GameObject _wallGhostPrefab;
        [SerializeField] private GameObject _rampGhostPrefab;

        [Header("Ghost Colors")]
        [SerializeField] private Color _validColor = new(0.2f, 1f, 0.35f, 0.45f);
        [SerializeField] private Color _invalidColor = new(1f, 0.25f, 0.25f, 0.45f);

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private GameObject _floorGhost;
        private GameObject _wallGhost;
        private GameObject _rampGhost;
        private BuildPieceType _selectedPiece;

        private readonly MaterialPropertyBlock _propertyBlock = new();

        private void Awake()
        {
            _floorGhost = InstantiateGhost(_floorGhostPrefab);
            _wallGhost = InstantiateGhost(_wallGhostPrefab);
            _rampGhost = InstantiateGhost(_rampGhostPrefab);
            Hide();
        }

        public void SetSelectedPiece(BuildPieceType pieceType)
        {
            _selectedPiece = pieceType;
        }

        public void Show(PlacementCandidate candidate, bool isValid)
        {
            GameObject selectedGhost = GetGhostForSelectedType();
            if (selectedGhost == null)
            {
                return;
            }

            HideAllExcept(selectedGhost);
            selectedGhost.transform.SetPositionAndRotation(candidate.WorldPosition, candidate.WorldRotation);
            selectedGhost.SetActive(true);

            SetGhostColor(selectedGhost, isValid ? _validColor : _invalidColor);
        }

        public void Hide()
        {
            HideAllExcept(null);
        }

        private GameObject InstantiateGhost(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = $"{prefab.name}_Ghost";

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            return instance;
        }

        private void HideAllExcept(GameObject keepActive)
        {
            if (_floorGhost != null)
            {
                _floorGhost.SetActive(_floorGhost == keepActive);
            }

            if (_wallGhost != null)
            {
                _wallGhost.SetActive(_wallGhost == keepActive);
            }

            if (_rampGhost != null)
            {
                _rampGhost.SetActive(_rampGhost == keepActive);
            }
        }

        private GameObject GetGhostForSelectedType()
        {
            return _selectedPiece switch
            {
                BuildPieceType.Floor => _floorGhost,
                BuildPieceType.Wall => _wallGhost,
                _ => _rampGhost,
            };
        }

        private void SetGhostColor(GameObject ghost, Color color)
        {
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColor, color);
                _propertyBlock.SetColor(ColorProp, color);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
