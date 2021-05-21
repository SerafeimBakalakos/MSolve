using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Tests.DDM.ScalabilityAnalysis
{
	public class CantilevelBeam3D : IModelBuilder
	{
		public double[] DomainLengthPerAxis { get; set; } = { 6, 2, 2 };

		public int[] NumElementsPerAxis { get; set; }

		public int[] NumSubdomainsPerAxis { get; set; }

		public int[] SubdomainSizePerElementSize 
		{ 
			get
			{
				var result = new int[3];
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
			return CreateModel(new int[] { 1, 1, 1 });
		}

		public (List<int[]> numElements, int[] numSubdomains) GetParametricConfigConstNumSubdomains()
		{
			int[] numSubdomains = { 12, 4, 4 };
			var numElements = new List<int[]>();

			numElements.Add(new int[] { 24, 8, 8 });
			numElements.Add(new int[] { 36, 12, 12 });
			numElements.Add(new int[] { 72, 24, 24 });
			numElements.Add(new int[] { 144, 48, 48 });
			numElements.Add(new int[] { 288, 96, 96 });
			numElements.Add(new int[] { 576, 192, 192 });
			numElements.Add(new int[] { 864, 288, 288 });

			return (numElements, numSubdomains);
		}

		public (int[] numElements, List<int[]> numSubdomains) GetParametricConfigConstNumElements()
		{
			int[] numElements = { 576, 192, 192 };
			var numSubdomains = new List<int[]>();

			numSubdomains.Add(new int[] { 3, 1, 1 });
			numSubdomains.Add(new int[] { 6, 2, 2 });
			numSubdomains.Add(new int[] { 9, 3, 3 });
			numSubdomains.Add(new int[] { 12, 4, 4 });
			numSubdomains.Add(new int[] { 18, 6, 6 });
			numSubdomains.Add(new int[] { 24, 8, 8 });
			numSubdomains.Add(new int[] { 36, 12, 12 });

			return (numElements, numSubdomains);
		}

		public (List<int[]> numElements, List<int[]> numSubdomains) GetParametricConfigConstSubdomainPerElementSize()
		{
			var numElements = new List<int[]>();
			var numSubdomains = new List<int[]>();

			numElements.Add(new int[] { 27, 9, 9 });
			numElements.Add(new int[] { 54, 18, 18 });
			numElements.Add(new int[] { 81, 27, 27 });
			numElements.Add(new int[] { 108, 36, 36});
			numElements.Add(new int[] { 162, 54, 54});
			numElements.Add(new int[] { 216, 72, 72 });
			numElements.Add(new int[] { 324, 108, 108 });

			numSubdomains.Add(new int[] { 3, 1, 1 });
			numSubdomains.Add(new int[] { 6, 2, 2 });
			numSubdomains.Add(new int[] { 9, 3, 3 });
			numSubdomains.Add(new int[] { 12, 4, 4 });
			numSubdomains.Add(new int[] { 18, 6, 6 });
			numSubdomains.Add(new int[] { 24, 8, 8 });
			numSubdomains.Add(new int[] { 36, 12, 12 });

			return (numElements, numSubdomains);
		}

		private IStructuralModel CreateModel(int[] numSubdomains)
		{
			var builder = new Uniform3DModelBuilder();
			builder.MinX = 0;
			builder.MaxX = DomainLengthPerAxis[0];
			builder.MinY = 0;
			builder.MaxY = DomainLengthPerAxis[1];
			builder.MinZ = 0;
			builder.MaxZ = DomainLengthPerAxis[2];
			builder.NumSubdomainsX = numSubdomains[0];
			builder.NumSubdomainsY = numSubdomains[1];
			builder.NumSubdomainsZ = numSubdomains[2];
			builder.NumTotalElementsX = NumElementsPerAxis[0];
			builder.NumTotalElementsY = NumElementsPerAxis[1];
			builder.NumTotalElementsZ = NumElementsPerAxis[2];
			builder.YoungModulus = this.YoungModulus;

			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationY, 0.0);
			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationZ, 0.0);
			builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationY,
				EndPointLoad);

			return builder.BuildModel();
		}
	}
}