using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Mesh;
using Xunit;

namespace MGroup.XFEM.Tests.Geometry.DualMesh
{
    public static class DualMesh2DTests
    {
        [Fact]
        public static void TestFindLsmNodesEdgesOfFemElement()
        {
            (DualMesh2D dualMesh, IDualMesh mockMesh) = PrepareMeshes(3);


            for (int femElem = 0; femElem < dualMesh.FemMesh.NumElementsTotal; ++femElem)
            {
                DualMesh2D.Submesh submeshExpected = ((MockMesh1To3)mockMesh).FindLsmNodesEdgesOfFemElement(femElem);
                DualMesh2D.Submesh submeshComputed = dualMesh.FindLsmNodesEdgesOfFemElement(femElem);

                // Check nodes
                Assert.Equal(submeshExpected.LsmNodeIDs.Count, submeshComputed.LsmNodeIDs.Count);
                for (int i = 0; i < submeshExpected.LsmNodeIDs.Count; ++i)
                {
                    Assert.Equal(submeshExpected.LsmNodeIDs[i], submeshComputed.LsmNodeIDs[i]);
                }

                // Check edges
                Assert.Equal(submeshExpected.LsmEdgesToNodes.Count, submeshComputed.LsmEdgesToNodes.Count);
                for (int i = 0; i < submeshExpected.LsmEdgesToNodes.Count; ++i)
                {
                    Assert.Equal(submeshExpected.LsmEdgesToNodes[i].Item1, submeshComputed.LsmEdgesToNodes[i].Item1);
                    Assert.Equal(submeshExpected.LsmEdgesToNodes[i].Item2, submeshComputed.LsmEdgesToNodes[i].Item2);
                }

                //// Check elements
                //Assert.Equal(submeshExpected.LsmElementToEdges.Count, submeshComputed.LsmElementToEdges.Count);
                //for (int i = 0; i < submeshExpected.LsmElementToEdges.Count; ++i)
                //{
                //    int[] expectedArray = submeshExpected.LsmElementToEdges[i];
                //    int[] computedArray = submeshComputed.LsmElementToEdges[i];
                //    Assert.Equal(expectedArray.Length, computedArray.Length);
                //    for (int j = 0; j < expectedArray.Length; ++j)
                //    {
                //        Assert.Equal(expectedArray[j], computedArray[j]);
                //    }
                //}
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapNodeLsmToFem(int multiplicity)
        {
            (DualMesh2D dualMesh, IDualMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int lsmNode = 0; lsmNode < dualMesh.LsmMesh.NumNodesTotal; ++lsmNode)
            {
                int femNodeExpected = mockMesh.MapNodeLsmToFem(lsmNode);
                int femNodeComputed = dualMesh.MapNodeLsmToFem(lsmNode);
                Assert.Equal(femNodeExpected, femNodeComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapNodeFemToLsm(int multiplicity)
        {
            (DualMesh2D dualMesh, IDualMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int femNode = 0; femNode < dualMesh.FemMesh.NumNodesTotal; ++femNode)
            {
                int lsmNodeExpected = mockMesh.MapNodeIDFemToLsm(femNode);
                int lsmNodeComputed = dualMesh.MapNodeIDFemToLsm(femNode);
                Assert.Equal(lsmNodeExpected, lsmNodeComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapElementLsmToFem(int multiplicity)
        {
            (DualMesh2D dualMesh, IDualMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int lsmElem = 0; lsmElem < dualMesh.LsmMesh.NumElementsTotal; ++lsmElem)
            {
                int femElemExpected = mockMesh.MapElementLsmToFem(lsmElem);
                int femElemComputed = dualMesh.MapElementLsmToFem(lsmElem);
                Assert.Equal(femElemExpected, femElemComputed);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public static void TestMapElementFemToLsm(int multiplicity)
        {
            (DualMesh2D dualMesh, IDualMesh mockMesh) = PrepareMeshes(multiplicity);

            for (int femElem = 0; femElem < dualMesh.FemMesh.NumElementsTotal; ++femElem)
            {
                int[] lsmElemExpected = mockMesh.MapElementFemToLsm(femElem);
                int[] lsmElemComputed = dualMesh.MapElementFemToLsm(femElem);
                Assert.Equal(lsmElemExpected.Length, lsmElemComputed.Length);
                for (int i = 0; i < lsmElemExpected.Length; ++i)
                {
                    Assert.Equal(lsmElemExpected[i], lsmElemComputed[i]);
                }
            }
        }

        private static (DualMesh2D dualMesh, IDualMesh mockMesh) PrepareMeshes(int multiplicity)
        {
            var minCoordinates = new double[] { 0, 0 };
            var maxCoordinates = new double[] { 2, 3 };
            var numElementsFem = new int[] { 2, 3 };

            IDualMesh mockMesh;
            DualMesh2D dualMesh;
            if (multiplicity == 1)
            {
                var numElementsLsm = new int[] { 2, 3 };
                mockMesh = new MockMesh1To1();
                dualMesh = new DualMesh2D(minCoordinates, maxCoordinates, numElementsFem, numElementsLsm);
            }
            else if (multiplicity == 3)
            {
                var numElementsLsm = new int[] { 6, 9 };
                mockMesh = new MockMesh1To3();
                dualMesh = new DualMesh2D(minCoordinates, maxCoordinates, numElementsFem, numElementsLsm);
            }
            else
            {
                throw new NotImplementedException();
            }

            return (dualMesh, mockMesh);
        }

        private class MockMesh1To1 : IDualMesh
        {
            public IStructuredMesh FemMesh => throw new NotImplementedException();

            public IStructuredMesh LsmMesh => throw new NotImplementedException();

            public DualMeshPoint CalcShapeFunctions(int femElementID, double[] femNaturalCoords)
            {
                throw new NotImplementedException();
            }

            public int[] MapElementFemToLsm(int femElementID)
            {
                return new int[] { femElementID };
            }

            public int MapElementLsmToFem(int lsmElementID)
            {
                return lsmElementID;
            }

            public int MapNodeIDFemToLsm(int femNodeID)
            {
                return femNodeID;
            }

            public int MapNodeLsmToFem(int lsmNodeID)
            {
                return lsmNodeID;
            }
        }

        private class MockMesh1To3 : IDualMesh
        {
            public IStructuredMesh FemMesh => throw new NotImplementedException();

            public IStructuredMesh LsmMesh => throw new NotImplementedException();

            public DualMeshPoint CalcShapeFunctions(int femElementID, double[] femNaturalCoords)
            {
                throw new NotImplementedException();
            }

            public DualMesh2D.Submesh FindLsmNodesEdgesOfFemElement(int femElementID)
            {
                // Elements to edges
                var elements = new List<int[]>();
                elements.Add(new int[] { 0, 13, 3, 12 });
                elements.Add(new int[] { 0, 14, 3, 13 });
                elements.Add(new int[] { 0, 15, 3, 14 });
                elements.Add(new int[] { 3, 17, 6, 16 });
                elements.Add(new int[] { 3, 18, 6, 17 });
                elements.Add(new int[] { 3, 19, 6, 18 });
                elements.Add(new int[] { 6, 21, 9, 20 });
                elements.Add(new int[] { 6, 22, 9, 21 });
                elements.Add(new int[] { 6, 23, 9, 22 });

                if (femElementID == 0)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        0, 1, 2, 3, 7, 8, 9, 10, 14, 15, 16, 17, 21, 22, 23, 24
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((0, 1)); edges.Add((1, 2)); edges.Add((2, 3));
                    edges.Add((7, 8)); edges.Add((8, 9)); edges.Add((9, 10));
                    edges.Add((14, 15)); edges.Add((15, 16)); edges.Add((16, 17));
                    edges.Add((21, 22)); edges.Add((22, 23)); edges.Add((23, 24));

                    // Vertical
                    edges.Add((0, 7)); edges.Add((1, 8)); edges.Add((2, 9)); edges.Add((3, 10));
                    edges.Add((7, 14)); edges.Add((8, 15)); edges.Add((9, 16)); edges.Add((10, 17));
                    edges.Add((14, 21)); edges.Add((15, 22)); edges.Add((16, 23)); edges.Add((17, 24));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else if (femElementID == 1)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        3, 4, 5, 6, 10, 11, 12, 13, 17, 18, 19, 20, 24, 25, 26, 27
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((3, 4)); edges.Add((4, 5)); edges.Add((5, 6));
                    edges.Add((10, 11)); edges.Add((11, 12)); edges.Add((12, 13));
                    edges.Add((17, 18)); edges.Add((18, 19)); edges.Add((19, 20));
                    edges.Add((24, 25)); edges.Add((25, 26)); edges.Add((26, 27));

                    // Vertical
                    edges.Add((3, 10)); edges.Add((4, 11)); edges.Add((5, 12)); edges.Add((6, 13));
                    edges.Add((10, 17)); edges.Add((11, 18)); edges.Add((12, 19)); edges.Add((13, 20));
                    edges.Add((17, 24)); edges.Add((18, 25)); edges.Add((19, 26)); edges.Add((20, 27));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else if (femElementID == 2)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        21, 22, 23, 24, 28, 29, 30, 31, 35, 36, 37, 38, 42, 43, 44, 45
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((21, 22)); edges.Add((22, 23)); edges.Add((23, 24));
                    edges.Add((28, 29)); edges.Add((29, 30)); edges.Add((30, 31));
                    edges.Add((35, 36)); edges.Add((36, 37)); edges.Add((37, 38));
                    edges.Add((42, 43)); edges.Add((43, 44)); edges.Add((44, 45));

                    // Vertical
                    edges.Add((21, 28)); edges.Add((22, 29)); edges.Add((23, 30)); edges.Add((24, 31));
                    edges.Add((28, 35)); edges.Add((29, 36)); edges.Add((30, 37)); edges.Add((31, 38));
                    edges.Add((35, 42)); edges.Add((36, 43)); edges.Add((37, 44)); edges.Add((38, 45));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else if (femElementID == 3)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        24, 25, 26, 27, 31, 32, 33, 34, 38, 39, 40, 41, 45, 46, 47, 48
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((24, 25)); edges.Add((25, 26)); edges.Add((26, 27));
                    edges.Add((31, 32)); edges.Add((32, 33)); edges.Add((33, 34));
                    edges.Add((38, 39)); edges.Add((39, 40)); edges.Add((40, 41));
                    edges.Add((45, 46)); edges.Add((46, 47)); edges.Add((47, 48));

                    // Vertical
                    edges.Add((24, 31)); edges.Add((25, 32)); edges.Add((26, 33)); edges.Add((27, 34));
                    edges.Add((31, 38)); edges.Add((32, 39)); edges.Add((33, 40)); edges.Add((34, 41));
                    edges.Add((38, 45)); edges.Add((39, 46)); edges.Add((40, 47)); edges.Add((41, 48));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else if (femElementID == 4)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        42, 43, 44, 45, 49, 50, 51, 52, 56, 57, 58, 59, 63, 64, 65, 66
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((42, 43)); edges.Add((43, 44)); edges.Add((44, 45));
                    edges.Add((49, 50)); edges.Add((50, 51)); edges.Add((51, 52));
                    edges.Add((56, 57)); edges.Add((57, 58)); edges.Add((58, 59));
                    edges.Add((63, 64)); edges.Add((64, 65)); edges.Add((65, 66));

                    // Vertical
                    edges.Add((42, 49)); edges.Add((43, 50)); edges.Add((44, 51)); edges.Add((45, 52));
                    edges.Add((49, 56)); edges.Add((50, 57)); edges.Add((51, 58)); edges.Add((52, 59));
                    edges.Add((56, 63)); edges.Add((57, 64)); edges.Add((58, 65)); edges.Add((59, 66));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else if (femElementID == 5)
                {
                    var lsmNodes = new List<int>(new int[]
                    {
                        45, 46, 47, 48, 52, 53, 54, 55, 59, 60, 61, 62, 66, 67, 68, 69
                    });

                    var edges = new List<(int, int)>();

                    //Horizontal
                    edges.Add((45, 46)); edges.Add((46, 47)); edges.Add((47, 48));
                    edges.Add((52, 53)); edges.Add((53, 54)); edges.Add((54, 55));
                    edges.Add((59, 60)); edges.Add((60, 61)); edges.Add((61, 62));
                    edges.Add((66, 67)); edges.Add((67, 68)); edges.Add((68, 69));

                    // Vertical
                    edges.Add((45, 52)); edges.Add((46, 53)); edges.Add((47, 54)); edges.Add((48, 55));
                    edges.Add((52, 59)); edges.Add((53, 60)); edges.Add((54, 61)); edges.Add((55, 62));
                    edges.Add((59, 66)); edges.Add((60, 67)); edges.Add((61, 68)); edges.Add((62, 69));

                    return new DualMesh2D.Submesh(lsmNodes, edges, elements);
                }
                else throw new IndexOutOfRangeException();
            }

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

            public int MapNodeIDFemToLsm(int femNodeID)
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
