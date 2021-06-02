using System.Collections.Generic;
using System.Diagnostics;

using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;

//TODO: Instead of storing the raw CSR arrays, use a reusable DOK or CsrIndexer class. That class should provide methods to 
//      assemble the values part of the global matrix more efficiently than the general purpose DOK. The general purpose DOK 
//      should only be used to assemble the first global matrix and whenever the dof ordering changes. Now it is used everytime 
//      and the indexing arrays are discarded.
namespace MGroup.Solvers.Assemblers
{
	

	public class DokRowMajorAssembler
	{
		private const string name = "CsrAssembler"; // for error messages
		private readonly bool sortColsOfEachRow;
		private readonly bool isSymmetric;
		private ConstrainedMatricesAssembler constrainedAssembler = new ConstrainedMatricesAssembler();

		bool isIndexerCached = false;
		private int[] cachedColIndices, cachedRowOffsets;

		public DokRowMajorAssembler(bool isSymmetric = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
			this.isSymmetric = isSymmetric;
		}

		public DokRowMajor BuildGlobalMatrix(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
		{
			int numFreeDofs = dofOrdering.NumFreeDofs;
			var subdomainMatrix = DokRowMajor.CreateEmpty(numFreeDofs, numFreeDofs);

			foreach (IElement element in elements)
			{
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapFreeDofsElementToSubdomain(element);
				IMatrix elementMatrix = matrixProvider.Matrix(element);
				if (isSymmetric)
				{
					subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, elementDofIndices, subdomainDofIndices);
				}
				else
				{
					subdomainMatrix.AddSubmatrix(elementMatrix, elementDofIndices, subdomainDofIndices, 
						elementDofIndices, subdomainDofIndices);
				}
			}
			
			return subdomainMatrix;
		}

		public (DokRowMajor matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree,
			IMatrixView matrixConstrConstr) BuildGlobalSubmatrices(
			ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering,
			IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
		{
			int numFreeDofs = freeDofOrdering.NumFreeDofs;
			var subdomainMatrix = DokRowMajor.CreateEmpty(numFreeDofs, numFreeDofs);

			//TODO: also reuse the indexers of the constrained matrices.
			constrainedAssembler.InitializeNewMatrices(freeDofOrdering.NumFreeDofs, constrainedDofOrdering.NumConstrainedDofs);

			// Process the stiffness of each element
			foreach (IElement element in elements)
			{
				(int[] elementDofsFree, int[] subdomainDofsFree) = freeDofOrdering.MapFreeDofsElementToSubdomain(element);
				(int[] elementDofsConstrained, int[] subdomainDofsConstrained) =
					constrainedDofOrdering.MapConstrainedDofsElementToSubdomain(element);

				IMatrix elementMatrix = matrixProvider.Matrix(element);
				subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, elementDofsFree, subdomainDofsFree);
				constrainedAssembler.AddElementMatrix(elementMatrix, elementDofsFree, subdomainDofsFree,
					elementDofsConstrained, subdomainDofsConstrained);
			}

			// Create the free and constrained matrices. 
			subdomainMatrix = null; // Let the DOK be garbaged collected early, in case there isn't sufficient memory.
			(CsrMatrix matrixConstrFree, CsrMatrix matrixConstrConstr) = constrainedAssembler.BuildMatrices();
			return (subdomainMatrix, matrixConstrFree, matrixConstrFree.TransposeToCSC(false), matrixConstrConstr);
		}

		public void HandleDofOrderingWillBeModified()
		{
			//TODO: perhaps the indexer should be disposed altogether. Then again it could be in use by other matrices.
			cachedColIndices = null;
			cachedRowOffsets = null;
			isIndexerCached = false;
		}
	}
}
