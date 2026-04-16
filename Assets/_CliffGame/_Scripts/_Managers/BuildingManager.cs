using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildingManager : MonoBehaviour
    {
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

        [Header("Piece Sizes (for collision validation)")]
        [SerializeField] private Vector3 _floorPieceSize = new(1f, 0.2f, 1f);
        [SerializeField] private Vector3 _wallPieceSize = new(0.2f, 1f, 1f);
        [SerializeField] private Vector3 _rampPieceSize = new(1f, 0.2f, 1.4f);

        [Header("Placement")]
        [SerializeField] private float _buildRange = 3f;
        [SerializeField] private LayerMask _buildRaycastMask = ~0;
        [SerializeField] private LayerMask _collisionMask = 0;

        [Header("Debug/Runtime State")]
        [SerializeField] private bool _buildModeEnabled;
        [SerializeField] private BuildPieceType _selectedPiece = BuildPieceType.Floor;
        [SerializeField] private RampDir _selectedRampDirection = RampDir.North;
        [SerializeField] private FaceDir _preferredWallFace = FaceDir.North;
        [SerializeField] private List<PlacedPieceRecord> _placedPieceRecords = new();

        public static BuildingManager Instance { get; private set; }

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
                _floorPieceSize,
                _wallPieceSize,
                _rampPieceSize);
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

            GameObject instance = Instantiate(prefab, candidate.WorldPosition, candidate.WorldRotation);
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
                return;
            }

            if (_selectedPiece == BuildPieceType.Wall)
            {
                _preferredWallFace = (FaceDir)(((int)_preferredWallFace + 1) % 4);
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
                _ => _rampPiecePrefab,
            };
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
                    break;
                }
            }
        }

    }
}
