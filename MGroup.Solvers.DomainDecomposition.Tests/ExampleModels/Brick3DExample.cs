using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using MGroup.Solvers.DomainDecomposition.Mesh;
using MGroup.Solvers.DomainDecomposition.Partitioning;

//TODO: different number of clusters, subdomains, elements per axis. Try to make this as nonsymmetric as possible, 
//      but keep subdomain-elements ratio constant to have the same stiffnesses.
//TODO: Finding the correct indexing data by hand is going to be very difficult. Take them from a correct solution and 
//      hardcode them.
namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    public class Brick3DExample
    {
        private const double E = 1.0, v = 0.3;
        private const double load = 100;

        public static double[] MinCoords => new double[] { 0, 0, 0 };

        public static double[] MaxCoords => new double[] { 6, 9, 12 };

        public static int[] NumElements => new int[] { 4, 6, 8 };

        public static int[] NumSubdomains => new int[] { 2, 3, 4 };

        public static int[] NumClusters => new int[] { 2, 1, 2 };

        public static ComputeNodeTopology CreateNodeTopology()
        {
            var nodeTopology = new ComputeNodeTopology();
            Dictionary<int, int> clustersOfSubdomains = Plane2DExample.GetSubdomainClusters();
            Dictionary<int, int[]> neighborsOfSubdomains = Plane2DExample.GetSubdomainNeighbors();
            for (int s = 0; s < NumSubdomains[0] * NumSubdomains[1]; ++s)
            {
                nodeTopology.AddNode(s, neighborsOfSubdomains[s], clustersOfSubdomains[s]);
            }
            return nodeTopology;

            //nodeTopology.AddNode(0, new int[] { 6 }, 0);
            //nodeTopology.AddNode(1, new int[] { 0, 6, 7 }, 1);
            //nodeTopology.AddNode(2, new int[] { 0, 6, 8 }, 0);
            //nodeTopology.AddNode(3, new int[] { 0, 1, 2, 6, 7, 8, 9 }, 1);
            //nodeTopology.AddNode(4, new int[] { 2, 8, 10 }, 0);
            //nodeTopology.AddNode(5, new int[] { 2, 3, 4, 8, 9, 10, 11 }, 1);
            //nodeTopology.AddNode(6, new int[] { 0, 12 }, 0);
            //nodeTopology.AddNode(7, new int[] { 0, 1, 6, 12, 13 }, 1);
            //nodeTopology.AddNode(8, new int[] { 0, 2, 6, 12, 14 }, 0);
            //nodeTopology.AddNode(9, new int[] { 0, 1, 2, 3, 6, 7, 8, 12, 13, 14, 15 }, 1);
            //nodeTopology.AddNode(10, new int[] { 2, 4, 8, 14, 16 }, 0);
            //nodeTopology.AddNode(11, new int[] { 2, 3, 4, 5, 8, 9, 10, 14, 15, 16, 17 }, 1);
            //nodeTopology.AddNode(12, new int[] { 6, 18 }, 2);
            //nodeTopology.AddNode(13, new int[] { 6, 7, 12, 18, 19 }, 3);
            //nodeTopology.AddNode(14, new int[] { 6, 8, 12, 18, 20 }, 2);
            //nodeTopology.AddNode(15, new int[] { 6, 7, 8, 9, 12, 13, 14, 18, 19, 20, 21 }, 3);
            //nodeTopology.AddNode(16, new int[] { 8, 10, 14, 20, 22 }, 2);
            //nodeTopology.AddNode(17, new int[] { 8, 9, 10, 11, 14, 15, 16, 20, 21, 22, 23 }, 3);
            //nodeTopology.AddNode(18, new int[] { 12 }, 2);
            //nodeTopology.AddNode(19, new int[] { 12, 13, 18 }, 3);
            //nodeTopology.AddNode(20, new int[] { 12, 14, 18 }, 2);
            //nodeTopology.AddNode(21, new int[] { 12, 13, 14, 15, 18, 19, 20 }, 3);
            //nodeTopology.AddNode(22, new int[] { 14, 16, 20 }, 2);
            //nodeTopology.AddNode(23, new int[] { 14, 15, 16, 17, 20, 21, 22 }, 3);
        }

        public static DistributedModel CreateSingleSubdomainDistributedModel(IComputeEnvironment environment)
        {
            AllDofs.AddDof(StructuralDof.TranslationX);
            AllDofs.AddDof(StructuralDof.TranslationY);
            AllDofs.AddDof(StructuralDof.TranslationZ);
            var model = new DistributedModel(environment);
            model.SubdomainsDictionary[0] = new Subdomain(0);

            var mesh = new UniformMesh3D.Builder(MinCoords, MaxCoords, NumElements).SetMajorMinorAxis(0, 2).BuildMesh();

            // Nodes
            foreach ((int id, double[] coords) in mesh.EnumerateNodes())
            {
                model.NodesDictionary[id] = new Node(id, coords[0], coords[1], coords[2]);
            }

            // Materials
            var material = new ElasticMaterial3D() { YoungModulus = E, PoissonRatio = v };
            var dynamicProperties = new DynamicMaterial(1.0, 1.0, 1.0);

            // Elements
            var elemFactory = new ContinuumElement3DFactory(material, dynamicProperties);
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
            //TODO: hardcode the node IDs
            var constrainedNodes = new List<int>();
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { 0, 0, 0 })*/0);
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { mesh.NumNodes[0] - 1, 0, 0 })*/6);
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { 0, mesh.NumNodes[1] - 1, 0 })*/63);
            foreach (int nodeID in constrainedNodes)
            {
                Node node = model.NodesDictionary[nodeID];
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = 0 });
            }

            var loadedNodes = new List<int>();
            loadedNodes.Add(/*mesh.GetNodeID(new int[] { mesh.NumNodes[0] - 1, mesh.NumNodes[1] - 1, mesh.NumNodes[2] - 1 })*/909);
            foreach (int nodeID in constrainedNodes)
            {
                Node node = model.NodesDictionary[nodeID];
                model.Loads.Add(new Load() { Node = node, DOF = StructuralDof.TranslationZ, Amount = load });
            }

            return model;
        }

        //TODOMPI: Remove this
        public static Model CreateSingleSubdomainModel()
        {
            AllDofs.AddDof(StructuralDof.TranslationX);
            AllDofs.AddDof(StructuralDof.TranslationY);
            AllDofs.AddDof(StructuralDof.TranslationZ);
            var model = new Model();
            model.SubdomainsDictionary[0] = new Subdomain(0);

            var mesh = new UniformMesh3D.Builder(MinCoords, MaxCoords, NumElements).SetMajorMinorAxis(0, 2).BuildMesh();

            // Nodes
            foreach ((int id, double[] coords) in mesh.EnumerateNodes())
            {
                model.NodesDictionary[id] = new Node(id, coords[0], coords[1], coords[2]);
            }

            // Materials
            var material = new ElasticMaterial3D() { YoungModulus = E, PoissonRatio = v };
            var dynamicProperties = new DynamicMaterial(1.0, 1.0, 1.0);

            // Elements
            var elemFactory = new ContinuumElement3DFactory(material, dynamicProperties);
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
            //TODO: hardcode the node IDs
            var constrainedNodes = new List<int>();
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { 0, 0, 0 })*/0);
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { mesh.NumNodes[0] - 1, 0, 0 })*/6);
            constrainedNodes.Add(/*mesh.GetNodeID(new int[] { 0, mesh.NumNodes[1] - 1, 0 })*/63);
            foreach (int nodeID in constrainedNodes)
            {
                Node node = model.NodesDictionary[nodeID];
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = 0 });
            }

            var loadedNodes = new List<int>();
            loadedNodes.Add(/*mesh.GetNodeID(new int[] { mesh.NumNodes[0] - 1, mesh.NumNodes[1] - 1, mesh.NumNodes[2] - 1 })*/909);
            foreach (int nodeID in constrainedNodes)
            {
                Node node = model.NodesDictionary[nodeID];
                model.Loads.Add(new Load() { Node = node, DOF = StructuralDof.TranslationZ, Amount = load });
            }

            return model;
        }

        public static IStructuralModel CreateMultiSubdomainModel(IComputeEnvironment environment)
        {
            Dictionary<int, int> elementsToSubdomains = GetSubdomainsOfElements();
            DistributedModel model = CreateSingleSubdomainDistributedModel(environment);
            model.DecomposeIntoSubdomains(NumSubdomains[0] * NumSubdomains[1] * NumSubdomains[2], e => elementsToSubdomains[e]);
            return model;
        }

        public static Dictionary<int, int> GetSubdomainsOfElements()
        {
            var elementsToSubdomains = new Dictionary<int, int>();
            #region long list of element -> subdomain associations
            elementsToSubdomains[0] = 0;
            elementsToSubdomains[1] = 0;
            elementsToSubdomains[2] = 1;
            elementsToSubdomains[3] = 1;
            elementsToSubdomains[4] = 0;
            elementsToSubdomains[5] = 0;
            elementsToSubdomains[6] = 1;
            elementsToSubdomains[7] = 1;
            elementsToSubdomains[8] = 2;
            elementsToSubdomains[9] = 2;
            elementsToSubdomains[10] = 3;
            elementsToSubdomains[11] = 3;
            elementsToSubdomains[12] = 2;
            elementsToSubdomains[13] = 2;
            elementsToSubdomains[14] = 3;
            elementsToSubdomains[15] = 3;
            elementsToSubdomains[16] = 4;
            elementsToSubdomains[17] = 4;
            elementsToSubdomains[18] = 5;
            elementsToSubdomains[19] = 5;
            elementsToSubdomains[20] = 4;
            elementsToSubdomains[21] = 4;
            elementsToSubdomains[22] = 5;
            elementsToSubdomains[23] = 5;
            elementsToSubdomains[24] = 0;
            elementsToSubdomains[25] = 0;
            elementsToSubdomains[26] = 1;
            elementsToSubdomains[27] = 1;
            elementsToSubdomains[28] = 0;
            elementsToSubdomains[29] = 0;
            elementsToSubdomains[30] = 1;
            elementsToSubdomains[31] = 1;
            elementsToSubdomains[32] = 2;
            elementsToSubdomains[33] = 2;
            elementsToSubdomains[34] = 3;
            elementsToSubdomains[35] = 3;
            elementsToSubdomains[36] = 2;
            elementsToSubdomains[37] = 2;
            elementsToSubdomains[38] = 3;
            elementsToSubdomains[39] = 3;
            elementsToSubdomains[40] = 4;
            elementsToSubdomains[41] = 4;
            elementsToSubdomains[42] = 5;
            elementsToSubdomains[43] = 5;
            elementsToSubdomains[44] = 4;
            elementsToSubdomains[45] = 4;
            elementsToSubdomains[46] = 5;
            elementsToSubdomains[47] = 5;
            elementsToSubdomains[48] = 6;
            elementsToSubdomains[49] = 6;
            elementsToSubdomains[50] = 7;
            elementsToSubdomains[51] = 7;
            elementsToSubdomains[52] = 6;
            elementsToSubdomains[53] = 6;
            elementsToSubdomains[54] = 7;
            elementsToSubdomains[55] = 7;
            elementsToSubdomains[56] = 8;
            elementsToSubdomains[57] = 8;
            elementsToSubdomains[58] = 9;
            elementsToSubdomains[59] = 9;
            elementsToSubdomains[60] = 8;
            elementsToSubdomains[61] = 8;
            elementsToSubdomains[62] = 9;
            elementsToSubdomains[63] = 9;
            elementsToSubdomains[64] = 10;
            elementsToSubdomains[65] = 10;
            elementsToSubdomains[66] = 11;
            elementsToSubdomains[67] = 11;
            elementsToSubdomains[68] = 10;
            elementsToSubdomains[69] = 10;
            elementsToSubdomains[70] = 11;
            elementsToSubdomains[71] = 11;
            elementsToSubdomains[72] = 6;
            elementsToSubdomains[73] = 6;
            elementsToSubdomains[74] = 7;
            elementsToSubdomains[75] = 7;
            elementsToSubdomains[76] = 6;
            elementsToSubdomains[77] = 6;
            elementsToSubdomains[78] = 7;
            elementsToSubdomains[79] = 7;
            elementsToSubdomains[80] = 8;
            elementsToSubdomains[81] = 8;
            elementsToSubdomains[82] = 9;
            elementsToSubdomains[83] = 9;
            elementsToSubdomains[84] = 8;
            elementsToSubdomains[85] = 8;
            elementsToSubdomains[86] = 9;
            elementsToSubdomains[87] = 9;
            elementsToSubdomains[88] = 10;
            elementsToSubdomains[89] = 10;
            elementsToSubdomains[90] = 11;
            elementsToSubdomains[91] = 11;
            elementsToSubdomains[92] = 10;
            elementsToSubdomains[93] = 10;
            elementsToSubdomains[94] = 11;
            elementsToSubdomains[95] = 11;
            elementsToSubdomains[96] = 12;
            elementsToSubdomains[97] = 12;
            elementsToSubdomains[98] = 13;
            elementsToSubdomains[99] = 13;
            elementsToSubdomains[100] = 12;
            elementsToSubdomains[101] = 12;
            elementsToSubdomains[102] = 13;
            elementsToSubdomains[103] = 13;
            elementsToSubdomains[104] = 14;
            elementsToSubdomains[105] = 14;
            elementsToSubdomains[106] = 15;
            elementsToSubdomains[107] = 15;
            elementsToSubdomains[108] = 14;
            elementsToSubdomains[109] = 14;
            elementsToSubdomains[110] = 15;
            elementsToSubdomains[111] = 15;
            elementsToSubdomains[112] = 16;
            elementsToSubdomains[113] = 16;
            elementsToSubdomains[114] = 17;
            elementsToSubdomains[115] = 17;
            elementsToSubdomains[116] = 16;
            elementsToSubdomains[117] = 16;
            elementsToSubdomains[118] = 17;
            elementsToSubdomains[119] = 17;
            elementsToSubdomains[120] = 12;
            elementsToSubdomains[121] = 12;
            elementsToSubdomains[122] = 13;
            elementsToSubdomains[123] = 13;
            elementsToSubdomains[124] = 12;
            elementsToSubdomains[125] = 12;
            elementsToSubdomains[126] = 13;
            elementsToSubdomains[127] = 13;
            elementsToSubdomains[128] = 14;
            elementsToSubdomains[129] = 14;
            elementsToSubdomains[130] = 15;
            elementsToSubdomains[131] = 15;
            elementsToSubdomains[132] = 14;
            elementsToSubdomains[133] = 14;
            elementsToSubdomains[134] = 15;
            elementsToSubdomains[135] = 15;
            elementsToSubdomains[136] = 16;
            elementsToSubdomains[137] = 16;
            elementsToSubdomains[138] = 17;
            elementsToSubdomains[139] = 17;
            elementsToSubdomains[140] = 16;
            elementsToSubdomains[141] = 16;
            elementsToSubdomains[142] = 17;
            elementsToSubdomains[143] = 17;
            elementsToSubdomains[144] = 18;
            elementsToSubdomains[145] = 18;
            elementsToSubdomains[146] = 19;
            elementsToSubdomains[147] = 19;
            elementsToSubdomains[148] = 18;
            elementsToSubdomains[149] = 18;
            elementsToSubdomains[150] = 19;
            elementsToSubdomains[151] = 19;
            elementsToSubdomains[152] = 20;
            elementsToSubdomains[153] = 20;
            elementsToSubdomains[154] = 21;
            elementsToSubdomains[155] = 21;
            elementsToSubdomains[156] = 20;
            elementsToSubdomains[157] = 20;
            elementsToSubdomains[158] = 21;
            elementsToSubdomains[159] = 21;
            elementsToSubdomains[160] = 22;
            elementsToSubdomains[161] = 22;
            elementsToSubdomains[162] = 23;
            elementsToSubdomains[163] = 23;
            elementsToSubdomains[164] = 22;
            elementsToSubdomains[165] = 22;
            elementsToSubdomains[166] = 23;
            elementsToSubdomains[167] = 23;
            elementsToSubdomains[168] = 18;
            elementsToSubdomains[169] = 18;
            elementsToSubdomains[170] = 19;
            elementsToSubdomains[171] = 19;
            elementsToSubdomains[172] = 18;
            elementsToSubdomains[173] = 18;
            elementsToSubdomains[174] = 19;
            elementsToSubdomains[175] = 19;
            elementsToSubdomains[176] = 20;
            elementsToSubdomains[177] = 20;
            elementsToSubdomains[178] = 21;
            elementsToSubdomains[179] = 21;
            elementsToSubdomains[180] = 20;
            elementsToSubdomains[181] = 20;
            elementsToSubdomains[182] = 21;
            elementsToSubdomains[183] = 21;
            elementsToSubdomains[184] = 22;
            elementsToSubdomains[185] = 22;
            elementsToSubdomains[186] = 23;
            elementsToSubdomains[187] = 23;
            elementsToSubdomains[188] = 22;
            elementsToSubdomains[189] = 22;
            elementsToSubdomains[190] = 23;
            elementsToSubdomains[191] = 23;
            #endregion

            return elementsToSubdomains;
        }

        public static Dictionary<int, int> GetSubdomainClusters()
        {
            var result = new Dictionary<int, int>();
            result[0] = 0;
            result[1] = 1;
            result[2] = 0;
            result[3] = 1;
            result[4] = 0;
            result[5] = 1;
            result[6] = 0;
            result[7] = 1;
            result[8] = 0;
            result[9] = 1;
            result[10] = 0;
            result[11] = 1;
            result[12] = 2;
            result[13] = 3;
            result[14] = 2;
            result[15] = 3;
            result[16] = 2;
            result[17] = 3;
            result[18] = 2;
            result[19] = 3;
            result[20] = 2;
            result[21] = 3;
            result[22] = 2;
            result[23] = 3;

            return result;
        }

        public static Dictionary<int, int[]> GetSubdomainNeighbors()
        {
            var result = new Dictionary<int, int[]>();
            result[0] = new int[] { 1, 2, 3, 6, 7, 8, 9, };
            result[1] = new int[] { 0, 2, 3, 6, 7, 8, 9, };
            result[2] = new int[] { 0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, };
            result[3] = new int[] { 0, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, };
            result[4] = new int[] { 2, 3, 5, 8, 9, 10, 11, };
            result[5] = new int[] { 2, 3, 4, 8, 9, 10, 11, };

            result[6] = new int[] { 0, 1, 2, 3, 7, 8, 9, 12, 13, 14, 15, };
            result[7] = new int[] { 0, 1, 2, 3, 6, 8, 9, 12, 13, 14, 15, };
            result[8] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, };
            result[9] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, };
            result[10] = new int[] { 2, 3, 4, 5, 8, 9, 11, 14, 15, 16, 17, };
            result[11] = new int[] { 2, 3, 4, 5, 8, 9, 10, 14, 15, 16, 17, };

            result[12] = new int[] { 6, 7, 8, 9, 13, 14, 15, 18, 19, 20, 21, };
            result[13] = new int[] { 6, 7, 8, 9, 12, 14, 15, 18, 19, 20, 21, };
            result[14] = new int[] { 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, };
            result[15] = new int[] { 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 18, 19, 20, 21, 22, 23, };
            result[16] = new int[] { 8, 9, 10, 11, 14, 15, 17, 20, 21, 22, 23, };
            result[17] = new int[] { 8, 9, 10, 11, 14, 15, 16, 20, 21, 22, 23, };

            result[18] = new int[] { 12, 13, 14, 15, 19, 20, 21, };
            result[19] = new int[] { 12, 13, 14, 15, 18, 20, 21, };
            result[20] = new int[] { 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23, };
            result[21] = new int[] { 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 23, };
            result[22] = new int[] { 14, 15, 16, 17, 20, 21, 23, };
            result[23] = new int[] { 14, 15, 16, 17, 20, 21, 22, };

            return result;
        }
    }
}
