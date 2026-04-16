using NUnit.Framework;
using UnityEngine;

namespace CliffGame.Tests
{
    public class BuildGridServiceTests
    {
        [Test]
        public void WorldToCell_CellToWorld_RoundTripsAtCellCenter()
        {
            BuildGridService grid = new BuildGridService(new Vector3(2f, 1f, -3f), 1f);
            CellKey input = new CellKey(4, 6, -2);

            Vector3 min = grid.CellMinToWorld(input);
            Vector3 center = min + new Vector3(0.5f, 0.5f, 0.5f);

            CellKey output = grid.WorldToCell(center);
            Assert.AreEqual(input, output);
        }

        [Test]
        public void CanonicalizeFace_MapsSharedBoundaryToSingleKey()
        {
            BuildGridService grid = new BuildGridService(Vector3.zero, 1f);

            FaceKey east = grid.Canonicalize(new FaceKey(new CellKey(0, 0, 0), FaceDir.East));
            FaceKey westNeighbor = grid.Canonicalize(new FaceKey(new CellKey(1, 0, 0), FaceDir.West));

            Assert.AreEqual(east, westNeighbor);
        }
    }
}
