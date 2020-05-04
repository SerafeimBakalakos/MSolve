
using System;
using System.Collections.Generic;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.NonLinear;
using ISAAR.MSolve.Analyzers.ObjectManagers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.MSAnalysis.remoteMatImplementations;
using ISAAR.MSolve.MultiscaleAnalysis;
using ISAAR.MSolve.MultiscaleAnalysis.Interfaces;
using ISAAR.MSolve.MultiscaleAnalysisMerge.SupportiveClasses;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using Xunit;

namespace ISAAR.MSolve.Tests.FEM
{
    public static class Hexa8NonLinearCantileverDefGradDevelop4MultiscaleGraphene
    {
        private const int subdomainID = 1;
                    
        public static void ParallelNonLinearCantilever(int numProcesses)
        {
                   
            int numSubdomains = numProcesses;
            var procs = ProcessDistribution.CreateDistribution(numProcesses, numSubdomains); // FetiDPDofSeparatorMpiTests .CreateModelAndDofSeparator

            Console.Write("thread sleeping for sychronization");
            //Console.Write($"waiting time = " + procs.OwnRank*20000);
            System.Threading.Thread.Sleep(/*(procs.OwnRank+1)**/20000);

            IRVEbuilder homogeneousRveBuilder1 = new HomogeneousRVEBuilderNonLinearNoRenum();
            IContinuumMaterial3DDefGrad material1 = new MicrostructureDefGrad3D(homogeneousRveBuilder1,
                m => (new SkylineSolver.Builder()).BuildSolver(m), false, 1);
            IMaterialManager materialManager = new MaterialManagerMpi2(material1, procs);

            Model model = new Model();//'

            if(procs.IsMasterProcess)
            {
                BuildCantileverModel(model, materialManager);
            }


            var increments = 2;//.
            var resTol = 1E-3;// sto multiscale xrhsimopoihsame thn default.
            int maxIters = 100;
            int itersRebuild = 1;


            (StaticAnalyzerDevelopMpi parentAnalyzer, LoadControlAnalyzerDevelop4Mpi childAnalyzer) = GetAnalyzers(procs, model, increments, maxIters,
                itersRebuild, resTol, materialManager);
            


            materialManager.Initialize();
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            if (procs.IsMasterProcess)
            {
                
                var log1 = childAnalyzer.TotalDisplacementsPerIterationLog;//
                IReadOnlyList<Dictionary<int, double>> expectedDisplacements = GetExpectedDisplacements();
                bool isProblemSolvedCorrectly = AreDisplacementsSame(expectedDisplacements, log1);
                if (isProblemSolvedCorrectly)
                {
                    Console.WriteLine($"Problem is solved correctly ");
                }
                else
                {
                    Console.WriteLine($"the problem has not been solved correctly");
                }
                if (CnstValues.writeFe2MacroscaleSolution)
                {
                    double[][] solutionVectors = ExtractCalculatedSolutionsMacro(log1, increments, expectedDisplacements);

                    DdmCalculationsGeneral.WriteToFileVector(globalU, rveBuilder.subdomainOutputPath + @"\Msolve_solution\Global_solution_Direct.txt");
                    DdmCalculationsGeneral.WriteToFileVector(uc, rveBuilder.subdomainOutputPath + @"\Msolve_solution\Corner_solution_Direct.txt");
                }

            }


        }

