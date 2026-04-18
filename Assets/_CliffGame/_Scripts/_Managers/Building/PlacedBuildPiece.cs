using UnityEngine;

namespace CliffGame
{
    public class PlacedBuildPiece : MonoBehaviour
    {
        [SerializeField] private BuildPieceType _pieceType;
        [SerializeField] private CellKey _cell;
        [SerializeField] private bool _hasFace;
        [SerializeField] private FaceKey _face;

        public BuildPieceType PieceType => _pieceType;
        public CellKey Cell => _cell;
        public bool HasFace => _hasFace;
        public FaceKey Face => _face;

        public void Initialize(BuildPieceType pieceType, CellKey cell, bool hasFace, FaceKey face)
        {
            _pieceType = pieceType;
            _cell = cell;
            _hasFace = hasFace;
            _face = face;
        }

        public CellKey GetAnchorCell()
        {
            return _hasFace ? _face.Owner : _cell;
        }
    }
}
