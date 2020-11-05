using System;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Cracks.Geometry
{
    //TODO: decide what data structures (arrays, tuples, matrix & vector classes I will use as arguments, return types 
    // and for operations. Implement convenience methods for those operations on these data types.
    // Perhaps vector-vector operations could be abstracted. 
    // Actually wouldn't all methods be clearer if I operated directly with cosa, sina, instead of rotation matrices?
    public class TipCoordinateSystem : TipCoordinateSystemBase
    {
        private readonly Vector localCoordinatesOfGlobalOrigin;

        public double RotationAngle { get; }
        public Matrix TransposeRotationMatrixGlobalToLocal { get; } // cache this for efficiency

        /// <summary>
        /// det(J_globToLoc) = det(Q) = (cosa)^2 + (sina)^2 = 1
        /// </summary>
        public double DeterminantOfJacobianGlobalToLocalCartesian { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tipCoordinates">Coordinates of the crack tip in the global cartesian system.</param>
        /// <param name="tipRotationAngle">Counter-clockwise angle from the O-x axis of the global cartesian system to  
        ///     the T-x1 axis of the local corrdinate system of the tip (T being the tip point)</param>
        public TipCoordinateSystem(double[] tipCoordinates, double tipRotationAngle)
        {
            this.RotationAngle = tipRotationAngle;

            double cosa = Math.Cos(tipRotationAngle);
            double sina = Math.Sin(tipRotationAngle);
            RotationMatrixGlobalToLocal = Matrix.CreateFromArray(new double[,] { { cosa, sina }, { -sina, cosa } });
            TransposeRotationMatrixGlobalToLocal = RotationMatrixGlobalToLocal.Transpose();
            localCoordinatesOfGlobalOrigin = -1 * (RotationMatrixGlobalToLocal * Vector.CreateFromArray(tipCoordinates));
            DeterminantOfJacobianGlobalToLocalCartesian = 1.0; // det = (cosa)^2 +(sina)^2 = 1
        }

        public double[] TransformPointGlobalCartesianToLocalCartesian(double[] cartesianGlobalPoint)
        {
            Vector local = RotationMatrixGlobalToLocal * Vector.CreateFromArray(cartesianGlobalPoint);
            local.AddIntoThis(localCoordinatesOfGlobalOrigin);
            return new double[] { local[0], local[1] };
        }

        public double[] TransformPointLocalCartesianToLocalPolar(double[] cartesianLocalPoint)
        {
            double x1 = cartesianLocalPoint[0];
            double x2 = cartesianLocalPoint[1];
            double r = Math.Sqrt(x1 * x1 + x2 * x2);
            double theta = Math.Atan2(x2, x1);
            return new double[] { r, theta };
        }

        protected override double[] TransformCoordsGlobalCartesianToLocalPolar(double[] cartesianGlobalPoint)
        {
            Vector local = RotationMatrixGlobalToLocal * Vector.CreateFromArray(cartesianGlobalPoint);
            local.AddIntoThis(localCoordinatesOfGlobalOrigin);
            double x1 = local[0];
            double x2 = local[1];
            double r = Math.Sqrt(x1 * x1 + x2 * x2);
            double theta = Math.Atan2(x2, x1);
            return new double[] { r, theta };
        }

        public Vector TransformScalarFieldDerivativesGlobalCartesianToLocalCartesian(Vector gradient)
        {
            return gradient * TransposeRotationMatrixGlobalToLocal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gradient">A 2x2 matrix, for which: Row i is the gradient of the ith component of the vector 
        ///     field, thus:    gradient = [Fx,x Fx,y; Fy,x Fy,y],
        ///     where Fi,j is the derivative of component i w.r.t. coordinate j</param>
        /// <returns></returns>
        public Matrix TransformVectorFieldDerivativesGlobalCartesianToLocalCartesian(Matrix gradient)
        {
            return RotationMatrixGlobalToLocal * (gradient * TransposeRotationMatrixGlobalToLocal);
        }

        public Tensor2D TransformTensorGlobalCartesianToLocalCartesian(Tensor2D tensor)
        {
            return tensor.Rotate(RotationAngle);
        }
    }
}
