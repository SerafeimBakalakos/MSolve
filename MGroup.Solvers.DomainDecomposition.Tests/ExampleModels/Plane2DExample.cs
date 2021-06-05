using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Materials;
using MGroup.Environments;
using MGroup.FEM.Entities;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using Xunit;

// Global
// 72--73--74--75--76--77--78--79--80
//  |   |   |   |   |   |   |   |   |
// 63--64--65--66--67--68--69--70--71
//  |   |   |   |   |   |   |   |   |
// 54--55--56--57--58--59--60--61--62
//  |   |   |   |   |   |   |   |   |
// 45--46--47--48--49--50--51--52--53
//  |   |   |   |   |   |   |   |   |
// 36--37--38--39--40--41--42--43--44
//  |   |   |   |   |   |   |   |   |
// 27--28--29--30--31--32--33--34--35
//  |   |   |   |   |   |   |   |   |
// 18--19--20--21--22--23--24--25--26
//  |   |   |   |   |   |   |   |   |
// 09--10--11--12--13--14--15--16--17
//  |   |   |   |   |   |   |   |   |
// 00--01--02--03--04--05--06--07--08
//
// Boundary Conditions: Ux(0)=Uy(0)=Uy(8)=0, Px(80)=100
//
// ********************************************************
//
// Subdomains:
// 72--73--74    74--75--76    76--77--78    78--79--80
//  | 56| 57|     | 58| 59|     | 60| 61|     | 62| 63|
// 63--64--65    65--66--67    67--68--69    69--70--71
//  | 48| 49|     | 50| 51|     | 52| 53|     | 54| 55|
// 54--55--56    56--57--58    58--59--60    60--61--62
// s12           s13           s14           s15
//
// 54--55--56    56--57--58    58--59--60    60--61--62
//  | 40| 41|     | 42| 43|     | 44| 45|     | 46| 47|
// 45--46--47    47--48--49    49--50--51    51--52--53
//  | 32| 33|     | 34| 35|     | 36| 37|     | 38| 39|
// 36--37--38    38--39--40    40--41--42    42--43--44
// s8            s9            s10           s11
//
// 36--37--38    38--39--40    40--41--42    42--43--44
//  | 24| 25|     | 26| 27|     | 28| 29|     | 30| 31|
// 27--28--29    29--30--31    31--32--33    33--34--35
//  | 16| 17|     | 18| 19|     | 20| 21|     | 22| 23|
// 18--19--20    20--21--22    22--23--24    24--25--26
// s4            s5            s6            s7
//
// 18--19--20    20--21--22    22--23--24    24--25--26
//  | 8 | 9 |     | 10| 11|     | 12| 13|     | 14| 15|
// 09--10--11    11--12--13    13--14--15    15--16--17
//  | 0 | 1 |     | 2 | 3 |     | 4 | 5 |     | 6 | 7 |
// 00--01--02    02--03--04    04--05--06    06--07--08
// s0            s1            s2            s3
//
// ********************************************************
//
// Clusters:
// +---+---+    +---+---+
// |s12|s13|    |s14|s15|
// +---+---+    +---+---+
// | s8| s9|    |s10|s11|
// +---+---+    +---+---+
// c2           c3
//
// +---+---+    +---+---+
// | s4| s5|    | s6| s7|
// +---+---+    +---+---+
// | s0| s1|    | s2| s3|
// +---+---+    +---+---+
// c0           c1

namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    //TODOMPI: In this class the partitioning and subdomain topologies should be hardcoded. However also provide an automatic 
    //      way for 1D, 2D, 3D rectilinear meshes. Then use these hardcoded data for testing the automatic ones.
    //TODO: Add another row of clusters up top. Being symmetric is not good for tests, since a lot of mistakes are covered by the symmetry
    public class Plane2DExample
    {
        private const double E = 1.0, v = 0.3, thickness = 1.0;
        private const double load = 100;
        private static readonly double[] minCoords = { 0, 0 };
        private static readonly double[] maxCoords = { 8, 8 };
        private static readonly int[] numElements = { 8, 8 };
        private static readonly int[] numSubdomains = { 4, 4 };
        private static readonly int[] numClusters = { 2, 2 };

        public static void CheckDistributedIndexer(IComputeEnvironment environment, ComputeNodeTopology nodeTopology,
            DistributedOverlappingIndexer indexer)
        {
            //WARNING: Disable any other style analyzer here and DO NOT let them automatically format this
#pragma warning disable IDE0055
            Action<int> checkIndexer = subdomainID =>
            {
                int[] multiplicitiesExpected; // Remember that only boundary dofs go into the distributed vectors 
                var commonEntriesExpected = new Dictionary<int, int[]>();
                if (subdomainID == 0)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9
                    // Boundary dofs:                      02x 02y 11x 11y 18x 18y 19x 19y 20x 20y
                    multiplicitiesExpected =   new int[] {   2,  2,  2,  2,  2,  2,  2,  2,  4,  4 };
                    commonEntriesExpected[1] = new int[] {   0,  1,  2,  3,                  8,  9 };
                    commonEntriesExpected[4] = new int[] {                   4,  5,  6,  7,  8,  9 };
                    commonEntriesExpected[5] = new int[] {                                   8,  9 };
                }
                else if (subdomainID == 1)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      02x 02y 04x 04y 11x 11y 13x 13y 20x 20y 21x 21y 22x 22y
                    multiplicitiesExpected =   new int[] {   2,  2,  2,  2,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[0] = new int[] {   0,  1,          4,  5,          8,  9                 };
                    commonEntriesExpected[2] = new int[] {           2,  3,          6,  7,                 12, 13 };
                    commonEntriesExpected[4] = new int[] {                                   8,  9                 };
                    commonEntriesExpected[5] = new int[] {                                  8,  9,  10, 11, 12, 13 };
                    commonEntriesExpected[6] = new int[] {                                                  12, 13 };
                }
                else if (subdomainID == 2)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      04x 04y 06x 06y 13x 13y 15x 15y 22x 22y 23x 23y 24x 24y
                    multiplicitiesExpected =   new int[] {   2,  2,  2,  2,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[1] = new int[] {   0,  1,          4,  5,          8,  9                 };
                    commonEntriesExpected[3] = new int[] {           2,  3,          6,  7,                 12, 13 };
                    commonEntriesExpected[5] = new int[] {                                   8,  9                 };
                    commonEntriesExpected[6] = new int[] {                                   8,  9, 10, 11, 12, 13 };
                    commonEntriesExpected[7] = new int[] {                                                  12, 13 };
                }
                else if (subdomainID == 3)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9
                    // Boundary dofs:                      06x 06y 15x 15y 24x 24y 25x 25y 26x 26y
                    multiplicitiesExpected =   new int[] {   2,  2,  2,  2,  4,  4,  2,  2,  2,  2 };
                    commonEntriesExpected[2] = new int[] {   0,  1,  2,  3,  4,  5                 };
                    commonEntriesExpected[6] = new int[] {                   4,  5,                };
                    commonEntriesExpected[7] = new int[] {                   4,  5,  6,  7,  8,  9 };
                }
                else if (subdomainID == 4)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      18x 18y 19x 19y 20x 20y 29x 29y 36x 36y 37x 37y 38x 38y
                    multiplicitiesExpected =   new int[] {   2,  2,  2,  2,  4,  4,  2,  2,  2,  2,  2,  2,  4,  4 };
                    commonEntriesExpected[0] = new int[] {   0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[1] = new int[] {                   4,  5                                 };
                    commonEntriesExpected[5] = new int[] {                   4,  5,  6,  7,                 12, 13 };
                    commonEntriesExpected[8] = new int[] {                                   8,  9, 10, 11, 12, 13 };
                    commonEntriesExpected[9] = new int[] {                                                  12, 13 };
                }
                else if (subdomainID == 5)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  
                    // Boundary dofs:                      20x 20y 21x 21y 22x 22y 29x 29y 31x 31y 38x 38y 39x 39y 40x 40y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[0] =  new int[] {  0,  1                                                         };
                    commonEntriesExpected[1] =  new int[] {  0,  1,  2,  3,  4,  5                                         };
                    commonEntriesExpected[2] =  new int[] {                  4,  5                                         };
                    commonEntriesExpected[4] =  new int[] {  0,  1,                  6,  7,         10, 11                 };
                    commonEntriesExpected[6] =  new int[] {                  4,  5,          8,  9,                 14, 15 };
                    commonEntriesExpected[8] =  new int[] {                                         10, 11                 };
                    commonEntriesExpected[9] =  new int[] {                                         10, 11, 12, 13, 14, 15 };
                    commonEntriesExpected[10] = new int[] {                                                         14, 15 };
                }
                else if (subdomainID == 6)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  
                    // Boundary dofs:                      22x 22y 23x 23y 24x 24y 31x 31y 33x 33y 40x 40y 41x 41y 42x 42y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[1] =  new int[] {  0,  1                                                         };
                    commonEntriesExpected[2] =  new int[] {  0,  1,  2,  3,  4,  5                                         };
                    commonEntriesExpected[3] =  new int[] {                  4,  5                                         };
                    commonEntriesExpected[5] =  new int[] {  0,  1,                  6,  7,         10, 11                 };
                    commonEntriesExpected[7] =  new int[] {                  4,  5,          8,  9,                 14, 15 };
                    commonEntriesExpected[9] =  new int[] {                                         10, 11                 };
                    commonEntriesExpected[10] = new int[] {                                         10, 11, 12, 13, 14, 15 };
                    commonEntriesExpected[11] = new int[] {                                                         14, 15 };
                }
                else if (subdomainID == 7)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      24x 24y 25x 25y 26x 26y 33x 33y 42x 42y 43x 43y 44x 44y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  2,  2,  2,  2,  4,  4,  2,  2,  2,  2 };
                    commonEntriesExpected[2] =  new int[] {  0,  1                                                 };
                    commonEntriesExpected[3] =  new int[] {  0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[6] =  new int[] {  0,  1,                  6,  7,  8,  9                 };
                    commonEntriesExpected[10] = new int[] {                                  8,  9                 };
                    commonEntriesExpected[11] = new int[] {                                  8,  9, 10, 11, 12, 13 };
                }
                else if (subdomainID == 8)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      36x 36y 37x 37y 38x 38y 47x 47y 54x 54y 55x 55y 56x 56y
                    multiplicitiesExpected =    new int[] {  2,  2,  2,  2,  4,  4,  2,  2,  2,  2,  2,  2,  4,  4 };
                    commonEntriesExpected[4] =  new int[] {  0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[5] =  new int[] {                  4,  5                                 };
                    commonEntriesExpected[9] =  new int[] {                  4,  5,  6,  7,                  12, 13 };
                    commonEntriesExpected[12] = new int[] {                                   8,  9, 10, 11, 12, 13 };
                    commonEntriesExpected[13] = new int[] {                                                  12, 13 };
                }
                else if (subdomainID == 9)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  
                    // Boundary dofs:                      38x 38y 39x 39y 40x 40y 47x 47y 49x 49y 56x 56y 57x 57y 58x 58y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[4] =  new int[] {  0,  1                                                         };
                    commonEntriesExpected[5] =  new int[] {  0,  1,  2,  3,  4,  5                                         };
                    commonEntriesExpected[6] =  new int[] {                  4,  5                                         };
                    commonEntriesExpected[8] =  new int[] {  0,  1,                  6,  7,         10, 11                 };
                    commonEntriesExpected[10] = new int[] {                  4,  5,          8,  9,                 14, 15 };
                    commonEntriesExpected[12] = new int[] {                                         10, 11                 };
                    commonEntriesExpected[13] = new int[] {                                         10, 11, 12, 13, 14, 15 };
                    commonEntriesExpected[14] = new int[] {                                                         14, 15 };
                }
                else if (subdomainID == 10)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  
                    // Boundary dofs:                      40x 40y 41x 41y 42x 42y 49x 49y 51x 51y 58x 58y 59x 59y 60x 60y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  4,  4,  2,  2,  4,  4 };
                    commonEntriesExpected[5] =  new int[] {  0,  1                                                         };
                    commonEntriesExpected[6] =  new int[] {  0,  1,  2,  3,  4,  5                                         };
                    commonEntriesExpected[7] =  new int[] {                  4,  5                                         };
                    commonEntriesExpected[9] =  new int[] {  0,  1,                  6,  7,         10, 11                 };
                    commonEntriesExpected[11] = new int[] {                  4,  5,          8,  9,                 14, 15 };
                    commonEntriesExpected[13] = new int[] {                                         10, 11                 };
                    commonEntriesExpected[14] = new int[] {                                         10, 11, 12, 13, 14, 15 };
                    commonEntriesExpected[15] = new int[] {                                                         14, 15 };
                }
                else if (subdomainID == 11)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      42x 42y 43x 43y 44x 44y 51x 51y 60x 60y 61x 61y 62x 62y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  2,  2,  2,  2,  4,  4,  2,  2,  2,  2 };
                    commonEntriesExpected[6] =  new int[] {  0,  1                                                 };
                    commonEntriesExpected[7] =  new int[] {  0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[10] = new int[] {  0,  1,                  6,  7,  8,  9                 };
                    commonEntriesExpected[14] = new int[] {                                  8,  9                 };
                    commonEntriesExpected[15] = new int[] {                                  8,  9, 10, 11, 12, 13 };
                }
                else if (subdomainID == 12)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9
                    // Boundary dofs:                      54x 54y 55x 55y 56x 56y 65x 65y 74x 74y
                    multiplicitiesExpected =    new int[] {  2,  2,  2,  2,  4,  4,  2,  2,  2,  2 };
                    commonEntriesExpected[8] =  new int[] {  0,  1,  2,  3,  4,  5 };
                    commonEntriesExpected[9] =  new int[] {                  4,  5 };
                    commonEntriesExpected[13] = new int[] {                  4,  5,  6,  7,  8,  9 };
                }
                else if (subdomainID == 13)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      56x 56y 57x 57y 58x 58y 65x 65y 67x 67y 74x 74y 76x 76y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  2,  2,  2,  2 };
                    commonEntriesExpected[8] =  new int[] {  0,  1                                                 };
                    commonEntriesExpected[9] =  new int[] {  0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[10] = new int[] {                  4,  5                                 };
                    commonEntriesExpected[12] = new int[] {  0,  1,                  6,  7,         10, 11         };
                    commonEntriesExpected[14] = new int[] {                  4,  5,          8,  9,         12, 13 };
                }
                else if (subdomainID == 14)
                {
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9  10  11  12  13
                    // Boundary dofs:                      58x 58y 59x 59y 60x 60y 67x 67y 69x 69y 76x 76y 78x 78y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  4,  4,  2,  2,  2,  2,  2,  2,  2,  2 };
                    commonEntriesExpected[9] =  new int[] {  0,  1                                                 };
                    commonEntriesExpected[10] = new int[] {  0,  1,  2,  3,  4,  5                                 };
                    commonEntriesExpected[11] = new int[] {                  4,  5                                 };
                    commonEntriesExpected[13] = new int[] {  0,  1,                  6,  7,         10, 11         };
                    commonEntriesExpected[15] = new int[] {                  4,  5,          8,  9,         12, 13 };
                }
                else
                {
                    Debug.Assert(subdomainID == 15);
                    // Boundary dof idx:                     0   1   2   3   4   5   6   7   8   9
                    // Boundary dofs:                      60x 60y 6x1 61y 62x 62y 69x 69y 78x 78y
                    multiplicitiesExpected =    new int[] {  4,  4,  2,  2,  2,  2,  2,  2,  2,  2 };
                    commonEntriesExpected[10] = new int[] {  0,  1                                 };
                    commonEntriesExpected[11] = new int[] {  0,  1,  2,  3,  4,  5                 };
                    commonEntriesExpected[14] = new int[] {  0,  1,                  6,  7,  8,  9 };
                }

                int[] multiplicitiesComputed = indexer.GetLocalComponent(subdomainID).Multiplicities;
                Assert.True(Utilities.AreEqual(multiplicitiesExpected, multiplicitiesComputed));
                foreach (int neighborID in commonEntriesExpected.Keys)
                {
                    int[] expected = commonEntriesExpected[neighborID];
                    int[] computed = indexer.GetLocalComponent(subdomainID).GetCommonEntriesWithNeighbor(neighborID);
                    Assert.True(Utilities.AreEqual(expected, computed));
                }
            };
            environment.DoPerNode(checkIndexer);
