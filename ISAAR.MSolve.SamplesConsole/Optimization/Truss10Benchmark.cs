using System;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Analyzers.Optimization.Problem;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Matrices;
using ISAAR.MSolve.PreProcessor;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Skyline;
using ISAAR.MSolve.PreProcessor.Materials;
using System.Collections.Generic;
using ISAAR.MSolve.PreProcessor.Elements;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    class Truss10Benchmark : IObjectiveFunction
    {
        private Model truss10;
        IAnalyzer parentAnalyzer; 

        public Truss10Benchmark()
        {
            VectorExtensions.AssignTotalAffinityCount();
            double youngModulus = 10e4;
            double poissonRatio = 0.3;
            double loadP = 100;
            double sectionArea = 1.5;
            double tesionStrength = 25;

            ElasticMaterial material = new ElasticMaterial() { YoungModulus = youngModulus, PoissonRatio = poissonRatio };

            IList<Node> nodes = new List<Node>();
            Node node1 = new Node { ID = 1, X = 720, Y = 360 };
            Node node2 = new Node { ID = 2, X = 720, Y = 0 };
            Node node3 = new Node { ID = 3, X = 360, Y = 360 };
            Node node4 = new Node { ID = 4, X = 360, Y = 0 };
            Node node5 = new Node { ID = 5, X = 0, Y = 360 };
            Node node6 = new Node { ID = 6, X = 0, Y = 0 };

            nodes.Add(node1);
            nodes.Add(node2);
            nodes.Add(node3);
            nodes.Add(node4);
            nodes.Add(node5);
            nodes.Add(node6);

            Model truss10 = new Model();

            truss10.SubdomainsDictionary.Add(1, new Subdomain() { ID = 1 });

            var element1 = new Element() { ID = 1, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element2 = new Element() { ID = 2, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element3 = new Element() { ID = 3, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element4 = new Element() { ID = 4, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element5 = new Element() { ID = 5, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element6 = new Element() { ID = 6, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element7 = new Element() { ID = 7, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element8 = new Element() { ID = 8, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element9 = new Element() { ID = 9, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };
            var element10 = new Element() { ID = 10, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = sectionArea } };

            element1.AddNode(truss10.NodesDictionary[3]);
            element1.AddNode(truss10.NodesDictionary[5]);
            element2.AddNode(truss10.NodesDictionary[1]);
            element2.AddNode(truss10.NodesDictionary[3]);
            element3.AddNode(truss10.NodesDictionary[4]);
            element3.AddNode(truss10.NodesDictionary[6]);
            element4.AddNode(truss10.NodesDictionary[2]);
            element4.AddNode(truss10.NodesDictionary[4]);
            element5.AddNode(truss10.NodesDictionary[3]);
            element5.AddNode(truss10.NodesDictionary[4]);
            element6.AddNode(truss10.NodesDictionary[1]);
            element6.AddNode(truss10.NodesDictionary[2]);
            element7.AddNode(truss10.NodesDictionary[4]);
            element7.AddNode(truss10.NodesDictionary[5]);
            element8.AddNode(truss10.NodesDictionary[3]);
            element8.AddNode(truss10.NodesDictionary[6]);
            element9.AddNode(truss10.NodesDictionary[2]);
            element9.AddNode(truss10.NodesDictionary[3]);
            element10.AddNode(truss10.NodesDictionary[1]);
            element10.AddNode(truss10.NodesDictionary[4]);

            truss10.ElementsDictionary.Add(element1.ID, element1);
            truss10.ElementsDictionary.Add(element2.ID, element2);
            truss10.ElementsDictionary.Add(element3.ID, element3);
            truss10.ElementsDictionary.Add(element4.ID, element4);
            truss10.ElementsDictionary.Add(element5.ID, element5);
            truss10.ElementsDictionary.Add(element6.ID, element6);
            truss10.ElementsDictionary.Add(element7.ID, element7);
            truss10.ElementsDictionary.Add(element8.ID, element8);
            truss10.ElementsDictionary.Add(element9.ID, element9);
            truss10.ElementsDictionary.Add(element10.ID, element10);

            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element1.ID, element1);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element2.ID, element2);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element3.ID, element3);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element4.ID, element4);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element5.ID, element5);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element6.ID, element6);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element7.ID, element7);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element8.ID, element8);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element9.ID, element9);
            truss10.SubdomainsDictionary[1].ElementsDictionary.Add(element10.ID, element10);

            for (int i = 0; i < nodes.Count; i++)
            {
                truss10.NodesDictionary.Add(i + 1, nodes[i]);
            }

            truss10.NodesDictionary[5].Constraints.Add(DOFType.X);
            truss10.NodesDictionary[5].Constraints.Add(DOFType.Y);
            truss10.NodesDictionary[6].Constraints.Add(DOFType.X);
            truss10.NodesDictionary[6].Constraints.Add(DOFType.Y);

            truss10.Loads.Add(new Load() { Amount = loadP, Node = truss10.NodesDictionary[2], DOF = DOFType.Y });
            truss10.Loads.Add(new Load() { Amount = loadP, Node = truss10.NodesDictionary[4], DOF = DOFType.Y });

            truss10.ConnectDataStructures();
        }

        public double Evaluate(double[] x)
        {
            //for (int i = 0; i < x.Length; i++)
            //{
            //    truss10.Elements[i].Section = x[i];
            //}
            //SolverSkyline solution = new SolverSkyline(truss10);
            //ProblemStructural provider = new ProblemStructural(truss10, solution.SubdomainsDictionary);
            //LinearAnalyzer childAnalyzer = new LinearAnalyzer(solution, solution.SubdomainsDictionary);
            //StaticAnalyzer parentAnalyzer = new StaticAnalyzer(provider, childAnalyzer, solution.SubdomainsDictionary);

            //parentAnalyzer.BuildMatrices();
            //parentAnalyzer.Initialize();
            //parentAnalyzer.Solve();

            throw new NotImplementedException();
        }
    }
}