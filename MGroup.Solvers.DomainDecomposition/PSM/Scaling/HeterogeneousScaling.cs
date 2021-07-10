using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using System.Collections.Concurrent;
using MGroup.Environments;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;
using MGroup.LinearAlgebra.Distributed.Overlapping;

//TODO: If Jacobi preconditioning is used, then the most time consuming part of finding the relative stiffnesses 
//		(distributedVector.SumOverlappingEntries()) is done there too. The two objects should synchronize to only do that once. 
namespace MGroup.Solvers.DomainDecomposition.PSM.Scaling
{
	public class HeterogeneousScaling : IBoundaryDofScaling
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly PsmDofManager dofManager;
		private readonly IDictionary<int, ISubdomainMatrixManager> matrixManagers;
		private readonly ConcurrentDictionary<int, double[]> relativeStiffnesses = new ConcurrentDictionary<int, double[]>();
		//private readonly ConcurrentDictionary<int, IMappingMatrix> dofMappingBoundaryClusterToSubdomain = 
		//new ConcurrentDictionary<int, IMappingMatrix>();

		public HeterogeneousScaling(IComputeEnvironment environment, IStructuralModel model,
			PsmDofManager dofManager, IDictionary<int, ISubdomainMatrixManager> matrixManagers)
		{
			this.environment = environment;
			this.model = model;
			this.dofManager = dofManager;
			this.matrixManagers = matrixManagers;
		}

		/// <summary>
		/// See eq (6.3) from Papagiannakis bachelor :
		/// Lpb^e = Db^e * Lb^e * inv( (Lb^e)^T * Db^e * Lb^e)
		/// </summary>
		public void CalcSubdomainScaling(DistributedOverlappingIndexer indexer)
		{
			// Build Db^s from each subdomain's Kff
			Func<int, Vector> calcSubdomainDb = subdomainID =>
			{
				//ISubdomain subdomain = model.GetSubdomain(subdomainID);
				IMatrixView Kff = matrixManagers[subdomainID].LinearSystem.Matrix;

				//TODO: This should be a polymorphic method in the LinearAlgebra project. Interface IDiagonalizable 
				//		with methods: GetDiagonal() and GetSubdiagonal(int[] indices). Optimized versions for most storage
				//		formats are possible. E.g. for Symmetric CSR/CSC with ordered indices, the diagonal entry is the 
				//		last of each row/col. For general CSC/CSC with ordered indices, bisection can be used for to locate
				//		the diagonal entry of each row/col in log(nnzPerRow). In any case these should be hidden from DDM classes.
				//TODO: It would be better to extract the diagonal from Kbb directly.
				Vector Df = Kff.GetDiagonal(); 
				
				int[] boundaryDofs = dofManager.GetSubdomainDofs(subdomainID).DofsBoundaryToFree;
				Vector Db = Df.GetSubvector(boundaryDofs);
				
				return Db;
			};
			Dictionary<int, Vector> diagonalStiffnesses = environment.CreateDictionaryPerNode(calcSubdomainDb);

			// Use distributed vectors to let each subdomain inform its neighbors about its stiffness at their common dofs
			var distributedVector = new DistributedOverlappingVector(environment, indexer, diagonalStiffnesses);
			distributedVector.RegularizeOverlappingEntries();

			Action<int> storeRelativeStiffness = subdomainID =>
			{
				relativeStiffnesses[subdomainID] = distributedVector.LocalVectors[subdomainID].RawData;

				// Calculate Lpb^s = Db^s * Lb^s * inv( (Lb^e)^T * Db^e * Lb^e) )
				//BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomainID);
				//var Lpb = new ScalingMatrixRowMajor(
				//	Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, relativeStiffnesses[subdomainID]);
				//dofMappingBoundaryClusterToSubdomain[subdomain.ID] = Lpb;
			};
			environment.DoPerNode(storeRelativeStiffness);
		}
		

		//public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
		//	=> dofMappingBoundaryClusterToSubdomain[subdomainID];

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
		{
			Func<int, SparseVector> calcSubdomainForces = subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
				DofTable boundaryDofs = dofManager.GetSubdomainDofs(subdomainID).DofOrderingBoundary;
				double[] coefficients = relativeStiffnesses[subdomainID];

				//TODO: I go through every node and ignore the ones that are not loaded. 
				//		It would be better to directly access the loaded ones.
				var nonZeroLoads = new SortedDictionary<int, double>();
				foreach (INode node in subdomain.Nodes)
				{
					bool isLoaded = nodalLoads.TryGetDataOfRow(node, out IReadOnlyDictionary<IDofType, double> loadsOfNode);
					if (!isLoaded) continue;

					if (node.SubdomainsDictionary.Count == 1) // optimization for internal dofs
					{
						foreach (var dofLoadPair in loadsOfNode)
						{
							int freeDofIdx = freeDofs[node, dofLoadPair.Key];
							nonZeroLoads[freeDofIdx] = dofLoadPair.Value;
						}
					}
					else
					{
						foreach (var dofLoadPair in loadsOfNode)
						{
							int freeDofIdx = freeDofs[node, dofLoadPair.Key];
							int boundaryDofIdx = boundaryDofs[node, dofLoadPair.Key];
							nonZeroLoads[freeDofIdx] = dofLoadPair.Value * coefficients[boundaryDofIdx];
						}
					}
				}

				return SparseVector.CreateFromDictionary(subdomain.FreeDofOrdering.NumFreeDofs, nonZeroLoads);
			};
			return environment.CreateDictionaryPerNode(calcSubdomainForces);
		}

		public void ScaleForceVector(int subdomainID, Vector subdomainForces)
		{
			int[] boundaryDofs = dofManager.GetSubdomainDofs(subdomainID).DofsBoundaryToFree;
			double[] coefficients = relativeStiffnesses[subdomainID];
			for (int i = 0; i < boundaryDofs.Length; i++)
			{
				double coeff = coefficients[i];
				subdomainForces[boundaryDofs[i]] *= coeff;
			}
		}
	}
}