#pragma warning restore IDE0055
        }

        public static ComputeNodeTopology CreateNodeTopology(IComputeEnvironment environment)
        {
            var nodeTopology = new ComputeNodeTopology();

            nodeTopology.AddNode(0, new int[] { 1, 4, 5 }, 0);
            nodeTopology.AddNode(1, new int[] { 0, 2, 4, 5, 6 }, 0);
            nodeTopology.AddNode(2, new int[] { 1, 3, 5, 6, 7 }, 1);
            nodeTopology.AddNode(3, new int[] { 2, 6, 7 }, 1);
            nodeTopology.AddNode(4, new int[] { 0, 1, 5, 8, 9 }, 0);
            nodeTopology.AddNode(5, new int[] { 0, 1, 2, 4, 6, 8, 9, 10 }, 0);
            nodeTopology.AddNode(6, new int[] { 1, 2, 3, 5, 7, 9, 10, 11 }, 1);
            nodeTopology.AddNode(7, new int[] { 2, 3, 6, 10, 11 }, 1);
            nodeTopology.AddNode(8, new int[] { 4, 5, 9, 12, 13 }, 2);
            nodeTopology.AddNode(9, new int[] { 4, 5, 6, 8, 10, 12, 13, 14 }, 2);
            nodeTopology.AddNode(10, new int[] { 5, 6, 7, 9, 11, 13, 14, 15 }, 3);
            nodeTopology.AddNode(11, new int[] { 6, 7, 10, 14, 15 }, 3);
            nodeTopology.AddNode(12, new int[] { 8, 9, 13 }, 2);
            nodeTopology.AddNode(13, new int[] { 8, 9, 10, 12, 14 }, 2);
            nodeTopology.AddNode(14, new int[] { 9, 10, 11, 13, 15 }, 3);
            nodeTopology.AddNode(15, new int[] { 10, 11, 14 }, 3);

            return nodeTopology;
        }

        public static DistributedModel CreateSingleSubdomainModel(IComputeEnvironment environment)
        {
            AllDofs.AddDof(StructuralDof.TranslationX);
            AllDofs.AddDof(StructuralDof.TranslationY);
            var model = new DistributedModel(environment);
            model.SubdomainsDictionary[0] = new Subdomain(0);

            var meshGenerator = new UniformMeshGenerator2D<Node>(minCoords[0], minCoords[1], maxCoords[0], maxCoords[1], 
                numElements[0], numElements[1]);
            (IReadOnlyList<Node> vertices, IReadOnlyList<CellConnectivity<Node>> cells) =
                meshGenerator.CreateMesh((id, x, y, z) => new Node(id: id, x: x, y: y, z: z));

            // Nodes
            foreach (Node node in vertices)
            {
                model.NodesDictionary[node.ID] = node;
            }

            // Materials
            var material = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = E, PoissonRatio = v };
            var dynamicProperties = new DynamicMaterial(1.0, 1.0, 1.0);

            // Elements
            var elemFactory = new ContinuumElement2DFactory(thickness, material, dynamicProperties);
            for (int e = 0; e < cells.Count; ++e)
            {
                IReadOnlyList<Node> nodes = cells[e].Vertices;
                var elementType = elemFactory.CreateElement(cells[e].CellType, nodes);
                var element = new Element() { ID = e, ElementType = elementType };
                foreach (var node in nodes) element.AddNode(node);
                model.ElementsDictionary[element.ID] = element;
                model.SubdomainsDictionary[0].Elements.Add(element);
            }

            // Boundary conditions
            model.NodesDictionary[0].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
            model.NodesDictionary[0].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
            model.NodesDictionary[8].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
            model.Loads.Add(new Load() { Node = model.NodesDictionary[80], DOF = StructuralDof.TranslationX, Amount = load });

            return model;
        }

        //TODOMPI: Remove this
        public static Model CreateSingleSubdomainModel_OLD()
        {
            AllDofs.AddDof(StructuralDof.TranslationX);
            AllDofs.AddDof(StructuralDof.TranslationY);
            var model = new Model();
            model.SubdomainsDictionary[0] = new Subdomain(0);

            var meshGenerator = new UniformMeshGenerator2D<Node>(minCoords[0], minCoords[1], maxCoords[0], maxCoords[1],
                numElements[0], numElements[1]);
            (IReadOnlyList<Node> vertices, IReadOnlyList<CellConnectivity<Node>> cells) =
                meshGenerator.CreateMesh((id, x, y, z) => new Node(id: id, x: x, y: y, z: z));

            // Nodes
            foreach (Node node in vertices)
            {
                model.NodesDictionary[node.ID] = node;
            }

            // Materials
            var material = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = E, PoissonRatio = v };
            var dynamicProperties = new DynamicMaterial(1.0, 1.0, 1.0);

            // Elements
            var elemFactory = new ContinuumElement2DFactory(thickness, material, dynamicProperties);
            for (int e = 0; e < cells.Count; ++e)
            {
                IReadOnlyList<Node> nodes = cells[e].Vertices;
                var elementType = elemFactory.CreateElement(cells[e].CellType, nodes);
                var element = new Element() { ID = e, ElementType = elementType };
                foreach (var node in nodes) element.AddNode(node);
                model.ElementsDictionary[element.ID] = element;
                model.SubdomainsDictionary[0].Elements.Add(element);
            }

            // Boundary conditions
            model.NodesDictionary[0].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
            model.NodesDictionary[0].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
            model.NodesDictionary[8].Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
            model.Loads.Add(new Load() { Node = model.NodesDictionary[80], DOF = StructuralDof.TranslationX, Amount = load });

            return model;
        }

        public static IStructuralModel CreateMultiSubdomainModel(IComputeEnvironment environment)
        {
            // Partition
            DistributedModel model = CreateSingleSubdomainModel(environment);
            var elementsToSubdomains = new Dictionary<int, int>();
            for (int j = 0; j < numElements[1]; ++j)
            {
                for (int i = 0; i < numElements[0]; ++i)
                {
                    int I = i / 2;
                    int J = j / 2;
                    int elementID = i + j * numElements[0];
                    int subdomainID = I + J * numSubdomains[0];
                    elementsToSubdomains[elementID] = subdomainID;
                }
            }

            ModelUtilities.Decompose(model, numSubdomains[0] * numSubdomains[1], e => elementsToSubdomains[e]);
            return model;
        }
    }
}
