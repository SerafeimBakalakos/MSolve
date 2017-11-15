using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Numerical.Optimization.Problem;
using ISAAR.MSolve.FEM.Problems.Structural;
using ISAAR.MSolve.FEM.Problems.Structural.Constitutive;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Logging;
using ISAAR.MSolve.Numerical.LinearAlgebra;
using ISAAR.MSolve.FEM.Problems.Structural.Elements;
using ISAAR.MSolve.Solvers.Skyline;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Analyzers.Interfaces;

namespace ISAAR.MSolve.SamplesConsole.Optimization.StructuralProblems
{
    class Truss10Benchmark : OptimizationProblem
    {
        public Truss10Benchmark()
        {
            this.Dimension = 10;
            this.LowerBound = new double[Dimension];
            LowerBound = LowerBound.Select(i => 0.1).ToArray();

            this.UpperBound = new double[Dimension];
            UpperBound = UpperBound.Select(i => 35.0).ToArray();
            this.DesignFactory = new Truss10Factory();
        }

        private class Truss10Factory : IDesignFactory
        {
            public IDesign CreateDesign(double[] x)
            {
                return new Truss10Design(x);
            }
        }

        private class Truss10Design : IDesign
        {
            public double[] ObjectiveValues { get; }

            public double[] ConstraintValues { get; }

            public Truss10Design(double[] x)
            {
                // Perform simulation
                Model model;
                LinearAnalyzer childAnalyzer;
                Rod2DResults rodResults;
                Solve(x, out model, out childAnalyzer, out rodResults);

                // Get objective value
                this.ObjectiveValues = EvaluateObjective(x, model);

                // Get constraint values
                this.ConstraintValues = EvaluateConstraints(model, childAnalyzer, rodResults);
            }

            private void Solve(double[] x, out Model model, out LinearAnalyzer childAnalyzer,
                out Rod2DResults rodResults)
            {
                VectorExtensions.AssignTotalAffinityCount();

                model = BuildModel(x);

                var linearSystem = new SkylineLinearSystem(1, model.SubdomainsDictionary[1].Forces);
                SolverFBSubstitution solver = new SolverFBSubstitution(linearSystem);
                ProblemStructural provider = new ProblemStructural(model);
                childAnalyzer = new LinearAnalyzer(solver, linearSystem);
                StaticAnalyzer parentAnalyzer = new StaticAnalyzer(provider, childAnalyzer, linearSystem);
                CreateLogs(model, childAnalyzer);
                rodResults = new Rod2DResults(model.SubdomainsDictionary[1], linearSystem); // Let's hope this is the one!

                parentAnalyzer.BuildMatrices();
                parentAnalyzer.Initialize();
                parentAnalyzer.Solve();
            }

