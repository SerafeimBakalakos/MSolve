using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Tests.DDM.UnitTests.FetiDP
{
	public static class Example4x4ExpectedResults
	{
		#region dofs
		public static (int[] cornerDofs, int[] remainderDofs) GetDofsCornerRemainderToFree(int s)
		{
			int[] cornerDofs;
			int[] remainderDofs;
			if (s == 0)
			{
				cornerDofs = new int[] { 6, 7, 10, 11 };
				remainderDofs = new int[] { 0, 1, 2, 3, 4, 5, 8, 9 };
			}
			else if (s == 1)
			{
				cornerDofs = new int[] { 6, 7, 10, 11 };
				remainderDofs = new int[] { 0, 1, 2, 3, 4, 5, 8, 9 };
			}
			else if (s == 2)
			{
				cornerDofs = new int[] { 0, 1, 4, 5, 16, 17 };
				remainderDofs = new int[] { 2, 3, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			}
			else if (s == 3)
			{
				cornerDofs = new int[] { 0, 1, 4, 5, 12, 13 };
				remainderDofs = new int[] { 2, 3, 6, 7, 8, 9, 10, 11, 14, 15, 16, 17 };
			}
			else
			{
				throw new ArgumentException();
			}
			return (cornerDofs, remainderDofs);
		}
		#endregion

		#region mapping matrices
		public static Matrix GetMatrixLc(int s)
		{
			Matrix Lc;
			if (s == 0)
			{
				Lc = Matrix.CreateZero(4, 8);
				Lc[0, 0] = 1;
				Lc[1, 1] = 1;
				Lc[2, 2] = 1;
				Lc[3, 3] = 1;
			}
			else if (s == 1)
			{
				Lc = Matrix.CreateZero(4, 8);
				Lc[0, 2] = 1;
				Lc[1, 3] = 1;
				Lc[2, 4] = 1;
				Lc[3, 5] = 1;
			}
			else if (s == 2)
			{
				Lc = Matrix.CreateZero(6, 8);
				Lc[0, 0] = 1;
				Lc[1, 1] = 1;
				Lc[2, 2] = 1;
				Lc[3, 3] = 1;
				Lc[4, 6] = 1;
				Lc[5, 7] = 1;
			}
			else if (s == 3)
			{
				Lc = Matrix.CreateZero(6, 8);
				Lc[0, 2] = 1;
				Lc[1, 3] = 1;
				Lc[2, 4] = 1;
				Lc[3, 5] = 1;
				Lc[4, 6] = 1;
				Lc[5, 7] = 1;
			}
			else
			{
				throw new ArgumentException();
			}
			return Lc;
		}
		#endregion

		#region stiffness matrices
		public static Matrix GetMatrixKcc(int s)
		{
			Matrix Kcc;
			if (s == 0)
			{
				Kcc = Matrix.CreateFromArray(new double[4,4]
				{
					{10384620	,	-3750000	,	0	,	0},
					{-3750000	,	10384620	,	0	,	0},
					{0	,	0	,	10384620	,	3750000	 },
					{ 0	,	0	,	3750000	,	10384620	 },
				});
			}
			else if (s == 1)
			{
				Kcc = Matrix.CreateFromArray(new double[4,4]
				{
					{10384620	,	-3750000	,	0	,	0 },
					{-3750000	,	10384620	,	0	,	0 },
					{0	,	0	,	10384620	,	3750000	  },
					{ 0	,	0	,	3750000	,	10384620	  },
				});
			}
			else if (s == 2)
			{
				Kcc = Matrix.CreateFromArray(new double[6,6]
				{
					{10384620	,	3750000	,	0	,	0	,	0	,	0		},
					{3750000	,	10384620	,	0	,	0	,	0	,	0	},
					{0	,	0	,	10384620	,	-3750000	,	0	,	0	},
					{0	,	0	,	-3750000	,	10384620	,	0	,	0	},
					{0	,	0	,	0	,	0	,	10384620	,	3750000		},
					{ 0	,	0	,	0	,	0	,	3750000	,	10384620		},
				});
			}
			else if (s == 3)
			{
				Kcc = Matrix.CreateFromArray(new double[6,6]
				{
					{10384620	,	3750000	,	0	,	0	,	0	,	0			},
					{3750000	,	10384620	,	0	,	0	,	0	,	0		},
					{0	,	0	,	10384620	,	-3750000	,	0	,	0		},
					{0	,	0	,	-3750000	,	10384620	,	0	,	0		},
					{0	,	0	,	0	,	0	,	10384620	,	-3750000		},
					{ 0	,	0	,	0	,	0	,	-3750000	,	10384620		},
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Kcc;
		}

		public static Matrix GetMatrixKcr(int s) => GetMatrixKrc(s).Transpose();

		public static Matrix GetMatrixKrc(int s)
		{
			Matrix Krc;
			if (s == 0)
			{
				Krc = Matrix.CreateFromArray(new double[8,4]
				{
					{-6346154	,	-288461.5	,	0	,	0					},
					{288461.5	,	1153846	,	0	,	0					   } ,
					{-5192308	,	3750000	,	-5192308	,	-3750000	   } ,
					{3750000	,	-5192308	,	-3750000	,	-5192308   } ,
					{0	,	0	,	-6346154	,	288461.5				   } ,
					{0	,	0	,	-288461.5	,	1153846					   } ,
					{1153846	,	288461.5	,	1153846	,	-288461.5	   } ,
					{ -288461.5	,	-6346154	,	288461.5	,	-6346154   } ,
				});
			}
			else if (s == 1)
			{
				Krc = Matrix.CreateFromArray(new double[8,4]
				{
					{-6346154	,	-288461.5	,	0	,	0				   } ,
					{288461.5	,	1153846	,	0	,	0					   } ,
					{-5192308	,	3750000	,	-5192308	,	-3750000	   } ,
					{3750000	,	-5192308	,	-3750000	,	-5192308   } ,
					{0	,	0	,	-6346154	,	288461.5				   } ,
					{0	,	0	,	-288461.5	,	1153846					   } ,
					{1153846	,	288461.5	,	1153846	,	-288461.5	   } ,
					{ -288461.5	,	-6346154	,	288461.5	,	-6346154   } ,
				});
			}
			else if (s == 2)
			{
				Krc = Matrix.CreateFromArray(new double[12,6]
				{
					{1153846	,	-288461.5	,	1153846	,	288461.5	,	0	,	0					},
					{288461.5	,	-6346154	,	-288461.5	,	-6346154	,	0	,	0				},
					{-6346154	,	288461.5	,	0	,	0	,	0	,	0								},
					{-288461.5	,	1153846	,	0	,	0	,	0	,	0									},
					{-5192308	,	-3750000	,	-5192308	,	3750000	,	-5192308	,	-3750000	},
					{-3750000	,	-5192308	,	3750000	,	-5192308	,	-3750000	,	-5192308	},
					{0	,	0	,	-6346154	,	-288461.5	,	-6346154	,	288461.5				},
					{0	,	0	,	288461.5	,	1153846	,	-288461.5	,	1153846						},
					{0	,	0	,	0	,	0	,	0	,	0												},
					{0	,	0	,	0	,	0	,	0	,	0												},
					{0	,	0	,	0	,	0	,	1153846	,	-288461.5									},
					{ 0	,	0	,	0	,	0	,	288461.5	,	-6346154								},

				});
			}
			else if (s == 3)
			{
				Krc = Matrix.CreateFromArray(new double[12,6]
				{
					{1153846	,	-288461.5	,	1153846	,	288461.5	,	0	,	0				},
					{288461.5	,	-6346154	,	-288461.5	,	-6346154	,	0	,	0			},
					{-6346154	,	288461.5	,	0	,	0	,	-6346154	,	-288461.5			},
					{-288461.5	,	1153846	,	0	,	0	,	288461.5	,	1153846					},
					{-5192308	,	-3750000	,	-5192308	,	3750000	,	-5192308	,	3750000	},
					{-3750000	,	-5192308	,	3750000	,	-5192308	,	3750000	,	-5192308	},
					{0	,	0	,	-6346154	,	-288461.5	,	0	,	0							},
					{0	,	0	,	288461.5	,	1153846	,	0	,	0								},
					{0	,	0	,	0	,	0	,	1153846	,	288461.5								},
					{0	,	0	,	0	,	0	,	-288461.5	,	-6346154							},
					{0	,	0	,	0	,	0	,	0	,	0											},
					{ 0	,	0	,	0	,	0	,	0	,	0											},
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Krc;
		}

		public static Matrix GetMatrixKrr(int s)
		{
			Matrix Krr;
			if (s == 0)
			{
				Krr = Matrix.CreateFromArray(new double[8,8]
				{
					{20769230	,	0	,	2307692	,	0	,	0	,	0	,	-5192308	,	-3750000							 },
					{0	,	20769230	,	5.82E-11	,	-12692310	,	0	,	0	,	-3750000	,	-5192308				 },
					{2307692	,	5.82E-11	,	41538460	,	0	,	2307692	,	0	,	-12692310	,	0					 },
					{5.82E-11	,	-12692310	,	0	,	41538460	,	5.82E-11	,	-12692310	,	-5.82E-11	,	2307692	 },
					{0	,	0	,	2307692	,	5.82E-11	,	20769230	,	0	,	-5192308	,	3750000						 },
					{0	,	0	,	5.82E-11	,	-12692310	,	0	,	20769230	,	3750000	,	-5192308					 },
					{-5192308	,	-3750000	,	-12692310	,	0	,	-5192308	,	3750000	,	20769230	,	0			 },
					{ -3750000	,	-5192308	,	5.82E-11	,	2307692	,	3750000	,	-5192308	,	0	,	20769230		 },
				});
			}
			else if (s == 1)
			{
				Krr = Matrix.CreateFromArray(new double[8,8]
				{
					{20769230	,	0	,	2307692	,	5.82E-11	,	0	,	0	,	-5192308	,	-3750000							},
					{0	,	20769230	,	0	,	-12692310	,	0	,	0	,	-3750000	,	-5192308								},
					{2307692	,	0	,	41538460	,	0	,	2307692	,	0	,	-12692310	,	-2.33E-10							},
					{0	,	-12692310	,	0	,	41538460	,	5.82E-11	,	-12692310	,	-2.91E-10	,	2307692					},
					{0	,	0	,	2307692	,	5.82E-11	,	20769230	,	0	,	-5192308	,	3750000								},
					{0	,	0	,	5.82E-11	,	-12692310	,	0	,	20769230	,	3750000	,	-5192308							},
					{-5192308	,	-3750000	,	-12692310	,	2.33E-10	,	-5192308	,	3750000	,	20769230	,	4.66E-10	},
					{ -3750000	,	-5192308	,	2.91E-10	,	2307692	,	3750000	,	-5192308	,	4.66E-10	,	20769230		},
				});
			}
			else if (s == 2)
			{
				Krr = Matrix.CreateFromArray(new double[12,12]
				{
					{20769230	,	-1.40E-09	,	-5192308	,	3750000	,	-12692310	,	1.16E-09	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0						},
					{-1.40E-09	,	20769230	,	3750000	,	-5192308	,	1.28E-09	,	2307692	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0							},
					{-5192308	,	3750000	,	20769230	,	-4.66E-10	,	2307692	,	1.75E-10	,	0	,	0	,	-6346154	,	-288461.5	,	-5192308	,	-3750000			},
					{3750000	,	-5192308	,	-4.66E-10	,	20769230	,	2.91E-10	,	-12692310	,	0	,	0	,	288461.5	,	1153846	,	-3750000	,	-5192308		},
					{-12692310	,	1.51E-09	,	2307692	,	1.75E-10	,	41538460	,	0	,	2307692	,	1.75E-10	,	-5192308	,	3750000	,	-12692310	,	-1.34E-09		},
					{1.51E-09	,	2307692	,	5.24E-10	,	-12692310	,	0	,	41538460	,	2.91E-10	,	-12692310	,	3750000	,	-5192308	,	-1.28E-09	,	2307692		},
					{-5192308	,	-3750000	,	0	,	0	,	2307692	,	1.75E-10	,	20769230	,	4.66E-10	,	0	,	0	,	-5192308	,	3750000							},
					{-3750000	,	-5192308	,	0	,	0	,	5.24E-10	,	-12692310	,	4.66E-10	,	20769230	,	0	,	0	,	3750000	,	-5192308						},
					{0	,	0	,	-6346154	,	288461.5	,	-5192308	,	3750000	,	0	,	0	,	10384620	,	-3750000	,	1153846	,	-288461.5							},
					{0	,	0	,	-288461.5	,	1153846	,	3750000	,	-5192308	,	0	,	0	,	-3750000	,	10384620	,	288461.5	,	-6346154							},
					{0	,	0	,	-5192308	,	-3750000	,	-12692310	,	-9.31E-10	,	-5192308	,	3750000	,	1153846	,	288461.5	,	20769230	,	9.31E-10			},
					{ 0	,	0	,	-3750000	,	-5192308	,	-1.16E-09	,	2307692	,	3750000	,	-5192308	,	-288461.5	,	-6346154	,	9.31E-10	,	20769230			},

				});
			}
			else if (s == 3)
			{
				Krr = Matrix.CreateFromArray(new double[12,12]
				{
					{20769230	,	-1.40E-09	,	-5192308	,	3750000	,	-12692310	,	6.98E-10	,	-5192308	,	-3750000	,	0	,	0	,	0	,	0								},
					{-1.86E-09	,	20769230	,	3750000	,	-5192308	,	1.16E-09	,	2307692	,	-3750000	,	-5192308	,	0	,	0	,	0	,	0									},
					{-5192308	,	3750000	,	20769230	,	-9.31E-10	,	2307692	,	5.82E-11	,	0	,	0	,	-5192308	,	-3750000	,	0	,	0									},
					{3750000	,	-5192308	,	-9.31E-10	,	20769230	,	-3.49E-10	,	-12692310	,	0	,	0	,	-3750000	,	-5192308	,	0	,	0							},
					{-12692310	,	1.28E-09	,	2307692	,	0	,	41538460	,	-4.66E-10	,	2307692	,	1.75E-10	,	-12692310	,	-1.22E-09	,	-5192308	,	-3750000			},
					{9.31E-10	,	2307692	,	2.33E-10	,	-12692310	,	-4.66E-10	,	41538460	,	2.91E-10	,	-12692310	,	-1.28E-09	,	2307692	,	-3750000	,	-5192308	},
					{-5192308	,	-3750000	,	0	,	0	,	2307692	,	1.75E-10	,	20769230	,	4.66E-10	,	-5192308	,	3750000	,	-6346154	,	288461.5					},
					{-3750000	,	-5192308	,	0	,	0	,	5.24E-10	,	-12692310	,	4.66E-10	,	20769230	,	3750000	,	-5192308	,	-288461.5	,	1153846					},
					{0	,	0	,	-5192308	,	-3750000	,	-12692310	,	-9.31E-10	,	-5192308	,	3750000	,	20769230	,	1.40E-09	,	1153846	,	-288461.5					},
					{0	,	0	,	-3750000	,	-5192308	,	-1.16E-09	,	2307692	,	3750000	,	-5192308	,	1.86E-09	,	20769230	,	288461.5	,	-6346154					},
					{0	,	0	,	0	,	0	,	-5192308	,	-3750000	,	-6346154	,	-288461.5	,	1153846	,	288461.5	,	10384620	,	3750000									},
					{ 0	,	0	,	0	,	0	,	-3750000	,	-5192308	,	288461.5	,	1153846	,	-288461.5	,	-6346154	,	3750000	,	10384620									},
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return Krr;
		}

		public static Matrix GetMatrixKrrInverse(int s)
		{
			Matrix KrrInverse;
			if (s == 0)
			{
				KrrInverse = Matrix.CreateFromArray(new double[8,8]
				{
					{5.3987715049497600E-08	,	6.7548644191971900E-09	,	1.8242105305696200E-09	,	1.7302885324183300E-09	,	1.8129091196700300E-09	,	9.3530331316611400E-10	,	1.6115707081361900E-08	,	1.1150739274927000E-08	  },
					{6.7548644191971900E-09	,	7.2242853802080900E-08	,	3.7411469811331200E-09	,	2.6785659586987900E-08	,	-9.3530331316611500E-10	,	1.9291256861808200E-08	,	1.3301852762642700E-08	,	2.1295848316270900E-08	  },
					{1.8242105305696200E-09	,	3.7411469811331200E-09	,	3.0202557891620000E-08	,	-1.2468446814699300E-25	,	1.8242105305696200E-09	,	-3.7411469811331200E-09	,	2.0720197897322500E-08	,	-2.1426589083642500E-25	  },
					{1.7302885324183300E-09	,	2.6785659586987900E-08	,	4.7589110221724100E-26	,	3.9910696339671800E-08	,	-1.7302885324183300E-09	,	2.6785659586987900E-08	,	4.0154245436415700E-25	,	9.5831361323089800E-09	  },
					{1.8129091196700300E-09	,	-9.3530331316611500E-10	,	1.8242105305696200E-09	,	-1.7302885324183300E-09	,	5.3987715049497600E-08	,	-6.7548644191972000E-09	,	1.6115707081361900E-08	,	-1.1150739274927000E-08	  },
					{9.3530331316611400E-10	,	1.9291256861808200E-08	,	-3.7411469811331200E-09	,	2.6785659586987900E-08	,	-6.7548644191972000E-09	,	7.2242853802080900E-08	,	-1.3301852762642700E-08	,	2.1295848316270900E-08	  },
					{1.6115707081361900E-08	,	1.3301852762642700E-08	,	2.0720197897322500E-08	,	1.2029641886658700E-25	,	1.6115707081361900E-08	,	-1.3301852762642700E-08	,	7.3671797187590000E-08	,	-1.0593371247092400E-24	  },
					{ 1.1150739274927000E-08	,	2.1295848316270900E-08	,	-4.2655392095159800E-25	,	9.5831361323089800E-09	,	-1.1150739274927000E-08	,	2.1295848316270900E-08	,	-1.4281777719521300E-24	,	6.1757938312264400E-08},
				});
			}
			else if (s == 1)
			{
				KrrInverse = Matrix.CreateFromArray(new double[8,8]
				{
					{5.398771504949760000E-08	,	6.754864419197190000E-09	,	1.824210530569620000E-09	,	1.730288532418330000E-09	,	1.812909119670030000E-09	,	9.353033131661140000E-10	,	1.611570708136190000E-08	,	1.115073927492700000E-08	},
					{6.754864419197190000E-09	,	7.224285380208090000E-08	,	3.741146981133120000E-09	,	2.678565958698790000E-08	,	-9.353033131661150000E-10	,	1.929125686180820000E-08	,	1.330185276264270000E-08	,	2.129584831627090000E-08	},
					{1.824210530569620000E-09	,	3.741146981133120000E-09	,	3.020255789162000000E-08	,	-4.177237708855090000E-25	,	1.824210530569620000E-09	,	-3.741146981133120000E-09	,	2.072019789732250000E-08	,	-4.285317816728500000E-25	},
					{1.730288532418330000E-09	,	2.678565958698790000E-08	,	4.850822780546460000E-25	,	3.991069633967180000E-08	,	-1.730288532418330000E-09	,	2.678565958698790000E-08	,	2.014095580418660000E-24	,	9.583136132308980000E-09	},
					{1.812909119670030000E-09	,	-9.353033131661150000E-10	,	1.824210530569620000E-09	,	-1.730288532418330000E-09	,	5.398771504949760000E-08	,	-6.754864419197200000E-09	,	1.611570708136190000E-08	,	-1.115073927492700000E-08	},
					{9.353033131661150000E-10	,	1.929125686180820000E-08	,	-3.741146981133120000E-09	,	2.678565958698790000E-08	,	-6.754864419197200000E-09	,	7.224285380208090000E-08	,	-1.330185276264270000E-08	,	2.129584831627090000E-08	},
					{1.611570708136190000E-08	,	1.330185276264270000E-08	,	2.072019789732250000E-08	,	-1.783559285089610000E-24	,	1.611570708136190000E-08	,	-1.330185276264270000E-08	,	7.367179718759000000E-08	,	-3.178011431875190000E-24	},
					{ 1.115073927492700000E-08	,	2.129584831627090000E-08	,	-1.161543063482750000E-24	,	9.583136132308980000E-09	,	-1.115073927492700000E-08	,	2.129584831627090000E-08	,	-2.816274436039440000E-24	,	6.175793831226440000E-08	},

				});
			}
			else if (s == 2)
			{
				KrrInverse = Matrix.CreateFromArray(new double[12,12]
				{
					{9.635549548245800E-08	,	-5.156350158384800E-09	,	4.498917553908760E-08	,	-1.659822008842410E-08	,	4.278951124154880E-08	,	-7.406333829689270E-09	,	2.733153906299230E-08	,	5.433854849925910E-09	,	4.864405390047500E-08	,	1.993311840429970E-09	,	3.752118613262030E-08	,	2.504698144047150E-09	},
					{-5.156350158384800E-09	,	6.473402784838520E-08	,	-1.669911368826220E-08	,	2.757700993995830E-08	,	-4.909332230632000E-09	,	1.481624381839220E-08	,	8.251590920499770E-09	,	2.733153906299230E-08	,	-1.607235397076890E-08	,	5.484559069590980E-09	,	-4.250973850214130E-09	,	9.028511398187800E-09	},
					{4.498917553908760E-08	,	-1.669911368826220E-08	,	1.020963225501250E-07	,	-3.870226460360440E-09	,	2.627498089052820E-08	,	-4.015614923099660E-09	,	9.028511398187790E-09	,	2.504698144047140E-09	,	8.887601958272380E-08	,	4.170695138780490E-08	,	3.717030002072430E-08	,	3.088688877499300E-08	},
					{-1.659822008842410E-08	,	2.757700993995830E-08	,	-3.870226460360430E-09	,	9.114725004926000E-08	,	-7.994347567740120E-09	,	4.069220921484990E-08	,	-4.250973850214130E-09	,	3.752118613262030E-08	,	-1.152189495917510E-08	,	3.146996839708130E-08	,	2.969770515805650E-09	,	3.717030002072430E-08	},
					{4.278951124154880E-08	,	-4.909332230632000E-09	,	2.627498089052820E-08	,	-7.994347567740120E-09	,	5.303540917765930E-08	,	-9.087995712350270E-09	,	1.481624381839220E-08	,	-7.406333829689270E-09	,	3.700416477944520E-08	,	-1.229925788192760E-08	,	4.069220921484980E-08	,	-4.015614923099650E-09	},
					{-7.406333829689280E-09	,	1.481624381839220E-08	,	-4.015614923099650E-09	,	4.069220921484990E-08	,	-9.087995712350270E-09	,	5.303540917765940E-08	,	-4.909332230632000E-09	,	4.278951124154880E-08	,	-1.229925788192760E-08	,	3.700416477944530E-08	,	-7.994347567740120E-09	,	2.627498089052820E-08	},
					{2.733153906299230E-08	,	8.251590920499780E-09	,	9.028511398187780E-09	,	-4.250973850214130E-09	,	1.481624381839220E-08	,	-4.909332230632000E-09	,	6.473402784838520E-08	,	-5.156350158384810E-09	,	5.484559069590960E-09	,	-1.607235397076890E-08	,	2.757700993995830E-08	,	-1.669911368826220E-08	},
					{5.433854849925900E-09	,	2.733153906299230E-08	,	2.504698144047150E-09	,	3.752118613262030E-08	,	-7.406333829689280E-09	,	4.278951124154880E-08	,	-5.156350158384810E-09	,	9.635549548245800E-08	,	1.993311840429960E-09	,	4.864405390047500E-08	,	-1.659822008842410E-08	,	4.498917553908760E-08	},
					{4.864405390047500E-08	,	-1.607235397076890E-08	,	8.887601958272380E-08	,	-1.152189495917510E-08	,	3.700416477944520E-08	,	-1.229925788192760E-08	,	5.484559069590970E-09	,	1.993311840429960E-09	,	2.009324470700230E-07	,	8.140906339547780E-08	,	3.146996839708130E-08	,	4.170695138780490E-08	},
					{1.993311840429960E-09	,	5.484559069590970E-09	,	4.170695138780490E-08	,	3.146996839708130E-08	,	-1.229925788192760E-08	,	3.700416477944530E-08	,	-1.607235397076890E-08	,	4.864405390047500E-08	,	8.140906339547780E-08	,	2.009324470700230E-07	,	-1.152189495917510E-08	,	8.887601958272380E-08	},
					{3.752118613262030E-08	,	-4.250973850214120E-09	,	3.717030002072430E-08	,	2.969770515805660E-09	,	4.069220921484980E-08	,	-7.994347567740110E-09	,	2.757700993995830E-08	,	-1.659822008842410E-08	,	3.146996839708130E-08	,	-1.152189495917510E-08	,	9.114725004926000E-08	,	-3.870226460360440E-09	},
					{ 2.504698144047140E-09	,	9.028511398187790E-09	,	3.088688877499300E-08	,	3.717030002072430E-08	,	-4.015614923099650E-09	,	2.627498089052820E-08	,	-1.669911368826220E-08	,	4.498917553908760E-08	,	4.170695138780490E-08	,	8.887601958272380E-08	,	-3.870226460360440E-09	,	1.020963225501250E-07	},

				});
			}
			else if (s == 3)
			{
				KrrInverse = Matrix.CreateFromArray(new double[12,12]
				{
					{9.635549548245800E-08	,	5.156350158384800E-09	,	2.733153906299230E-08	,	-5.433854849925910E-09	,	4.278951124154880E-08	,	7.406333829689270E-09	,	4.498917553908760E-08	,	1.659822008842410E-08	,	3.752118613262020E-08	,	-2.504698144047140E-09	,	4.864405390047500E-08	,	-1.993311840429960E-09	 },
					{5.156350158384800E-09	,	6.473402784838520E-08	,	-8.251590920499770E-09	,	2.733153906299230E-08	,	4.909332230631990E-09	,	1.481624381839220E-08	,	1.669911368826220E-08	,	2.757700993995830E-08	,	4.250973850214120E-09	,	9.028511398187790E-09	,	1.607235397076890E-08	,	5.484559069590970E-09	 },
					{2.733153906299230E-08	,	-8.251590920499770E-09	,	6.473402784838520E-08	,	5.156350158384810E-09	,	1.481624381839220E-08	,	4.909332230632000E-09	,	9.028511398187780E-09	,	4.250973850214130E-09	,	2.757700993995830E-08	,	1.669911368826230E-08	,	5.484559069590960E-09	,	1.607235397076890E-08	 },
					{-5.433854849925900E-09	,	2.733153906299230E-08	,	5.156350158384810E-09	,	9.635549548245810E-08	,	7.406333829689280E-09	,	4.278951124154880E-08	,	-2.504698144047150E-09	,	3.752118613262030E-08	,	1.659822008842410E-08	,	4.498917553908760E-08	,	-1.993311840429970E-09	,	4.864405390047500E-08	 },
					{4.278951124154880E-08	,	4.909332230632000E-09	,	1.481624381839220E-08	,	7.406333829689280E-09	,	5.303540917765930E-08	,	9.087995712350270E-09	,	2.627498089052820E-08	,	7.994347567740120E-09	,	4.069220921484980E-08	,	4.015614923099660E-09	,	3.700416477944520E-08	,	1.229925788192760E-08	 },
					{7.406333829689270E-09	,	1.481624381839220E-08	,	4.909332230632000E-09	,	4.278951124154890E-08	,	9.087995712350270E-09	,	5.303540917765940E-08	,	4.015614923099650E-09	,	4.069220921484990E-08	,	7.994347567740120E-09	,	2.627498089052820E-08	,	1.229925788192760E-08	,	3.700416477944530E-08	 },
					{4.498917553908760E-08	,	1.669911368826220E-08	,	9.028511398187790E-09	,	-2.504698144047150E-09	,	2.627498089052820E-08	,	4.015614923099650E-09	,	1.020963225501250E-07	,	3.870226460360430E-09	,	3.717030002072430E-08	,	-3.088688877499300E-08	,	8.887601958272370E-08	,	-4.170695138780490E-08	 },
					{1.659822008842410E-08	,	2.757700993995830E-08	,	4.250973850214120E-09	,	3.752118613262030E-08	,	7.994347567740110E-09	,	4.069220921484990E-08	,	3.870226460360430E-09	,	9.114725004926000E-08	,	-2.969770515805660E-09	,	3.717030002072430E-08	,	1.152189495917510E-08	,	3.146996839708130E-08	 },
					{3.752118613262020E-08	,	4.250973850214120E-09	,	2.757700993995830E-08	,	1.659822008842410E-08	,	4.069220921484980E-08	,	7.994347567740120E-09	,	3.717030002072430E-08	,	-2.969770515805650E-09	,	9.114725004926000E-08	,	3.870226460360440E-09	,	3.146996839708120E-08	,	1.152189495917510E-08	 },
					{-2.504698144047140E-09	,	9.028511398187790E-09	,	1.669911368826220E-08	,	4.498917553908760E-08	,	4.015614923099660E-09	,	2.627498089052820E-08	,	-3.088688877499300E-08	,	3.717030002072430E-08	,	3.870226460360440E-09	,	1.020963225501250E-07	,	-4.170695138780490E-08	,	8.887601958272380E-08	 },
					{4.864405390047500E-08	,	1.607235397076890E-08	,	5.484559069590960E-09	,	-1.993311840429960E-09	,	3.700416477944520E-08	,	1.229925788192760E-08	,	8.887601958272370E-08	,	1.152189495917510E-08	,	3.146996839708130E-08	,	-4.170695138780490E-08	,	2.009324470700230E-07	,	-8.140906339547770E-08	 },
					{ -1.993311840429960E-09	,	5.484559069590970E-09	,	1.607235397076890E-08	,	4.864405390047500E-08	,	1.229925788192760E-08	,	3.700416477944530E-08	,	-4.170695138780490E-08	,	3.146996839708130E-08	,	1.152189495917510E-08	,	8.887601958272380E-08	,	-8.140906339547780E-08	,	2.009324470700230E-07},

				});
			}
			else
			{
				throw new ArgumentException();
			}

			return KrrInverse;
		}

		public static Matrix GetMatrixKccStar(int s)
		{
			Matrix KccStar;
			if (s == 0)
			{
				KccStar = Matrix.CreateFromArray(new double[,]
				{
					{ 7124600.21307216, -2899919.93248257, -62230.7643390761, -233310.973259978 },
					{ -2899919.93248257, 6170212.76085613, 233310.973259978, -3122570.80500982 },
					{ -62230.7643390762, 233310.973259978, 7124600.21307216, 2899919.93248257 },
					{ -233310.973259978, -3122570.80500982, 2899919.93248257, 6170212.76085613 },
				});
			}
			else if (s == 1)
			{
				KccStar = Matrix.CreateFromArray(new double[,]
				{
					{ 7124600.21307216, -2899919.93248257, -62230.7643390764, -233310.973259978 },
					{ -2899919.93248257, 6170212.76085613, 233310.973259978, -3122570.80500982 },
					{ -62230.7643390763, 233310.973259978, 7124600.21307216, 2899919.93248257 },
					{ -233310.973259978, -3122570.80500982, 2899919.93248257, 6170212.76085613 },
				});
			}
			else if (s == 2)
			{
				KccStar = Matrix.CreateFromArray(new double[,]
				{
					{ 3780120.80798458, 2427526.03433718, -1352594.77364741, 1352594.77364741, -2427526.03433718, -3780120.80798459 },
					{ 2427526.03433718, 5828625.08438509, -99663.6433493446, -3401099.05004791, -2327862.39098783, -2427526.03433718 },
					{ -1352594.77364741, -99663.6433493439, 4753693.82369532, -1252931.13029806, -3401099.05004791, 1352594.77364741 },
					{ 1352594.77364741, -3401099.05004791, -1252931.13029806, 4753693.82369531, -99663.6433493452, -1352594.77364741 },
					{ -2427526.03433718, -2327862.39098783, -3401099.05004791, -99663.6433493449, 5828625.08438509, 2427526.03433718 },
					{ -3780120.80798459, -2427526.03433718, 1352594.77364741, -1352594.77364741, 2427526.03433718, 3780120.80798458 },
				});
			}
			else if (s == 3)
			{
				KccStar = Matrix.CreateFromArray(new double[,]
				{
					{ 4753693.82369531, 1252931.13029806, -1352594.77364741, 99663.6433493444, -3401099.05004791, -1352594.7736474 },
					{ 1252931.13029806, 4753693.82369531, -1352594.77364741, -3401099.05004791, 99663.6433493437, -1352594.7736474 },
					{ -1352594.77364741, -1352594.77364741, 3780120.80798459, -2427526.03433718, -2427526.03433718, 3780120.80798458 },
					{ 99663.6433493447, -3401099.05004791, -2427526.03433718, 5828625.08438509, 2327862.39098783, -2427526.03433718 },
					{ -3401099.05004791, 99663.6433493425, -2427526.03433718, 2327862.39098784, 5828625.08438509, -2427526.03433718 },
					{ -1352594.7736474, -1352594.7736474, 3780120.80798458, -2427526.03433718, -2427526.03433718, 3780120.80798459 },
				});
			}
			else
			{
				throw new ArgumentException();
			}

			return KccStar;
		}

		public static Matrix GetInverseCoarseProblemMatrix()
		{
			Matrix globKccStarInverse = Matrix.CreateFromArray(new double[8, 8]
			{
				{ 1.50968477962071E-07, 6.47351877732403E-08, 1.74360720204402E-08, 4.13268867804468E-08, -3.71921703598146E-08, 4.60559272413584E-08, 3.75973155336062E-08, 1.44441859883247E-07 },
				{ 6.47351877732403E-08, 1.75999001764221E-07, 8.53170378636455E-09, 9.23447933265357E-08, -4.60559272413584E-08, 8.13364805253358E-08, 2.77715626644858E-08, 1.71066298852853E-07 },
				{ 1.74360720204402E-08, 8.53170378636455E-09, 5.65830969778524E-08, -1.01186992510256E-23, 1.74360720204402E-08, -8.53170378636457E-09, 4.36864380252944E-08, -2.09352398297082E-23 },
				{ 4.13268867804468E-08, 9.23447933265357E-08, -1.16084408695519E-23, 1.14203970609158E-07, -4.13268867804468E-08, 9.23447933265357E-08, -1.0485908973556E-23, 1.41493284349433E-07 },
				{ -3.71921703598146E-08, -4.60559272413584E-08, 1.74360720204402E-08, -4.13268867804468E-08, 1.50968477962071E-07, -6.47351877732404E-08, 3.75973155336062E-08, -1.44441859883247E-07 },
				{ 4.60559272413584E-08, 8.13364805253358E-08, -8.53170378636457E-09, 9.23447933265358E-08, -6.47351877732404E-08, 1.75999001764222E-07, -2.77715626644858E-08, 1.71066298852853E-07 },
				{ 3.75973155336062E-08, 2.77715626644858E-08, 4.36864380252944E-08, -9.09421484612439E-24, 3.75973155336062E-08, -2.77715626644858E-08, 1.38025474235373E-07, -2.81000352151372E-23 },
				{ 1.44441859883247E-07, 1.71066298852853E-07, -2.3872665821384E-23, 1.41493284349433E-07, -1.44441859883247E-07, 1.71066298852853E-07, -2.81000352151372E-23, 4.37197310619827E-07 },
			});

			return globKccStarInverse;
		}
		#endregion

	}
}
