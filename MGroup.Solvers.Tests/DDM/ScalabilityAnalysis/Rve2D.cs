using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Tests.DDM.ScalabilityAnalysis
{
	public class Rve2D : IModelBuilder
	{
		public double[] DomainLengthPerAxis { get; set; } = { 2, 2 };

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

		public double[] CenterDisplacement { get; set; } = { 0.01, -0.02 };

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
			int[] numSubdomains = { 8, 8 };
			var numElements = new List<int[]>();

			numElements.Add(new int[] { 16, 16 });
			numElements.Add(new int[] { 32, 32 });
			numElements.Add(new int[] { 64, 64 });
			numElements.Add(new int[] { 128, 128 });
			numElements.Add(new int[] { 256, 256 });
			numElements.Add(new int[] { 512, 512 });
			numElements.Add(new int[] { 768, 768 });

			return (numElements, numSubdomains);
		}

		public (int[] numElements, List<int[]> numSubdomains) GetParametricConfigConstNumElements()
		{
			int[] numElements = { 256, 256 };
			var numSubdomains = new List<int[]>();

			numSubdomains.Add(new int[] { 4, 4 });
			numSubdomains.Add(new int[] { 8, 8 });
			numSubdomains.Add(new int[] { 16, 16 });
			numSubdomains.Add(new int[] { 32, 32 });
			numSubdomains.Add(new int[] { 64, 64 });
			numSubdomains.Add(new int[] { 128, 128 });
			//numSubdomains.Add(new int[] { 256, 256 });

			return (numElements, numSubdomains);
		}

		public (List<int[]> numElements, List<int[]> numSubdomains) GetParametricConfigConstSubdomainPerElementSize()
		{
			var numElements = new List<int[]>();
			var numSubdomains = new List<int[]>();

			//numSubdomains.Add(new int[] { 4, 4 });
			//numSubdomains.Add(new int[] { 8, 8 });
			//numSubdomains.Add(new int[] { 16, 16 });
			//numSubdomains.Add(new int[] { 32, 32 });
			//numSubdomains.Add(new int[] { 64, 64 });
			numSubdomains.Add(new int[] { 96, 96 });
			//numSubdomains.Add(new int[] { 128, 128 });
			//numSubdomains.Add(new int[] { 256, 256 });

			//numElements.Add(new int[] { 32, 32 });
			//numElements.Add(new int[] { 64, 64 });
			//numElements.Add(new int[] { 128, 128 });
			//numElements.Add(new int[] { 256, 256 });
			//numElements.Add(new int[] { 512, 512 });
			numElements.Add(new int[] { 768, 768 });
			//numElements.Add(new int[] { 1024, 1024 });
			//numElements.Add(new int[] { 2048, 2048 });

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
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationY, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.UpperSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.UpperSide, StructuralDof.TranslationY, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LowerSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LowerSide, StructuralDof.TranslationY, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.Center, StructuralDof.TranslationX,
				CenterDisplacement[0]);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.Center, StructuralDof.TranslationY,
				CenterDisplacement[1]);

			return builder.BuildModel();
		}
	}
}
