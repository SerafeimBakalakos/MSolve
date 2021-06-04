using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Materials;
using MGroup.Environments;
using MGroup.Solvers;

// Global:
// 0--1--2--3--4--5--6--7--8--9--10--11--12--13--14--15--[16] -> 16 is constrained, 0 is loaded
//
// Clusters:
// 0--1--2--3--4  c0
//             4--5--6--7--8  c1
//                         8--9--10--11--12  c2
//                                       12--13--14--15--[16]  c3
//
// Subdomains:
// 0--1--2  s0
//       2--3--4  s1
//             4--5--6  s2
//                   6--7--8  s3
//                         8--9--10  s4
//                               10--11--12  s5
//                                       12--13--14  s6
//                                               14--15--[16]  s7
//
namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    public class Line1DExample
    {
        private const double length = 2.0, sectionArea = 1.0;
        private const double conductivity = 1.0, specialHeat = 1.0, density = 1.0;

        public const int NumSubdomains = 8;

        public static ComputeNodeTopology CreateNodeTopology(IComputeEnvironment environment)
        {
            var nodeTopology = new ComputeNodeTopology();
            nodeTopology.AddNode(0, new int[] { 1 }, 0);
            nodeTopology.AddNode(1, new int[] { 0, 2 }, 0);
            nodeTopology.AddNode(2, new int[] { 1, 3 }, 1);
            nodeTopology.AddNode(3, new int[] { 2, 4 }, 1);
            nodeTopology.AddNode(4, new int[] { 3, 5 }, 2);
            nodeTopology.AddNode(5, new int[] { 4, 6 }, 2);
            nodeTopology.AddNode(6, new int[] { 5, 7 }, 3);
            nodeTopology.AddNode(7, new int[] { 6 }, 3);

            return nodeTopology;
        }

        public static Model CreateSingleSubdomainModel()
        {
            AllDofs.AddDof(ThermalDof.Temperature);
            var model = new Model();
            model.SubdomainsDictionary[0] = new Subdomain(0);

            // Nodes
            for (int n = 0; n <= 16; ++n)
            {
                model.NodesDictionary[n] = new Node(n, n * length, 0.0, 0.0);
            }

            // Materials
            var material = new ThermalMaterial(density, specialHeat, conductivity);

            // Elements
            for (int e = 0; e < 16; ++e)
            {
                Node[] nodes = { model.Nodes[e], model.Nodes[e + 1] };
                var elementType = new ThermalRod(nodes, sectionArea, material);
                var element = new Element() { ID = e, ElementType = elementType };
                foreach (var node in nodes) element.AddNode(node);
                model.ElementsDictionary[element.ID] = element;
                model.SubdomainsDictionary[0].Elements.Add(element);
            }

            // Boundary conditions
            model.NodesDictionary[16].Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0 });
            model.Loads.Add(new Load() { Node = model.NodesDictionary[0], DOF = ThermalDof.Temperature, Amount = 1 });

            return model;
        }

        public static Model CreateMultiSubdomainModel()
        {
            // Partition
            Model model = CreateSingleSubdomainModel();
            var elementsToSubdomains = new Dictionary<int, int>();
            elementsToSubdomains[0] = 0;
            elementsToSubdomains[1] = 0;
            elementsToSubdomains[2] = 1;
            elementsToSubdomains[3] = 1;
            elementsToSubdomains[4] = 2;
            elementsToSubdomains[5] = 2;
            elementsToSubdomains[6] = 3;
            elementsToSubdomains[7] = 3;
            elementsToSubdomains[8] = 4;
            elementsToSubdomains[9] = 4;
            elementsToSubdomains[10] = 5;
            elementsToSubdomains[11] = 5;
            elementsToSubdomains[12] = 6;
            elementsToSubdomains[13] = 6;
            elementsToSubdomains[14] = 7;
            elementsToSubdomains[15] = 7;
            ModelUtilities.Decompose(model, 8, e => elementsToSubdomains[e]);

            return model;
        }
    }
}
