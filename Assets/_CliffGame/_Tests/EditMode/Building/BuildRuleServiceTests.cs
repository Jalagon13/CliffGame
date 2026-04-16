using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CliffGame.Tests
{
    public class BuildRuleServiceTests
    {
        [Test]
        public void Occupancy_AllowsFloorAndRampInSameCell_ButRejectsDuplicates()
        {
            BuildGridService grid = new BuildGridService(Vector3.zero, 1f);
            Dictionary<CellKey, CellRecord> cells = new();
            Dictionary<FaceKey, GameObject> walls = new();
            BuildRuleService rules = new BuildRuleService(
                grid,
                cells,
                walls,
                collisionMask: 0,
                floorSize: new Vector3(1f, 0.2f, 1f),
                wallSize: new Vector3(0.2f, 1f, 1f),
                rampSize: new Vector3(1f, 0.2f, 1.4f));

            CellKey cell = new CellKey(0, 0, 0);
            PlacementCandidate floorCandidate = PlacementCandidate.ForCell(BuildPieceType.Floor, cell, RampDir.North, Vector3.zero, Quaternion.identity);
            PlacementCandidate rampCandidate = PlacementCandidate.ForCell(BuildPieceType.Ramp, cell, RampDir.North, Vector3.zero, Quaternion.identity);

            Assert.IsTrue(rules.Validate(floorCandidate, checkRange: false, inRange: true).IsValid);

            cells[cell] = new CellRecord { FloorPiece = new GameObject("Floor") };
            Assert.IsTrue(rules.Validate(rampCandidate, checkRange: false, inRange: true).IsValid);
            Assert.IsFalse(rules.Validate(floorCandidate, checkRange: false, inRange: true).IsValid);

            Object.DestroyImmediate(cells[cell].FloorPiece);
        }

        [Test]
        public void Wall_RequiresAdjacentFloor()
        {
            BuildGridService grid = new BuildGridService(Vector3.zero, 1f);
            Dictionary<CellKey, CellRecord> cells = new();
            Dictionary<FaceKey, GameObject> walls = new();
            BuildRuleService rules = new BuildRuleService(
                grid,
                cells,
                walls,
                collisionMask: 0,
                floorSize: new Vector3(1f, 0.2f, 1f),
                wallSize: new Vector3(0.2f, 1f, 1f),
                rampSize: new Vector3(1f, 0.2f, 1.4f));

            FaceKey face = grid.Canonicalize(new FaceKey(new CellKey(0, 0, 0), FaceDir.North));
            PlacementCandidate wallCandidate = PlacementCandidate.ForFace(face, Vector3.zero, Quaternion.identity);

            PlacementValidationResult noFloorResult = rules.Validate(wallCandidate, checkRange: false, inRange: true);
            Assert.IsFalse(noFloorResult.IsValid);
            Assert.AreEqual(PlacementInvalidReason.NeedsAdjacentFloor, noFloorResult.InvalidReason);

            cells[new CellKey(0, 0, 0)] = new CellRecord { FloorPiece = new GameObject("Floor") };
            PlacementValidationResult withFloorResult = rules.Validate(wallCandidate, checkRange: false, inRange: true);
            Assert.IsTrue(withFloorResult.IsValid);

            Object.DestroyImmediate(cells[new CellKey(0, 0, 0)].FloorPiece);
        }
    }
}
