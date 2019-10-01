using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Analyzers.Loading;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.DomainDecomposition;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.XFEM.CrackGeometry;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Solvers;

// TODO: fix a bug that happens when the crack has almost reached the boundary, is inside but no tip can be found
namespace ISAAR.MSolve.XFEM.Analyzers
{
    /// <summary>
    /// Implements crack propagation under static loading with linear material behavior. Based on Linear Elastic Fracture 
    /// Mechanics. Appropriate for brittle materials or fatigue crack propagation analysis. For now, it only works with XFEM.
    /// </summary>
    public class QuasiStaticCrackPropagationAnalyzerMpi //: IAnalyzer
    {
        private const int displacementsTag = 0;

        private readonly ICrackDescriptionMpi crack;
        private readonly double fractureToughness;
        private readonly int maxIterations;
        private readonly XModelMpi model;
        private readonly TipAdaptivePartitioner partitioner; //TODO: Refactor its injection and usage
        private readonly ProcessDistribution procs;
        private readonly bool reanalysis = true;

        //private readonly IStaticProvider problem; //TODO: refactor and use this instead
        private readonly ElementStructuralStiffnessProvider problem = new ElementStructuralStiffnessProvider();
        private readonly DirichletEquivalentLoadsAssembler loadsAssembler; 

        private readonly ISolverMpi solver;
        private HashSet<ISubdomain> newTipEnrichedSubdomains_master;
        private CrackPropagationTermination termination;

        public QuasiStaticCrackPropagationAnalyzerMpi(ProcessDistribution processDistribution, XModelMpi model, ISolverMpi solver,
            /*IStaticProvider problem,*/ ICrackDescriptionMpi crack, double fractureToughness, int maxIterations, 
            TipAdaptivePartitioner partitioner = null)
        {
            this.procs = processDistribution;
            this.model = model;
            this.solver = solver;
            //this.problem = problem;
            this.crack = crack;
            this.fractureToughness = fractureToughness;
            this.maxIterations = maxIterations;
            this.partitioner = partitioner;

            //TODO: Refactor problem structural and remove the next
            problem = new ElementStructuralStiffnessProvider();
            loadsAssembler = new DirichletEquivalentLoadsAssembler(problem); ;
        }

        public IDomainDecompositionLogger DDLogger { get; set; }
        public CrackPropagationTermination Termination => termination;

        public void Initialize(bool isFirstAnalysis = true)
        {
            // The order in which the next initializations happen is very important.
            if (isFirstAnalysis) model.ConnectDataStructures();

            //solver.Initialize(); //TODO: not sure about this one.
        }

