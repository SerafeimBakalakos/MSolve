using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Materials;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.Distributed.Environments;

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
namespace MGroup.Solvers.Tests.DDM.ExampleModels
{
    public class Line1DExample //TODOMPI: Merge this with Line1DTopology
    {
        private const double length = 2.0, sectionArea = 1.0;
        private const double conductivity = 1.0, specialHeat = 1.0, density = 1.0;

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

        public static (Model model, ClusterTopology clusterTopology) CreateMultiSubdomainModel(IComputeEnvironment environment)
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

            // Clusters
            var clusterTopology = new ClusterTopology(environment);
            clusterTopology.Clusters[0] = new Solvers.DDM.Cluster(0);
            clusterTopology.Clusters[0].Subdomains.Add(model.SubdomainsDictionary[0]); 
            clusterTopology.Clusters[0].Subdomains.Add(model.SubdomainsDictionary[1]);
            clusterTopology.Clusters[1] = new Solvers.DDM.Cluster(1);
            clusterTopology.Clusters[1].Subdomains.Add(model.SubdomainsDictionary[2]);
            clusterTopology.Clusters[1].Subdomains.Add(model.SubdomainsDictionary[3]);
            clusterTopology.Clusters[2] = new Solvers.DDM.Cluster(2);
            clusterTopology.Clusters[2].Subdomains.Add(model.SubdomainsDictionary[4]);
            clusterTopology.Clusters[2].Subdomains.Add(model.SubdomainsDictionary[5]);
            clusterTopology.Clusters[3] = new Solvers.DDM.Cluster(3);
            clusterTopology.Clusters[3].Subdomains.Add(model.SubdomainsDictionary[6]);
            clusterTopology.Clusters[3].Subdomains.Add(model.SubdomainsDictionary[7]);

            return (model, clusterTopology);
        }
    }
}
