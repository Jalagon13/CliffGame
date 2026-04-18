using NUnit.Framework;
using UnityEngine;

namespace CliffGame.Tests
{
    public class ConnectorTargetingTests
    {
        [Test]
        public void Connector_WorldFace_FollowsParentRotation()
        {
            GameObject root = new GameObject("Root");
            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject connectorObject = new GameObject("Connector");
            connectorObject.transform.SetParent(root.transform, false);
            Connector connector = connectorObject.AddComponent<Connector>();

            FaceDir worldFace = connector.ResolveWorldFace();
            Assert.AreEqual(FaceDir.East, worldFace);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ConnectorTargeting_PicksNearestCandidate()
        {
            BuildGridService grid = new BuildGridService(Vector3.zero, 1f);
            BuildTargetingService targeting = new BuildTargetingService(grid);

            GameObject nearRoot = new GameObject("NearRoot");
            nearRoot.transform.position = new Vector3(0.1f, 0f, 0f);
            PlacedBuildPiece nearPlaced = nearRoot.AddComponent<PlacedBuildPiece>();
            nearPlaced.Initialize(BuildPieceType.Floor, new CellKey(0, 0, 0), false, default);
            GameObject nearConnectorObject = new GameObject("NearConnector");
            nearConnectorObject.transform.SetParent(nearRoot.transform, false);
            SphereCollider nearCollider = nearConnectorObject.AddComponent<SphereCollider>();
            nearCollider.isTrigger = true;
            nearConnectorObject.AddComponent<Connector>();

            GameObject farRoot = new GameObject("FarRoot");
            farRoot.transform.position = new Vector3(0.45f, 0f, 0f);
            PlacedBuildPiece farPlaced = farRoot.AddComponent<PlacedBuildPiece>();
            farPlaced.Initialize(BuildPieceType.Floor, new CellKey(6, 0, 0), false, default);
            GameObject farConnectorObject = new GameObject("FarConnector");
            farConnectorObject.transform.SetParent(farRoot.transform, false);
            SphereCollider farCollider = farConnectorObject.AddComponent<SphereCollider>();
            farCollider.isTrigger = true;
            farConnectorObject.AddComponent<Connector>();

            Physics.SyncTransforms();

            bool found = targeting.TryGetCandidateFromConnector(
                BuildPieceType.Floor,
                hitPoint: Vector3.zero,
                connectorMask: 1 << 0,
                connectorSearchRadius: 1f,
                wallAttachMode: WallAttachMode.Perpendicular,
                isCandidatePreferred: null,
                out PlacementCandidate candidate);

            Assert.IsTrue(found);
            Assert.AreEqual(new CellKey(0, 0, 1), candidate.Cell);

            Object.DestroyImmediate(nearRoot);
            Object.DestroyImmediate(farRoot);
        }

        [Test]
        public void ConnectorTargeting_WallCandidate_IsCanonicalized()
        {
            BuildGridService grid = new BuildGridService(Vector3.zero, 1f);
            BuildTargetingService targeting = new BuildTargetingService(grid);

            GameObject root = new GameObject("WallAnchorRoot");
            root.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            PlacedBuildPiece placed = root.AddComponent<PlacedBuildPiece>();
            placed.Initialize(BuildPieceType.Wall, default, true, new FaceKey(new CellKey(1, 0, 0), FaceDir.East));

            GameObject connectorObject = new GameObject("Connector");
            connectorObject.transform.SetParent(root.transform, false);
            SphereCollider collider = connectorObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            connectorObject.AddComponent<Connector>();

            Physics.SyncTransforms();

            bool found = targeting.TryGetCandidateFromConnector(
                BuildPieceType.Wall,
                hitPoint: root.transform.position,
                connectorMask: 1 << 0,
                connectorSearchRadius: 1f,
                wallAttachMode: WallAttachMode.Perpendicular,
                isCandidatePreferred: _ => true,
                out PlacementCandidate candidate);

            Assert.IsTrue(found);
            Assert.IsTrue(candidate.HasFace);
            Assert.AreEqual(FaceDir.East, candidate.Face.Face);
            Assert.AreEqual(new CellKey(0, 0, 0), candidate.Face.Owner);

            Object.DestroyImmediate(root);
        }
    }
}
