using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Tests.DDM.ScalabilityAnalysis
{
	public class CantilevelBeam2D : IModelBuilder
	{
		public double[] DomainLengthPerAxis { get; set; } = { 8, 2 };

		public int[] NumElementsPerAxis { get; set; }

		public int[] NumSubdomainsPerAxis { get; set; }

		public int[] SubdomainSizePerElementSize 
		{ 
			get
			{
				var result = new int[2];
				for (int i = 0; i < result.Length; i++)
				{
					if (NumElementsPerAxis[i] % NumSubdomainsPerAxis[i] != 0)
					{
						throw new ArgumentException("Elements per axis must be a multple of subdomains per axis");
					}
					result[i] = NumElementsPerAxis[i] / NumSubdomainsPerAxis[i];
				}
				return result;
			}
		}


		public double YoungModulus { get; set; } = 2.1E7;

		public double EndPointLoad { get; set; } = -2E4;

		public IStructuralModel CreateMultiSubdomainModel()
		{
			return CreateModel(NumSubdomainsPerAxis);
		}

		public IStructuralModel CreateSingleSubdomainModel()
		{
			return CreateModel(new int[] { 1, 1 });
		}

		public (List<int[]> numElements, int[] numSubdomains) GetParametricConfigConstNumSubdomains()
		{
			int[] numSubdomains = { 16, 4 };
			var numElements = new List<int[]>();

			numElements.Add(new int[] { 32, 8 });
			numElements.Add(new int[] { 64, 16 });
			numElements.Add(new int[] { 128, 32 });
			numElements.Add(new int[] { 256, 64 });
			numElements.Add(new int[] { 512, 128 });
			numElements.Add(new int[] { 1024, 256 });
			//numElements.Add(new int[] { 2048, 512 });

			return (numElements, numSubdomains);
		}

		public (int[] numElements, List<int[]> numSubdomains) GetParametricConfigConstNumElements()
		{
			int[] numElements = { 512, 128 };
			var numSubdomains = new List<int[]>();

			//numSubdomains.Add(new int[] { 4, 1 });
			numSubdomains.Add(new int[] { 8, 2 });
			numSubdomains.Add(new int[] { 16, 4 });
			numSubdomains.Add(new int[] { 32, 8 });
			numSubdomains.Add(new int[] { 64, 16 });
			numSubdomains.Add(new int[] { 128, 32 });
			//numSubdomains.Add(new int[] { 256, 64 });

			return (numElements, numSubdomains);
		}

		public (List<int[]> numElements, List<int[]> numSubdomains) GetParametricConfigConstSubdomainPerElementSize()
		{
			var numElements = new List<int[]>();
			var numSubdomains = new List<int[]>();

			numElements.Add(new int[] { 32, 8 });
			numElements.Add(new int[] { 64, 16 });
			numElements.Add(new int[] { 128, 32 });
			numElements.Add(new int[] { 256, 64 });
			numElements.Add(new int[] { 512, 128 });
			numElements.Add(new int[] { 1024, 256 });
			//numElements.Add(new int[] { 2048, 512 });

			numSubdomains.Add(new int[] { 4, 1 });
			numSubdomains.Add(new int[] { 8, 2 });
			numSubdomains.Add(new int[] { 16, 4 });
			numSubdomains.Add(new int[] { 32, 8 });
			numSubdomains.Add(new int[] { 64, 16 });
			numSubdomains.Add(new int[] { 128, 32 });
			//numSubdomains.Add(new int[] { 256, 64 });

			return (numElements, numSubdomains);
		}

		private IStructuralModel CreateModel(int[] numSubdomains)
		{
			var builder = new Uniform2DModelBuilder();
			builder.DomainLengthX = DomainLengthPerAxis[0];
			builder.DomainLengthY = DomainLengthPerAxis[1];
			builder.NumSubdomainsX = numSubdomains[0];
			builder.NumSubdomainsY = numSubdomains[1];
			builder.NumTotalElementsX = NumElementsPerAxis[0];
			builder.NumTotalElementsY = NumElementsPerAxis[1];
			builder.YoungModulus = this.YoungModulus;

			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
			builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationY,
				EndPointLoad);

			return builder.BuildModel();
		}
	}
}
