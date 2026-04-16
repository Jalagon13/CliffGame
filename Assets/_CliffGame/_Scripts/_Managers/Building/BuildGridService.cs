using UnityEngine;

namespace CliffGame
{
    public class BuildGridService
    {
        private readonly Vector3 _origin;
        private readonly float _cellSize;

        public BuildGridService(Vector3 origin, float cellSize)
        {
            _origin = origin;
            _cellSize = Mathf.Max(0.01f, cellSize);
        }

        public CellKey WorldToCell(Vector3 worldPosition)
        {
            Vector3 local = (worldPosition - _origin) / _cellSize;
            return new CellKey(
                Mathf.FloorToInt(local.x),
                Mathf.FloorToInt(local.y),
                Mathf.FloorToInt(local.z));
        }

        public Vector3 CellMinToWorld(CellKey cell)
        {
            return _origin + new Vector3(cell.X * _cellSize, cell.Y * _cellSize, cell.Z * _cellSize);
        }

        public Vector3 GetCellBottomCenter(CellKey cell)
        {
            Vector3 min = CellMinToWorld(cell);
            return min + new Vector3(_cellSize * 0.5f, 0f, _cellSize * 0.5f);
        }

        public Vector3 GetCellCenter(CellKey cell)
        {
            Vector3 min = CellMinToWorld(cell);
            return min + new Vector3(_cellSize * 0.5f, _cellSize * 0.5f, _cellSize * 0.5f);
        }

        public FaceKey Canonicalize(FaceKey key)
        {
            return key.Face switch
            {
                FaceDir.West => new FaceKey(new CellKey(key.Owner.X - 1, key.Owner.Y, key.Owner.Z), FaceDir.East),
                FaceDir.South => new FaceKey(new CellKey(key.Owner.X, key.Owner.Y, key.Owner.Z - 1), FaceDir.North),
                _ => key,
            };
        }

        public Vector3 FaceToNormal(FaceDir faceDir)
        {
            return faceDir switch
            {
                FaceDir.North => Vector3.forward,
                FaceDir.East => Vector3.right,
                FaceDir.South => Vector3.back,
                _ => Vector3.left,
            };
        }

        public Quaternion GetWallRotation(FaceDir faceDir)
        {
            float y = faceDir switch
            {
                FaceDir.North => 0f,
                FaceDir.East => 90f,
                FaceDir.South => 180f,
                _ => 270f,
            };
            return Quaternion.Euler(0f, y, 0f);
        }

        public Quaternion GetRampRotation(RampDir rampDir)
        {
            float y = rampDir switch
            {
                RampDir.North => 0f,
                RampDir.East => 90f,
                RampDir.South => 180f,
                _ => 270f,
            };
            return Quaternion.Euler(0f, y, 0f);
        }

        public Vector3 GetWallCenter(FaceKey canonicalFaceKey)
        {
            FaceKey key = Canonicalize(canonicalFaceKey);
            Vector3 min = CellMinToWorld(key.Owner);
            return key.Face switch
            {
                FaceDir.North => min + new Vector3(_cellSize * 0.5f, _cellSize * 0.5f, _cellSize),
                FaceDir.East => min + new Vector3(_cellSize, _cellSize * 0.5f, _cellSize * 0.5f),
                FaceDir.South => min + new Vector3(_cellSize * 0.5f, _cellSize * 0.5f, 0f),
                _ => min + new Vector3(0f, _cellSize * 0.5f, _cellSize * 0.5f),
            };
        }

        public void GetWallAdjacentCells(FaceKey canonicalFaceKey, out CellKey a, out CellKey b)
        {
            FaceKey key = Canonicalize(canonicalFaceKey);
            a = key.Owner;

            b = key.Face switch
            {
                FaceDir.North => new CellKey(key.Owner.X, key.Owner.Y, key.Owner.Z + 1),
                FaceDir.East => new CellKey(key.Owner.X + 1, key.Owner.Y, key.Owner.Z),
                FaceDir.South => new CellKey(key.Owner.X, key.Owner.Y, key.Owner.Z - 1),
                _ => new CellKey(key.Owner.X - 1, key.Owner.Y, key.Owner.Z),
            };
        }
    }
}
