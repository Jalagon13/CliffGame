using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private Camera _buildCamera;
        [SerializeField] private BuildGhostController _ghostController;

        [Header("Piece Prefabs")]
        [SerializeField] private GameObject _floorPiecePrefab;
        [SerializeField] private GameObject _wallPiecePrefab;
        [SerializeField] private GameObject _rampPiecePrefab;

        [Header("Grid")]
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;
        [SerializeField] private float _cellSize = 1f;

        [Header("Placement")]
        [SerializeField] private float _buildRange = 3f;
        [SerializeField] private LayerMask _buildRaycastMask = ~0;
        [SerializeField] private LayerMask _collisionMask = 0;
        
        [Header("Connectors")]
        [SerializeField] private LayerMask _connectorMask = 0;
        [SerializeField] private float _connectorSearchRadius = 0.45f;

        [Header("Debug/Runtime State")]
        [SerializeField] private bool _buildModeEnabled;
        [SerializeField] private BuildPieceType _selectedPiece = BuildPieceType.Floor;
        [SerializeField] private RampDir _selectedRampDirection = RampDir.North;
        [SerializeField] private FaceDir _preferredWallFace = FaceDir.North;
        [SerializeField] private List<PlacedPieceRecord> _placedPieceRecords = new();

        private readonly Dictionary<CellKey, CellRecord> _cells = new();
        private readonly Dictionary<FaceKey, GameObject> _walls = new();

        private BuildGridService _gridService;
        private BuildRuleService _ruleService;
        private BuildTargetingService _targetingService;

        private void Awake()
        {
            Instance = this;
            _gridService = new BuildGridService(_gridOrigin, _cellSize);
            _ruleService = new BuildRuleService(
                _gridService,
                _cells,
                _walls,
                _collisionMask,
                GetPrefabBoundsSize(_floorPiecePrefab, new Vector3(1f, 0.2f, 1f)),
                GetPrefabBoundsSize(_wallPiecePrefab, new Vector3(0.2f, 1f, 1f)),
                GetPrefabBoundsSize(_rampPiecePrefab, new Vector3(1f, 0.2f, 1.4f)));
            _targetingService = new BuildTargetingService(_gridService);

            if (_ghostController != null)
            {
                _ghostController.SetSelectedPiece(_selectedPiece);
            }
        }

        private void Update()
        {
            HandleBuildInput();

            if (!_buildModeEnabled)
            {
                if (_ghostController != null)
                {
                    _ghostController.Hide();
                }

                return;
            }

            if (!TryGetCandidate(_selectedPiece, out PlacementCandidate candidate))
            {
                if (_ghostController != null)
                {
                    _ghostController.Hide();
                }

                return;
            }

            bool inRange = IsInBuildRange(candidate.WorldPosition);
            PlacementValidationResult validation = Validate(candidate, checkRange: true, inRange);

            if (_ghostController != null)
            {
                _ghostController.Show(candidate, validation.IsValid);
            }

            if (validation.IsValid && GameInput.Instance != null && GameInput.Instance.ConsumePlaceBuildPressed())
            {
                TryPlace(candidate);
            }
        }

        public bool TryGetCandidate(BuildPieceType selected, out PlacementCandidate candidate)
        {
            candidate = default;

            Camera buildCam = GetBuildCamera();
            if (buildCam == null)
            {
                return false;
            }

            Ray ray = new Ray(buildCam.transform.position, buildCam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _buildRange, _buildRaycastMask, QueryTriggerInteraction.Ignore))
            {
                if (_targetingService.TryGetCandidateFromConnector(
                        selected,
                        hit.point,
                        _connectorMask,
                        _connectorSearchRadius,
                        IsConnectorCandidatePreferred,
                        out candidate))
                {
                    return true;
                }
            }

            return _targetingService.TryGetCandidate(
                selected,
                _selectedRampDirection,
                _preferredWallFace,
                ray,
                _buildRange,
                _buildRaycastMask,
                out candidate);
        }

        public PlacementValidationResult Validate(PlacementCandidate candidate)
        {
            bool inRange = IsInBuildRange(candidate.WorldPosition);
            return Validate(candidate, checkRange: true, inRange);
        }

        public bool TryPlace(PlacementCandidate candidate)
        {
            PlacementValidationResult validation = Validate(candidate);
            if (!validation.IsValid)
            {
                return false;
            }

            GameObject prefab = GetPrefab(candidate.PieceType);
            if (prefab == null)
            {
                return false;
            }

            Quaternion prefabAuthoredRotation = prefab.transform.rotation;
            Quaternion finalRotation = candidate.WorldRotation * prefabAuthoredRotation;
            GameObject instance = Instantiate(prefab, candidate.WorldPosition, finalRotation);
            RegisterPlacedPiece(candidate, instance);

            return true;
        }

        private void HandleBuildInput()
        {
            if (GameInput.Instance == null)
            {
                return;
            }

            if (GameInput.Instance.ConsumeToggleBuildModePressed())
            {
                _buildModeEnabled = !_buildModeEnabled;
                Debug.Log($"BuildMode Active: {_buildModeEnabled}");
                GameInput.Instance.ClearBuildCommandBuffer();
                if (!_buildModeEnabled && _ghostController != null)
                {
                    _ghostController.Hide();
                }
            }

            if (GameInput.Instance.TryConsumeBuildPieceSelection(out BuildPieceType selectedPiece))
            {
                SetSelectedPiece(selectedPiece);
            }

            if (GameInput.Instance.ConsumeRotateBuildPressed())
            {
                RotateSelection();
            }
        }

        private void SetSelectedPiece(BuildPieceType pieceType)
        {
            _selectedPiece = pieceType;
            if (_ghostController != null)
            {
                _ghostController.SetSelectedPiece(_selectedPiece);
            }
        }

        private void RotateSelection()
        {
            if (_selectedPiece == BuildPieceType.Ramp)
            {
                _selectedRampDirection = (RampDir)(((int)_selectedRampDirection + 1) % 4);
            }
        }

        private PlacementValidationResult Validate(PlacementCandidate candidate, bool checkRange, bool inRange)
        {
            return _ruleService.Validate(candidate, checkRange, inRange);
        }

        private bool IsInBuildRange(Vector3 point)
        {
            Camera buildCam = GetBuildCamera();
            if (buildCam == null)
            {
                return false;
            }

            float sqrDistance = (point - buildCam.transform.position).sqrMagnitude;
            return sqrDistance <= (_buildRange * _buildRange);
        }

        private Camera GetBuildCamera()
        {
            if (_buildCamera != null)
            {
                return _buildCamera;
            }

            return Camera.main;
        }

        private GameObject GetPrefab(BuildPieceType pieceType)
        {
            return pieceType switch
            {
                BuildPieceType.Floor => _floorPiecePrefab,
                BuildPieceType.Wall => _wallPiecePrefab,
                BuildPieceType.Ramp => _rampPiecePrefab,
                _ => _floorPiecePrefab,
            };
        }

        private static Vector3 GetPrefabBoundsSize(GameObject prefab, Vector3 fallback)
        {
            if (prefab == null)
            {
                return fallback;
            }

            Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
            if (colliders.Length > 0)
            {
                Bounds colliderBounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    colliderBounds.Encapsulate(colliders[i].bounds);
                }

                return colliderBounds.size;
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds renderBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    renderBounds.Encapsulate(renderers[i].bounds);
                }

                return renderBounds.size;
            }

            return fallback;
        }

        private void RegisterPlacedPiece(PlacementCandidate candidate, GameObject pieceInstance)
        {
            switch (candidate.PieceType)
            {
                case BuildPieceType.Floor:
                {
                    if (!_cells.TryGetValue(candidate.Cell, out CellRecord record))
                    {
                        record = new CellRecord();
                    }

                    record.FloorPiece = pieceInstance;
                    _cells[candidate.Cell] = record;

                    _placedPieceRecords.Add(new PlacedPieceRecord
                    {
                        PieceType = BuildPieceType.Floor,
                        Cell = candidate.Cell,
                        HasFace = false,
                        Face = default,
                        RampDirection = RampDir.North,
                    });
                    AttachPlacedPieceMetadata(pieceInstance, BuildPieceType.Floor, candidate.Cell, false, default);
                    break;
                }

                case BuildPieceType.Ramp:
                {
                    if (!_cells.TryGetValue(candidate.Cell, out CellRecord record))
                    {
                        record = new CellRecord();
                    }

                    record.RampPiece = pieceInstance;
                    record.RampDirection = candidate.RampDirection;
                    _cells[candidate.Cell] = record;

                    _placedPieceRecords.Add(new PlacedPieceRecord
                    {
                        PieceType = BuildPieceType.Ramp,
                        Cell = candidate.Cell,
                        HasFace = false,
                        Face = default,
                        RampDirection = candidate.RampDirection,
                    });
                    AttachPlacedPieceMetadata(pieceInstance, BuildPieceType.Ramp, candidate.Cell, false, default);
                    break;
                }

                case BuildPieceType.Wall:
                {
                    FaceKey canonicalFace = _gridService.Canonicalize(candidate.Face);
                    _walls[canonicalFace] = pieceInstance;

                    _placedPieceRecords.Add(new PlacedPieceRecord
                    {
                        PieceType = BuildPieceType.Wall,
                        Cell = default,
                        HasFace = true,
                        Face = canonicalFace,
                        RampDirection = RampDir.North,
                    });
                    AttachPlacedPieceMetadata(pieceInstance, BuildPieceType.Wall, default, true, canonicalFace);
                    break;
                }
            }
        }

        private bool IsConnectorCandidatePreferred(PlacementCandidate candidate)
        {
            return _ruleService.Validate(candidate, checkRange: false, inRange: true).IsValid;
        }

        private static void AttachPlacedPieceMetadata(
            GameObject piece,
            BuildPieceType type,
            CellKey cell,
            bool hasFace,
            FaceKey face)
        {
            if (piece == null)
            {
                return;
            }

            PlacedBuildPiece metadata = piece.GetComponent<PlacedBuildPiece>();
            if (metadata == null)
            {
                metadata = piece.AddComponent<PlacedBuildPiece>();
            }

            metadata.Initialize(type, cell, hasFace, face);
        }

    }
}
