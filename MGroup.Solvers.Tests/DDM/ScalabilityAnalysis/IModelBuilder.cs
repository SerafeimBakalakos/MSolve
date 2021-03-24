using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Tests.DDM.ScalabilityAnalysis
{
	public interface IModelBuilder
	{
		double[] DomainLengthPerAxis { get; set; }

		int[] NumElementsPerAxis { get; set; }

		int[] NumSubdomainsPerAxis { get; set; }

		int[] SubdomainSizePerElementSize { get; }

		IStructuralModel CreateMultiSubdomainModel();

		IStructuralModel CreateSingleSubdomainModel();

		(List<int[]> numElements, int[] numSubdomains) GetParametricConfigConstNumSubdomains();

		(int[] numElements, List<int[]> numSubdomains) GetParametricConfigConstNumElements();

		(List<int[]> numElements, List<int[]> numSubdomains) GetParametricConfigConstSubdomainPerElementSize();
	}
}
