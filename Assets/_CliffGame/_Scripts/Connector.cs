using UnityEngine;

namespace CliffGame
{
    public class Connector : MonoBehaviour
    {
        [SerializeField] private FaceDir _connectorFace = FaceDir.North;
        [SerializeField] private BuildPieceMask _allowedPieces = BuildPieceMask.Floor | BuildPieceMask.Wall;
        [SerializeField] private float _axisDominanceThreshold = 0.35f;

        public FaceDir ConnectorFace => _connectorFace;
        public BuildPieceMask AllowedPieces => _allowedPieces;

        public bool Allows(BuildPieceType pieceType)
        {
            BuildPieceMask flag = pieceType switch
            {
                BuildPieceType.Floor => BuildPieceMask.Floor,
                BuildPieceType.Wall => BuildPieceMask.Wall,
                BuildPieceType.Ramp => BuildPieceMask.Ramp,
                _ => BuildPieceMask.None,
            };

            return (_allowedPieces & flag) != 0;
        }

        public FaceDir ResolveWorldFace(Transform pieceTransform = null)
        {
            Vector3 localDirection = FaceToLocalDirection(_connectorFace);
            Transform basis = pieceTransform != null ? pieceTransform : transform;
            Vector3 worldDirection = basis.TransformDirection(localDirection);
            return WorldDirectionToFace(worldDirection, _axisDominanceThreshold);
        }

        private static Vector3 FaceToLocalDirection(FaceDir face)
        {
            return face switch
            {
                FaceDir.North => Vector3.forward,
                FaceDir.East => Vector3.right,
                FaceDir.South => Vector3.back,
                FaceDir.West => Vector3.left,
                FaceDir.Up => Vector3.up,
                FaceDir.Down => Vector3.down,
                _ => Vector3.forward,
            };
        }

        private static FaceDir WorldDirectionToFace(Vector3 direction, float dominanceThreshold)
        {
            Vector3 normalized = direction.normalized;
            float absX = Mathf.Abs(normalized.x);
            float absY = Mathf.Abs(normalized.y);
            float absZ = Mathf.Abs(normalized.z);
            float largest = Mathf.Max(absX, Mathf.Max(absY, absZ));

            if (largest < Mathf.Max(0.01f, dominanceThreshold))
            {
                return FaceDir.North;
            }

            if (absY >= absX && absY >= absZ)
            {
                return normalized.y >= 0f ? FaceDir.Up : FaceDir.Down;
            }

            if (absX >= absZ)
            {
                return normalized.x >= 0f ? FaceDir.East : FaceDir.West;
            }

            return normalized.z >= 0f ? FaceDir.North : FaceDir.South;
        }
    }
}
