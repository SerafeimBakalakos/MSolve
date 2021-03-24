#pragma warning disable SA1005
#pragma warning disable SA1028
using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Entities;

namespace MGroup.Tests.DDM
{
	public class Example4x4QuadsHomogeneous
	{
		//                                    Λ P
		//                                    | 
		//                                     
		// |> 20 ---- 21 ---- 22 ---- 23 ---- 24
		//    |  (12) |  (13) |  (14) |  (15) |
		//    |       |       |       |       |
		// |> 15 ---- 16 ---- 17 ---- 18 ---- 19
		//    |  (8)  |  (9)  |  (10) |  (11) |
		//    |       |       |       |       |
		// |> 10 ---- 11 ---- 12 ---- 13 ---- 14
		//    |  (4)  |  (5)  |  (6)  |  (7)  |
		//    |       |       |       |       |
		// |> 5 ----- 6 ----- 7 ----- 8 ----- 9
		//    |  (0)  |  (1)  |  (2)  |  (3)  |
		//    |       |       |       |       |
		// |> 0 ----- 1 ----- 2 ----- 3 ----- 4


		// subdomain 2            subdomain 3                      
		// |> 20 ---- 21 ---- 22  22---- 23 ---- 24
		//    |  (12) |  (13) |   | (14) |  (15) |
		//    |       |       |   |      |       |
		// |> 15 ---- 16 ---- 17  17---- 18 ---- 19
		//    |  (8)  |  (9)  |   | (10) |  (11) |
		//    |       |       |   |      |       |
		// |> 10 ---- 11 ---- 12  12---- 13 ---- 14

		// subdomain 0            subdomain 1
		// |> 10 ---- 11 ---- 12  12---- 13 ---- 14
		//    |  (4)  |  (5)  |   | (6)  |  (7)  |
		//    |       |       |   |      |       |
		// |> 5 ----- 6 ----- 7   7 ---- 8 ----- 9
		//    |  (0)  |  (1)  |   | (2)  |  (3)  |
		//    |       |       |   |      |       |
		// |> 0 ----- 1 ----- 2   2 ---- 3 ----- 4

		// Boundary dofs
		// global: 2,3,10,11,16,17,18,19,20,21,22,23,26,27,34,35
		// sub0: 2, 3, 6, 7, 8, 9, 10, 11
		// sub1: 0, 1, 6, 7, 12, 13, 14, 15, 16, 17
		// sub2: 0, 1, 2, 3, 6, 7, 10, 11
		// sub3: 0, 1, 2, 3, 4, 5, 6, 7, 12, 13


		public static Model CreateModel()
		{
			var builder = new Uniform2DModelBuilder();
			builder.DomainLengthX = 4.0;
			builder.DomainLengthY = 4.0;
			builder.NumSubdomainsX = 2;
			builder.NumSubdomainsY = 2;
			builder.NumTotalElementsX = 4;
			builder.NumTotalElementsY = 4;
			builder.YoungModulus = 1.0;
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
			builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.UpperRightCorner, StructuralDof.TranslationY, 10.0);

			return builder.BuildModel();
		}

		public static int[] GetDofsBoundary(int subdomainID)
		{
			if (subdomainID == 0)
			{
				return new int[] { 2, 3, 6, 7, 8, 9, 10, 11 };
			}
			else if (subdomainID == 1)
			{
				return new int[] { 0, 1, 6, 7, 12, 13, 14, 15, 16, 17 };
			}
			else if (subdomainID == 2)
			{
				return new int[] { 0, 1, 2, 3, 6, 7, 10, 11 };
			}
			else if (subdomainID == 3)
			{
				return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 12, 13 };
			}
			else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
		}

		public static int[] GetDofsInternal(int subdomainID)
		{
			if (subdomainID == 0)
			{
				return new int[] { 0, 1, 4, 5 };
			}
			else if (subdomainID == 1)
			{
				return new int[] { 2, 3, 4, 5, 8, 9, 10, 11 };
			}
			else if (subdomainID == 2)
			{
				return new int[] { 4, 5, 8, 9 };
			}
			else if (subdomainID == 3)
			{
				return new int[] { 8, 9, 10, 11, 14, 15, 16, 17 };
			}
			else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
		}

		// Boundary dofs
		// global: 2,3,10,11,16,17,18,19,20,21,22,23,26,27,34,35
		// sub0: 2, 3, 6, 7, 8, 9, 10, 11
		// sub1: 0, 1, 6, 7, 12, 13, 14, 15, 16, 17
		// sub2: 0, 1, 2, 3, 6, 7, 10, 11
		// sub3: 0, 1, 2, 3, 4, 5, 6, 7, 12, 13
		public static double[,] GetMatrixLb(int subdomainID)
		{
			if (subdomainID == 0)
			{
				var Lb = new double[8, 16];
				Lb[0, 0] = 1;
				Lb[1, 1] = 1;
				Lb[2, 2] = 1;
				Lb[3, 3] = 1;
				Lb[4, 4] = 1;
				Lb[5, 5] = 1;
				Lb[6, 6] = 1;
				Lb[7, 7] = 1;
				return Lb;
			}
			else if (subdomainID == 1)
			{
				var Lb = new double[10, 16];
				Lb[0, 0] = 1;
				Lb[1, 1] = 1;
				Lb[2, 2] = 1;
				Lb[3, 3] = 1;
				Lb[4, 6] = 1;
				Lb[5, 7] = 1;
				Lb[6, 8] = 1;
				Lb[7, 9] = 1;
				Lb[8, 10] = 1;
				Lb[9, 11] = 1;
				return Lb;
			}
			else if (subdomainID == 2)
			{
				var Lb = new double[8, 16];
				Lb[0, 4] = 1;
				Lb[1, 5] = 1;
				Lb[2, 6] = 1;
				Lb[3, 7] = 1;
				Lb[4, 12] = 1;
				Lb[5, 13] = 1;
				Lb[6, 14] = 1;
				Lb[7, 15] = 1;
				return Lb;
			}
			else if (subdomainID == 3)
			{
				var Lb = new double[10, 16];
				Lb[0, 6] = 1;
				Lb[1, 7] = 1;
				Lb[2, 8] = 1;
				Lb[3, 9] = 1;
				Lb[4, 10] = 1;
				Lb[5, 11] = 1;
				Lb[6, 12] = 1;
				Lb[7, 13] = 1;
				Lb[8, 14] = 1;
				Lb[9, 15] = 1;
				return Lb;
			}
			else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
		}
	}
}
#pragma warning restore SA1005 // Single line comments should begin with single space
#pragma warning restore SA1028 // Code should not contain trailing whitespace