        /// <summary>
        /// Returns the crack path after repeatedly executing: XFEM analysis, SIF calculation, crack propagation
        /// </summary>
        /// <returns></returns>
        public void Analyze()
        {
            int analysisStep;
            for (analysisStep = 0; analysisStep < maxIterations; ++analysisStep)
            {
                if (procs.IsMasterProcess)
                {
                    Debug.WriteLine($"Process {procs.OwnRank}: Crack propagation step {analysisStep}");
                    Console.WriteLine($"Process {procs.OwnRank}: Crack propagation step {analysisStep}");
                    Console.WriteLine($"Process {procs.OwnRank}: Crack tip ({crack.CrackTips[0].X}, {crack.CrackTips[0].Y})");


                    // Apply the updated enrichements.
                    crack.UpdateEnrichments();

                    // Update the mesh partitioning and identify unmodified subdomains to avoid fully processing them again.
                    UpdateSubdomains_master();
                }

                // Scatter and crack data
                Console.WriteLine($"Process {procs.OwnRank}: Scattering subdomain and crack data");
                model.ScatterSubdomains();
                crack.ScatterCrackData(model);

                // Order and count dofs
                Console.WriteLine($"Process {procs.OwnRank}: Ordering dofs and crack data");
                solver.OrderDofs(false);
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                ILinearSystem linearSystem = solver.GetLinearSystem(subdomain);
                linearSystem.Reset(); // Necessary to define the linear system's size 
                linearSystem.Subdomain.Forces = Vector.CreateZero(linearSystem.Size);

                // Create the stiffness matrix and then the forces vector
                Console.WriteLine($"Process {procs.OwnRank}: Calculating matrix and rhs");
                //problem.ClearMatrices();
                BuildMatrices();
                model.ApplyLoads();
                LoadingUtilities.ApplyNodalLoadsMpi(procs, model, solver);
                linearSystem.RhsVector = linearSystem.Subdomain.Forces;
                loadsAssembler.ApplyEquivalentNodalLoads(subdomain, linearSystem.RhsVector);

                // Plot domain decomposition data, if necessary
                if (procs.IsMasterProcess)
                {
                    if (DDLogger != null) DDLogger.PlotSubdomains(model);
                }

                // Solve the linear system
                Console.WriteLine($"Process {procs.OwnRank}: Solving linear system(s)");
                solver.Solve();

                //// Output field data
                //if (fieldOutput != null)
                //{
                //    fieldOutput.WriteOutputData(solver.DofOrderer, freeDisplacements, constrainedDisplacements, iteration);
                //}

                // Let the crack propagate
                Console.WriteLine($"Process {procs.OwnRank}: Propagating the crack.");
                Dictionary<int, Vector> freeDisplacements = GatherDisplacementsToMaster(linearSystem);
                GatherSubdomainFreeDofOrderingsToMaster();
                if (procs.IsMasterProcess) crack.Propagate(freeDisplacements);

                // Check convergence 
                Console.WriteLine($"Process {procs.OwnRank}: Checking convergence.");
                bool mustTerminate = false;
                if (procs.IsMasterProcess) mustTerminate = MustTerminate_master(analysisStep);
                procs.Communicator.Broadcast(ref mustTerminate, procs.MasterProcess);
                //procs.Communicator.Broadcast(ref termination, procs.MasterProcess); //TODO: This needs serialization of the enum and might not be necessary
            }
            termination = CrackPropagationTermination.RequiredIterationsWereCompleted;
        }
        
        private void BuildMatrices()
        {
            solver.BuildGlobalMatrix(problem);
        }

        // TODO: Abstract this and add Tanaka_1974 approach
        private double CalculateEquivalentSIF_master(double sifMode1, double sifMode2)
        {
            return Math.Sqrt(sifMode1 * sifMode1 + sifMode2 * sifMode2);
        }

        private HashSet<ISubdomain> FindSubdomainsWithNewHeavisideEnrichedNodes_master()
        {
            var newHeavisideEnrichedSubdomains = new HashSet<ISubdomain>();
            foreach (ISet<XNode> heavisideNodes in crack.CrackBodyNodesNew.Values)
            {
                foreach (XNode node in heavisideNodes)
                {
                    newHeavisideEnrichedSubdomains.UnionWith(node.SubdomainsDictionary.Values);
                }
            }
            return newHeavisideEnrichedSubdomains;
        }

        private HashSet<ISubdomain> FindSubdomainsWithNewTipEnrichedNodes_master()
        {
            var newTipEnrichedSubdomains = new HashSet<ISubdomain>();
            foreach (ISet<XNode> tipNodes in crack.CrackTipNodesNew.Values)
            {
                foreach (XNode node in tipNodes)
                {
                    newTipEnrichedSubdomains.UnionWith(node.SubdomainsDictionary.Values);
                }
            }
            return newTipEnrichedSubdomains;
        }

