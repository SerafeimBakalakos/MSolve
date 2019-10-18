using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d
{
    public static class Example4x4x4
    {
        public static Matrix MatrixFIrr => Matrix.CreateFromArray(new double[,]
        {
            
        });

        public static Matrix MatrixFIrcTilde => Matrix.CreateFromArray(new double[,]
        {
                     
        });

        public static int NumGlobalAugmentationConstraints => -1;
        public static int NumGlobalCornerDofs => -1;
        public static int NumGlobalLagrangeMultipliers => -1;

        public static Vector SolutionLagrangeMultipliers => Vector.CreateFromArray(new double[]
        {
            
        });

        public static Vector VectorGlobalFcStar => throw new NotImplementedException();

        public static Matrix GetMatrixBc(int subdomainID)
        {
            throw new NotImplementedException();
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
            else if (subdomainID == 4)
            {

            }
            else if (subdomainID == 5)
            {

            }
            else if (subdomainID == 6)
            {

            }
            else if (subdomainID == 7)
            {

            }
            else throw new ArgumentException("Subdomain ID must be in [0, 8)");
        }

        public static Matrix GetMatrixBr(int subdomainID)
        {
            throw new NotImplementedException();
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
            else if (subdomainID == 4)
            {

            }
            else if (subdomainID == 5)
            {

            }
            else if (subdomainID == 6)
            {

            }
            else if (subdomainID == 7)
            {

            }
            else throw new ArgumentException("Subdomain ID must be in [0, 8)");
        }

        public static Matrix GetMatrixKrr(int subdomainID)
        {
            throw new NotImplementedException();
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 4)
            {

            }
            else if (subdomainID == 5)
            {

            }
            else if (subdomainID == 6)
            {

            }
            else if (subdomainID == 7)
            {

            }
            else throw new ArgumentException("Subdomain ID must be in [0, 8)");
        }

        public static Matrix GetMatrixKrc(int subdomainID)
        {
            throw new NotImplementedException();
            if (subdomainID == 0)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 1)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 2)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 3)
            {
                return Matrix.CreateFromArray(new double[,]
                {
                    
                });
            }
            else if (subdomainID == 4)
            {

            }
            else if (subdomainID == 5)
            {

            }
            else if (subdomainID == 6)
            {

            }
            else if (subdomainID == 7)
            {

            }
            else throw new ArgumentException("Subdomain ID must be in [0, 8)");
        }
    }
}
