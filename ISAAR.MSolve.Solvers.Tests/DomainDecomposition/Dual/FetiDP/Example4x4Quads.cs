using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP
{
    public static class Example4x4Quads
    {
        public const double GlobalForcesNorm = 10.0; // sqrt(0^2 + 0^2 + ... + 0^2 + 10^2)

        public static Vector DisplacementsCornerHomogeneousSolution => Vector.CreateFromArray(new double[]
        {
            21.1181096194325, 27.2052778266603, 1.63160365361360, 25.0125476374046, 1.69318450304898,
            61.8894615542161, -24.7267309921556, 26.3640977652349
        });

        public static Vector DisplacementsGlobalHeterogeneousSolution => Vector.CreateFromArray(new double[]
        {
            17.623494584618864, 12.564560593215612, 31.832863897135404, 34.496634608059082, 40.255481382985629,
            66.49190654178912, 42.572002358887204, 99.798764204232072, 4.267568672307144, 9.00506902466324,
            9.100928263505315, 31.107370029452451, 12.1615036308774, 66.065492717632239, 11.510673148931499,
            102.06649895017948, -3.0529124682202156, 9.24107474483673, -7.8531777412741217, 26.728892403726846,
            -16.890006178831449, 70.602493468916791, -21.80233265288679, 109.39882637058051, -4.7311061272016808,
            10.030926199331375, -5.6722429958962142, 18.837815470700932, 146.94209278892487, 392.04674590737193,
            -35.619167413693908, 1407.200332011206, -9.9609496807814057, 10.46574373452243, -17.603838651152756,
            20.760800663270086, -843.13592713307355, 371.10700308359418, -1666.2547486301742, 3714.1637893447919
        });

        public static Vector DisplacementsGlobalHomogeneousSolution => Vector.CreateFromArray(new double[]
        {
            13.3258563908201, 12.3999624809163, 21.1181095863809, 27.2052777811441, 24.3525812415758,
            43.2777053704649, 24.8992347210378, 57.3521080292628, 4.74521148903743, 9.87352108397423,
            9.37569840211612, 25.9120840082139, 11.8910608093164, 43.4314599456699, 12.2652060584230,
            57.9466725072280, 0.450346260334126, 9.02020634682474, 1.63160365355026, 25.0125475922504,
            2.58948267402381, 45.0651412625480, 1.69318450300533, 61.8894614604312, -4.68849826343688,
            8.90417219731433, -8.76400355420594, 24.5661224138922, -9.40948533633272, 47.1084814579881,
            -11.2141368968962, 73.2559168929990, -14.0271645568764, 11.3572597884005, -24.7267309592324,
            26.3640977197317, -34.3702668180117, 46.6307017985724, -42.8927307907656, 96.0971764416081
        });

        public static Matrix InterfaceProblemMatrix => Matrix.CreateFromArray(new double[,]
        {
            { 4.97303228211014, 0.0303495596681853, 0.478870888249040, -0.510074267142073, -0.554638064586832, -0.639203724429228, 0.342142442028879, -0.0259960944946401        },
            { 0.0303495596681853, 2.75206109564910, -0.132450092323064, 0.393630305623386, -0.137248503790822, -0.602781684493727, 0.0259960944946400, 0.0326814286491487        },
            { 0.478870888249040, -0.132450092323064, 2.50773589560585, -6.12608644034637e-17, -0.00517803522230879, -1.40567757982761e-16, -0.478870888249040, -0.132450092323064},
            { -0.510074267142073, 0.393630305623386, -1.10893466892511e-16, 3.35755264805804, 1.18406464657668e-16, 0.293147921294313, -0.510074267142073, -0.393630305623386    },
            { -0.554638064586832, -0.137248503790822, -0.00517803522230876, 1.39289728666574e-16, 2.79928131129566, -1.91565648067391e-16, 0.554638064586830, -0.137248503790822 },
            { -0.639203724429228, -0.602781684493727, -2.44519425851536e-16, 0.293147921294313, -2.04349167449641e-16, 4.95200731231851, -0.639203724429227, 0.602781684493727   },
            { 0.342142442028879, 0.0259960944946401, -0.478870888249040, -0.510074267142073, 0.554638064586830, -0.639203724429227, 4.97303228211014, -0.0303495596681853        },
            { -0.0259960944946401, 0.0326814286491487, -0.132450092323064, -0.393630305623386, -0.137248503790822, 0.602781684493727, -0.0303495596681853, 2.75206109564910      }
        });

        public static Vector InterfaceProblemRhsHomogeneous => Vector.CreateFromArray(new double[]
        {
            -14.9810838729735, -5.69975426333296, -10.5434726428584, 0.244121938779135, -5.89291361392317,
            -13.1189445403298, 16.4060122931895, -5.93260749341458
        });

        public static Vector LagrangeMultipliersHomogeneousSolution => Vector.CreateFromArray(new double[]
        {
            -3.67505611805653, -3.06916047739931, -3.12635180105707, 0.427127980701075, -3.73923329344533,
            -2.87580179407164, 3.34727977833535, -1.76301688321532
        });

        public static Matrix MatrixFIrr => Matrix.CreateFromArray(new double[,]
        {
            { 3.57057200993606, -0.108283270560292, 0.338429752179871, -0.279338843056072, -0.573961878785917, -0.114111168546807, 0, 0  },
            { -0.108283270560292, 2.65633088920628, -0.234165486478537, 0.447212600200740, -0.173283461887574, -0.573961878785916, 0, 0  },
            { 0.338429752179871, -0.234165486478537, 2.26748388676785, -2.77555756156289e-17, 0, 0, -0.338429752179871, -0.23416548647853},
            { -0.279338843056072, 0.447212600200740, -2.77555756156289e-17, 3.03419905760385, 0, 0, -0.279338843056072, -0.44721260020073},
            { -0.573961878785917, -0.173283461887574, 0, 0, 2.71882869786337, -2.63677968348475e-16, 0.573961878785916, -0.17328346188757},
            { -0.114111168546807, -0.573961878785916, 0, 0, -2.63677968348475e-16, 4.04692914347278, -0.114111168546807, 0.57396187878591},
            { 0, 0, -0.338429752179871, -0.279338843056072, 0.573961878785916, -0.114111168546807, 3.57057200993606, 0.108283270560292   },
            { 0, 0, -0.234165486478537, -0.447212600200739, -0.173283461887574, 0.573961878785916, 0.108283270560292, 2.65633088920628   }
        });

        public static Matrix MatrixFIrc => Matrix.CreateFromArray(new double[,]
        {
            { 0.244415273977447, 0.232902352320994, 0.188150279438879, -0.367471730456911, 0.325403750731022, 0.134569378056537, 0, 0                                           },
            { -0.127173102613820, 0.0205141879116909, 0.0345524284084688, 0.0581554138518863, 0.0926206740937292, 0.0806737768451912, 0, 0                                      },
            { -0.00592361806200106, 0.0896358681318229, -5.55111512312578e-17, 0.152076488272397, 0, 0, 0.00592361806200106, 0.0896358681318228                                 },
            { 0.0980092297384746, -0.270103007383575, -0.136396263304725, 0, 0, 0, 0.0980092297384746, 0.270103007383575                                                        },
            { -0.0806737768451914, -0.0926206740937293, -2.22044604925031e-16, 0.0238937950679602, -1.66533453693773e-16, 0.161347553342741, 0.0806737768451914, -0.092620674093},
            { -0.134569378056537, -0.325403750731022, 0.515640037124902, 5.55111512312578e-17, -0.246501280853069, -2.22044604925031e-16, -0.134569378056537, 0.325403750731022 },
            { 0, 0, 0.188150279438879, 0.367471730456911, 0.325403750731022, -0.134569378056537, 0.244415273977447, -0.232902352320994                                          },
            { 0, 0, -0.0345524284084688, 0.0581554138518863, -0.0926206740937292, 0.0806737768451912, 0.127173102613820, 0.0205141879116910                                     }
        });

        public static Matrix MatrixGlobalKccStar => Matrix.CreateFromArray(new double[,]
        {
            {0.519272429341174, -0.0224949475741955, -0.0673726448231679, 0.0532992286325700, -0.115596477967984, -0.180005752865632, 0, 0                                   },
            {-0.0224949475741955, 0.571373230561769, 0.00636415859565785, -0.310650945646995, -0.110850590090827, -0.115596477967984, 0, 0                                   },
            {-0.0673726448231679, 0.00636415859565783, 1.13126609833817, 0, -0.323914195445245, 2.77555756156289e-17, -0.0673726448231678, -0.00636415859565788              },
            {0.0532992286325700, -0.310650945646995, 0, 1.04037205521382, 4.16333634234434e-17, -0.128818550126366, -0.0532992286325699, -0.310650945646995                  },
            {-0.115596477967984, -0.110850590090827, -0.323914195445245, 5.72458747072346e-17, 0.555107150696809, 1.38777878078145e-17, -0.115596477967984, 0.110850590090827},
            {-0.180005752865631, -0.115596477967984, 2.77555756156289e-17, -0.128818550126366, 1.38777878078145e-17, 0.360011505131263, 0.180005752865631, -0.115596477967984},
            {0, 0, -0.0673726448231679, -0.0532992286325699, -0.115596477967984, 0.180005752865631, 0.519272429341174, 0.0224949475741954                                    },
            {0, 0, -0.00636415859565789, -0.310650945646995, 0.110850590090827, -0.115596477967984, 0.0224949475741954, 0.571373230561769                                    }
        });

        public static Vector VectorDrHomogeneous => Vector.CreateFromArray(new double[]
        {
            0, 0, 0, 0, -3.375195492420367, -10.215251712309035, 0.418600986802971, -1.151753569240856
        });

        public static Vector VectorGlobalFcStarHomogeneous => Vector.CreateFromArray(new double[]
        {
            0, 0, 3.19374601989718, 2.11725876973317, 0.254044579955345, 6.55220940504195, -3.44779060118368, 1.33053183634499
        });

        public static Model CreateHeterogeneousModel(double stiffnessRatio)
        {
            //                                    Λ P
            //                                    | 
            //                                     
            // |> 20 ---- 21 ---- 22 ---- 23 ---- 24
            //    |  (12) |  (13) |  (14) |  (15) |
            //    |  E0   |  E0   |  E1   |  E1   |
            // |> 15 ---- 16 ---- 17 ---- 18 ---- 19
            //    |  (8)  |  (9)  |  (10) |  (11) |
            //    |  E0   |  E0   |  E1   |  E1   |
            // |> 10 ---- 11 ---- 12 ---- 13 ---- 14
            //    |  (4)  |  (5)  |  (6)  |  (7)  |
            //    |  E0   |  E0   |  E0   |  E0   |
            // |> 5 ----- 6 ----- 7 ----- 8 ----- 9
            //    |  (0)  |  (1)  |  (2)  |  (3)  |
            //    |  E0   |  E0   |  E0   |  E0   |
            // |> 0 ----- 1 ----- 2 ----- 3 ----- 4


            var builder = new Uniform2DModelBuilder();
            builder.DomainLengthX = 4.0;
            builder.DomainLengthY = 4.0;
            builder.NumSubdomainsX = 2;
            builder.NumSubdomainsY = 2;
            builder.NumTotalElementsX = 4;
            builder.NumTotalElementsY = 4;
            //builder.YoungModulus = 1.0;
            double E0 = 1.0;
            builder.YoungModuliOfSubdomains = new double[,]
            {
                { E0, E0 }, { E0, stiffnessRatio * E0}
            };
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
            builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.UpperRightCorner, StructuralDof.TranslationY, 10.0);

            return builder.BuildModel();
        }

        public static Model CreateHomogeneousModel()
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

            // Or just:
            //return CreateHeterogeneousModel(1.0);
        }

        public static HashSet<INode> DefineCornerNodesGlobal(IModel model)
        {
            return new HashSet<INode>(new INode[]
            {
                model.GetNode(2), model.GetNode(12), model.GetNode(14), model.GetNode(22)
            });
        }

        public static HashSet<INode> DefineCornerNodesSubdomain(ISubdomain subdomain)
        {
            // subdomain 2         subdomain 3                      
            // 20 ---- 21 ---- 22  22---- 23 ---- 24
            // |  (12) |  (13) |   | (14) |  (15) |
            // |       |       |   |      |       |
            // 15 ---- 16 ---- 17  17---- 18 ---- 19
            // |  (8)  |  (9)  |   | (10) |  (11) |
            // |       |       |   |      |       |
            // 10 ---- 11 ---- 12  12---- 13 ---- 14

            // subdomain 0         subdomain 1
            // 10 ---- 11 ---- 12  12---- 13 ---- 14
            // |  (4)  |  (5)  |   | (6)  |  (7)  |
            // |       |       |   |      |       |
            // 5 ----- 6 ----- 7   7 ---- 8 ----- 9
            // |  (0)  |  (1)  |   | (2)  |  (3)  |
            // |       |       |   |      |       |
            // 0 ----- 1 ----- 2   2 ---- 3 ----- 4

            if (subdomain.ID == 0)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(2), subdomain.GetNode(12) });
            }
            else if (subdomain.ID == 1)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(2), subdomain.GetNode(12), subdomain.GetNode(14) });
            }
            else if (subdomain.ID == 2)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(12), subdomain.GetNode(22) });
            }
            else if (subdomain.ID == 3)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(12), subdomain.GetNode(14), subdomain.GetNode(22) });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Dictionary<ISubdomain, HashSet<INode>> DefineCornerNodesSubdomainsAll(IModel model)
        {
            // subdomain 2         subdomain 3                      
            // 20 ---- 21 ---- 22  22---- 23 ---- 24
            // |  (12) |  (13) |   | (14) |  (15) |
            // |       |       |   |      |       |
            // 15 ---- 16 ---- 17  17---- 18 ---- 19
            // |  (8)  |  (9)  |   | (10) |  (11) |
            // |       |       |   |      |       |
            // 10 ---- 11 ---- 12  12---- 13 ---- 14

            // subdomain 0         subdomain 1
            // 10 ---- 11 ---- 12  12---- 13 ---- 14
            // |  (4)  |  (5)  |   | (6)  |  (7)  |
            // |       |       |   |      |       |
            // 5 ----- 6 ----- 7   7 ---- 8 ----- 9
            // |  (0)  |  (1)  |   | (2)  |  (3)  |
            // |       |       |   |      |       |
            // 0 ----- 1 ----- 2   2 ---- 3 ----- 4

            var cornerNodes = new Dictionary<ISubdomain, HashSet<INode>>();
            cornerNodes[model.GetSubdomain(0)] = new HashSet<INode>(
                new INode[] { model.GetNode(2), model.GetNode(12) });
            cornerNodes[model.GetSubdomain(1)] = new HashSet<INode>(
                new INode[] { model.GetNode(2), model.GetNode(12), model.GetNode(14) });
            cornerNodes[model.GetSubdomain(2)] = new HashSet<INode>(
                new INode[] { model.GetNode(12), model.GetNode(22) });
            cornerNodes[model.GetSubdomain(3)] = new HashSet<INode>(
                new INode[] { model.GetNode(12), model.GetNode(14), model.GetNode(22) });
            return cornerNodes;
        }

        /// <summary>
        /// Model.ConnectDataStructures() must be called first.
        /// </summary>
        public static UsedDefinedCornerNodesMpi DefineCornerNodeSelectionMpi(ProcessDistribution procs, IModel model)
        {
            var cornerNodes = new Dictionary<ISubdomain, HashSet<INode>>();
            if (procs.IsMasterProcess)
            {
                cornerNodes = DefineCornerNodesSubdomainsAll(model);
            }
            else
            {
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                cornerNodes[subdomain] = DefineCornerNodesSubdomain(subdomain);
            }
            return new UsedDefinedCornerNodesMpi(procs, cornerNodes);
        }

        public static UsedDefinedCornerNodes DefineCornerNodeSelectionSerial(IModel model)
            => new UsedDefinedCornerNodes(DefineCornerNodesSubdomainsAll(model));

        public static (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs)
            GetDofSeparation(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var cornerDofs = new int[] { 2, 3, 10, 11 };
                var remainderDofs = new int[] { 0, 1, 4, 5, 6, 7, 8, 9 };
                var boundaryDofs = new int[] { 4, 5, 6, 7 };
                var internalDofs = new int[] { 0, 1, 2, 3 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 1)
            {
                var cornerDofs = new int[] { 0, 1, 12, 13, 16, 17 };
                var remainderDofs = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14, 15 };
                var boundaryDofs = new int[] { 4, 5, 10, 11 };
                var internalDofs = new int[] { 0, 1, 2, 3, 6, 7, 8, 9 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 2)
            {
                var cornerDofs = new int[] { 2, 3, 10, 11 };
                var remainderDofs = new int[] { 0, 1, 4, 5, 6, 7, 8, 9 };
                var boundaryDofs = new int[] { 0, 1, 4, 5 };
                var internalDofs = new int[] { 2, 3, 6, 7 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 3)
            {
                var cornerDofs = new int[] { 0, 1, 4, 5, 12, 13 };
                var remainderDofs = new int[] { 2, 3, 6, 7, 8, 9, 10, 11, 14, 15, 16, 17 };
                var boundaryDofs = new int[] { 0, 1, 2, 3 };
                var internalDofs = new int[] { 4, 5, 6, 7, 8, 9, 10, 11 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixBc(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var Bc = Matrix.CreateZero(4, 8);
                Bc[0, 0] = 1;
                Bc[1, 1] = 1;
                Bc[2, 2] = 1;
                Bc[3, 3] = 1;
                return Bc;
            }
            else if (subdomainID == 1)
            {
                var Bc = Matrix.CreateZero(6, 8);
                Bc[0, 0] = 1;
                Bc[1, 1] = 1;
                Bc[2, 2] = 1;
                Bc[3, 3] = 1;
                Bc[4, 4] = 1;
                Bc[5, 5] = 1;
                return Bc;
            }
            else if (subdomainID == 2)
            {
                var Bc = Matrix.CreateZero(4, 8);
                Bc[0, 2] = 1;
                Bc[1, 3] = 1;
                Bc[2, 6] = 1;
                Bc[3, 7] = 1;
                return Bc;
            }
            else if (subdomainID == 3)
            {
                var Bc = Matrix.CreateZero(6, 8);
                Bc[0, 2] = 1;
                Bc[1, 3] = 1;
                Bc[2, 4] = 1;
                Bc[3, 5] = 1;
                Bc[4, 6] = 1;
                Bc[5, 7] = 1;
                return Bc;
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixBr(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var Br = Matrix.CreateZero(8, 8);
                Br[0, 4] = +1;
                Br[1, 5] = +1;
                Br[2, 6] = +1;
                Br[3, 7] = +1;
                return Br;
            }
            else if (subdomainID == 1)
            {
                var Br = Matrix.CreateZero(8, 12);
                Br[0, 4] = -1;
                Br[1, 5] = -1;
                Br[4, 10] = +1;
                Br[5, 11] = +1;
                return Br;
            }
            else if (subdomainID == 2)
            {
                var Br = Matrix.CreateZero(8, 8);
                Br[2, 0] = -1;
                Br[3, 1] = -1;
                Br[6, 4] = +1;
                Br[7, 5] = +1;
                return Br;
            }
            else if (subdomainID == 3)
            {
                var Br = Matrix.CreateZero(8, 12);
                Br[4, 0] = -1;
                Br[5, 1] = -1;
                Br[6, 2] = -1;
                Br[7, 3] = -1;
                return Br;
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixBpbrHeterogeneous(int subdomainID)
        {
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.5, 0, 0, 0 },
                    { 0, 0.5, 0, 0 },
                    { 0, 0, 0.5, 0 },
                    { 0, 0, 0, 0.5 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { -0.5, 0, 0, 0 },
                    { 0, -0.5, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0.00990099009900990, 0 },
                    { 0, 0, 0, 0.00990099009900990 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { -0.5, 0, 0, 0 },
                    { 0, -0.5, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0.00990099009900990, 0 },
                    { 0, 0, 0, 0.00990099009900990 }
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { -0.990099009900990, 0, 0, 0 },
                    { 0, -0.990099009900990, 0, 0 },
                    { 0, 0, -0.990099009900990, 0 },
                    { 0, 0, 0, -0.990099009900990 }
                });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixBpbrHomogeneous(int subdomainID)
        {
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.5, 0, 0, 0 },
                    { 0, 0.5, 0, 0 },
                    { 0, 0, 0.5, 0 },
                    { 0, 0, 0, 0.5 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { -0.5, 0, 0, 0 },
                    { 0, -0.5, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0.5, 0 },
                    { 0, 0, 0, 0.5 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { -0.5, 0, 0, 0 },
                    { 0, -0.5, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0.5, 0 },
                    { 0, 0, 0, 0.5 }
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { -0.5, 0, 0, 0 },
                    { 0, -0.5, 0, 0 },
                    { 0, 0, -0.5, 0 },
                    { 0, 0, 0, -0.5 }
                });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixKbb(int subdomainID) //TODO: This should be hardcoded instead
        {
            (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                        Example4x4Quads.GetDofSeparation(subdomainID);
            return GetMatrixKrr(subdomainID).GetSubmatrix(boundaryRemainderDofs, boundaryRemainderDofs);
        }

        public static Matrix GetMatrixKbi(int subdomainID) //TODO: This should be hardcoded instead
        {
            (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                        Example4x4Quads.GetDofSeparation(subdomainID);
            return GetMatrixKrr(subdomainID).GetSubmatrix(boundaryRemainderDofs, internalRemainderDofs);
        }

        public static Matrix GetMatrixKcc(int subdomainID)
        {
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {0.494505494500000, -0.178571428600000, 0, 0},
                    {-0.178571428600000, 0.494505494500000, 0, 0},
                    {0, 0, 0.494505494500000, 0.178571428600000},
                    {0, 0, 0.178571428600000, 0.494505494500000}
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {0.494505494500000, 0.178571428600000, 0, 0, 0, 0},
                    {0.178571428600000, 0.494505494500000, 0, 0, 0, 0},
                    {0, 0, 0.494505494500000, -0.178571428600000, 0, 0},
                    {0, 0, -0.178571428600000, 0.494505494500000, 0, 0},
                    {0, 0, 0, 0, 0.494505494500000, 0.178571428600000},
                    {0, 0, 0, 0, 0.178571428600000, 0.494505494500000}
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {0.494505494500000, -0.178571428600000, 0, 0},
                    {-0.178571428600000, 0.494505494500000, 0, 0},
                    {0, 0, 0.494505494500000, 0.178571428600000 },
                    {0, 0, 0.178571428600000, 0.494505494500000 }
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {0.494505494500000, 0.178571428600000, 0, 0, 0, 0  },
                    {0.178571428600000, 0.494505494500000, 0, 0, 0, 0  },
                    {0, 0, 0.494505494500000, -0.178571428600000, 0, 0 },
                    {0, 0, -0.178571428600000, 0.494505494500000, 0, 0 },
                    {0, 0, 0, 0, 0.494505494500000, -0.178571428600000 },
                    {0, 0, 0, 0, -0.178571428600000, 0.494505494500000 }
                });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixKii(int subdomainID) //TODO: This should be hardcoded instead
        {
            (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                        Example4x4Quads.GetDofSeparation(subdomainID);
            return GetMatrixKrr(subdomainID).GetSubmatrix(internalRemainderDofs, internalRemainderDofs);
        }

        public static Matrix GetMatrixKrr(int subdomainID)
        {
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.9890109890, 0, 0.10989010990, 0, -0.24725274730, -0.17857142860, 0, 0 },
                    { 0, 0.9890109890,  0, -0.60439560440, -0.17857142860, -0.24725274730, 0, 0 },
                    { 0.10989010990, 0, 1.9780219780,  0, -0.60439560440, 0, 0.10989010990, 0 },
                    { 0, -0.60439560440, 0, 1.9780219780,  0, 0.10989010990, 0, -0.60439560440 },
                    { -0.24725274730,  -0.17857142860, -0.60439560440, 0, 0.9890109890,  0, -0.24725274730, 0.17857142860 },
                    { -0.17857142860,  -0.24725274730, 0, 0.10989010990, 0, 0.9890109890,  0.17857142860, -0.24725274730 },
                    { 0, 0, 0.10989010990, 0, -0.24725274730, 0.17857142860, 0.9890109890,  0 },
                    { 0, 0, 0, -0.60439560440, 0.17857142860, -0.24725274730, 0, 0.9890109890 }
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.9890109890,    0,  -0.3021978022, -0.01373626370,-0.2472527473, 0.1785714286,  0.1098901099,  0,  -0.2472527473, -0.1785714286, 0,  0      },
                    { 0, 0.9890109890,  0.01373626370, 0.05494505490, 0.1785714286,  -0.2472527473, 0,  -0.6043956044, -0.1785714286, -0.2472527473, 0,  0         },
                    { -0.3021978022,   0.01373626370, 0.4945054945,  -0.1785714286, 0,  0,  -0.2472527473, 0.1785714286,  0.05494505490, -0.01373626370,    0,  0  },
                    { -0.01373626370,  0.05494505490, -0.1785714286, 0.4945054945,  0,  0,  0.1785714286,  -0.2472527473, 0.01373626370, -0.3021978022, 0,  0      },
                    { -0.2472527473,   0.1785714286,  0,  0,  0.9890109890,  0,  -0.6043956044, 0,  0,  0,  -0.2472527473, -0.1785714286                           },
                    { 0.1785714286,    -0.2472527473, 0,  0,  0,  0.9890109890,  0,  0.1098901099,  0,  0,  -0.1785714286, -0.2472527473                           },
                    { 0.1098901099,    0,  -0.2472527473, 0.1785714286,  -0.6043956044, 0,  1.978021978,   0,  -0.6043956044, 0,  0.1098901099,  0                 },
                    { 0, -0.6043956044, 0.1785714286,  -0.2472527473, 0,  0.1098901099,  0,  1.978021978,   0,  0.1098901099,  0,  -0.6043956044                   },
                    { -0.2472527473,   -0.1785714286, 0.05494505490, 0.01373626370, 0,  0,  -0.6043956044, 0,  0.9890109890,  0,  -0.2472527473, 0.1785714286      },
                    { -0.1785714286,   -0.2472527473, -0.01373626370,    -0.3021978022, 0,  0,  0,  0.1098901099,  0,  0.9890109890,  0.1785714286,  -0.2472527473 },
                    { 0,    0,  0,  0,  -0.2472527473, -0.1785714286, 0.1098901099,  0,  -0.2472527473, 0.1785714286,  0.9890109890,  0                            },
                    { 0,    0,  0,  0,  -0.1785714286, -0.2472527473, 0,  -0.6043956044, 0.1785714286,  -0.2472527473, 0,  0.9890109890                            }
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.9890109890, 0, 0.10989010990, 0, -0.24725274730, -0.17857142860, 0, 0 },
                    { 0, 0.9890109890, 0, -0.60439560440, -0.17857142860, -0.24725274730, 0, 0 },
                    { 0.10989010990, 0, 1.9780219780, 0, -0.60439560440, 0, 0.10989010990, 0 },
                    { 0, -0.60439560440, 0, 1.9780219780, 0, 0.10989010990, 0, -0.60439560440 },
                    { -0.24725274730, -0.17857142860, -0.60439560440, 0, 0.9890109890, 0, -0.24725274730, 0.17857142860 },
                    { -0.17857142860, -0.24725274730, 0, 0.10989010990, 0, 0.9890109890, 0.17857142860, -0.24725274730 },
                    { 0, 0, 0.10989010990, 0, -0.24725274730, 0.17857142860, 0.9890109890, 0 },
                    { 0, 0, 0, -0.60439560440, 0.17857142860, -0.24725274730, 0, 0.9890109890 }
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    { 0.9890109890, 0, -0.24725274730, 0.17857142860, 0.10989010990, 0, -0.24725274730, -0.17857142860, 0, 0, 0, 0                           },
                    { 0, 0.9890109890, 0.17857142860, -0.24725274730, 0, -0.60439560440, -0.17857142860, -0.24725274730, 0, 0, 0, 0                          },
                    { -0.24725274730, 0.17857142860, 0.9890109890, 0, -0.60439560440, 0, 0, 0, -0.24725274730, -0.17857142860, 0, 0                          },
                    { 0.17857142860, -0.24725274730, 0, 0.9890109890, 0, 0.10989010990, 0, 0, -0.17857142860, -0.24725274730, 0, 0                           },
                    { 0.10989010990, 0, -0.60439560440, 0, 1.9780219780, 0, -0.60439560440, 0, 0.10989010990, 0, -0.24725274730, -0.17857142860              },
                    { 0, -0.60439560440, 0, 0.10989010990, 0, 1.9780219780, 0, 0.10989010990, 0, -0.60439560440, -0.17857142860, -0.24725274730              },
                    { -0.24725274730, -0.17857142860, 0, 0, -0.60439560440, 0, 0.9890109890, 0, -0.24725274730, 0.17857142860, 0.05494505490, -0.01373626370 },
                    { -0.17857142860, -0.24725274730, 0, 0, 0, 0.10989010990, 0, 0.9890109890, 0.17857142860, -0.24725274730, 0.01373626370, -0.30219780220  },
                    { 0, 0, -0.24725274730, -0.17857142860, 0.10989010990, 0, -0.24725274730, 0.17857142860, 0.9890109890, 0, -0.30219780220, 0.01373626370  },
                    { 0, 0, -0.17857142860, -0.24725274730, 0, -0.60439560440, 0.17857142860, -0.24725274730, 0, 0.9890109890, -0.01373626370, 0.05494505490 },
                    { 0, 0, 0, 0, -0.24725274730, -0.17857142860, 0.05494505490, 0.01373626370, -0.30219780220, -0.01373626370, 0.49450549450, 0.17857142860 },
                    { 0, 0, 0, 0, -0.17857142860, -0.24725274730, -0.01373626370, -0.30219780220, 0.01373626370, 0.05494505490, 0.17857142860, 0.49450549450 }
                });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetMatrixKrc(int subdomainID)
        {
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {-0.30219780220, -0.01373626370, 0, 0                         },
                    {0.01373626370, 0.05494505490, 0, 0                           },
                    {-0.24725274730, 0.17857142860, -0.24725274730, -0.17857142860},
                    {0.17857142860, -0.24725274730, -0.17857142860, -0.24725274730},
                    {0.05494505490, 0.01373626370, 0.05494505490, -0.01373626370  },
                    {-0.01373626370, -0.30219780220, 0.01373626370, -0.30219780220},
                    {0, 0, -0.30219780220, 0.01373626370                          },
                    {0, 0, -0.01373626370, 0.05494505490                          }
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {-0.30219780220, 0.01373626370, 0, 0, 0, 0                                                     },
                    {-0.01373626370, 0.05494505490, 0, 0, 0, 0                                                     },
                    {0, 0, 0, 0, 0, 0                                                                              },
                    {0, 0, 0, 0, 0, 0                                                                              },
                    {0.05494505490, -0.01373626370, 0.05494505490, 0.01373626370, 0, 0                             },
                    {0.01373626370, -0.30219780220, -0.01373626370, -0.30219780220, 0, 0                           },
                    {-0.24725274730, -0.17857142860, -0.24725274730, 0.17857142860, -0.24725274730, -0.17857142860 },
                    {-0.17857142860, -0.24725274730, 0.17857142860, -0.24725274730, -0.17857142860, -0.24725274730 },
                    {0, 0, 0, 0, 0.05494505490, -0.01373626370                                                     },
                    {0, 0, 0, 0, 0.01373626370, -0.30219780220                                                     },
                    {0, 0, -0.30219780220, -0.01373626370, -0.30219780220, 0.01373626370                           },
                    {0, 0, 0.01373626370, 0.05494505490, -0.01373626370, 0.05494505490                             }
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {-0.30219780220, -0.01373626370, 0, 0                          },
                    {0.01373626370, 0.05494505490, 0, 0                            },
                    {-0.24725274730, 0.17857142860, -0.24725274730, -0.17857142860 },
                    {0.17857142860, -0.24725274730, -0.17857142860, -0.24725274730 },
                    {0.05494505490, 0.01373626370, 0.05494505490, -0.01373626370   },
                    {-0.01373626370, -0.30219780220, 0.01373626370, -0.30219780220 },
                    {0, 0, -0.30219780220, 0.01373626370                           },
                    {0, 0, -0.01373626370, 0.05494505490                           }
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    {-0.30219780220, 0.01373626370, -0.30219780220, -0.01373626370, 0, 0                         },
                    {-0.01373626370, 0.05494505490, 0.01373626370, 0.05494505490, 0, 0                           },
                    {0.05494505490, -0.01373626370, 0, 0, 0.05494505490, 0.01373626370                           },
                    {0.01373626370, -0.30219780220, 0, 0, -0.01373626370, -0.30219780220                         },
                    {-0.24725274730, -0.17857142860, -0.24725274730, 0.17857142860, -0.24725274730, 0.17857142860},
                    {-0.17857142860, -0.24725274730, 0.17857142860, -0.24725274730, 0.17857142860, -0.24725274730},
                    {0, 0, 0.05494505490, 0.01373626370, 0, 0                                                    },
                    {0, 0, -0.01373626370, -0.30219780220, 0, 0                                                  },
                    {0, 0, 0, 0, -0.30219780220, -0.01373626370                                                  },
                    {0, 0, 0, 0, 0.01373626370, 0.05494505490                                                    },
                    {0, 0, 0, 0, 0, 0                                                                            },
                    {0, 0, 0, 0, 0, 0                                                                            }
                });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Vector GetVectorFbcHomogeneous(int subdomainID)
        {
            if (subdomainID == 0) return Vector.CreateZero(4);
            else if (subdomainID == 1) return Vector.CreateZero(6);
            else if (subdomainID == 2) return Vector.CreateZero(4);
            else if (subdomainID == 3) return Vector.CreateZero(6);
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Vector GetVectorFrHomogeneous(int subdomainID)
        {
            if (subdomainID == 0) return Vector.CreateZero(8);
            else if (subdomainID == 1) return Vector.CreateZero(12);
            else if (subdomainID == 2) return Vector.CreateZero(8);
            else if (subdomainID == 3)
            {
                var fr = Vector.CreateZero(12);
                fr[11] = 10;
                return fr;
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

    }
}
