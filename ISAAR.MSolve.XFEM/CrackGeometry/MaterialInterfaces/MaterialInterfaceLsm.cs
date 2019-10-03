using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;

namespace ISAAR.MSolve.XFEM.CrackGeometry.MaterialInterfaces
{
    public class MaterialInterfaceLsm : IMaterialInterface
    {
        private readonly Dictionary<XNode, double> levelSets;

        public MaterialInterfaceLsm(double interfaceThickness = 1.0)
        {
            this.levelSets = new Dictionary<XNode, double>();
            this.Thickness = interfaceThickness;
        }

        public double Thickness { get; }

        public void Initialize(IEnumerable<XNode> nodes, ICurve2D discontinuity)
        {
            foreach (XNode node in nodes) levelSets[node] = discontinuity.SignedDistanceOf(node);
        }

        public GaussPoint[] IntegrationPointsAlongInterface(IXFiniteElement element, int numIntegrationPoints)
        {
            NaturalPoint[] intersectionPoints = IntersectElement(element);

            if (intersectionPoints.Length < 2) return new GaussPoint[0];
            else if (intersectionPoints.Length == 2)
            {
                NaturalPoint start = intersectionPoints[0];
                NaturalPoint end = intersectionPoints[1];

                double detJ = 0.5 * start.CalcDistanceFrom(end);
                IReadOnlyList<GaussPoint> gaussPoints1D =
                    GaussLegendre1D.GetQuadratureWithOrder(numIntegrationPoints).IntegrationPoints;
                var gaussPoints2D = new GaussPoint[numIntegrationPoints];
                for (int i = 0; i < numIntegrationPoints; ++i)
                {
                    GaussPoint gp1D = gaussPoints1D[i];
                    double a = 0.5 * (1.0 - gp1D.Xi);
                    double b = 0.5 * (1.0 + gp1D.Xi);
                    double xi = a * start.Xi + b * end.Xi;
                    double eta = a * start.Eta + b * end.Eta;
                    double zeta = a * start.Zeta + b * end.Zeta;
                    gaussPoints2D[i] = new GaussPoint(xi, eta, zeta, gp1D.Weight * detJ);
                }
                return gaussPoints2D;
            }
            else throw new Exception("Intersection points must be 0, 1 or 2, but were " + intersectionPoints.Length);

        }

        public double SignedDistanceOf(XNode node) => levelSets[node];

        public double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double signedDistance = 0.0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                signedDistance += shapeFunctionsAtNaturalPoint[n] * levelSets[element.Nodes[n]];
            }
            return signedDistance;
        }

        private NaturalPoint[] IntersectElement(IXFiniteElement element)
        {
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;

            var intersections = new HashSet<NaturalPoint>();
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                XNode node1Cartesian = edgesCartesian[i].node1;
                XNode node2Cartesian = edgesCartesian[i].node2;
                NaturalPoint node1Natural = edgesNatural[i].node1;
                NaturalPoint node2Natural = edgesNatural[i].node2;

                double levelSet1 = levelSets[node1Cartesian];
                double levelSet2 = levelSets[node2Cartesian];

                if (levelSet1 * levelSet2 < 0.0)
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -levelSet1 / (levelSet2 - levelSet1);
                    double xi = node1Natural.Xi + k * (node2Natural.Xi - node1Natural.Xi);
                    double eta = node1Natural.Eta + k * (node1Natural.Eta - node1Natural.Eta);

                    intersections.Add(new NaturalPoint(xi, eta));
                }
                else if (levelSet1 == 0.0) intersections.Add(node1Natural); // TODO: perhaps some tolerance is needed.
                else if (levelSet2 == 0.0) intersections.Add(node2Natural);
            }

            return intersections.ToArray();
        }
    }
}
