using System;
using UnityEngine;

namespace CliffGame
{
    public enum BuildPieceType
    {
        Floor = 0,
        Wall = 1,
        Ramp = 2,
    }

    public enum FaceDir
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
        Up = 4,
        Down = 5,
    }

    public enum RampDir
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public enum PlacementInvalidReason
    {
        None = 0,
        OutOfRange = 1,
        NoTarget = 2,
        Occupied = 3,
        Unsupported = 4,
        NeedsAdjacentFloor = 5,
        BlockedByCollision = 6,
    }

    [Serializable]
    public struct CellKey : IEquatable<CellKey>
    {
        public int X;
        public int Y;
        public int Z;

        public CellKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(CellKey other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is CellKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static CellKey operator +(CellKey a, CellKey b)
        {
            return new CellKey(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
    }

    [Serializable]
    public struct FaceKey : IEquatable<FaceKey>
    {
        public CellKey Owner;
        public FaceDir Face;

        public FaceKey(CellKey owner, FaceDir face)
        {
            Owner = owner;
            Face = face;
        }

        public bool Equals(FaceKey other)
        {
            return Owner.Equals(other.Owner) && Face == other.Face;
        }

        public override bool Equals(object obj)
        {
            return obj is FaceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Owner, (int)Face);
        }
    }

    public struct CellRecord
    {
        public GameObject FloorPiece;
        public GameObject RampPiece;
        public RampDir RampDirection;

        public bool HasFloor => FloorPiece != null;
        public bool HasRamp => RampPiece != null;
    }

    [Serializable]
    public struct PlacementCandidate
    {
        public BuildPieceType PieceType;
        public CellKey Cell;
        public FaceKey Face;
        public bool HasFace;
        public RampDir RampDirection;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;

        public static PlacementCandidate ForCell(
            BuildPieceType pieceType,
            CellKey cell,
            RampDir rampDirection,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            return new PlacementCandidate
            {
                PieceType = pieceType,
                Cell = cell,
                Face = default,
                HasFace = false,
                RampDirection = rampDirection,
                WorldPosition = worldPosition,
                WorldRotation = worldRotation,
            };
        }

        public static PlacementCandidate ForFace(
            FaceKey face,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            return new PlacementCandidate
            {
                PieceType = BuildPieceType.Wall,
                Cell = default,
                Face = face,
                HasFace = true,
                RampDirection = RampDir.North,
                WorldPosition = worldPosition,
                WorldRotation = worldRotation,
            };
        }
    }

    public readonly struct PlacementValidationResult
    {
        public bool IsValid { get; }
        public PlacementInvalidReason InvalidReason { get; }

        public PlacementValidationResult(bool isValid, PlacementInvalidReason invalidReason)
        {
            IsValid = isValid;
            InvalidReason = invalidReason;
        }

        public static PlacementValidationResult Valid()
        {
            return new PlacementValidationResult(true, PlacementInvalidReason.None);
        }

        public static PlacementValidationResult Invalid(PlacementInvalidReason reason)
        {
            return new PlacementValidationResult(false, reason);
        }
    }

    [Serializable]
    public struct PlacedPieceRecord
    {
        public BuildPieceType PieceType;
        public CellKey Cell;
        public FaceKey Face;
        public bool HasFace;
        public RampDir RampDirection;
    }
}