            private Model BuildModel(double[] x)
            {
                double youngModulus = 10e4;
                double poissonRatio = 0.3;
                double loadP = 100;

                ElasticMaterial material = new ElasticMaterial() { YoungModulus = youngModulus, PoissonRatio = poissonRatio };

                Model model = new Model();

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

                for (int i = 0; i < nodes.Count; i++)
                {
                    model.NodesDictionary.Add(i + 1, nodes[i]);
                }

                IList<Element> elements = new List<Element>();

                var element1 = new Element() { ID = 1, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[0] } };
                var element2 = new Element() { ID = 2, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[1] } };
                var element3 = new Element() { ID = 3, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[2] } };
                var element4 = new Element() { ID = 4, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[3] } };
                var element5 = new Element() { ID = 5, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[4] } };
                var element6 = new Element() { ID = 6, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[5] } };
                var element7 = new Element() { ID = 7, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[6] } };
                var element8 = new Element() { ID = 8, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[7] } };
                var element9 = new Element() { ID = 9, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[8] } };
                var element10 = new Element() { ID = 10, ElementType = new Rod2D(material) { Density = 0.1, SectionArea = x[9] } };

                element1.AddNode(model.NodesDictionary[3]);
                element1.AddNode(model.NodesDictionary[5]);
                element2.AddNode(model.NodesDictionary[1]);
                element2.AddNode(model.NodesDictionary[3]);
                element3.AddNode(model.NodesDictionary[4]);
                element3.AddNode(model.NodesDictionary[6]);
                element4.AddNode(model.NodesDictionary[2]);
                element4.AddNode(model.NodesDictionary[4]);
                element5.AddNode(model.NodesDictionary[3]);
                element5.AddNode(model.NodesDictionary[4]);
                element6.AddNode(model.NodesDictionary[1]);
                element6.AddNode(model.NodesDictionary[2]);
                element7.AddNode(model.NodesDictionary[4]);
                element7.AddNode(model.NodesDictionary[5]);
                element8.AddNode(model.NodesDictionary[3]);
                element8.AddNode(model.NodesDictionary[6]);
                element9.AddNode(model.NodesDictionary[2]);
                element9.AddNode(model.NodesDictionary[3]);
                element10.AddNode(model.NodesDictionary[1]);
                element10.AddNode(model.NodesDictionary[4]);

                model.ElementsDictionary.Add(element1.ID, element1);
                model.ElementsDictionary.Add(element2.ID, element2);
                model.ElementsDictionary.Add(element3.ID, element3);
                model.ElementsDictionary.Add(element4.ID, element4);
                model.ElementsDictionary.Add(element5.ID, element5);
                model.ElementsDictionary.Add(element6.ID, element6);
                model.ElementsDictionary.Add(element7.ID, element7);
                model.ElementsDictionary.Add(element8.ID, element8);
                model.ElementsDictionary.Add(element9.ID, element9);
                model.ElementsDictionary.Add(element10.ID, element10);

                model.SubdomainsDictionary.Add(1, new Subdomain() { ID = 1 });

                model.SubdomainsDictionary[1].ElementsDictionary.Add(element1.ID, element1);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element2.ID, element2);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element3.ID, element3);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element4.ID, element4);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element5.ID, element5);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element6.ID, element6);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element7.ID, element7);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element8.ID, element8);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element9.ID, element9);
                model.SubdomainsDictionary[1].ElementsDictionary.Add(element10.ID, element10);

                model.NodesDictionary[5].Constraints.Add(DOFType.X);
                model.NodesDictionary[5].Constraints.Add(DOFType.Y);
                model.NodesDictionary[6].Constraints.Add(DOFType.X);
                model.NodesDictionary[6].Constraints.Add(DOFType.Y);

                model.Loads.Add(new Load() { Amount = -loadP, Node = model.NodesDictionary[2], DOF = DOFType.Y });
                model.Loads.Add(new Load() { Amount = -loadP, Node = model.NodesDictionary[4], DOF = DOFType.Y });

                model.ConnectDataStructures();
                return model;
            }

            private void CreateLogs(Model model, LinearAnalyzer childAnalyzer)
            {
                int[] monitoredDOFs = new int[] {
                    model.NodalDOFsDictionary[1][DOFType.Y],
                    model.NodalDOFsDictionary[2][DOFType.Y],
                    model.NodalDOFsDictionary[3][DOFType.Y],
                    model.NodalDOFsDictionary[4][DOFType.Y]
                };
                childAnalyzer.LogFactories[1] = new LinearAnalyzerLogFactory(monitoredDOFs);
                //Element[] stressElements = model.ElementsDictionary.Values.ToArray<Element>();
                //childAnalyzer.LogFactories[1] = new LinearAnalyzerLogFactory(monitoredDOFs,
                //    stressElements, new Element[0]);
            }

            private double[] EvaluateObjective(double[] x, Model model)
            {
                double weight = 0;

                for (int i = 0; i < x.Length; i++)
                {
                    var element_i = model.Elements[i].ElementType as Rod2D;
                    var nodeStart = model.Nodes[0];
                    var nodeEnd = model.Nodes[1];
                    var Length_i = Math.Sqrt(Math.Pow(nodeEnd.X - nodeStart.X, 2) + Math.Pow(nodeEnd.Y - nodeStart.Y, 2));

                    weight += element_i.SectionArea * Length_i;
                }
                return new double[] { weight };
            }

            private double[] EvaluateConstraints(Model model, LinearAnalyzer childAnalyzer,
                Rod2DResults rodResults)
            {
                var constraints = new LinkedList<double>();

                // Displacements
                const double maxDisplacement = 2;
                double max = 0;

                foreach (var displacement in ((DOFSLog)childAnalyzer.Logs[1][0]).DOFValues.Values)
                {
                    if (Math.Abs(displacement) > max)
                    {
                        max = Math.Abs(displacement);
                    }
                }
                constraints.AddLast(max / maxDisplacement - 1.0);

                // Stresses
                double[] minStresses = new double[10];
                minStresses = minStresses.Select(i => -25.0).ToArray();
                double[] maxStresses = new double[10];
                maxStresses = maxStresses.Select(i => 25.0).ToArray();
                double[] stresses = new double[10];

                int counter = 0;
                foreach (var element in model.Elements)
                {
                    stresses[counter++] = rodResults.AxialRod2DStress(element);
                }

                for (int i = 0; i < stresses.Length; ++i)
                {
                    constraints.AddLast(stresses[i] / maxStresses[i] - 1.0);
                    constraints.AddLast(stresses[i] / minStresses[i] - 1.0);
                }

                return constraints.ToArray();
            }
        }
    }
}