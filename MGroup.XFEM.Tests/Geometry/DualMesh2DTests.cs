using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Mesh;
using Xunit;

namespace MGroup.XFEM.Tests.Geometry
{
    public static class DualMesh2DTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapNodeLsmToFem(int multiplicity)
        {
            (DualMesh2D lsmMesh, ILsmMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int lsmNode = 0; lsmNode < lsmMesh.LsmMesh.NumNodesTotal; ++lsmNode)
            {
                int femNodeExpected = mockMesh.MapNodeLsmToFem(lsmNode);
                int femNodeComputed = lsmMesh.MapNodeLsmToFem(lsmNode);
                Assert.Equal(femNodeExpected, femNodeComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapNodeFemToLsm(int multiplicity)
        {
            (DualMesh2D lsmMesh, ILsmMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int femNode = 0; femNode < lsmMesh.FemMesh.NumNodesTotal; ++femNode)
            {
                int lsmNodeExpected = mockMesh.MapNodeFemToLsm(femNode);
                int lsmNodeComputed = lsmMesh.MapNodeFemToLsm(femNode);
                Assert.Equal(lsmNodeExpected, lsmNodeComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapElementLsmToFem(int multiplicity)
        {
            (DualMesh2D lsmMesh, ILsmMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int lsmElem = 0; lsmElem < lsmMesh.LsmMesh.NumElementsTotal; ++lsmElem)
            {
                int femElemExpected = mockMesh.MapElementLsmToFem(lsmElem);
                int femElemComputed = lsmMesh.MapElementLsmToFem(lsmElem);
                Assert.Equal(femElemExpected, femElemComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapElementFemToLsm(int multiplicity)
        {
            (DualMesh2D lsmMesh, ILsmMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int femElem = 0; femElem < lsmMesh.FemMesh.NumElementsTotal; ++femElem)
            {
                int[] lsmElemExpected = mockMesh.MapElementFemToLsm(femElem);
                int[] lsmElemComputed = lsmMesh.MapElementFemToLsm(femElem);
                Assert.Equal(lsmElemExpected.Length, lsmElemComputed.Length);
                for (int i = 0; i < lsmElemExpected.Length; ++i)
                {
                    Assert.Equal(lsmElemExpected[i], lsmElemComputed[i]);
                }
            }
        }

        private static (DualMesh2D lsmMesh, ILsmMesh mockMesh) PrepareMeshes(int multiplicity)
        {
            var minCoordinates = new double[] { 0, 0 };
            var maxCoordinates = new double[] { 2, 3 };
            var numElementsFem = new int[] { 2, 3 };

            ILsmMesh mockMesh;
            DualMesh2D lsmMesh;
            if (multiplicity == 1)
            {
                var numElementsLsm = new int[] { 2, 3 };
                mockMesh = new MockMesh1To1();
                lsmMesh = new DualMesh2D(minCoordinates, maxCoordinates, numElementsFem, numElementsLsm);
            }
            else if (multiplicity == 3)
            {
                var numElementsLsm = new int[] { 6, 9 };
                mockMesh = new MockMesh1To3();
                lsmMesh = new DualMesh2D(minCoordinates, maxCoordinates, numElementsFem, numElementsLsm);
            }
            else
            {
                throw new NotImplementedException();
            }

            return (lsmMesh, mockMesh);
        }

        private class MockMesh1To1 : ILsmMesh
        {
            public IStructuredMesh FemMesh => throw new NotImplementedException();

            public IStructuredMesh LsmMesh => throw new NotImplementedException();

            public int[] MapElementFemToLsm(int femElementID)
            {
                return new int[] { femElementID };
            }

            public int MapElementLsmToFem(int lsmElementID)
            {
                return lsmElementID;
            }

            public int MapNodeFemToLsm(int femNodeID)
            {
                return femNodeID;
            }

            public int MapNodeLsmToFem(int lsmNodeID)
            {
                return lsmNodeID;
            }
        }

        private class MockMesh1To3 : ILsmMesh
        {
            public IStructuredMesh FemMesh => throw new NotImplementedException();

            public IStructuredMesh LsmMesh => throw new NotImplementedException();

            public int[] MapElementFemToLsm(int femElementID)
            {
                var femToLsmElements = new int[6][];
                femToLsmElements[0] = new int[] { 0, 1, 2, 6, 7, 8, 12, 13, 14 };
                femToLsmElements[1] = new int[] { 3, 4, 5, 9, 10, 11, 15, 16, 17 };
                femToLsmElements[2] = new int[] { 18, 19, 20, 24, 25, 26, 30, 31, 32 };
                femToLsmElements[3] = new int[] { 21, 22, 23, 27, 28, 29, 33, 34, 35 };
                femToLsmElements[4] = new int[] { 36, 37, 38, 42, 43, 44, 48, 49, 50 };
                femToLsmElements[5] = new int[] { 39, 40, 41, 45, 46, 47, 51, 52, 53 };
                return femToLsmElements[femElementID];
            }

            public int MapElementLsmToFem(int lsmElementID)
            {
                var lsmToFemElements = new int[54];

                lsmToFemElements[0] = 0;
                lsmToFemElements[1] = 0;
                lsmToFemElements[2] = 0;
                lsmToFemElements[6] = 0;
                lsmToFemElements[7] = 0;
                lsmToFemElements[8] = 0;
                lsmToFemElements[12] = 0;
                lsmToFemElements[32] = 0;
                lsmToFemElements[14] = 0;

                lsmToFemElements[3] = 1;
                lsmToFemElements[4] = 1;
                lsmToFemElements[5] = 1;
                lsmToFemElements[9] = 1;
                lsmToFemElements[10] = 1;
                lsmToFemElements[11] = 1;
                lsmToFemElements[15] = 1;
                lsmToFemElements[16] = 1;
                lsmToFemElements[17] = 1;

                lsmToFemElements[18] = 2;
                lsmToFemElements[19] = 2;
                lsmToFemElements[20] = 2;
                lsmToFemElements[24] = 2;
                lsmToFemElements[25] = 2;
                lsmToFemElements[26] = 2;
                lsmToFemElements[30] = 2;
                lsmToFemElements[31] = 2;
                lsmToFemElements[32] = 2;

                lsmToFemElements[21] = 3;
                lsmToFemElements[22] = 3;
                lsmToFemElements[23] = 3;
                lsmToFemElements[27] = 3;
                lsmToFemElements[28] = 3;
                lsmToFemElements[29] = 3;
                lsmToFemElements[33] = 3;
                lsmToFemElements[34] = 3;
                lsmToFemElements[35] = 3;

                lsmToFemElements[36] = 4;
                lsmToFemElements[37] = 4;
                lsmToFemElements[38] = 4;
                lsmToFemElements[42] = 4;
                lsmToFemElements[43] = 4;
                lsmToFemElements[44] = 4;
                lsmToFemElements[48] = 4;
                lsmToFemElements[49] = 4;
                lsmToFemElements[50] = 4;

                lsmToFemElements[39] = 5;
                lsmToFemElements[40] = 5;
                lsmToFemElements[41] = 5;
                lsmToFemElements[45] = 5;
                lsmToFemElements[46] = 5;
                lsmToFemElements[47] = 5;
                lsmToFemElements[51] = 5;
                lsmToFemElements[52] = 5;
                lsmToFemElements[53] = 5;

                return lsmToFemElements[lsmElementID];
            }

            public int MapNodeFemToLsm(int femNodeID)
            {
                var femToLsmNodes = new int[12];
                femToLsmNodes[0] = 0;
                femToLsmNodes[1] = 3;
                femToLsmNodes[2] = 6;
                femToLsmNodes[3] = 21;
                femToLsmNodes[4] = 24;
                femToLsmNodes[5] = 27;
                femToLsmNodes[6] = 42;
                femToLsmNodes[7] = 45;
                femToLsmNodes[8] = 48;
                femToLsmNodes[9] = 63;
                femToLsmNodes[10] = 66;
                femToLsmNodes[11] = 69;

                return femToLsmNodes[femNodeID];
            }

            public int MapNodeLsmToFem(int lsmNodeID)
            {
                var lsmToFemNodes = new int[70];
                for (int i = 0; i < lsmToFemNodes.Length; ++i) lsmToFemNodes[i] = -1;

                lsmToFemNodes[0] = 0;
                lsmToFemNodes[3] = 1;
                lsmToFemNodes[6] = 2;
                lsmToFemNodes[21] = 3;
                lsmToFemNodes[24] = 4;
                lsmToFemNodes[27] = 5;
                lsmToFemNodes[42] = 6;
                lsmToFemNodes[45] = 7;
                lsmToFemNodes[48] = 8;
                lsmToFemNodes[63] = 9;
                lsmToFemNodes[66] = 10;
                lsmToFemNodes[69] = 11;

                return lsmToFemNodes[lsmNodeID];

            }
        }
    }
}
