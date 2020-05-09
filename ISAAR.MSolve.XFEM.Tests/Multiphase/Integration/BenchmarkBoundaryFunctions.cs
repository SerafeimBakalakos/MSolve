using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;
using static ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration.Utilities;

//TODO: Hardcode as much calculations (integrals, areas, jacobians) as possible and write part of the calculations in comments
//TODO: Add more functions
namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration
{
    public class BoundaryConstantFunction : IBenchmarkBoundaryFunction
    {
        private readonly double value;

        public BoundaryConstantFunction(double value = 1.0) => this.value = value;

        public double Evaluate(GaussPoint point, IXFiniteElement element) => value;

        public double GetExpectedIntegral(CartesianPoint start, CartesianPoint end)
        {
            // Define mapping from reference integral system: t belongs in [-1, 1] to s belongs in AB (A=start, B=end)

            // Jacobian
            double detJ = 0.5 * end.CalculateDistanceFrom(start);

            // Integration in reference system
            double referenceIntegral = 2.0 * value;

            return referenceIntegral * detJ;
        }
        
    }

    public class BoundaryLinearFunction : IBenchmarkBoundaryFunction
    {
        public double Evaluate(GaussPoint point, IXFiniteElement element)
        {
            CartesianPoint cartesian = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, point);
            return cartesian.X + 3 * cartesian.Y;
        }

        public double GetExpectedIntegral(CartesianPoint start, CartesianPoint end)
        {
            // Define mapping from reference integral system: t belongs in [-1, 1] to s belongs in AB (A=start, B=end)

            // Jacobian
            double detJ = 0.5 * end.CalculateDistanceFrom(start);

            // Integration in reference system
            double referenceIntegral = (start.X + end.X) + 3 * (start.Y + end.Y);

            return referenceIntegral * detJ;
        }
    }
        
}
