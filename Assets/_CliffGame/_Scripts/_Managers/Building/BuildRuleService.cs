using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildRuleService
    {
        private readonly BuildGridService _grid;
        private readonly Dictionary<CellKey, CellRecord> _cells;
        private readonly Dictionary<FaceKey, GameObject> _walls;
        private readonly LayerMask _collisionMask;
        private readonly Vector3 _floorSize;
        private readonly Vector3 _wallSize;
        private readonly Vector3 _rampSize;

        public BuildRuleService(
            BuildGridService grid,
            Dictionary<CellKey, CellRecord> cells,
            Dictionary<FaceKey, GameObject> walls,
            LayerMask collisionMask,
            Vector3 floorSize,
            Vector3 wallSize,
            Vector3 rampSize)
        {
            _grid = grid;
            _cells = cells;
            _walls = walls;
            _collisionMask = collisionMask;
            _floorSize = floorSize;
            _wallSize = wallSize;
            _rampSize = rampSize;
        }

        public PlacementValidationResult Validate(PlacementCandidate candidate, bool checkRange, bool inRange)
        {
            if (checkRange && !inRange)
            {
                return PlacementValidationResult.Invalid(PlacementInvalidReason.OutOfRange);
            }

            switch (candidate.PieceType)
            {
                case BuildPieceType.Floor:
                    if (HasFloor(candidate.Cell))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.Occupied);
                    }

                    if (!HasAnySupport(candidate.Cell))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.Unsupported);
                    }
                    break;

                case BuildPieceType.Ramp:
                    if (HasRamp(candidate.Cell))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.Occupied);
                    }

                    if (!HasAnySupport(candidate.Cell))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.Unsupported);
                    }
                    break;

                case BuildPieceType.Wall:
                    FaceKey canonicalFace = _grid.Canonicalize(candidate.Face);
                    if (_walls.ContainsKey(canonicalFace))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.Occupied);
                    }

                    if (!HasAdjacentFloor(canonicalFace))
                    {
                        return PlacementValidationResult.Invalid(PlacementInvalidReason.NeedsAdjacentFloor);
                    }
                    break;
            }

            if (!IsCollisionFree(candidate))
            {
                return PlacementValidationResult.Invalid(PlacementInvalidReason.BlockedByCollision);
            }

            return PlacementValidationResult.Valid();
        }

        private bool HasFloor(CellKey cell)
        {
            return _cells.TryGetValue(cell, out CellRecord record) && record.HasFloor;
        }

        private bool HasRamp(CellKey cell)
        {
            return _cells.TryGetValue(cell, out CellRecord record) && record.HasRamp;
        }

        private bool HasAdjacentFloor(FaceKey faceKey)
        {
            _grid.GetWallAdjacentCells(faceKey, out CellKey a, out CellKey b);
            return HasFloor(a) || HasFloor(b);
        }

        private bool HasAnySupport(CellKey cell)
        {
            if (cell.Y <= 0)
            {
                return true;
            }

            CellKey below = new CellKey(cell.X, cell.Y - 1, cell.Z);
            if (HasFloor(below) || HasRamp(below))
            {
                return true;
            }

            CellKey[] neighbors =
            {
                new(cell.X + 1, cell.Y, cell.Z),
                new(cell.X - 1, cell.Y, cell.Z),
                new(cell.X, cell.Y, cell.Z + 1),
                new(cell.X, cell.Y, cell.Z - 1),
                new(cell.X, cell.Y + 1, cell.Z),
                new(cell.X, cell.Y - 1, cell.Z),
            };

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (_cells.TryGetValue(neighbors[i], out CellRecord cellRecord) && (cellRecord.HasFloor || cellRecord.HasRamp))
                {
                    return true;
                }
            }

            // Shared walls can also provide support for new pieces.
            FaceKey[] touchingFaces =
            {
                _grid.Canonicalize(new FaceKey(cell, FaceDir.North)),
                _grid.Canonicalize(new FaceKey(cell, FaceDir.East)),
                _grid.Canonicalize(new FaceKey(cell, FaceDir.South)),
                _grid.Canonicalize(new FaceKey(cell, FaceDir.West)),
            };

            for (int i = 0; i < touchingFaces.Length; i++)
            {
                if (_walls.ContainsKey(touchingFaces[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCollisionFree(PlacementCandidate candidate)
        {
            if (_collisionMask.value == 0)
            {
                return true;
            }

            Vector3 size = candidate.PieceType switch
            {
                BuildPieceType.Floor => _floorSize,
                BuildPieceType.Ramp => _rampSize,
                _ => _wallSize,
            };

            Vector3 extents = new Vector3(
                Mathf.Max(0.01f, size.x * 0.5f - 0.01f),
                Mathf.Max(0.01f, size.y * 0.5f - 0.01f),
                Mathf.Max(0.01f, size.z * 0.5f - 0.01f));

            return !Physics.CheckBox(
                candidate.WorldPosition,
                extents,
                candidate.WorldRotation,
                _collisionMask,
                QueryTriggerInteraction.Ignore);
        }
    }
}
