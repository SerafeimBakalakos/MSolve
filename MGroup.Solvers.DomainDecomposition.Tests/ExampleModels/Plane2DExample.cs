using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Commons;
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
using MGroup.Solvers.DomainDecomposition.Partitioning;
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

            var mesh = UniformMesh2D.Create(minCoords, maxCoords, numElements, 0);

            // Nodes
            foreach ((int id, double[] coords) in mesh.EnumerateNodes())
            {
                model.NodesDictionary[id] = new Node(id, coords[0], coords[1]);
            }

            // Materials
            var material = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = E, PoissonRatio = v };
            var dynamicProperties = new DynamicMaterial(1.0, 1.0, 1.0);

            // Elements
            var elemFactory = new ContinuumElement2DFactory(thickness, material, dynamicProperties);
            foreach ((int elementID, int[] nodeIDs) in mesh.EnumerateElements())
            {
                Node[] nodes = nodeIDs.Select(n => model.NodesDictionary[n]).ToArray();
                var elementType = elemFactory.CreateElement(mesh.CellType, nodes);
                var element = new Element() { ID = elementID, ElementType = elementType };
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

        public static Table<int, int, double> GetExpectedNodalValues()
        {
            //var model = CreateSingleSubdomainModel_OLD();
            //var solver = new ISAAR.MSolve.Solvers.Direct.SkylineSolver.Builder().BuildSolver(model);
            //var problem = new ISAAR.MSolve.Problems.ProblemThermalSteadyState(model, solver);
            //var childAnalyzer = new ISAAR.MSolve.Analyzers.LinearAnalyzer(model, solver, problem);
            //var parentAnalyzer = new ISAAR.MSolve.Analyzers.StaticAnalyzer(model, solver, problem, childAnalyzer);
            //parentAnalyzer.Initialize();
            //parentAnalyzer.Solve();
            //Table<int, int, double> result =
            //    Utilities.FindNodalFieldValues(model.Subdomains.First(), solver.LinearSystems.First().Value.Solution);

            //using var writer = new StreamWriter(@"C:\Users\Serafeim\Desktop\PFETIDP\solution2D.txt");
            //writer.WriteLine(result);

            var result = new Table<int, int, double>();
            result[0, 0] = 0;
            result[0, 1] = 0;
            result[1, 0] = 150.1000802198485;
            result[1, 1] = 66.0931314769534;
            result[2, 0] = 209.1956299086393;
            result[2, 1] = 4.108111394651694;
            result[3, 0] = 244.30619107657705;
            result[3, 1] = -65.70787578919311;
            result[4, 0] = 275.5145123577474;
            result[4, 1] = -143.7789503887835;
            result[5, 0] = 310.9820696441611;
            result[5, 1] = -219.0962997007431;
            result[6, 0] = 360.58357755214274;
            result[6, 1] = -275.2450418749667;
            result[7, 0] = 434.3287503604323;
            result[7, 1] = -289.7137836350665;
            result[8, 0] = 515.7013066725633;
            result[8, 1] = 0;
            result[9, 0] = 272.6359049640539;
            result[9, 1] = 150.08004029482072;
            result[10, 0] = 252.78020747268184;
            result[10, 1] = 46.32961764070971;
            result[11, 0] = 293.8503312870056;
            result[11, 1] = -1.7443278815822723;
            result[12, 0] = 326.4095289392989;
            result[12, 1] = -73.49070643258257;
            result[13, 0] = 351.75879754287325;
            result[13, 1] = -150.6195892651938;
            result[14, 0] = 371.86509727481035;
            result[14, 1] = -225.85849979395613;
            result[15, 0] = 385.24033375505536;
            result[15, 1] = -286.23854752027336;
            result[16, 0] = 378.3432980307993;
            result[16, 1] = -286.3224550149545;
            result[17, 0] = 394.9484813476963;
            result[17, 1] = -232.27660571031268;
            result[18, 0] = 417.54856262267583;
            result[18, 1] = 208.80895110877685;
            result[19, 0] = 411.2439064986351;
            result[19, 1] = 87.80601372931197;
            result[20, 0] = 412.3702182134087;
            result[20, 1] = 2.2745995313056504;
            result[21, 0] = 426.3131179538608;
            result[21, 1] = -72.66524240626192;
            result[22, 0] = 437.3854495891751;
            result[22, 1] = -150.17089381009788;
            result[23, 0] = 441.5315009766441;
            result[23, 1] = -224.82357750196996;
            result[24, 0] = 436.4711271034646;
            result[24, 1] = -284.59569325130064;
            result[25, 0] = 433.7125389107416;
            result[25, 1] = -320.6803824770333;
            result[26, 0] = 451.79182247473295;
            result[26, 1] = -368.15093155855277;
            result[27, 0] = 555.699870036769;
            result[27, 1] = 241.5055394213765;
            result[28, 0] = 547.6227125634863;
            result[28, 1] = 120.65889928467423;
            result[29, 0] = 543.3539929002895;
            result[29, 1] = 22.465613854123653;
            result[30, 0] = 542.5266903063331;
            result[30, 1] = -62.36662223115644;
            result[31, 0] = 542.0039823977013;
            result[31, 1] = -142.261841244437;
            result[32, 0] = 537.5778988014689;
            result[32, 1] = -219.53304085182225;
            result[33, 0] = 531.8986496143507;
            result[33, 1] = -292.48395371557837;
            result[34, 0] = 533.8423366603457;
            result[34, 1] = -365.4661452270108;
            result[35, 0] = 549.5083401123986;
            result[35, 1] = -456.99921095913686;
            result[36, 0] = 685.5850587063886;
            result[36, 1] = 264.12566942589245;
            result[37, 0] = 679.9174122725947;
            result[37, 1] = 144.87822569894286;
            result[38, 0] = 674.4962201087524;
            result[38, 1] = 42.86748064528494;
            result[39, 0] = 670.1190505007278;
            result[39, 1] = -46.773800599539214;
            result[40, 0] = 665.2183523624371;
            result[40, 1] = -130.41758856464403;
            result[41, 0] = 658.7853356236066;
            result[41, 1] = -214.0613765297487;
            result[42, 0] = 653.5483942047391;
            result[42, 1] = -303.7026577745726;
            result[43, 0] = 655.7711373677146;
            result[43, 1] = -405.7134028282301;
            result[44, 0] = 669.1551394111781;
            result[44, 1] = -524.9608465551795;
            result[45, 0] = 812.6701113852116;
            result[45, 1] = 278.4034109056682;
            result[46, 0] = 809.3317700635641;
            result[46, 1] = 161.50160672706784;
            result[47, 0] = 806.2763156728598;
            result[47, 1] = 59.068234606991794;
            result[48, 0] = 804.1683684889136;
            result[48, 1] = -32.50837197199394;
            result[49, 0] = 802.3182675762101;
            result[49, 1] = -118.57333588485118;
            result[50, 0] = 799.2195769840496;
            result[50, 1] = -207.26231920360337;
            result[51, 0] = 794.8209723869212;
            result[51, 1] = -310.72024900411236;
            result[52, 0] = 795.5513941604238;
            result[52, 1] = -438.36471504330586;
            result[53, 0] = 806.4785814608416;
            result[53, 1] = -584.5800936264815;
            result[54, 0] = 935.5890229324615;
            result[54, 1] = 285.093677831206;
            result[55, 0] = 934.7123291228971;
            result[55, 1] = 171.3381585281947;
            result[56, 0] = 935.3863653899402;
            result[56, 1] = 69.07779838031117;
            result[57, 0] = 939.1478145808935;
            result[57, 1] = -23.49039638687864;
            result[58, 0] = 946.0190333867533;
            result[58, 1] = -110.6642833191903;
            result[59, 0] = 954.3661976036765;
            result[59, 1] = -200.69113796346565;
            result[60, 0] = 959.4872742799955;
            result[60, 1] = -308.42705891889176;
            result[61, 0] = 957.1809615350032;
            result[61, 1] = -460.13414403904795;
            result[62, 0] = 969.8322827845182;
            result[62, 1] = -647.4220516400029;
            result[63, 0] = 1052.2061027387206;
            result[63, 1] = 286.3804712031824;
            result[64, 0] = 1052.9250328911655;
            result[64, 1] = 175.49596313767336;
            result[65, 0] = 1056.6765652263657;
            result[65, 1] = 72.64445034546856;
            result[66, 0] = 1066.2194291244234;
            result[66, 1] = -21.451667088065413;
            result[67, 0] = 1083.9727523418535;
            result[67, 1] = -110.21558786409435;
            result[68, 0] = 1111.6749974599336;
            result[68, 1] = -200.86948094397187;
            result[69, 0] = 1148.0665676944136;
            result[69, 1] = -306.3319292021886;
            result[70, 0] = 1178.4881234492807;
            result[70, 1] = -457.1734800220033;
            result[71, 0] = 1174.5186791223607;
            result[71, 1] = -725.8542600462622;
            result[72, 0] = 1163.4194269822522;
            result[72, 1] = 286.15284032333284;
            result[73, 0] = 1163.2118360274305;
            result[73, 1] = 176.30707694524676;
            result[74, 0] = 1164.8652682742418;
            result[74, 1] = 72.21997338371091;
            result[75, 0] = 1173.9695080551178;
            result[75, 1] = -24.49568157981949;
            result[76, 0] = 1196.835440811547;
            result[76, 1] = -117.05622674050431;
            result[77, 0] = 1240.6453866227;
            result[77, 1] = -212.37049718881994;
            result[78, 0] = 1316.2532159177426;
            result[78, 1] = -322.75339716197135;
            result[79, 0] = 1447.4405061680093;
            result[79, 1] = -474.35677904570855;
            result[80, 0] = 1679.120733654807;
            result[80, 1] = -807.8231945819023;

            return result;
        }
    }
}