        private Dictionary<int, Vector> GatherDisplacementsToMaster(ILinearSystem linearSystem)
        {
            Dictionary<int, Vector> freeDisplacements = null;
            if (procs.IsMasterProcess)
            {
                freeDisplacements = new Dictionary<int, Vector>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) freeDisplacements[linearSystem.Subdomain.ID] = (Vector)(linearSystem.Solution);
                    else
                    {
                        double[] u = MpiUtilities.ReceiveArray<double>(procs.Communicator, p, displacementsTag);
                        freeDisplacements[procs.GetSubdomainIdOfProcess(p)] = Vector.CreateFromArray(u);
                    }
                }
            }
            else
            {
                MpiUtilities.SendArray<double>(procs.Communicator, linearSystem.Solution.CopyToArray(),
                    procs.MasterProcess, displacementsTag);
            }
            return freeDisplacements;
        }

        private void GatherSubdomainFreeDofOrderingsToMaster()
        {
            var globalDofOrdering = (GlobalFreeDofOrderingMpi)model.GlobalDofOrdering;
            globalDofOrdering.GatherSubdomainDofOrderings();
            if (procs.IsMasterProcess)
            {
                foreach (ISubdomain subdomain in model.EnumerateSubdomains())
                {
                    subdomain.FreeDofOrdering = model.GlobalDofOrdering.GetSubdomainDofOrdering(subdomain);
                }
            }
        }

        private bool MustTerminate_master(int analysisStep)
        {
            // Check convergence 
            //TODO: Perhaps this should be done by the crack geometry or the Propagator itself and handled via exceptions 
            foreach (var tipPropagator in crack.CrackTipPropagators)
            {
                double sifEffective = CalculateEquivalentSIF_master(tipPropagator.Value.Logger.SIFsMode1[analysisStep],
                    tipPropagator.Value.Logger.SIFsMode2[analysisStep]);
                //Console.WriteLine("Keff = " + sifEffective);
                if (sifEffective >= fractureToughness)
                {
                    termination = CrackPropagationTermination.FractureToughnessIsExceeded;
                    return true;
                }
                if (!model.Boundary.IsInside(tipPropagator.Key))
                {
                    termination = CrackPropagationTermination.CrackExitsDomainBoundary;
                    return true;
                }
            }
            return false;
        }

        private void UpdateSubdomains_master()
        {
            if (model.NumSubdomains == 1) return;

            if (newTipEnrichedSubdomains_master == null) 
            {
                // First analysis step: All subdomains must be fully processed.
                if (partitioner != null) partitioner.UpdateSubdomains();
                foreach (ISubdomain subdomain in model.EnumerateSubdomains())
                {
                    subdomain.ConnectivityModified = true;
                    subdomain.StiffnessModified = true;
                }

                // Prepare for the next analysis step
                newTipEnrichedSubdomains_master = FindSubdomainsWithNewTipEnrichedNodes_master();
            }
            else
            {
                // Update the mesh partitioning, if necessary
                HashSet<ISubdomain> modifiedSubdomains;
                if (partitioner != null) modifiedSubdomains = partitioner.UpdateSubdomains();
                else modifiedSubdomains = new HashSet<ISubdomain>();

                if (reanalysis)
                {
                    // Set them all to unmodified.
                    foreach (ISubdomain subdomain in model.EnumerateSubdomains())
                    {
                        subdomain.ConnectivityModified = false;
                        subdomain.StiffnessModified = false;
                    }

                    // The modified subdomains are the ones containing nodes enriched with tip or Heaviside functions during the  
                    // current analysis step. Also the ones that had tip enriched nodes in the previous step.
                    modifiedSubdomains.UnionWith(newTipEnrichedSubdomains_master);
                    newTipEnrichedSubdomains_master = FindSubdomainsWithNewTipEnrichedNodes_master(); // Prepare for the next analysis step
                    modifiedSubdomains.UnionWith(newTipEnrichedSubdomains_master);
                    HashSet<ISubdomain> newHeavisideSubdomains = FindSubdomainsWithNewHeavisideEnrichedNodes_master();
                    modifiedSubdomains.UnionWith(newHeavisideSubdomains);

                    foreach (ISubdomain subdomain in modifiedSubdomains)
                    {
                        subdomain.ConnectivityModified = true;
                        subdomain.StiffnessModified = true;
                    }
                }
            }
        }
    }
}
