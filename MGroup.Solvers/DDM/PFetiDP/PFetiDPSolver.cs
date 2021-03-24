using System;
using System.Collections.Generic;

using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.CoarseProblem;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.PFetiDP.Dofs;
using MGroup.Solvers.DDM.PFetiDP.Preconditioner;
using MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;
using MGroup.Solvers.DDM.Psm.Preconditioner;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering;
using MGroup.Solvers.DofOrdering.Reordering;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DDM.PFetiDP
{
	public class PFetiDPSolver : PsmSolver
	{
		private readonly ICornerDofSelection cornerDofSelection;
		private readonly IFetiDPCoarseProblem coarseProblem;
		private readonly IFetiDPDofSeparator dofSeparatorFetiDP;
		private readonly IPFetiDPDofSeparator dofSeparatorPFetiDP;
		private readonly IFetiDPMatrixManager matrixManagerFetiDP;

		private PFetiDPSolver(IProcessingEnvironment environment, IStructuralModel model, IList<Cluster> clusters, 
			ICornerDofSelection cornerDofSelection, IDofOrderer dofOrderer, IPsmDofSeparator dofSeparatorPsm, 
			IFetiDPDofSeparator dofSeparatorFetiDP, IPFetiDPDofSeparator dofSeparatorPFetiDP,
			IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm, IFetiDPMatrixManager matrixManagerFetiDP,
			IPsmPreconditioner preconditioner, IFetiDPCoarseProblem coarseProblem, 
			IInterfaceProblemSolver interfaceProblemSolver, bool isHomogeneous)
			: base(environment, model, clusters, dofOrderer, dofSeparatorPsm, matrixManagerBasic, matrixManagerPsm,
				  preconditioner, interfaceProblemSolver, isHomogeneous, "P-Feti-DP Solver")
		{
			this.cornerDofSelection = cornerDofSelection;
			this.dofSeparatorFetiDP = dofSeparatorFetiDP;
			this.dofSeparatorPFetiDP = dofSeparatorPFetiDP;
			this.matrixManagerFetiDP = matrixManagerFetiDP;
			this.coarseProblem = coarseProblem;
		}

		public override void Initialize()
		{
			base.Initialize();

			dofSeparatorFetiDP.SeparateCornerRemainderDofs(cornerDofSelection);
			Action<ISubdomain> reorderRemainderDofs = subdomain =>
			{
				matrixManagerFetiDP.ReorderRemainderDofs(subdomain.ID);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, reorderRemainderDofs);

			dofSeparatorFetiDP.OrderGlobalCornerDofs();
			coarseProblem.ReorderGlobalCornerDofs(dofSeparatorFetiDP);
			dofSeparatorFetiDP.MapCornerDofs();

			dofSeparatorPFetiDP.MapDofsPsmFetiDP(stiffnessDistribution);

			Logger.LogNumDofs("Global corner dofs", dofSeparatorFetiDP.NumGlobalCornerDofs);
		}

		public override void Solve()
		{
			var mappingsLc = new Dictionary<int, BooleanMatrixRowsToColumns>();
			var matricesKccStar = new Dictionary<int, IMatrix>();
			Action<ISubdomain> calcSubdomainMatrices = subdomain =>
			{
				int s = subdomain.ID;
				matrixManagerPsm.ExtractKiiKbbKib(s);
				matrixManagerPsm.InvertKii(s);

				matrixManagerFetiDP.ExtractKrrKccKrc(s);
				matrixManagerFetiDP.InvertKrr(s);
				matrixManagerFetiDP.CalcSchurComplementOfRemainderDofs(s);

				lock (mappingsLc) mappingsLc[s] = dofSeparatorFetiDP.GetDofMappingCornerGlobalToSubdomain(s);
				lock (matricesKccStar) matricesKccStar[s] = matrixManagerFetiDP.GetSchurComplementOfRemainderDofs(s);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, calcSubdomainMatrices);

			coarseProblem.CreateAndInvertCoarseProblemMatrix(mappingsLc, matricesKccStar);

			IInterfaceProblemMatrix interfaceProblemMatrix = 
				new InterfaceProblemMatrix(environment, model, dofSeparatorPsm, matrixManagerPsm);
			SolveInterfaceProblem(interfaceProblemMatrix);
		}

		public class Builder
		{
			public Builder()
			{
				DofOrderer = new ReusingDofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

				//TODO: perhaps use a custom convergence check like in FETI
				var pcgBuilder = new PcgAlgorithm.Builder();
				pcgBuilder.ResidualTolerance = 1E-6;
				pcgBuilder.MaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
				InterfaceProblemSolver = new InterfaceProblemSolverPcg(pcgBuilder.Build());
				IsHomogeneousProblem = true;

				CoarseProblemFactory = new FetiDPCoarseProblemCSparse.Factory();
				MatrixManagerFactory = new PFetiDPMatrixManagerFactoryCSparse();
			}

			public IFetiDPCoarseProblemFactory CoarseProblemFactory { get; set; }

			public IProcessingEnvironment ComputingEnvironment { get; set; } = new ProcessingEnvironment(
				new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());

			public IDofOrderer DofOrderer { get; set; }

			public IInterfaceProblemSolver InterfaceProblemSolver { get; set; }

			public bool IsHomogeneousProblem { get; set; }

			public IPFetiDPMatrixManagerFactory MatrixManagerFactory { get; set; }

			public PFetiDPSolver BuildSolver(IStructuralModel model, ICornerDofSelection cornerDofSelection, IList<Cluster> clusters)
			{
				var dofSeparatorPsm = new PsmDofSeparator(ComputingEnvironment, model, clusters);
				var dofSeparatorFetiDP = new FetiDPDofSeparator(ComputingEnvironment, model, clusters);
				var dofSeparatorPFetiDP = new PFetiDPDofSeparator(
					ComputingEnvironment, model, clusters, dofSeparatorPsm, dofSeparatorFetiDP);

				var (matrixManagerBasic, matrixManagerPsm, matrixManagerFetiDP) =
					MatrixManagerFactory.CreateMatrixManagers(model, dofSeparatorPsm, dofSeparatorFetiDP);

				var coarseProblem = CoarseProblemFactory.Create(ComputingEnvironment, model);
				var preconditioner = new PFetiDPPreconditioner(ComputingEnvironment, model, clusters, dofSeparatorPsm,
						dofSeparatorFetiDP, dofSeparatorPFetiDP, matrixManagerFetiDP, coarseProblem);

				return new PFetiDPSolver(ComputingEnvironment, model, clusters, cornerDofSelection, DofOrderer, dofSeparatorPsm,
					dofSeparatorFetiDP, dofSeparatorPFetiDP, matrixManagerBasic, matrixManagerPsm, matrixManagerFetiDP,
					preconditioner, coarseProblem, InterfaceProblemSolver, IsHomogeneousProblem);
			}
		}
	}
}
