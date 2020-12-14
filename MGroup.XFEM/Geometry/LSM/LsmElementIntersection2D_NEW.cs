using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;

//MODIFICATION NEEDED: Rename the old one to SimpleLsmElementIntersection2D or merge the 2 into 1 class. 
//                     Perhaps the IntersectionMesh2D is not needed, since it is just a series of points.
namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// A curve resulting from the intersection of a parent curve with a 2D element.
    /// </summary>
    public class LsmElementIntersection2D_NEW : IElementGeometryIntersection
    {
        private readonly IntersectionMesh2D intersectionMesh;

        public LsmElementIntersection2D_NEW(int parentGeometryID, RelativePositionCurveElement relativePosition, 
            IXFiniteElement element, IntersectionMesh2D intersectionMesh)
        {
            this.ParentGeometryID = parentGeometryID;
            if ((relativePosition == RelativePositionCurveElement.Disjoint) /*|| (relativePosition == RelativePositionCurveElement.Tangent)*/)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMesh = intersectionMesh;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public int ParentGeometryID { get; }

        public IIntersectionMesh ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh2D();
            foreach (double[] vertexNatural in intersectionMesh.Vertices)
            {
                meshCartesian.Vertices.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, vertexNatural));
            }

            foreach ((CellType, int[]) cell in intersectionMesh.Cells) // same connectivity
            {
                meshCartesian.Cells.Add(cell);
            }
            return meshCartesian;
        }

        //TODO: Perhaps a dedicated IBoundaryIntegration component is needed
        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int order)
        {
            var integrationPoints = new List<GaussPoint>();

            // Map intersection points to cartesian system
            var intersectionsCartesian = new List<double[]>(intersectionMesh.Vertices.Count);
            foreach (double[] vertexNatural in intersectionMesh.Vertices)
            {
                intersectionsCartesian.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, vertexNatural));
            }

            for (int c = 0; c < intersectionMesh.Cells.Count; ++c)
            {
                double[] startNatural = intersectionMesh.Vertices[c];
                double[] endNatural = intersectionMesh.Vertices[c + 1];

                // Conforming curves intersect 2 elements, thus the integral will be computed twice. Halve the weights to avoid 
                // obtaining double the value of the integral.
                double weightModifier = 1.0;
                if (RelativePosition == RelativePositionCurveElement.Conforming) weightModifier = 0.5; //MODIFICATION NEEDED: This should be different for each segment

                // Absolute determinant of Jacobian of mapping from auxiliary to cartesian system. Constant for all Gauss points.
                double length = Utilities.Distance2D(intersectionsCartesian[c], intersectionsCartesian[c + 1]);
                double detJ = Math.Abs(0.5 * length);

                var quadrature1D = GaussLegendre1D.GetQuadratureWithOrder(order);
                int numIntegrationPoints = quadrature1D.IntegrationPoints.Count;
                for (int i = 0; i < numIntegrationPoints; ++i)
                {
                    GaussPoint gp1D = quadrature1D.IntegrationPoints[i];
                    double N0 = 0.5 * (1.0 - gp1D.Coordinates[0]);
                    double N1 = 0.5 * (1.0 + gp1D.Coordinates[0]);
                    double xi = N0 * startNatural[0] + N1 * endNatural[0];
                    double eta = N0 * startNatural[1] + N1 * endNatural[1];
                    integrationPoints.Add(new GaussPoint(new double[] { xi, eta }, gp1D.Weight * detJ * weightModifier));
                }
            }

            return integrationPoints;
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return intersectionMesh.Vertices;
        }
    }
}
