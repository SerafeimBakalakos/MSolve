using System;
using System.Collections.Generic;
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

//TODO: different number of clusters, subdomains, elements per axis. Try to make this as nonsymmetric as possible, 
//      but keep subdomain-elements ratio constant to have the same stiffnesses.
//TODO: Finding the correct indexing data by hand is going to be very difficult. Take them from a correct solution and 
//      hardcode them.
namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    public class Brick3DExample
    {
        private const double E = 1.0, v = 0.3, thickness = 1.0;
        private const double load = 100;
        private static readonly double[] minCoords = { 0, 0 };
        private static readonly double[] maxCoords = { 8, 8 };
        private static readonly int[] numElements = { 8, 8 };
        private static readonly int[] numSubdomains = { 4, 4 };
        private static readonly int[] numClusters = { 2, 2 };

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
