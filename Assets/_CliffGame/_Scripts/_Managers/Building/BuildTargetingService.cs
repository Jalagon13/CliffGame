using UnityEngine;

namespace CliffGame
{
    public class BuildTargetingService
    {
        private const float HitBias = 0.05f;

        private readonly BuildGridService _grid;

        public BuildTargetingService(BuildGridService grid)
        {
            _grid = grid;
        }

        public bool TryGetCandidate(
            BuildPieceType selectedPiece,
            RampDir rampDir,
            FaceDir preferredWallFace,
            Ray ray,
            float maxDistance,
            LayerMask raycastMask,
            out PlacementCandidate candidate)
        {
            candidate = default;

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 pointTowardPlayer = hit.point - (hit.normal * HitBias);
            Vector3 pointAwayFromSurface = hit.point + (hit.normal * HitBias);
            CellKey baseCell = _grid.WorldToCell(pointTowardPlayer);
            CellKey adjacentCell = _grid.WorldToCell(pointAwayFromSurface);

            switch (selectedPiece)
            {
                case BuildPieceType.Floor:
                {
                    Vector3 position = _grid.GetCellBottomCenter(adjacentCell);
                    candidate = PlacementCandidate.ForCell(
                        BuildPieceType.Floor,
                        adjacentCell,
                        RampDir.North,
                        position,
                        Quaternion.identity);
                    return true;
                }

                case BuildPieceType.Ramp:
                {
                    Vector3 position = _grid.GetCellBottomCenter(adjacentCell);
                    Quaternion rotation = _grid.GetRampRotation(rampDir);
                    candidate = PlacementCandidate.ForCell(
                        BuildPieceType.Ramp,
                        adjacentCell,
                        rampDir,
                        position,
                        rotation);
                    return true;
                }

                case BuildPieceType.Wall:
                {
                    FaceDir face = ResolveWallFace(hit.normal, preferredWallFace);
                    FaceKey rawFace = new FaceKey(baseCell, face);
                    FaceKey canonical = _grid.Canonicalize(rawFace);

                    Vector3 position = _grid.GetWallCenter(canonical);
                    Quaternion rotation = _grid.GetWallRotation(canonical.Face);
                    candidate = PlacementCandidate.ForFace(canonical, position, rotation);
                    return true;
                }
            }

            return false;
        }

        private static FaceDir ResolveWallFace(Vector3 hitNormal, FaceDir preferredWallFace)
        {
            Vector3 normal = hitNormal.normalized;
            float absX = Mathf.Abs(normal.x);
            float absZ = Mathf.Abs(normal.z);

            bool mostlyHorizontalSurface = Mathf.Abs(normal.y) > Mathf.Max(absX, absZ);
            if (mostlyHorizontalSurface)
            {
                return preferredWallFace;
            }

            if (absX >= absZ)
            {
                return normal.x >= 0f ? FaceDir.East : FaceDir.West;
            }

            return normal.z >= 0f ? FaceDir.North : FaceDir.South;
        }
    }
}
