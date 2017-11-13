using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ISAAR.MSolve.PreProcessor;
using ISAAR.MSolve.Solvers.Skyline;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.PreProcessor.Materials;
using ISAAR.MSolve.PreProcessor.Elements;
using ISAAR.MSolve.Matrices;

namespace ISAAR.MSolve.SamplesConsole
{
    public static class TrussExample
    {
        public static IList<Node> CreateNodes()
        {
            IList<Node> nodes = new List<Node>();
            Node node1 = new Node { ID = 1, X = 0, Y = 0 };
            Node node2 = new Node { ID = 2, X = 0, Y = 40 };
            Node node3 = new Node { ID = 3, X = 40, Y = 40 };
          
            nodes.Add(node1);
            nodes.Add(node2);
            nodes.Add(node3);

            return nodes;
        }

        public static void Truss2DExample()
        {
            VectorExtensions.AssignTotalAffinityCount();
            double youngMod = 10e6;
            double poisson = 0.3;
            double loadX = 500;
            double loadY = 300;
            double sectionArea = 1.5;

            ElasticMaterial material = new ElasticMaterial() { YoungModulus = youngMod, PoissonRatio = poisson };

            IList<Node> nodes = TrussExample.CreateNodes();

            Model trussModel = new Model();

            trussModel.SubdomainsDictionary.Add(1, new Subdomain() { ID = 1 });

            for (int i = 0; i < nodes.Count; i++)
            {
                trussModel.NodesDictionary.Add(i + 1, nodes[i]);
            }

            trussModel.NodesDictionary[1].Constraints.Add(DOFType.X);
            trussModel.NodesDictionary[1].Constraints.Add(DOFType.Y);
            trussModel.NodesDictionary[2].Constraints.Add(DOFType.X);
            trussModel.NodesDictionary[2].Constraints.Add(DOFType.Y);


            var element1 = new Element() { ID = 1, ElementType = new Rod2D(material) { Density = 1, SectionArea = sectionArea} };
            var element2 = new Element() { ID = 2, ElementType = new Rod2D(material) { Density = 1, SectionArea = sectionArea} };

            element1.AddNode(trussModel.NodesDictionary[1]);
            element1.AddNode(trussModel.NodesDictionary[3]);

            element2.AddNode(trussModel.NodesDictionary[2]);
            element2.AddNode(trussModel.NodesDictionary[3]);

            trussModel.ElementsDictionary.Add(element1.ID, element1);
            trussModel.ElementsDictionary.Add(element2.ID, element2);

            trussModel.SubdomainsDictionary[1].ElementsDictionary.Add(element1.ID, element1);
            trussModel.SubdomainsDictionary[1].ElementsDictionary.Add(element2.ID, element2);

            trussModel.Loads.Add(new Load() { Amount = loadX, Node = trussModel.NodesDictionary[3], DOF = DOFType.X });
            trussModel.Loads.Add(new Load() { Amount = loadY, Node = trussModel.NodesDictionary[3], DOF = DOFType.Y });

            trussModel.ConnectDataStructures();
            SolverSkyline solution = new SolverSkyline(trussModel);

            ProblemStructural provider = new ProblemStructural(trussModel, solution.SubdomainsDictionary);

            Analyzers.LinearAnalyzer childAnalyzer = new LinearAnalyzer(solution, solution.SubdomainsDictionary);
            StaticAnalyzer parentAnalyzer = new StaticAnalyzer(provider, childAnalyzer, solution.SubdomainsDictionary);

            childAnalyzer.LogFactories[1] = new LinearAnalyzerLogFactory(new int[] {
                trussModel.NodalDOFsDictionary[3][DOFType.X],
                trussModel.NodalDOFsDictionary[3][DOFType.Y],
                });

            parentAnalyzer.BuildMatrices();
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            Console.WriteLine("Writing results for node 3");
            Console.WriteLine("Dof and Values for Displacement X and Y");
            Console.WriteLine(childAnalyzer.Logs[1][0]);
        }
    }
}