        private static double[][] ExtractCalculatedSolutionsMacro(TotalDisplacementsPerIterationLog log1, int increments, IReadOnlyList<Dictionary<int, double>> expectedDisplacements)
        {
            var comparer = new ValueComparer(1E-13);
            for (int iter = 0; iter < increments;iter++)
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

        private static (StaticAnalyzerDevelopMpi parentAnalyzer, LoadControlAnalyzerDevelop4Mpi childAnalyzer) GetAnalyzers(ProcessDistribution procs,
            Model model, int increments, int maxIters, int itersRebuild, double resTol, IMaterialManager materialManager)
        {
            if (procs.IsMasterProcess)
            {
                // Solver
                var solverBuilder = new SkylineSolver.Builder();
                ISolver solver = solverBuilder.BuildSolver(model);
                // Problem type
                var provider = new ProblemStructural(model, solver);
                var childAnalyzer = new LoadControlAnalyzerDevelop4Mpi(model, solver, provider, increments, maxIters, itersRebuild, resTol, materialManager, procs);
                var parentAnalyzer = new StaticAnalyzerDevelopMpi(model, solver, provider, childAnalyzer, procs);
                var watchDofs = new Dictionary<int, int[]>();
                watchDofs.Add(subdomainID, new int[5] { 0, 11, 23, 35, 47 });
                //var log1 = new TotalDisplacementsPerIterationLog(watchDofs);
                //childAnalyzer.TotalDisplacementsPerIterationLog = log1; //.
                var log1 = new IncrementalDisplacementsLog(watchDofs);
                childAnalyzer.IncrementalDisplacementsLog = log1;

                return (parentAnalyzer, childAnalyzer);
            }
            else
            {
                var childAnalyzer = new LoadControlAnalyzerDevelop4Mpi(materialManager, procs, increments, maxIters, itersRebuild);
                var parentAnalyzer = new StaticAnalyzerDevelopMpi(childAnalyzer, procs);

                return (parentAnalyzer, childAnalyzer);
            }
        }

        public static void BuildCantileverModel(Model model, IMaterialManager materialManager)
        {
            double load_value = 0.00219881744271988174427;
            int subdomainID = 1;
            var subd1 = new Subdomain(subdomainID);
            model.SubdomainsDictionary.Add(subdomainID, subd1);
            IContinuumMaterial3DDefGrad material1 = new RemoteMaterial(materialManager);

            

            double[,] nodeData = new double[,] { {-0.250000,-0.250000,-1.000000},
            {0.250000,-0.250000,-1.000000},
            {-0.250000,0.250000,-1.000000},
            {0.250000,0.250000,-1.000000},
            {-0.250000,-0.250000,-0.500000},
            {0.250000,-0.250000,-0.500000},
            {-0.250000,0.250000,-0.500000},
            {0.250000,0.250000,-0.500000},
            {-0.250000,-0.250000,0.000000},
            {0.250000,-0.250000,0.000000},
            {-0.250000,0.250000,0.000000},
            {0.250000,0.250000,0.000000},
            {-0.250000,-0.250000,0.500000},
            {0.250000,-0.250000,0.500000},
            {-0.250000,0.250000,0.500000},
            {0.250000,0.250000,0.500000},
            {-0.250000,-0.250000,1.000000},
            {0.250000,-0.250000,1.000000},
            {-0.250000,0.250000,1.000000},
            {0.250000,0.250000,1.000000}};

            int[,] elementData = new int[,] {{1,8,7,5,6,4,3,1,2},
            {2,12,11,9,10,8,7,5,6},
            {3,16,15,13,14,12,11,9,10},
            {4,20,19,17,18,16,15,13,14}, };

            // orismos shmeiwn
            for (int nNode = 0; nNode < nodeData.GetLength(0); nNode++)
            {
                model.NodesDictionary.Add(nNode + 1, new Node(id: nNode + 1, x: nodeData[nNode, 0], y: nodeData[nNode, 1], z: nodeData[nNode, 2]));

            }

            // orismos elements 
            Element e1;
            for (int nElement = 0; nElement < elementData.GetLength(0); nElement++)
            {
                e1 = new Element()
                {
                    ID = nElement + 1,
                    ElementType = new Hexa8NonLinearDefGrad(material1, GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2)) // dixws to e. exoume sfalma enw sto beambuilding oxi//edw kaleitai me ena orisma to Hexa8
                };
                for (int j = 0; j < 8; j++)
                {
                    e1.NodesDictionary.Add(elementData[nElement, j + 1], model.NodesDictionary[elementData[nElement, j + 1]]);
                }
                model.ElementsDictionary.Add(e1.ID, e1);
                model.SubdomainsDictionary[subdomainID].Elements.Add(e1.ID, e1);
            }

            // constraint vashh opou z=-1
            for (int k = 1; k < 5; k++)
            {
                model.NodesDictionary[k].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationX });
                model.NodesDictionary[k].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationY });
                model.NodesDictionary[k].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationZ });
            }

            // fortish korufhs
            Load load1;
            for (int k = 17; k < 21; k++)
            {
                load1 = new Load()
                {
                    Node = model.NodesDictionary[k],
                    DOF = StructuralDof.TranslationX,
                    Amount = 1 * load_value
                };
                model.Loads.Add(load1);
            }
        }
        

        private static bool AreDisplacementsSame(IReadOnlyList<Dictionary<int, double>> expectedDisplacements, TotalDisplacementsPerIterationLog computedDisplacements)
        {
            var comparer = new ValueComparer(1E-13);
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
            var expectedDisplacements = new Dictionary<int, double>[2]; //TODO: this should be 11 EINAI ARRAY APO DICTIONARIES

            expectedDisplacements[0] = new Dictionary<int, double> {
                { 0, 0.039075524153873623}, {11, -0.032541895181220408}, {23, -0.057387148941853101}, {35, -0.071994381984550326}, {47, -0.077053554770404833}
            };


            expectedDisplacements[1] = new Dictionary<int, double> {
    { 0,2* 0.039075524153873623}, {11,2*( -0.032541895181220408)}, {23,2*( -0.057387148941853101)}, {35,2*( -0.071994381984550326)}, {47,2*( -0.077053554770404833)}};


            return expectedDisplacements;
        }



    }

}
