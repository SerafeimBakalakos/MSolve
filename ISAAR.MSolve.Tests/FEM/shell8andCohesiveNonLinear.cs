﻿using System.Collections.Generic;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Numerical.Commons;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Interfaces;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Skyline;
using Xunit;

namespace ISAAR.MSolve.Tests.FEM
{
    public static class Shell8andCohesiveNonLinear
    {
        private const int subdomainID = 1;

        [Fact]
        private static void RunTest()
        {
            IReadOnlyList<Dictionary<int, double>> expectedDisplacements = GetExpectedDisplacements();
            IncrementalDisplacementsLog computedDisplacements = SolveModel();
            Assert.True(AreDisplacementsSame(expectedDisplacements, computedDisplacements));
        }

        [Fact]
        private static void RunTest_v2()
        {
            IReadOnlyList<Dictionary<int, double>> expectedDisplacements = GetExpectedDisplacements();
            IncrementalDisplacementsLog computedDisplacements = SolveModel_v2();
            Assert.True(AreDisplacementsSame(expectedDisplacements, computedDisplacements));
        }

        private static bool AreDisplacementsSame(IReadOnlyList<Dictionary<int, double>> expectedDisplacements, IncrementalDisplacementsLog computedDisplacements)
        {
            var comparer = new ValueComparer(1E-10);
            for (int iter = 0; iter < expectedDisplacements.Count; ++iter)
            {
                foreach (int dof in expectedDisplacements[iter].Keys)
                {
                    if (!comparer.AreEqual(expectedDisplacements[iter][dof], computedDisplacements.GetTotalDisplacement(iter, subdomainID, dof)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static IReadOnlyList<Dictionary<int, double>> GetExpectedDisplacements()
        {
            var expectedDisplacements = new Dictionary<int, double>[5]; //TODO: this should be 11 EINAI ARRAY APO DICTIONARIES

            expectedDisplacements[0] = new Dictionary<int, double> {
    { 0,-1.501306714739351400e-05 }, {11,4.963733738129490800e-06 }, {23,-1.780945407868029400e-05 }, {35,-1.499214801866540600e-05 }, {39,-5.822833969672272200e-05 }};
            expectedDisplacements[1] = new Dictionary<int, double> {
    { 0,-1.500991892603005000e-05 }, {11,4.962619842302796000e-06 }, {23,-1.780557361553905700e-05 }, {35,-1.498958552758854400e-05 }, {39,-5.821676140520536400e-05 }};
            expectedDisplacements[2] = new Dictionary<int, double> {
    { 0,-3.001954880280401800e-05 }, {11,9.925100656477526600e-06 }, {23,-3.561116405104391700e-05 }, {35,-2.997946837566090700e-05 }, {39,-1.164336113147322500e-04 }};
            expectedDisplacements[3] = new Dictionary<int, double> {
    { 0,-3.074327250558424700e-05 }, {11,1.064972618932890100e-05 }, {23,-3.846410374898863100e-05 }, {35,-3.069783728664514200e-05 }, {39,-1.191612724600880000e-04 }};
            expectedDisplacements[4] = new Dictionary<int, double> {
    { 0,-3.074281618479765600e-05 }, {11,1.064926767853693300e-05 }, {23,-3.846254167901110600e-05 }, {35,-3.069737876082750600e-05 }, {39,-1.191596225034872200e-04 }};


            return expectedDisplacements;
        }

        private static IncrementalDisplacementsLog SolveModel()
        {
            Numerical.LinearAlgebra.VectorExtensions.AssignTotalAffinityCount();
            Model model = new Model();
            model.SubdomainsDictionary.Add(subdomainID, new Subdomain() { ID = subdomainID });

            ShellAndCohesiveRAM_11tlkShellPaktwsh(model);


            model.ConnectDataStructures();            

            var linearSystems = new Dictionary<int, ILinearSystem>(); //I think this should be done automatically 
            linearSystems[subdomainID] = new SkylineLinearSystem(subdomainID, model.Subdomains[0].Forces);

            ProblemStructural provider = new ProblemStructural(model, linearSystems);

            var solver = new SolverSkyline(linearSystems[subdomainID]);
            var linearSystemsArray = new[] { linearSystems[subdomainID] };
            var subdomainUpdaters = new[] { new NonLinearSubdomainUpdater(model.Subdomains[0]) };
            var subdomainMappers = new[] { new SubdomainGlobalMapping(model.Subdomains[0]) };

            var increments = 2;
            var childAnalyzer = new NewtonRaphsonNonLinearAnalyzer(solver, linearSystemsArray, subdomainUpdaters, subdomainMappers, provider, increments, model.TotalDOFs);

            var watchDofs = new Dictionary<int, int[]>();
            watchDofs.Add(subdomainID, new int[5] { 0, 11, 23, 35, 39 });
            var log1 = new IncrementalDisplacementsLog(watchDofs);
            childAnalyzer.IncrementalDisplacementsLog = log1;


            childAnalyzer.SetMaxIterations = 100;
            childAnalyzer.SetIterationsForMatrixRebuild = 1;

            StaticAnalyzer parentAnalyzer = new StaticAnalyzer(provider, childAnalyzer, linearSystems);
            
            parentAnalyzer.BuildMatrices();
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();


            return log1;
        }

        private static IncrementalDisplacementsLog SolveModel_v2()
        {
            Model model = new Model();
            model.SubdomainsDictionary.Add(subdomainID, new Subdomain() { ID = subdomainID });
            ShellAndCohesiveRAM_11tlkShellPaktwsh(model);

            model.ConnectDataStructures();

            // Solver
            var solverBuilder = new SkylineSolver.Builder();
            solverBuilder.DofOrderer = new NodeMajorDofOrderer();
            SkylineSolver solver = solverBuilder.BuildSolver(model);

            //TODO: this should be hidden and handled by the analyzer at another phase
            solver.LinearSystems[subdomainID].RhsVector = Vector.CreateFromArray(model.SubdomainsDictionary[subdomainID].Forces);

            // Problem type
            var provider = new ProblemStructural_v2(model, solver);

            // Analyzers
            var subdomainUpdaters = new[] { new NonLinearSubdomainUpdater_v2(model.SubdomainsDictionary[subdomainID]) };
            var subdomainMappers = new[] { new SubdomainGlobalMapping_v2(model.SubdomainsDictionary[subdomainID]) };
            int increments = 2;
            var childAnalyzer = new NewtonRaphsonNonLinearAnalyzer_v2(solver, subdomainUpdaters, subdomainMappers,
                provider, increments, model.TotalDOFs);
            childAnalyzer.SetMaxIterations = 100;
            childAnalyzer.SetIterationsForMatrixRebuild = 1;
            var parentAnalyzer = new StaticAnalyzer_v2(solver, provider, childAnalyzer);

            // Output
            var watchDofs = new Dictionary<int, int[]>();
            watchDofs.Add(subdomainID, new int[5] { 0, 11, 23, 35, 39 });
            var log1 = new IncrementalDisplacementsLog(watchDofs);
            childAnalyzer.IncrementalDisplacementsLog = log1;

            // Run the anlaysis 
            parentAnalyzer.BuildMatrices(); //TODO: this should be hidden and handled by the (child?) analyzer
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            return log1;
        }

        private static void ShellAndCohesiveRAM_11tlkShellPaktwsh(Model model)
        {
            //PROELEFSI: dhmiourgithike kata to ParadeigmataElegxwnBuilder.ShellAndCohesiveRAM_11ShellPaktwsh(model);
            // allaxame to cohesive element
            // gewmetria
            double Tk = 0.5;

            int nodeID = 1;

            double startX = 0;
            double startY = 0;
            double startZ = 0;
            for (int l = 0; l < 3; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.25, Z = startZ });
                nodeID++;
            }

            startX = 0.25;
            for (int l = 0; l < 2; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.5, Z = startZ });
                nodeID++;
            }

            startX = 0.5;
            for (int l = 0; l < 3; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.25, Z = startZ });
                nodeID++;
            }

