using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class Example4x4ExpectedResults
	{
		#region dofs
		public static (int[] boundaryDofs, int[] internalDofs) GetDofsBoundaryInternalToFree(int s)
		{
			int[] boundaryDofs;
			int[] internalDofs;
			if (s == 0)
			{
				boundaryDofs = new int[] { 4, 5, 6, 7, 8, 9, 10, 11 };
				internalDofs = new int[] { 0, 1, 2, 3 };
			}
			else if (s == 1)
			{
				boundaryDofs = new int[] { 0, 1, 6, 7, 8, 9, 10, 11 };
				internalDofs = new int[] { 2, 3, 4, 5 };
			}
			else if (s == 2)
			{
				boundaryDofs = new int[] { 0, 1, 2, 3, 4, 5, 10, 11, 16, 17 };
				internalDofs = new int[] { 6, 7, 8, 9, 12, 13, 14, 15 };
			}
			else if (s == 3)
			{
				boundaryDofs = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 12, 13 };
				internalDofs = new int[] { 8, 9, 10, 11, 14, 15, 16, 17 };
			}
			else
			{
				throw new ArgumentException();
			}
			return (boundaryDofs, internalDofs);
		}
		#endregion

		#region mapping matrices
		public static Matrix GetMatrixLb(int s)
		{
			Matrix Lb;
			if (s == 0)
			{
				Lb = Matrix.CreateZero(8, 16);
				Lb[0, 0] = 1;
				Lb[1, 1] = 1;
				Lb[2, 2] = 1;
				Lb[3, 3] = 1;
				Lb[4, 4] = 1;
				Lb[5, 5] = 1;
				Lb[6, 6] = 1;
				Lb[7, 7] = 1;
			}
			else if (s == 1)
			{
				Lb = Matrix.CreateZero(8, 16);
				Lb[0, 0] = 1;
				Lb[1, 1] = 1;
				Lb[2, 6] = 1;
				Lb[3, 7] = 1;
				Lb[4, 8] = 1;
				Lb[5, 9] = 1;
				Lb[6, 10] = 1;
				Lb[7, 11] = 1;
			}
			else if (s == 2)
			{
				Lb = Matrix.CreateZero(10, 16);
				Lb[0, 2] = 1;
				Lb[1, 3] = 1;
				Lb[2, 4] = 1;
				Lb[3, 5] = 1;
				Lb[4, 6] = 1;
				Lb[5, 7] = 1;
				Lb[6, 12] = 1;
				Lb[7, 13] = 1;
				Lb[8, 14] = 1;
				Lb[9, 15] = 1;
			}
			else if (s == 3)
			{
				Lb = Matrix.CreateZero(10, 16);
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
			}
			else
			{
				throw new ArgumentException();
			}

			return Lb;
		}

		public static Matrix GetMatrixLpbHomogeneous(int s)
		{
			Matrix Lpb;
			if (s == 0)
			{
				Lpb = Matrix.CreateZero(8, 16);
				Lpb[0, 0] = 0.5;
				Lpb[1, 1] = 0.5;
				Lpb[2, 2] = 0.5;
				Lpb[3, 3] = 0.5;
				Lpb[4, 4] = 0.5;
				Lpb[5, 5] = 0.5;
				Lpb[6, 6] = 0.25;
				Lpb[7, 7] = 0.25;
			}
			else if (s == 1)
			{
				Lpb = Matrix.CreateZero(8, 16);
				Lpb[0, 0] = 0.5;
				Lpb[1, 1] = 0.5;
				Lpb[2, 6] = 0.25;
				Lpb[3, 7] = 0.25;
				Lpb[4, 8] = 0.5;
				Lpb[5, 9] = 0.5;
				Lpb[6, 10] = 0.5;
				Lpb[7, 11] = 0.5;
			}
			else if (s == 2)
			{
				Lpb = Matrix.CreateZero(10, 16);
				Lpb[0, 2] = 0.5;
				Lpb[1, 3] = 0.5;
				Lpb[2, 4] = 0.5;
				Lpb[3, 5] = 0.5;
				Lpb[4, 6] = 0.25;
				Lpb[5, 7] = 0.25;
				Lpb[6, 12] = 0.5;
				Lpb[7, 13] = 0.5;
				Lpb[8, 14] = 0.5;
				Lpb[9, 15] = 0.5;
			}
			else if (s == 3)
			{
				Lpb = Matrix.CreateZero(10, 16);
				Lpb[0, 6] = 0.25;
				Lpb[1, 7] = 0.25;
				Lpb[2, 8] = 0.5;
				Lpb[3, 9] = 0.5;
				Lpb[4, 10] = 0.5;
				Lpb[5, 11] = 0.5;
				Lpb[6, 12] = 0.5;
				Lpb[7, 13] = 0.5;
				Lpb[8, 14] = 0.5;
				Lpb[9, 15] = 0.5;
			}
			else
			{
				throw new ArgumentException();
			}
			return Lpb;
		}

		public static Matrix GetMatrixLpbHeterogeneous(int s)
		{
			Matrix Lpb;
			if (s == 0)
			{
				Lpb = Matrix.CreateFromArray(new double[,]
				{
					{ 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  },
					{ 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0 },
				});
				
			}
			else if (s == 1)
			{
				Lpb = Matrix.CreateFromArray(new double[,]
				{
				{ 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0 },
				});
			}
			else if (s == 2)
			{
				Lpb = Matrix.CreateFromArray(new double[,]
				{
					{ 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5 },
				});
			}
			else if (s == 3)
			{
				Lpb = Matrix.CreateFromArray(new double[,]
				{
					{ 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0.25, 0, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5, 0 },
					{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5 }
				});
			}
			else
			{
				throw new ArgumentException();
			}
			return Lpb;
		}
		#endregion

		#region stiffness matrices
		public static Matrix GetMatrixKff(int s)
		{
			Matrix Kff;
			if (s == 0)
			{
				Kff = Matrix.CreateFromArray(new double[12,12]
				{
					{20769230	,	0	,	2307692	,	0	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0									 },
					{0	,	20769230	,	5.82E-11	,	-12692310	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0							 },
					{2307692	,	5.82E-11	,	41538460	,	0	,	2307692	,	0	,	-5192308	,	3750000	,	-12692310	,	0	,	-5192308	,	-3750000				 },
					{5.82E-11	,	-12692310	,	0	,	41538460	,	5.82E-11	,	-12692310	,	3750000	,	-5192308	,	-5.82E-11	,	2307692	,	-3750000	,	-5192308 },
					{0	,	0	,	2307692	,	5.82E-11	,	20769230	,	0	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5								 },
					{0	,	0	,	5.82E-11	,	-12692310	,	0	,	20769230	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846								 },
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0						 },
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0						 },
					{-5192308	,	-3750000	,	-12692310	,	0	,	-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	0	,	1153846	,	-288461.5			 },
					{-3750000	,	-5192308	,	5.82E-11	,	2307692	,	3750000	,	-5192308	,	-288461.5	,	-6346154	,	0	,	20769230	,	288461.5	,	-6346154 },
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000						 },
					{ 0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620						 },
				});
			}
			else if (s == 1)
			{
				Kff = Matrix.CreateFromArray(new double[12,12]
				{
					{20769230	,	0	,	2307692	,	5.82E-11	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0										},
					{0	,	20769230	,	0	,	-12692310	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0												},
					{2307692	,	0	,	41538460	,	0	,	2307692	,	0	,	-5192308	,	3750000	,	-12692310	,	-2.33E-10	,	-5192308	,	-3750000							},
					{0	,	-12692310	,	0	,	41538460	,	5.82E-11	,	-12692310	,	3750000	,	-5192308	,	-2.91E-10	,	2307692	,	-3750000	,	-5192308					},
					{0	,	0	,	2307692	,	5.82E-11	,	20769230	,	0	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5											},
					{0	,	0	,	5.82E-11	,	-12692310	,	0	,	20769230	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846											},
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0									},
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0									},
					{-5192308	,	-3750000	,	-12692310	,	2.33E-10	,	-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	4.66E-10	,	1153846	,	-288461.5		},
					{-3750000	,	-5192308	,	2.91E-10	,	2307692	,	3750000	,	-5192308	,	-288461.5	,	-6346154	,	4.66E-10	,	20769230	,	288461.5	,	-6346154	},
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000									},
					{ 0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620									},
					});
			}
			else if (s == 2)
			{
				Kff = Matrix.CreateFromArray(new double[18,18]
				{
					{10384620	,	3750000	,	1153846	,	288461.5	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																	},
					{3750000	,	10384620	,	-288461.5	,	-6346154	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0															   } ,
					{1153846	,	-288461.5	,	20769230	,	-1.40E-09	,	1153846	,	288461.5	,	-5192308	,	3750000	,	-12692310	,	1.16E-09	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0	,	0	,	0								   } ,
					{288461.5	,	-6346154	,	-1.40E-09	,	20769230	,	-288461.5	,	-6346154	,	3750000	,	-5192308	,	1.28E-09	,	2307692	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0	,	0	,	0								   } ,
					{0	,	0	,	1153846	,	-288461.5	,	10384620	,	-3750000	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5	,	0	,	0	,	0	,	0	,	0	,	0																   } ,
					{0	,	0	,	288461.5	,	-6346154	,	-3750000	,	10384620	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846	,	0	,	0	,	0	,	0	,	0	,	0																   } ,
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	20769230	,	-4.66E-10	,	2307692	,	1.75E-10	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0								   } ,
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-4.66E-10	,	20769230	,	2.91E-10	,	-12692310	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0									   } ,
					{-5192308	,	-3750000	,	-12692310	,	1.51E-09	,	-5192308	,	3750000	,	2307692	,	1.75E-10	,	41538460	,	0	,	2307692	,	1.75E-10	,	-5192308	,	3750000	,	-12692310	,	-1.34E-09	,	-5192308	,	-3750000   } ,
					{-3750000	,	-5192308	,	1.51E-09	,	2307692	,	3750000	,	-5192308	,	5.24E-10	,	-12692310	,	0	,	41538460	,	2.91E-10	,	-12692310	,	3750000	,	-5192308	,	-1.28E-09	,	2307692	,	-3750000	,	-5192308   } ,
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	2307692	,	1.75E-10	,	20769230	,	4.66E-10	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5								   } ,
					{0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	5.24E-10	,	-12692310	,	4.66E-10	,	20769230	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846									   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0																   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0																   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	-12692310	,	-9.31E-10	,	-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	9.31E-10	,	1153846	,	-288461.5									   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	-1.16E-09	,	2307692	,	3750000	,	-5192308	,	-288461.5	,	-6346154	,	9.31E-10	,	20769230	,	288461.5	,	-6346154								   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000																   } ,
					{ 0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620																   } ,

				});
			}
			else if (s == 3)
			{
				Kff = Matrix.CreateFromArray(new double[,]
				{
					{10384620	,	3750000	,	1153846	,	288461.5	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																			},
					{3750000	,	10384620	,	-288461.5	,	-6346154	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																	   } ,
					{1153846	,	-288461.5	,	20769230	,	-1.40E-09	,	1153846	,	288461.5	,	-5192308	,	3750000	,	-12692310	,	6.98E-10	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0	,	0	,	0										   } ,
					{288461.5	,	-6346154	,	-1.86E-09	,	20769230	,	-288461.5	,	-6346154	,	3750000	,	-5192308	,	1.16E-09	,	2307692	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0	,	0	,	0										   } ,
					{0	,	0	,	1153846	,	-288461.5	,	10384620	,	-3750000	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5	,	0	,	0	,	0	,	0	,	0	,	0																		   } ,
					{0	,	0	,	288461.5	,	-6346154	,	-3750000	,	10384620	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846	,	0	,	0	,	0	,	0	,	0	,	0																		   } ,
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	20769230	,	-9.31E-10	,	2307692	,	5.82E-11	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0										   } ,
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-9.31E-10	,	20769230	,	-3.49E-10	,	-12692310	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0											   } ,
					{-5192308	,	-3750000	,	-12692310	,	1.28E-09	,	-5192308	,	3750000	,	2307692	,	0	,	41538460	,	-4.66E-10	,	2307692	,	1.75E-10	,	-5192308	,	3750000	,	-12692310	,	-1.22E-09	,	-5192308	,	-3750000		   } ,
					{-3750000	,	-5192308	,	9.31E-10	,	2307692	,	3750000	,	-5192308	,	2.33E-10	,	-12692310	,	-4.66E-10	,	41538460	,	2.91E-10	,	-12692310	,	3750000	,	-5192308	,	-1.28E-09	,	2307692	,	-3750000	,	-5192308   } ,
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	2307692	,	1.75E-10	,	20769230	,	4.66E-10	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5										   } ,
					{0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	5.24E-10	,	-12692310	,	4.66E-10	,	20769230	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846											   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0																		   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0																		   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	-12692310	,	-9.31E-10	,	-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	1.40E-09	,	1153846	,	-288461.5											   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	-1.16E-09	,	2307692	,	3750000	,	-5192308	,	-288461.5	,	-6346154	,	1.86E-09	,	20769230	,	288461.5	,	-6346154										   } ,
					{0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000																		   } ,
					{ 0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620																		   } ,
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Kff;
		}

		public static Matrix GetMatrixKbb(int s)
		{
			Matrix Kbb;
			if (s == 0)
			{
				Kbb = Matrix.CreateFromArray(new double[8,8]
				{
					{20769230	,	0	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5						},
					{0	,	20769230	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846							},
					{0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0								},
					{0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0							},
					{-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	0	,	1153846	,	-288461.5				},
					{3750000	,	-5192308	,	-288461.5	,	-6346154	,	0	,	20769230	,	288461.5	,	-6346154	},
					{-6346154	,	-288461.5	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000					},
					{ 288461.5	,	1153846	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620					},
				});
			}
			else if (s == 1)
			{
				Kbb = Matrix.CreateFromArray(new double[8,8]
				{
					{20769230	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0							},
					{0	,	20769230	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0								},
					{-6346154	,	288461.5	,	10384620	,	-3750000	,	1153846	,	-288461.5	,	0	,	0						},
					{-288461.5	,	1153846	,	-3750000	,	10384620	,	288461.5	,	-6346154	,	0	,	0						},
					{-5192308	,	-3750000	,	1153846	,	288461.5	,	20769230	,	4.66E-10	,	1153846	,	-288461.5			},
					{-3750000	,	-5192308	,	-288461.5	,	-6346154	,	4.66E-10	,	20769230	,	288461.5	,	-6346154	},
					{0	,	0	,	0	,	0	,	1153846	,	288461.5	,	10384620	,	3750000											},
					{ 0	,	0	,	0	,	0	,	-288461.5	,	-6346154	,	3750000	,	10384620										},
				});
			}
			else if (s == 2)
			{
				Kbb = Matrix.CreateFromArray(new double[10,10]
				{
					{10384620	,	3750000	,	1153846	,	288461.5	,	0	,	0	,	0	,	0	,	0	,	0											},
					{3750000	,	10384620	,	-288461.5	,	-6346154	,	0	,	0	,	0	,	0	,	0	,	0									},
					{1153846	,	-288461.5	,	20769230	,	-1.40E-09	,	1153846	,	288461.5	,	-5192308	,	-3750000	,	0	,	0		},
					{288461.5	,	-6346154	,	-1.40E-09	,	20769230	,	-288461.5	,	-6346154	,	-3750000	,	-5192308	,	0	,	0	},
					{0	,	0	,	1153846	,	-288461.5	,	10384620	,	-3750000	,	-6346154	,	288461.5	,	0	,	0						},
					{0	,	0	,	288461.5	,	-6346154	,	-3750000	,	10384620	,	-288461.5	,	1153846	,	0	,	0						},
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	20769230	,	4.66E-10	,	-6346154	,	288461.5	},
					{0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	4.66E-10	,	20769230	,	-288461.5	,	1153846			},
					{0	,	0	,	0	,	0	,	0	,	0	,	-6346154	,	-288461.5	,	10384620	,	3750000										},
					{ 0	,	0	,	0	,	0	,	0	,	0	,	288461.5	,	1153846	,	3750000	,	10384620 },

				});
			}
			else if (s == 3)
			{
				Kbb = Matrix.CreateFromArray(new double[10,10]
				{
					{10384620.000	,	3750000.000	,	1153846.000	,	288461.500	,	0.000	,	0.000	,	-6346154.000	,	-288461.500	,	0.000	,	0.000				  },
					{3750000.000	,	10384620.000	,	-288461.500	,	-6346154.000	,	0.000	,	0.000	,	288461.500	,	1153846.000	,	0.000	,	0.000			  },
					{1153846.000	,	-288461.500	,	20769230.000	,	0.000	,	1153846.000	,	288461.500	,	-5192308.000	,	3750000.000	,	0.000	,	0.000		  },
					{288461.500	,	-6346154.000	,	0.000	,	20769230.000	,	-288461.500	,	-6346154.000	,	3750000.000	,	-5192308.000	,	0.000	,	0.000	  },
					{0.000	,	0.000	,	1153846.000	,	-288461.500	,	10384620.000	,	-3750000.000	,	0.000	,	0.000	,	0.000	,	0.000						  },
					{0.000	,	0.000	,	288461.500	,	-6346154.000	,	-3750000.000	,	10384620.000	,	0.000	,	0.000	,	0.000	,	0.000					  },
					{-6346154.000	,	288461.500	,	-5192308.000	,	3750000.000	,	0.000	,	0.000	,	20769230.000	,	0.000	,	-6346154.000	,	-288461.500	  },
					{-288461.500	,	1153846.000	,	3750000.000	,	-5192308.000	,	0.000	,	0.000	,	0.000	,	20769230.000	,	288461.500	,	1153846.000		  },
					{0.000	,	0.000	,	0.000	,	0.000	,	0.000	,	0.000	,	-6346154.000	,	288461.500	,	10384620.000	,	-3750000.000					  },
					{ 0.000	,	0.000	,	0.000	,	0.000	,	0.000	,	0.000	,	-288461.500	,	1153846.000	,	-3750000.000	,	10384620.000						  },

				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Kbb;
		}

		public static Matrix GetMatrixKbi(int s) => GetMatrixKib(s).Transpose();

		public static Matrix GetMatrixKib(int s)
		{
			Matrix Kib;
			if (s == 0)
			{
				Kib = Matrix.CreateFromArray(new double[4,8]
				{
					{0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000	,	0	,	0							 },
					{0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308	,	0	,	0								 },
					{2307692	,	0	,	-5192308	,	3750000	,	-12692310	,	0	,	-5192308	,	-3750000				 },
					{ 5.82E-11	,	-12692310	,	3750000	,	-5192308	,	-5.82E-11	,	2307692	,	-3750000	,	-5192308	 },
				});
			}
			else if (s == 1)
			{
				Kib = Matrix.CreateFromArray(new double[4,8]
				{
					{2307692	,	0	,	-5192308	,	3750000	,	-12692310	,	-2.33E-10	,	-5192308	,	-3750000	},
					{0	,	-12692310	,	3750000	,	-5192308	,	-2.91E-10	,	2307692	,	-3750000	,	-5192308		},
					{0	,	0	,	0	,	0	,	-5192308	,	3750000	,	-6346154	,	288461.5							},
					{ 0	,	0	,	0	,	0	,	3750000	,	-5192308	,	-288461.5	,	1153846								},
				});
			}
			else if (s == 2)
			{
				Kib = Matrix.CreateFromArray(new double[8,10]
				{
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	0	,	0	,	0	,	0												},
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	0	,	0	,	0	,	0													},
					{-5192308	,	-3750000	,	-12692310	,	1.51E-09	,	-5192308	,	3750000	,	2307692	,	1.75E-10	,	-5192308	,	-3750000	},
					{-3750000	,	-5192308	,	1.51E-09	,	2307692	,	3750000	,	-5192308	,	2.91E-10	,	-12692310	,	-3750000	,	-5192308	},
					{0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																			},
					{0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																			},
					{0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	3750000	,	1153846	,	-288461.5													},
					{ 0	,	0	,	0	,	0	,	0	,	0	,	3750000	,	-5192308	,	288461.5	,	-6346154												},
				});
			}
			else if (s == 3)
			{
				Kib = Matrix.CreateFromArray(new double[8,10]
				{
					{-5192308	,	-3750000	,	-12692310	,	1.28E-09	,	-5192308	,	3750000	,	2307692	,	0	,	-5192308	,	3750000			},
					{-3750000	,	-5192308	,	9.31E-10	,	2307692	,	3750000	,	-5192308	,	2.33E-10	,	-12692310	,	3750000	,	-5192308	},
					{0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	0	,	0	,	0	,	0										},
					{0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	0	,	0	,	0	,	0											},
					{0	,	0	,	0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	1153846	,	288461.5											},
					{0	,	0	,	0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	-288461.5	,	-6346154										},
					{0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																		},
					{ 0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0	,	0																		},
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Kib;
		}

		public static Matrix GetMatrixKii(int s)
		{
			Matrix Kii;
			if (s == 0)
			{
				Kii = Matrix.CreateFromArray(new double[4,4]
				{
					{20769230	,	0	,	2307692	,	0				},	
					{0	,	20769230	,	5.82E-11	,	-12692310	},
					{2307692	,	5.82E-11	,	41538460	,	0	},
					{ 5.82E-11	,	-12692310	,	0	,	41538460	},
				});
			}
			else if (s == 1)
			{
				Kii = Matrix.CreateFromArray(new double[4,4]
				{
					{41538460	,	0	,	2307692	,	0				},
					{0	,	41538460	,	5.82E-11	,	-12692310	},
					{2307692	,	5.82E-11	,	20769230	,	0	},
					{ 5.82E-11	,	-12692310	,	0	,	20769230	},

				});
			}
			else if (s == 2)
			{
				Kii = Matrix.CreateFromArray(new double[8,8]
				{
					{20769230	,	-4.66E-10	,	2307692	,	1.75E-10	,	-6346154	,	-288461.5	,	-5192308	,	-3750000},
					{-4.66E-10	,	20769230	,	2.91E-10	,	-12692310	,	288461.5	,	1153846	,	-3750000	,	-5192308},
					{2307692	,	1.75E-10	,	41538460	,	0	,	-5192308	,	3750000	,	-12692310	,	-1.34E-09		},
					{5.24E-10	,	-12692310	,	0	,	41538460	,	3750000	,	-5192308	,	-1.28E-09	,	2307692			},
					{-6346154	,	288461.5	,	-5192308	,	3750000	,	10384620	,	-3750000	,	1153846	,	-288461.5	},
					{-288461.5	,	1153846	,	3750000	,	-5192308	,	-3750000	,	10384620	,	288461.5	,	-6346154	},
					{-5192308	,	-3750000	,	-12692310	,	-9.31E-10	,	1153846	,	288461.5	,	20769230	,	9.31E-10},
					{ -3750000	,	-5192308	,	-1.16E-09	,	2307692	,	-288461.5	,	-6346154	,	9.31E-10	,	20769230} ,
				});
			}
			else if (s == 3)
			{
				Kii = Matrix.CreateFromArray(new double[8,8]
				{
					{41538460	,	-4.66E-10	,	2307692	,	1.75E-10	,	-12692310	,	-1.22E-09	,	-5192308	,	-3750000  },
					{-4.66E-10	,	41538460	,	2.91E-10	,	-12692310	,	-1.28E-09	,	2307692	,	-3750000	,	-5192308  },
					{2307692	,	1.75E-10	,	20769230	,	4.66E-10	,	-5192308	,	3750000	,	-6346154	,	288461.5  },
					{5.24E-10	,	-12692310	,	4.66E-10	,	20769230	,	3750000	,	-5192308	,	-288461.5	,	1153846	  },
					{-12692310	,	-9.31E-10	,	-5192308	,	3750000	,	20769230	,	1.40E-09	,	1153846	,	-288461.5	  },
					{-1.16E-09	,	2307692	,	3750000	,	-5192308	,	1.86E-09	,	20769230	,	288461.5	,	-6346154	  },
					{-5192308	,	-3750000	,	-6346154	,	-288461.5	,	1153846	,	288461.5	,	10384620	,	3750000	  },
					{ -3750000	,	-5192308	,	288461.5	,	1153846	,	-288461.5	,	-6346154	,	3750000	,	10384620	  },
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Kii;
		}

		public static Matrix GetMatrixKiiInverse(int s)
		{
			Matrix KiiInverse;
			if (s == 0)
			{
				KiiInverse = Matrix.CreateFromArray(new double[4,4]
				{
					{4.84472067053312E-08	,	9.27513788183307E-27	,	-2.69151122444692E-09	,	2.83407052858890E-27  },
					{-4.17381365451566E-26	,	5.92030441956825E-08	,	-8.06421784463626E-26	,	1.80898230188433E-08  },
					{-2.69151122444692E-09	,	-8.34762489749515E-26	,	2.42236033526656E-08	,	-2.55066372134949E-26 },
					{ -8.06421784463626E-26	,	1.80898230188433E-08	,	-2.08690682725783E-26	,	2.96015220978413E-08  },
				});
			}
			else if (s == 1)
			{
				KiiInverse = Matrix.CreateFromArray(new double[4,4]
				{
					{2.42236033526656E-08	,	4.63756894091654E-27	,	-2.69151122444692E-09	,	2.83407052858890E-27   },
					{-2.08690682725783E-26	,	2.96015220978413E-08	,	-8.06421784463626E-26	,	1.80898230188433E-08   },
					{-2.69151122444692E-09	,	-8.34762489749515E-26	,	4.84472067053312E-08	,	-5.10132744269897E-26  },
					{ -8.06421784463626E-26	,	1.80898230188433E-08	,	-4.17381365451566E-26	,	5.92030441956825E-08   },
				});
			}
			else if (s == 2)
			{
				KiiInverse = Matrix.CreateFromArray(new double[8,8]
				{
					{7.74694663304400E-08	,	7.84117679280106E-09	,	6.37316704560002E-09	,	-4.20228047739300E-11	,	6.27314075696927E-08	,	3.85094131808525E-08	,	2.06579198069399E-08	,	2.85905419623262E-08  },
					{7.84117679280106E-09	,	6.85677648033735E-08	,	3.25455164580609E-09	,	2.15648200425618E-08	,	8.14710872567024E-10	,	1.46781296296156E-08	,	1.60803542178158E-08	,	2.06579198069399E-08  },
					{6.37316704560002E-09	,	3.25455164580610E-09	,	3.29584405895466E-08	,	-1.38155812413455E-09	,	1.55768970479597E-08	,	-7.77673185742535E-09	,	2.15648200425618E-08	,	-4.20228047739242E-11 },
					{-4.20228047739295E-11	,	2.15648200425618E-08	,	-1.38155812413455E-09	,	3.29584405895466E-08	,	-7.77673185742536E-09	,	1.55768970479597E-08	,	3.25455164580610E-09	,	6.37316704560001E-09  },
					{6.27314075696927E-08	,	8.14710872567022E-10	,	1.55768970479597E-08	,	-7.77673185742536E-09	,	1.72674027107216E-07	,	7.76189390592441E-08	,	1.46781296296156E-08	,	3.85094131808525E-08  },
					{3.85094131808525E-08	,	1.46781296296156E-08	,	-7.77673185742535E-09	,	1.55768970479597E-08	,	7.76189390592441E-08	,	1.72674027107215E-07	,	8.14710872567026E-10	,	6.27314075696927E-08  },
					{2.06579198069399E-08	,	1.60803542178158E-08	,	2.15648200425618E-08	,	3.25455164580610E-09	,	1.46781296296156E-08	,	8.14710872567028E-10	,	6.85677648033735E-08	,	7.84117679280106E-09  },
					{ 2.85905419623262E-08	,	2.06579198069399E-08	,	-4.20228047739247E-11	,	6.37316704560001E-09	,	3.85094131808525E-08	,	6.27314075696927E-08	,	7.84117679280106E-09	,	7.74694663304400E-08  },

				});
			}
			else if (s == 3)
			{
				KiiInverse = Matrix.CreateFromArray(new double[8,8]
				{
					{3.295844058954660E-08	,	1.381558124134550E-09	,	6.373167045600010E-09	,	-3.254551645806090E-09	,	2.156482004256180E-08	,	4.202280477393000E-11	,	1.557689704795970E-08	,	7.776731857425360E-09 },
					{1.381558124134550E-09	,	3.295844058954660E-08	,	4.202280477392620E-11	,	2.156482004256180E-08	,	-3.254551645806090E-09	,	6.373167045600020E-09	,	7.776731857425360E-09	,	1.557689704795970E-08 },
					{6.373167045600020E-09	,	4.202280477392650E-11	,	7.746946633044000E-08	,	-7.841176792801070E-09	,	2.065791980693990E-08	,	-2.859054196232620E-08	,	6.273140756969270E-08	,	-3.850941318085250E-08},
					{-3.254551645806100E-09	,	2.156482004256180E-08	,	-7.841176792801070E-09	,	6.856776480337350E-08	,	-1.608035421781580E-08	,	2.065791980693990E-08	,	-8.147108725670320E-10	,	1.467812962961560E-08 },
					{2.156482004256180E-08	,	-3.254551645806090E-09	,	2.065791980693990E-08	,	-1.608035421781580E-08	,	6.856776480337350E-08	,	-7.841176792801070E-09	,	1.467812962961560E-08	,	-8.147108725670280E-10},
					{4.202280477392670E-11	,	6.373167045600020E-09	,	-2.859054196232620E-08	,	2.065791980693990E-08	,	-7.841176792801070E-09	,	7.746946633044000E-08	,	-3.850941318085250E-08	,	6.273140756969270E-08 },
					{1.557689704795970E-08	,	7.776731857425350E-09	,	6.273140756969270E-08	,	-8.147108725670300E-10	,	1.467812962961560E-08	,	-3.850941318085250E-08	,	1.726740271072150E-07	,	-7.761893905924410E-08},
					{ 7.776731857425360E-09	,	1.557689704795970E-08	,	-3.850941318085250E-08	,	1.467812962961560E-08	,	-8.147108725670360E-10	,	6.273140756969270E-08	,	-7.761893905924410E-08	,	1.726740271072150E-07 },
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return KiiInverse;
		}

		public static Matrix GetMatrixSb(int s)
		{
			Matrix Sb;
			if (s == 0)
			{
				Sb = Matrix.CreateFromArray(new double[,]
				{
				});
			}
			else if (s == 1)
			{
				Sb = Matrix.CreateFromArray(new double[,]
				{
				});
			}
			else if (s == 2)
			{
				Sb = Matrix.CreateFromArray(new double[,]
				{
				});
			}
			else if (s == 3)
			{
				Sb = Matrix.CreateFromArray(new double[,]
				{
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Sb;
		}

		public static Matrix GetInterfaceProblemMatrix()
		{
			Matrix S = Matrix.CreateFromArray(new double[16, 16]
			{
				{ 41280458.671763, 0, 250836.120401338, -211419.015766842, -4515050.16722408, 3726708.07453416, -12111801.242236, 5.82076609134674E-10, -4515050.16722408, -3726708.07453416, 250836.120401337, 211419.015766842, 0, 0, 0, 0 },
				{ 0, 32001167.7127427, 1475149.61319515, -1685885.27222303, 2888994.30740038, -5517442.70909356, 0, -1593927.89373814, -2888994.30740038, -5517442.70909356, -1475149.61319515, -1685885.27222303, 0, 0, 0, 0 },
				{ 250836.120401338, 1475149.61319515, 12955014.2849117, -239446.704720112, -5016901.54338442, 461451.725591981, -746175.68838845, 337955.336362159, 0, 0, 0, 0, -79533.37406431, -1674537.35287004, -1174655.77810168, -2827429.24908865 },
				{ -211419.015766842, -1685885.27222303, -239446.704720112, 18657638.3158038, -264339.142267463, -12534622.8862337, -68810.7453761356, -663049.526738014, 0, 0, 0, 0, -121410.190949333, -1520335.71604356, -1019726.12596891, -1174655.77810168 },
				{ -4515050.16722408, 2888994.30740038, -5016901.54338442, -264339.142267462, 27265143.7773315, 571693.803499881, -2120845.51437231, 706320.25344358, 0, 0, 0, 0, -5579575.33022425, -1661561.13691083, -1520335.71604356, -1674537.35287004 },
				{ 3726708.07453416, -5517442.70909356, 461451.725591981, -12534622.8862337, 571693.80349988, 37246274.605952, 111405.552163922, -12989055.7745953, 0, 0, 0, 0, -3806068.71242246, -5579575.33022425, -121410.190949334, -79533.3740643123 },
				{ -12111801.242236, 0, -746175.68838845, -68810.7453761353, -2120845.51437231, 111405.552163922, 36588098.3293138, 0, -2120845.51437231, -111405.552163922, -746175.68838845, 68810.7453761349, -13095483.0554585, 0, -633850.177444874, 0 },
				{ 0, -1593927.89373814, 337955.33636216, -663049.526738015, 706320.25344358, -12989055.7745953, 0, 36449374.3614658, -706320.25344358, -12989055.7745953, -337955.33636216, -663049.526738015, 0, -2992948.38247908, 0, -1235268.15185893 },
				{ -4515050.16722408, -2888994.30740038, 0, 0, 0, 0, -2120845.51437231, -706320.25344358, 27265143.7773315, -571693.80349988, -5016901.54338442, 264339.142267463, -5579575.33022425, 1661561.13691083, -1520335.71604356, 1674537.35287004 },
				{ -3726708.07453416, -5517442.70909356, 0, 0, 0, 0, -111405.552163922, -12989055.7745953, -571693.803499882, 37246274.6059521, -461451.725591982, -12534622.8862337, 3806068.71242246, -5579575.33022425, 121410.190949334, -79533.3740643109 },
				{ 250836.120401337, -1475149.61319515, 0, 0, 0, 0, -746175.68838845, -337955.33636216, -5016901.54338442, -461451.725591981, 12955014.2849117, 239446.704720112, -79533.3740643113, 1674537.35287004, -1174655.77810168, 2827429.24908865 },
				{ 211419.015766842, -1685885.27222303, 0, 0, 0, 0, 68810.7453761352, -663049.526738015, 264339.142267462, -12534622.8862337, 239446.704720112, 18657638.3158038, 121410.190949334, -1520335.71604356, 1019726.12596891, -1174655.77810168 },
				{ 0, 0, -79533.3740643099, -121410.190949333, -5579575.33022425, -3806068.71242246, -13095483.0554585, 9.31322574615479E-10, -5579575.33022425, 3806068.71242246, -79533.3740643116, 121410.190949334, 36957155.752546, 0, -12543455.2885104, 0 },
				{ 0, 0, -1674537.35287004, -1520335.71604356, -1661561.13691083, -5579575.33022425, 9.31322574615479E-10, -2992948.38247908, 1661561.13691083, -5579575.33022425, 1674537.35287004, -1520335.71604356, 0, 24364269.2647276, 0, -7171498.78971287 },
				{ 0, 0, -1174655.77810168, -1019726.12596891, -1520335.71604356, -121410.190949334, -633850.177444875, -5.38420863449574E-10, -1520335.71604356, 121410.190949334, -1174655.77810168, 1019726.12596891, -12543455.2885104, 0, 18567288.4542457, 0 },
				{ 0, 0, -2827429.24908865, -1174655.77810168, -1674537.35287004, -79533.3740643119, 4.07453626394272E-10, -1235268.15185893, 1674537.35287004, -79533.3740643107, 2827429.24908865, -1174655.77810168, 0, -7171498.78971287, 0, 10915145.2459038 },
			});

			return S;
		}
		#endregion

		#region force vectors
		public static Vector GetRhsFfHomogeneous(int s)
		{
			Vector Ff;
			if (s == 0)
			{
				Ff = Vector.CreateFromArray(new double[12]
				{
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
				});
			}
			else if (s == 1)
			{
				Ff = Vector.CreateFromArray(new double[12]
				{
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
				});
			}
			else if (s == 2)
			{
				Ff = Vector.CreateFromArray(new double[18]
				{
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 20, 0, 20, 0, 10
				});
			}
			else if (s == 3)
			{
				Ff = Vector.CreateFromArray(new double[18]
				{
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 0, 20, 0, 20
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Ff;
		}

		public static Vector GetRhsFbHomogeneous(int s)
		{
			Vector Fb;
			if (s == 0)
			{
				Fb = Vector.CreateFromArray(new double[]
				{
				});
			}
			else if (s == 1)
			{
				Fb = Vector.CreateFromArray(new double[]
				{
				});
			}
			else if (s == 2)
			{
				Fb = Vector.CreateFromArray(new double[]
				{
				});
			}
			else if (s == 3)
			{
				Fb = Vector.CreateFromArray(new double[]
				{
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Fb;
		}

		public static Vector GetRhsFiHomogeneous(int s)
		{
			Vector Fi;
			if (s == 0)
			{
				Fi = Vector.CreateFromArray(new double[]
				{
					0,0,0,0
				});
			}
			else if (s == 1)
			{
				Fi = Vector.CreateFromArray(new double[]
				{
					0,0,0,0
				});
			}
			else if (s == 2)
			{
				Fi = Vector.CreateFromArray(new double[]
				{
					0,0,0,0,0,20,0,20
				});
			}
			else if (s == 3)
			{
				Fi = Vector.CreateFromArray(new double[]
				{
					0,0,0,0,0,20,0,20
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Fi;
		}

		public static Vector GetRhsFbHatHomogeneous(int s)
		{
			Vector FbHat;
			if (s == 0)
			{
				FbHat = Vector.CreateFromArray(new double[]
				{
					0,0,0,0,0,0,0,0
				});
			}
			else if (s == 1)
			{
				FbHat = Vector.CreateFromArray(new double[]
				{
					0,0,0,0,0,0,0,0
				});
			}
			else if (s == 2)
			{
				FbHat = Vector.CreateFromArray(new double[]
				{
					9.55470933909065,
					0.49046022272998169,
					2.3331137093003314,
					-2.3760688565484251,
					-2.4582027757615816,
					2.8658366081294022,
					-9.2553243230264766,
					19.482076438859689,
					-0.17429594960291395,
					29.537695586829358
				});
			}
			else if (s == 3)
			{
				FbHat = Vector.CreateFromArray(new double[]
				{
					2.4582027757615803,
					2.8658366081294,
					-2.3331137093003242,
					-2.376068856548426,
					-9.5547093390906372,
					0.49046022272998568,
					9.2553243230264712,
					19.482076438859686,
					0.17429594960291339,
					29.537695586829354
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return FbHat;
		}

		public static Vector GetRhsInterfaceProblem()
		{
			Vector globalFbHat = Vector.CreateFromArray(new double[16]
			{
				0,
				0,
				9.55470933909065,
				0.49046022272998169,
				2.3331137093003314,
				-2.3760688565484251,
				-1.3322676295501878E-15,
				5.7316732162588018,
				-2.3331137093003242,
				-2.376068856548426,
				-9.5547093390906372,
				0.49046022272998568,
				-5.3290705182007514E-15,
				38.964152877719371,
				-5.5511151231257827E-16,
				59.075391173658716 
			});

			return globalFbHat;
		}
		#endregion

		#region displacement vectors
		public static Vector GetSolutionInterfaceProblem()
		{
			Vector globalFbHat = Vector.CreateFromArray(new double[16]
			//{
			//}
			);

			return globalFbHat;
		}

		public static Vector GetSolutionUf(int s)
		{
			Vector Uf;
			if (s == 0)
			{
				Uf = Vector.CreateFromArray(new double[12]
				{
					0,0,0,0,0,0,0,0,0,0,0,0
				});
			}
			else if (s == 1)
			{
				Uf = Vector.CreateFromArray(new double[12]
				{
					0,0,0,0,0,0,0,0,0,0,0,0
				});
			}
			else if (s == 2)
			{
				Uf = Vector.CreateFromArray(new double[18]
				{
					0,
					0,
					0,
					0,
					0,
					0,
					1.342000349071124E-06,
					7.0672089551370921E-07,
					-1.5637516172789848E-07,
					4.390012855029211E-07,
					0,
					0,
					2.3225703208878412E-06,
					4.7081128068543131E-06,
					1.7311773220173943E-07,
					2.8040188302522791E-06,
					0,
					0
				});
			}
			else if (s == 3)
			{
				Uf = Vector.CreateFromArray(new double[18]
				{
					0,
					0,
					0,
					0,
					0,
					0,
					0,
					0,
					1.5637516172789858E-07,
					4.3900128550292079E-07,
					-1.3420003490711223E-06,
					7.0672089551370847E-07,
					0,
					0,
					-1.7311773220173938E-07,
					2.8040188302522766E-06,
					-2.3225703208878383E-06,
					4.7081128068543081E-06
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Uf;
		}
		#endregion
	}
}