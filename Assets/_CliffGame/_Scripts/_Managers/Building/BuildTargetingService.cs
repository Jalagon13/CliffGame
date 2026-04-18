using System;
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

        public bool TryGetCandidateFromConnector(
            BuildPieceType selectedPiece,
            Vector3 hitPoint,
            LayerMask connectorMask,
            float connectorSearchRadius,
            Func<PlacementCandidate, bool> isCandidatePreferred,
            out PlacementCandidate candidate)
        {
            candidate = default;

            if (selectedPiece != BuildPieceType.Floor && selectedPiece != BuildPieceType.Wall)
            {
                return false;
            }

            Collider[] overlaps = Physics.OverlapSphere(
                hitPoint,
                Mathf.Max(0.01f, connectorSearchRadius),
                connectorMask,
                QueryTriggerInteraction.Collide);

            float bestPreferredSqrDistance = float.MaxValue;
            float bestAnySqrDistance = float.MaxValue;
            bool foundPreferred = false;
            bool foundAny = false;
            PlacementCandidate bestAnyCandidate = default;
            PlacementCandidate bestPreferredCandidate = default;

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider connectorCollider = overlaps[i];
                if (connectorCollider == null)
                {
                    continue;
                }

                Connector connector = connectorCollider.GetComponent<Connector>();
                if (connector == null)
                {
                    connector = connectorCollider.GetComponentInParent<Connector>();
                }

                if (connector == null || !connector.Allows(selectedPiece))
                {
                    continue;
                }

                PlacedBuildPiece placed = connector.GetComponentInParent<PlacedBuildPiece>();
                if (placed == null)
                {
                    continue;
                }

                if (!TryBuildCandidateFromConnector(selectedPiece, connector, placed, out PlacementCandidate probe))
                {
                    continue;
                }

                float sqrDistance = (connectorCollider.transform.position - hitPoint).sqrMagnitude;
                bool preferred = isCandidatePreferred != null && isCandidatePreferred(probe);

                if (preferred)
                {
                    if (!foundPreferred || sqrDistance < bestPreferredSqrDistance)
                    {
                        bestPreferredSqrDistance = sqrDistance;
                        bestPreferredCandidate = probe;
                        foundPreferred = true;
                    }

                    continue;
                }

                if (!foundAny || sqrDistance < bestAnySqrDistance)
                {
                    bestAnySqrDistance = sqrDistance;
                    bestAnyCandidate = probe;
                    foundAny = true;
                }
            }

            if (foundPreferred)
            {
                candidate = bestPreferredCandidate;
                return true;
            }

            if (foundAny)
            {
                candidate = bestAnyCandidate;
                return true;
            }

            return false;
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

        private bool TryBuildCandidateFromConnector(
            BuildPieceType selectedPiece,
            Connector connector,
            PlacedBuildPiece anchorPiece,
            out PlacementCandidate candidate)
        {
            candidate = default;
            CellKey anchorCell = anchorPiece.GetAnchorCell();
            FaceDir worldFace = connector.ResolveWorldFace(anchorPiece.transform);

            if (selectedPiece == BuildPieceType.Floor)
            {
                CellKey targetCell = _grid.GetNeighborCell(anchorCell, worldFace);
                Vector3 position = _grid.GetCellBottomCenter(targetCell);
                candidate = PlacementCandidate.ForCell(
                    BuildPieceType.Floor,
                    targetCell,
                    RampDir.North,
                    position,
                    Quaternion.identity);
                return true;
            }

            if (selectedPiece == BuildPieceType.Wall)
            {
                if (worldFace == FaceDir.Up || worldFace == FaceDir.Down)
                {
                    if (!anchorPiece.HasFace)
                    {
                        return false;
                    }

                    int yOffset = worldFace == FaceDir.Up ? 1 : -1;
                    FaceKey stacked = anchorPiece.Face;
                    stacked.Owner = new CellKey(stacked.Owner.X, stacked.Owner.Y + yOffset, stacked.Owner.Z);
                    FaceKey canonicalStacked = _grid.Canonicalize(stacked);
                    Vector3 stackedPosition = _grid.GetWallCenter(canonicalStacked);
                    Quaternion stackedRotation = _grid.GetWallRotation(canonicalStacked.Face);
                    candidate = PlacementCandidate.ForFace(canonicalStacked, stackedPosition, stackedRotation);
                    return true;
                }

                FaceKey canonicalFace;
                if (anchorPiece.HasFace && IsHorizontalFace(worldFace))
                {
                    // Keep the same wall orientation, move the segment sideways.
                    FaceDir anchorFace = anchorPiece.Face.Face;
                    CellKey shiftedOwner = _grid.GetNeighborCell(anchorPiece.Face.Owner, worldFace);
                    canonicalFace = _grid.CanonicalFaceForCell(shiftedOwner, anchorFace);
                }
                else
                {
                    canonicalFace = _grid.CanonicalFaceForCell(anchorCell, worldFace);
                }

                Vector3 position = _grid.GetWallCenter(canonicalFace);
                Quaternion rotation = _grid.GetWallRotation(canonicalFace.Face);
                candidate = PlacementCandidate.ForFace(canonicalFace, position, rotation);
                return true;
            }

            return false;
        }

        private static bool IsHorizontalFace(FaceDir face)
        {
            return face == FaceDir.North || face == FaceDir.East || face == FaceDir.South || face == FaceDir.West;
        }
    }
}