            // katw strwsh pou tha paktwthei

            startX = 0;
            for (int l = 0; l < 3; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.25, Z = startZ - 0.5 * Tk });
                nodeID++;
            }

            startX = 0.25;
            for (int l = 0; l < 2; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.5, Z = startZ - 0.5 * Tk });
                nodeID++;
            }

            startX = 0.5;
            for (int l = 0; l < 3; l++)
            {
                model.NodesDictionary.Add(nodeID, new Node() { ID = nodeID, X = startX, Y = startY + l * 0.25, Z = startZ - 0.5 * Tk });
                nodeID++;
            }

            double[][] VH = new double[8][];

            for (int j = 0; j < 8; j++)
            {
                VH[j] = new double[3];
                VH[j][0] = 0;
                VH[j][1] = 0;
                VH[j][2] = 1;
            }
            // perioxh gewmetrias ews edw

            // constraints

            nodeID = 9;
            for (int j = 0; j < 8; j++)
            {
                model.NodesDictionary[nodeID].Constraints.Add(new Constraint { DOF = DOFType.X });
                model.NodesDictionary[nodeID].Constraints.Add(new Constraint { DOF = DOFType.Y });
                model.NodesDictionary[nodeID].Constraints.Add(new Constraint { DOF = DOFType.Z });
                nodeID++;
            }
            //perioxh constraints ews edw

            // perioxh materials 
            BenzeggaghKenaneCohesiveMaterial material1 = new BenzeggaghKenaneCohesiveMaterial()
            {
                T_o_3 = 57, // New load case argurhs NR_shell_coh.m
                D_o_3 = 5.7e-5,
                D_f_3 = 0.0098245610,
                T_o_1 = 57,
                D_o_1 = 5.7e-5,
                D_f_1 = 0.0098245610,
                n_curve = 1.4,
            };

            //ElasticMaterial3D material2 = new ElasticMaterial3D()
            //{
            //    YoungModulus = 1353000,
            //    PoissonRatio = 0.3,
            //};
            ShellElasticMaterial material2 = new ShellElasticMaterial()
            {
                YoungModulus = 1353000,
                PoissonRatio = 0.3,
                ShearCorrectionCoefficientK = 5 / 6,
            };
            // perioxh materials ews edw


            //eisagwgh tou shell element
            double[] Tk_vec = new double[8];
            for (int j = 0; j < 8; j++)
            {
                Tk_vec[j] = Tk;
            }

            Element e1;
            e1 = new Element()
            {
                ID = 1,
                ElementType = new Shell8NonLinear(material2, GaussLegendre3D.GetQuadratureWithOrder(3, 3, 3))// 3, 3, 3
                {
                    oVn_i = VH,
                    tk = Tk_vec,
                }
            };
            e1.NodesDictionary.Add(8, model.NodesDictionary[8]);
            e1.NodesDictionary.Add(3, model.NodesDictionary[3]);
            e1.NodesDictionary.Add(1, model.NodesDictionary[1]);
            e1.NodesDictionary.Add(6, model.NodesDictionary[6]);
            e1.NodesDictionary.Add(5, model.NodesDictionary[5]);
            e1.NodesDictionary.Add(2, model.NodesDictionary[2]);
            e1.NodesDictionary.Add(4, model.NodesDictionary[4]);
            e1.NodesDictionary.Add(7, model.NodesDictionary[7]);

            int subdomainID = 1; // tha mporei kai na dinetai sto hexabuilder opws sto MakeBeamBuilding
            model.ElementsDictionary.Add(e1.ID, e1);
            model.SubdomainsDictionary[subdomainID].ElementsDictionary.Add(e1.ID, e1);
            //eisagwgh shell ews edw

            // eisagwgh tou cohesive element
            int[] coh_global_nodes;
            coh_global_nodes = new int[] { 8, 3, 1, 6, 5, 2, 4, 7, 16, 11, 9, 14, 13, 10, 12, 15 };

            Element e2;
            e2 = new Element()
            {
                ID = 2,
                ElementType = new CohesiveShell8ToHexa20(material1, GaussLegendre2D.GetQuadratureWithOrder(3,3))
                {
                    oVn_i = VH,
                    tk = Tk_vec,
                    ShellElementSide = 0,
                }
            };

            for (int j = 0; j < 16; j++)
            {
                e2.NodesDictionary.Add(coh_global_nodes[j], model.NodesDictionary[coh_global_nodes[j]]);
            }

            model.ElementsDictionary.Add(e2.ID, e2);
            model.SubdomainsDictionary[subdomainID].ElementsDictionary.Add(e2.ID, e2);
            // eisagwgh cohesive ews edw

            // perioxh loads
            double value_ext;
            value_ext = 2 * 2.5 * 0.5;

            int[] points_with_negative_load;
            points_with_negative_load = new int[] { 1, 3, 6, 8 };
            int[] points_with_positive_load;
            points_with_positive_load = new int[] { 2, 4, 5, 7 };

            Load load1;
            Load load2;

            // LOADCASE '' orthi ''
            //for (int j = 0; j < 4; j++)
            //{
            //    load1 = new Load()
            //    {
            //        Node = model.NodesDictionary[points_with_negative_load[j]],
            //        DOF = DOFType.Z,
            //        Amount = -0.3333333 * value_ext,
            //    };
            //    model.Loads.Add(load1);

            //    load2 = new Load()
            //    {
            //        Node = model.NodesDictionary[points_with_positive_load[j]],
            //        DOF = DOFType.Z,
            //        Amount = 1.3333333 * value_ext,
            //    };
            //    model.Loads.Add(load2);
            //}

            // LOADCASE '' orthi '' dixws ta duo prwta fortia  (-0.3333) kai (1.3333)
            for (int j = 0; j < 3; j++)
            {
                load1 = new Load()
                {
                    Node = model.NodesDictionary[points_with_negative_load[j + 1]],
                    DOF = DOFType.Z,
                    Amount = -0.3333333 * value_ext,
                };
                model.Loads.Add(load1);

                load2 = new Load()
                {
                    Node = model.NodesDictionary[points_with_positive_load[j + 1]],
                    DOF = DOFType.Z,
                    Amount = 1.3333333 * value_ext,
                };
                model.Loads.Add(load2);
            }


            // perioxh loads ews edw
        }

    }

}